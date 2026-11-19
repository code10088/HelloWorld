/*
 * kcp-client.c - skynet 游戏服 KCP 通道测试客户端
 *
 * 编译（Windows / MSYS2）：
 *   gcc -O2 -o kcp-client.exe kcp-client.c ../../3rd/kcp/ikcp.c -lws2_32
 * 编译（Linux）：
 *   gcc -O2 -o kcp-client kcp-client.c ../../3rd/kcp/ikcp.c
 *
 * 用法：
 *   kcp-client <host> <port> <token> [--drop N] [--expect-bad-token]
 *
 * 流程：握手 -> CSAuth -> CSBag -> CSHeart，全部通过打印 PASS。
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <time.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
typedef SOCKET sock_t;
#define SOCK_INVALID INVALID_SOCKET
#define close_sock closesocket
#else
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
typedef int sock_t;
#define SOCK_INVALID (-1)
#define close_sock close
#endif

#include "ikcp.h"

/* ---------------- 协议常量 ---------------- */
#define CS_SESSION   0
#define CS_AUTH      1
#define CS_HEART     2
#define CS_BAG       106
#define SC_SESSION   10000
#define SC_AUTH      10001
#define SC_HEART     10002
#define SC_PLAYER    10003
#define SC_BAG       10106

/* ---------------- 输出队列 ---------------- */
struct kcp_out {
	struct kcp_out *next;
	int len;
	unsigned char data[1600];
};

struct kcp_state {
	ikcpcb *kcp;
	struct kcp_out *head;
	struct kcp_out *tail;
	sock_t fd;
	struct sockaddr_in peer;
	int drop_percent;
	int handshake_done;
	uint32_t conv;
	long recv_bytes;
};

static int g_pass = 0, g_fail = 0;

static void check(const char *name, int ok, const char *detail) {
	if (ok) {
		g_pass++;
		printf("  [PASS] %s  %s\n", name, detail ? detail : "");
	} else {
		g_fail++;
		printf("  [FAIL] %s  %s\n", name, detail ? detail : "");
	}
	fflush(stdout);
}

static long long now_ms(void) {
#ifdef _WIN32
	return (long long)GetTickCount64();
#else
	struct timespec ts;
	clock_gettime(CLOCK_MONOTONIC, &ts);
	return (long long)ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
#endif
}

