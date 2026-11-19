/*
 * lkcp.c - skynet luaclib：skywind3000/kcp 的 Lua 绑定
 *
 * 采用「拉模式」：C 侧只把 ikcp 的输出数据报缓存起来，
 * 由 Lua 调 flush() 一次取走，避免 C -> Lua 回调带来的重入问题。
 *
 *   local kcp = require "kcp"
 *   local obj = kcp.new(conv)
 *   obj:input(udp_bytes)          -- 收到 UDP 包喂给 kcp
 *   obj:update(now_ms)            -- 驱动重传/ack（内部会 flush 到输出队列）
 *   local outs = obj:flush()      -- 取走本次产生的 UDP 包，逐个 sendto
 *   obj:check(now_ms)             -- 下次建议的 update 时间
 *   local size = obj:peek()       -- 下一条消息长度，-1 表示没有
 *   local msg  = obj:recv()       -- 取出一条消息
 *   obj:send(msg)                 -- 发送一条消息
 *   obj:waitsnd()                 -- 待发字节数
 *   obj:nodelay(1, 10, 2, 1)
 *   obj:wndsize(32, 128)
 *   obj:setmtu(1400)
 *   obj:conv()                    -- conv
 *   obj = nil                     -- gc 时自动 ikcp_release
 */
#include <lua.h>
#include <lauxlib.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

#include "ikcp.h"

#define KCP_METATABLE "kcp.handle"

struct kcp_out {
	struct kcp_out *next;
	int len;
	char data[1];
};

struct kcp_handle {
	ikcpcb *kcp;
	struct kcp_out *head;
	struct kcp_out *tail;
	int count;
};

static int
kcp_output(const char *buf, int len, ikcpcb *kcp, void *user) {
	struct kcp_handle *h = (struct kcp_handle *)user;
	struct kcp_out *o;
	(void)kcp;

	if (len <= 0) {
		return 0;
	}
	o = (struct kcp_out *)malloc(sizeof(struct kcp_out) + len);
	if (o == NULL) {
		return -1;
	}
	o->next = NULL;
	o->len = len;
	memcpy(o->data, buf, len);

	if (h->tail) {
		h->tail->next = o;
	} else {
		h->head = o;
	}
	h->tail = o;
	h->count++;

	return 0;
}

static void
free_outputs(struct kcp_handle *h) {
	struct kcp_out *o = h->head;
	while (o) {
		struct kcp_out *next = o->next;
		free(o);
		o = next;
	}
	h->head = NULL;
	h->tail = NULL;
	h->count = 0;
}

static struct kcp_handle *
check_handle(lua_State *L) {
	return (struct kcp_handle *)luaL_checkudata(L, 1, KCP_METATABLE);
}

static int
lnew(lua_State *L) {
	IUINT32 conv = (IUINT32)luaL_checkinteger(L, 1);
	struct kcp_handle *h = (struct kcp_handle *)lua_newuserdata(L, sizeof(struct kcp_handle));
	memset(h, 0, sizeof(*h));

	h->kcp = ikcp_create(conv, h);
	if (h->kcp == NULL) {
		luaL_error(L, "ikcp_create failed");
	}
	ikcp_setoutput(h->kcp, kcp_output);

	luaL_getmetatable(L, KCP_METATABLE);
	lua_setmetatable(L, -2);
	return 1;
}

static int
linput(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	size_t len = 0;
	const char *data = luaL_checklstring(L, 2, &len);
	int r = ikcp_input(h->kcp, data, (long)len);
	lua_pushinteger(L, r);
	return 1;
}

static int
lrecv(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	int size = ikcp_peeksize(h->kcp);
	if (size < 0) {
		return 0;
	}
	luaL_Buffer b;
	char *p = luaL_buffinitsize(L, &b, size);
	int r = ikcp_recv(h->kcp, p, size);
	if (r < 0) {
		return 0;
	}
	luaL_pushresultsize(&b, r);
	return 1;
}

static int
lpeek(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	lua_pushinteger(L, ikcp_peeksize(h->kcp));
	return 1;
}

static int
lsend(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	size_t len = 0;
	const char *data = luaL_checklstring(L, 2, &len);
	int r = ikcp_send(h->kcp, data, (int)len);
	lua_pushinteger(L, r);
	return 1;
}

static int
lupdate(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	IUINT32 now = (IUINT32)luaL_checkinteger(L, 2);
	ikcp_update(h->kcp, now);
	return 0;
}

static int
lcheck(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	IUINT32 now = (IUINT32)luaL_checkinteger(L, 2);
	lua_pushinteger(L, (lua_Integer)ikcp_check(h->kcp, now));
	return 1;
}

static int
lflush(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	int n = 0;
	struct kcp_out *o;

	lua_newtable(L);
	for (o = h->head; o; o = o->next) {
		lua_pushlstring(L, o->data, (size_t)o->len);
		lua_rawseti(L, -2, ++n);
	}
	free_outputs(h);
	return 1;
}

static int
lnodelay(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	int nodelay = (int)luaL_checkinteger(L, 2);
	int interval = (int)luaL_checkinteger(L, 3);
	int fastresend = (int)luaL_checkinteger(L, 4);
	int nocwnd = (int)luaL_optinteger(L, 5, 0);
	lua_pushinteger(L, ikcp_nodelay(h->kcp, nodelay, interval, fastresend, nocwnd));
	return 1;
}

static int
lwndsize(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	int sndwnd = (int)luaL_optinteger(L, 2, 32);
	int rcvwnd = (int)luaL_optinteger(L, 3, 128);
	lua_pushinteger(L, ikcp_wndsize(h->kcp, sndwnd, rcvwnd));
	return 1;
}

static int
lsetmtu(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	int mtu = (int)luaL_checkinteger(L, 2);
	lua_pushinteger(L, ikcp_setmtu(h->kcp, mtu));
	return 1;
}

static int
lwaitsnd(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	lua_pushinteger(L, (lua_Integer)ikcp_waitsnd(h->kcp));
	return 1;
}

static int
lconv(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	lua_pushinteger(L, (lua_Integer)h->kcp->conv);
	return 1;
}

static int
lgc(lua_State *L) {
	struct kcp_handle *h = check_handle(L);
	free_outputs(h);
	if (h->kcp) {
		ikcp_release(h->kcp);
		h->kcp = NULL;
	}
	return 0;
}

static const luaL_Reg kcp_methods[] = {
	{ "input",    linput },
	{ "recv",     lrecv },
	{ "peek",     lpeek },
	{ "send",     lsend },
	{ "update",   lupdate },
	{ "check",    lcheck },
	{ "flush",    lflush },
	{ "nodelay",  lnodelay },
	{ "wndsize",  lwndsize },
	{ "setmtu",   lsetmtu },
	{ "waitsnd",  lwaitsnd },
	{ "conv",     lconv },
	{ "__gc",     lgc },
	{ NULL, NULL },
};

int
luaopen_kcp(lua_State *L) {
	luaL_newmetatable(L, KCP_METATABLE);
	luaL_setfuncs(L, kcp_methods, 0);
	lua_pushvalue(L, -1);
	lua_setfield(L, -2, "__index");
	lua_pop(L, 1);

	lua_newtable(L);
	lua_pushcfunction(L, lnew);
	lua_setfield(L, -2, "new");
	return 1;
}