static void put_u16(unsigned char *p, unsigned v) { p[0] = v & 0xff; p[1] = (v >> 8) & 0xff; }
static void put_u32(unsigned char *p, uint32_t v) {
	p[0] = v & 0xff; p[1] = (v >> 8) & 0xff; p[2] = (v >> 16) & 0xff; p[3] = (v >> 24) & 0xff;
}
static unsigned get_u16(const unsigned char *p) { return p[0] | (p[1] << 8); }
static uint32_t get_u32(const unsigned char *p) {
	return (uint32_t)p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

static int kcp_output(const char *buf, int len, ikcpcb *kcp, void *user) {
	struct kcp_state *st = (struct kcp_state *)user;
	struct kcp_out *o;
	(void)kcp;
	if (len <= 0 || len >= (int)sizeof(o->data)) return 0;
	o = (struct kcp_out *)malloc(sizeof(*o));
	if (!o) return -1;
	o->next = NULL;
	o->len = len;
	memcpy(o->data, buf, len);
	if (st->tail) st->tail->next = o; else st->head = o;
	st->tail = o;
	return 0;
}

static void flush_outputs(struct kcp_state *st) {
	struct kcp_out *o = st->head;
	while (o) {
		struct kcp_out *next = o->next;
		int drop = st->drop_percent > 0 && (rand() % 100) < st->drop_percent;
		if (!drop) {
			sendto(st->fd, (const char *)o->data, o->len, 0,
				(struct sockaddr *)&st->peer, sizeof(st->peer));
		}
		free(o);
		o = next;
	}
	st->head = st->tail = NULL;
}

static int send_msg(struct kcp_state *st, unsigned id, const void *payload, int plen) {
	unsigned char buf[8192];
	int n = 0;
	if (plen + 2 > (int)sizeof(buf)) return -1;
	put_u16(buf, id);
	n = 2;
	if (plen > 0) {
		memcpy(buf + n, payload, plen);
		n += plen;
	}
	return ikcp_send(st->kcp, (const char *)buf, n);
}

/* 收到的一条业务消息 */
struct recv_msg {
	unsigned id;
	unsigned char body[8192];
	int body_len;
};

/* 处理一次网络事件；返回 1 表示拿到一条消息 */
static int pump(struct kcp_state *st, struct recv_msg *out, long long timeout_ms) {
	fd_set rfds;
	struct timeval tv;
	long long deadline = now_ms() + timeout_ms;

	while (now_ms() < deadline) {
		unsigned char buf[2048];
		int n;

		FD_ZERO(&rfds);
		FD_SET(st->fd, &rfds);
		tv.tv_sec = 0;
		tv.tv_usec = 5000;

		n = select((int)st->fd + 1, &rfds, NULL, NULL, &tv);
		if (n > 0 && FD_ISSET(st->fd, &rfds)) {
			struct sockaddr_in from;
			socklen_t fl = sizeof(from);
			int r = (int)recvfrom(st->fd, (char *)buf, sizeof(buf), 0,
				(struct sockaddr *)&from, &fl);
			if (r > 0) {
				st->recv_bytes += r;
				ikcp_input(st->kcp, (const char *)buf, r);
			}
		}

		ikcp_update(st->kcp, (IUINT32)now_ms());
		flush_outputs(st);

		for (;;) {
			int size = ikcp_peeksize(st->kcp);
			unsigned char msg[8192];
			if (size <= 0 || size > (int)sizeof(msg)) break;
			if (ikcp_recv(st->kcp, (char *)msg, size) < 0) break;
			if (size < 2) continue;
			out->id = get_u16(msg);
			out->body_len = size - 2;
			memcpy(out->body, msg + 2, out->body_len);
			return 1;
		}
	}
	return 0;
}

/* 等待若干 id 中的一个，返回 id（0 超时） */
static unsigned wait_msg(struct kcp_state *st, long long timeout_ms) {
	struct recv_msg m;
	long long deadline = now_ms() + timeout_ms;
	while (now_ms() < deadline) {
		long long left = deadline - now_ms();
		if (left <= 0) break;
		if (pump(st, &m, left > 50 ? 50 : left)) {
			return m.id;
		}
	}
	return 0;
}

int main(int argc, char **argv) {
	struct kcp_state st;
	struct sockaddr_in peer;
	const char *host, *token;
	int port, i, expect_bad = 0, hs_tries;
	unsigned conv = 0;
	long long deadline;

	if (argc < 4) {
		fprintf(stderr, "usage: %s <host> <port> <token> [--drop N] [--expect-bad-token]\n", argv[0]);
		return 2;
	}
	host = argv[1];
	port = atoi(argv[2]);
	token = argv[3];

	memset(&st, 0, sizeof(st));
	for (i = 4; i < argc; i++) {
		if (!strcmp(argv[i], "--drop") && i + 1 < argc) {
			st.drop_percent = atoi(argv[++i]);
		} else if (!strcmp(argv[i], "--expect-bad-token")) {
			expect_bad = 1;
		}
	}
	srand((unsigned)now_ms());

#ifdef _WIN32
	{
		WSADATA wsa;
		WSAStartup(MAKEWORD(2, 2), &wsa);
	}
#endif

	memset(&peer, 0, sizeof(peer));
	peer.sin_family = AF_INET;
	peer.sin_port = htons((unsigned short)port);
	if (inet_pton(AF_INET, host, &peer.sin_addr) != 1) {
		fprintf(stderr, "bad host: %s\n", host);
		return 2;
	}

	st.fd = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	if (st.fd == SOCK_INVALID) {
		fprintf(stderr, "socket failed\n");
		return 2;
	}
	st.peer = peer;

	printf("\n=== KCP 测试 %s:%d drop=%d%% ===\n", host, port, st.drop_percent);

	/* ---------- 1. 握手 ---------- */
	{
		unsigned char pkt[512];
		int n = 0, tlen = (int)strlen(token);
		put_u16(pkt, CS_SESSION); n = 2;
		put_u16(pkt + n, (unsigned)tlen); n += 2;
		memcpy(pkt + n, token, tlen); n += tlen;
		put_u32(pkt + n, 1u); n += 4;   /* version */

		deadline = now_ms() + 6000;
		for (hs_tries = 0; hs_tries < 6 && now_ms() < deadline; hs_tries++) {
			unsigned char rbuf[256];
			struct timeval tv = { 1, 0 };
			fd_set rfds;
			int r;

			sendto(st.fd, (const char *)pkt, n, 0, (struct sockaddr *)&peer, sizeof(peer));

			FD_ZERO(&rfds);
			FD_SET(st.fd, &rfds);
			r = select((int)st.fd + 1, &rfds, NULL, NULL, &tv);
			if (r > 0) {
				struct sockaddr_in from;
				socklen_t fl = sizeof(from);
				r = (int)recvfrom(st.fd, (char *)rbuf, sizeof(rbuf), 0,
					(struct sockaddr *)&from, &fl);
				if (r >= 10 && get_u16(rbuf) == SC_SESSION) {
					conv = get_u32(rbuf + 6);
					break;
				}
			}
			conv = 0;
		}
	}

	if (expect_bad) {
		char detail[64];
		snprintf(detail, sizeof(detail), "conv=%u", conv);
		check("伪造 token 被拒（conv=0）", conv == 0, detail);
		close_sock(st.fd);
		printf("\n结果: PASS=%d FAIL=%d\n", g_pass, g_fail);
		return g_fail ? 1 : 0;
	}

	{
		char detail[64];
		snprintf(detail, sizeof(detail), "conv=%u tries=%d", conv, hs_tries);
		check("握手拿到 conv", conv != 0, detail);
	}
	if (conv == 0) {
		close_sock(st.fd);
		printf("\n结果: PASS=%d FAIL=%d\n", g_pass, g_fail);
		return 1;
	}
	st.conv = conv;

	/* ---------- 2. 建 KCP 会话 ---------- */
	st.kcp = ikcp_create(conv, &st);
	ikcp_setoutput(st.kcp, kcp_output);
	ikcp_nodelay(st.kcp, 1, 10, 2, 1);
	ikcp_wndsize(st.kcp, 32, 128);
	ikcp_setmtu(st.kcp, 1400);

	/* ---------- 3. CSAuth ---------- */
	{
		unsigned id;
		int got_auth = 0, got_player = 0, items = -1;
		send_msg(&st, CS_AUTH, token, (int)strlen(token));

		deadline = now_ms() + 8000;
		while (now_ms() < deadline) {
			struct recv_msg m;
			if (!pump(&st, &m, 100)) continue;
			if (m.id == SC_AUTH) {
				got_auth = 1;
			} else if (m.id == SC_PLAYER) {
				got_player = 1;
				/* u32 playerId | str nick | str avatar | u32 level | i64 reg | u32 mail | i32 items */
				int pos = 4;
				int nick = get_u16(m.body + pos); pos += 2 + nick;
				int avatar = get_u16(m.body + pos); pos += 2 + avatar;
				pos += 4 + 8 + 4;
				if (pos + 4 <= m.body_len) items = (int)get_u32(m.body + pos);
			}
			if (got_auth && got_player) break;
		}
		check("SCAuth 返回", got_auth, "");
		check("SCPlayerData 返回", got_player, "");
		{
			char d[64];
			snprintf(d, sizeof(d), "items=%d", items);
			check("SCPlayerData 带背包", items > 0, d);
		}
		/* 消费掉可能跟着来的 SCBag 等 */
		(void)id;
	}

	/* ---------- 4. CSBag ---------- */
	{
		unsigned id;
		send_msg(&st, CS_BAG, NULL, 0);
		id = wait_msg(&st, 5000);
		check("CSBag -> SCBag", id == SC_BAG, "");
	}

	/* ---------- 5. 心跳 ---------- */
	{
		unsigned id;
		send_msg(&st, CS_HEART, NULL, 0);
		id = wait_msg(&st, 5000);
		check("CSHeart -> SCHeart", id == SC_HEART, "");
	}

	/* ---------- 6. 大数据量（验证分片/重传） ---------- */
	{
		int ok = 1;
		for (i = 0; i < 6; i++) {
			unsigned id;
			send_msg(&st, CS_BAG, NULL, 0);
			id = wait_msg(&st, 5000);
			if (id != SC_BAG) { ok = 0; break; }
		}
		{
			char d[64];
			snprintf(d, sizeof(d), "收到 %ld 字节", st.recv_bytes);
			check("连续 6 次请求都收到响应", ok, d);
		}
	}

	ikcp_release(st.kcp);
	close_sock(st.fd);

	printf("\n结果: PASS=%d FAIL=%d\n", g_pass, g_fail);
	return g_fail ? 1 : 0;
}