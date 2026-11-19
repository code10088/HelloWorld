-- 数据层：MySQL + Redis
--
-- 连接池：MySQL / Redis 各自维护一个连接池。skynet 的驱动在等待响应时会 yield，
--         同一个服务内的请求可能交错，所以每个并发请求必须拿到独立连接。
--
-- Redis 键：
--   token:<access>         -> userId                  TTL token_ttl
--   refresh:<refresh>      -> userId                  TTL token_ttl，用一次即删
--   online:<userId>        -> 1                       TTL 120s
--   player:<userId>        -> hash（玩家字段）          TTL player_cache_ttl
--   mails:<userId>         -> skynet.packstring(邮件表) TTL mail_cache_ttl
--
-- 缓存策略：读时回填（cache-aside），写时删除（delete-after-write）。
local skynet = require "skynet"
local mysql = require "skynet.db.mysql"
local redis = require "skynet.db.redis"
local md5 = require "md5"

local MYSQL_CONF = {
	host = skynet.getenv("mysql_host") or "127.0.0.1",
	port = tonumber(skynet.getenv("mysql_port") or "3306"),
	database = skynet.getenv("mysql_db") or "helloworld",
	user = skynet.getenv("mysql_user") or "hw",
	password = skynet.getenv("mysql_password") or "hw123456",
	charset = "utf8mb4",
	max_packet_size = 1024 * 1024,
}

local REDIS_CONF = {
	host = skynet.getenv("redis_host") or "127.0.0.1",
	port = tonumber(skynet.getenv("redis_port") or "6379"),
}

local MYSQL_POOL_SIZE = tonumber(skynet.getenv("mysql_pool_size") or "4")
local REDIS_POOL_SIZE = tonumber(skynet.getenv("redis_pool_size") or "4")

local TOKEN_TTL = tonumber(skynet.getenv("token_ttl") or "86400")
local PLAYER_CACHE_TTL = tonumber(skynet.getenv("player_cache_ttl") or "300")
local MAIL_CACHE_TTL = tonumber(skynet.getenv("mail_cache_ttl") or "60")
local BAG_CACHE_TTL = tonumber(skynet.getenv("bag_cache_ttl") or "60")
local ONLINE_TTL = 120

------------------------------------------------------------------
-- 连接池
------------------------------------------------------------------
local function new_pool(name, max, connect_fn)
	local pool = {
		name = name,
		max = max,
		created = 0,
		free = {},
		waiters = {},
		handoff = {},
	}

	function pool:acquire()
		if #self.free > 0 then
			return table.remove(self.free)
		end

		if self.created < self.max then
			self.created = self.created + 1
			local ok, conn = pcall(connect_fn)
			if ok and conn then
				return conn
			end
			self.created = self.created - 1
			error(string.format("%s 连接失败: %s", self.name, tostring(conn)))
		end

		-- 池满：挂起当前协程，等别人归还
		local co = coroutine.running()
		self.waiters[#self.waiters + 1] = co
		skynet.wait(co)
		local conn = self.handoff[co]
		self.handoff[co] = nil
		return conn
	end

	function pool:release(conn)
		local co = table.remove(self.waiters, 1)
		if co then
			-- 直接交给等待者，避免被其他协程抢走
			self.handoff[co] = conn
			skynet.wakeup(co)
		else
			self.free[#self.free + 1] = conn
		end
	end

	function pool:discard(conn)
		self.created = self.created - 1
		if conn then
			pcall(function() conn:close() end)
		end
	end

	return pool
end

local mysql_pool = new_pool("MySQL", MYSQL_POOL_SIZE, function()
	return mysql.connect(MYSQL_CONF)
end)

local redis_pool = new_pool("Redis", REDIS_POOL_SIZE, function()
	return redis.connect(REDIS_CONF)
end)

------------------------------------------------------------------
-- 访问封装
------------------------------------------------------------------
local function mysql_call(sql)
	local last_err

	for _ = 1, 2 do   -- 失败重试一次（多半是连接被回收）
		local conn = mysql_pool:acquire()
		local ok, res, err, errno, sqlstate = pcall(function()
			return conn:query(sql)
		end)

		if ok and res then
			mysql_pool:release(conn)
			return res
		end

		last_err = ok and err or res
		mysql_pool:discard(conn)
		skynet.error(string.format("MySQL 错误: %s (errno=%s sqlstate=%s)",
			tostring(ok and err or res), tostring(errno), tostring(sqlstate)))
	end

	skynet.error(string.format("MySQL 重试后仍失败, sql=%s, err=%s", sql, tostring(last_err)))
	return nil
end

local function redis_call(cmd, ...)
	local conn = redis_pool:acquire()
	local args = table.pack(...)
	local ok, a, b = pcall(function()
		return conn[cmd](conn, table.unpack(args, 1, args.n))
	end)

	if not ok then
		redis_pool:discard(conn)
		error(string.format("Redis %s 失败: %s", cmd, tostring(a)))
	end

	if a == false then
		redis_pool:release(conn)
		error(string.format("Redis %s 返回错误: %s", cmd, tostring(b)))
	end

	redis_pool:release(conn)
	return a
end

local function quote(s)
	return mysql.quote_sql_str(tostring(s))
end

local function to_number(v, default)
	if v == nil then
		return default
	end
	return tonumber(v) or default
end

local function new_token()
	local raw = string.format("%d|%d|%d|%s",
		os.time(), math.random(100000, 999999), math.random(100000, 999999), tostring({}))
	return md5.sumhexa(raw)
end

------------------------------------------------------------------
-- 缓存键
------------------------------------------------------------------
local function player_key(user_id)
	return "player:" .. user_id
end

local function mails_key(user_id)
	return "mails:" .. user_id
end

local function bag_key(user_id)
	return "bag:" .. user_id
end

-- 写后删除：下次读会重新回源
local function invalidate_player(user_id)
	redis_call("del", player_key(user_id))
	return true
end

local function invalidate_mails(user_id)
	redis_call("del", mails_key(user_id))
	return true
end

local function invalidate_bag(user_id)
	redis_call("del", bag_key(user_id))
	return true
end

local function invalidate_all(user_id)
	redis_call("del", player_key(user_id))
	redis_call("del", mails_key(user_id))
	redis_call("del", bag_key(user_id))
	return true
end

------------------------------------------------------------------
-- token
------------------------------------------------------------------
local function issue_token(user_id)
	local access = new_token()
	local refresh = new_token()

	redis_call("setex", "token:" .. access, TOKEN_TTL, user_id)
	redis_call("setex", "refresh:" .. refresh, TOKEN_TTL, user_id)
	redis_call("setex", "online:" .. user_id, ONLINE_TTL, "1")

	return { access = access, refresh = refresh }, user_id
end

local function resolve_token(token)
	if not token or token == "" then
		return nil
	end
	return redis_call("get", "token:" .. token)
end

local function refresh_tokens(refresh_token)
	if not refresh_token or refresh_token == "" then
		return nil
	end

	local user_id = redis_call("get", "refresh:" .. refresh_token)
	if not user_id then
		return nil
	end

	-- 只允许用一次
	redis_call("del", "refresh:" .. refresh_token)
	return issue_token(user_id)
end

local function touch_online(user_id)
	if user_id then
		redis_call("expire", "online:" .. user_id, ONLINE_TTL)
	end
end

local function offline(user_id)
	if user_id then
		redis_call("del", "online:" .. user_id)
	end
end

------------------------------------------------------------------
-- 玩家
------------------------------------------------------------------
local function load_player_row(user_id)
	local rows = mysql_call(string.format(
		"SELECT player_id, nick_name, avatar, level, register_time FROM players WHERE user_id=%s",
		quote(user_id)))

	if not rows or #rows == 0 then
		return nil
	end

	local p = rows[1]
	return {
		playerId = to_number(p.player_id, 10001),
		nickName = p.nick_name or "TestPlayer",
		avatar = p.avatar or "avatar_01",
		level = to_number(p.level, 1),
		registerTime = to_number(p.register_time, os.time()),
	}
end

local function read_player_cache(user_id)
	local cached = redis_call("hgetall", player_key(user_id))
	if not cached or #cached == 0 then
		return nil
	end
	local t = {}
	for i = 1, #cached, 2 do
		t[cached[i]] = cached[i + 1]
	end
	return {
		playerId = to_number(t.player_id, 10001),
		nickName = t.nick_name or "TestPlayer",
		avatar = t.avatar or "avatar_01",
		level = to_number(t.level, 1),
		registerTime = to_number(t.register_time, os.time()),
	}
end

local function write_player_cache(user_id, p)
	redis_call("hset", player_key(user_id),
		"player_id", tostring(p.playerId),
		"nick_name", tostring(p.nickName),
		"avatar", tostring(p.avatar),
		"level", tostring(p.level),
		"register_time", tostring(p.registerTime))
	redis_call("expire", player_key(user_id), PLAYER_CACHE_TTL)
end

local load_mails   -- 前向声明，定义在下面

local function create_player(user_id)
	local rows = mysql_call("SELECT COALESCE(MAX(player_id), 10000) + 1 AS next_id FROM players")
	local player_id = to_number(rows[1].next_id, 10001)
	mysql_call(string.format(
		"INSERT INTO players (user_id, player_id, nick_name, avatar, level, register_time) VALUES (%s, %d, 'Player', 'avatar_01', 1, %d)",
		quote(user_id), player_id, os.time()))
	skynet.error(string.format("新账号建号: %s -> player_id=%d", user_id, player_id))
end
local function load_player(user_id)
	local player = read_player_cache(user_id)
	if player then
		skynet.error(string.format("玩家缓存命中: %s", user_id))
	else
		player = load_player_row(user_id)
		if not player then
			create_player(user_id)
			player = load_player_row(user_id)
		end
		write_player_cache(user_id, player)
		skynet.error(string.format("玩家缓存未命中，已从 MySQL 载入: %s", user_id))
	end

	return { player = player, mails = load_mails(user_id) }
end

-- 更新玩家字段；白名单字段，写后清缓存
local PLAYER_FIELDS = { nick_name = true, avatar = true, level = true }

local function save_player(user_id, fields)
	local sets = {}
	for k, v in pairs(fields or {}) do
		if PLAYER_FIELDS[k] then
			if type(v) == "number" then
				sets[#sets + 1] = string.format("%s=%d", k, v)
			else
				sets[#sets + 1] = string.format("%s=%s", k, quote(v))
			end
		end
	end

	if #sets == 0 then
		return true
	end

	local res = mysql_call(string.format(
		"UPDATE players SET %s WHERE user_id=%s",
		table.concat(sets, ","), quote(user_id)))

	if not res then
		return false, "update failed"
	end

	invalidate_player(user_id)
	skynet.error(string.format("玩家已更新并清除缓存: %s", user_id))
	return true
end

------------------------------------------------------------------
-- 邮件
------------------------------------------------------------------
local function load_mails_from_db(user_id)
	local uid = quote(user_id)

	local mrows = mysql_call(string.format(
		"SELECT mail_id, type, status, title, content, create_time FROM mails WHERE user_id=%s ORDER BY mail_id", uid))
	local rrows = mysql_call(string.format(
		"SELECT mail_id, item_id, item_count FROM mail_rewards WHERE user_id=%s", uid))

	local reward_map = {}
	for _, rr in ipairs(rrows or {}) do
		local mid = tostring(rr.mail_id)
		local list = reward_map[mid]
		if not list then
			list = {}
			reward_map[mid] = list
		end
		list[#list + 1] = {
			itemId = to_number(rr.item_id, 0),
			count = to_number(rr.item_count, 0),
		}
	end

	local mails = {}
	for _, m in ipairs(mrows or {}) do
		mails[#mails + 1] = {
			id = to_number(m.mail_id, 0),
			type = to_number(m.type, 0),
			status = to_number(m.status, 0),
			time = to_number(m.create_time, os.time()),
			title = m.title,
			content = m.content,
			rewards = reward_map[tostring(m.mail_id)] or {},
		}
	end
	return mails
end

function load_mails(user_id)
	local raw = redis_call("get", mails_key(user_id))
	if raw then
		local ok, cached = pcall(skynet.unpack, raw, #raw)
		if ok and cached then
			skynet.error(string.format("邮件缓存命中: %s (%d 封)", user_id, #cached))
			return cached
		end
		skynet.error(string.format("邮件缓存损坏，丢弃: %s", user_id))
		redis_call("del", mails_key(user_id))
	end

	local mails = load_mails_from_db(user_id)
	redis_call("setex", mails_key(user_id), MAIL_CACHE_TTL, skynet.packstring(mails))
	skynet.error(string.format("邮件缓存未命中，已从 MySQL 载入: %s (%d 封)", user_id, #mails))
	return mails
end

local function set_mail_status(user_id, mail_ids, status)
	local list = {}
	for _, id in ipairs(mail_ids) do
		list[#list + 1] = tostring(to_number(id, 0))
	end
	if #list == 0 then
		return true
	end

	local res = mysql_call(string.format(
		"UPDATE mails SET status=%d WHERE user_id=%s AND mail_id IN (%s)",
		status, quote(user_id), table.concat(list, ",")))

	if not res then
		return false
	end
	invalidate_mails(user_id)
	return true
end

local function set_all_mail_status(user_id, status)
	local res = mysql_call(string.format(
		"UPDATE mails SET status=%d WHERE user_id=%s AND status<%d",
		status, quote(user_id), status))
	if not res then
		return false
	end
	invalidate_mails(user_id)
	return true
end

local function delete_mail(user_id, mail_id)
	local uid = quote(user_id)
	local mid = tostring(to_number(mail_id, 0))

	local a = mysql_call(string.format("DELETE FROM mail_rewards WHERE user_id=%s AND mail_id=%s", uid, mid))
	local b = mysql_call(string.format("DELETE FROM mails WHERE user_id=%s AND mail_id=%s", uid, mid))
	if not (a and b) then
		return false
	end
	invalidate_mails(user_id)
	return true
end

local function delete_all_mails(user_id)
	local uid = quote(user_id)
	local a = mysql_call("DELETE FROM mail_rewards WHERE user_id=" .. uid)
	local b = mysql_call("DELETE FROM mails WHERE user_id=" .. uid)
	if not (a and b) then
		return false
	end
	invalidate_mails(user_id)
	return true
end

------------------------------------------------------------------
-- 背包
------------------------------------------------------------------
-- 首次登录发放演示数据（用 player_flags 里的 bag_init 做幂等标记）
local function ensure_bag_init(user_id)
	local uid = quote(user_id)

	local res = mysql_call(string.format(
		"INSERT IGNORE INTO player_flags (user_id, flag, value) VALUES (%s, 'bag_init', 0)", uid))
	if not res then
		return
	end
	if to_number(res.affected_rows, 0) ~= 1 then
		return   -- 已经初始化过（或并发请求里别人先插入了）
	end

	mysql_call(string.format(
		"INSERT INTO bag_items (user_id, item_id, count) VALUES (%s, 9, 5), (%s, 10, 3)", uid, uid))
	mysql_call(string.format(
		"INSERT INTO bag_equips (user_id, equip_id, level, bind) VALUES (%s, 1, 1, 0), (%s, 2, 1, 0)", uid, uid))
	mysql_call(string.format(
		"UPDATE player_flags SET value=1 WHERE user_id=%s AND flag='bag_init'", uid))

	skynet.error(string.format("首次登录，已发放背包演示数据: %s", user_id))
end

local function load_bag(user_id)
	local raw = redis_call("get", bag_key(user_id))
	if raw then
		local ok, cached = pcall(skynet.unpack, raw, #raw)
		if ok and cached then
			skynet.error(string.format("背包缓存命中: %s", user_id))
			return cached
		end
		redis_call("del", bag_key(user_id))
	end

	ensure_bag_init(user_id)

	local uid = quote(user_id)
	local irows = mysql_call(string.format(
		"SELECT item_id, count FROM bag_items WHERE user_id=%s ORDER BY id", uid))
	local erows = mysql_call(string.format(
		"SELECT uid, equip_id, level, bind FROM bag_equips WHERE user_id=%s ORDER BY uid", uid))

	local bag = { items = {}, equips = {} }
	for _, row in ipairs(irows or {}) do
		bag.items[#bag.items + 1] = {
			itemId = to_number(row.item_id, 0),
			count = to_number(row.count, 0),
		}
	end
	for _, row in ipairs(erows or {}) do
		bag.equips[#bag.equips + 1] = {
			uid = to_number(row.uid, 0),
			equipId = to_number(row.equip_id, 0),
			level = to_number(row.level, 1),
			bind = to_number(row.bind, 0),
		}
	end

	redis_call("setex", bag_key(user_id), BAG_CACHE_TTL, skynet.packstring(bag))
	skynet.error(string.format("背包缓存未命中，已从 MySQL 载入: %s (%d 道具 / %d 装备)",
		user_id, #bag.items, #bag.equips))
	return bag
end

-- 使用道具：按格子顺序扣减，格子清零就删除
-- 返回 ok, 实际使用数量
local function use_item(user_id, item_id, count)
	item_id = to_number(item_id, 0)
	count = to_number(count, 0)
	if item_id <= 0 or count <= 0 then
		return false, 0
	end

	local uid = quote(user_id)
	local rows = mysql_call(string.format(
		"SELECT id, count FROM bag_items WHERE user_id=%s AND item_id=%d ORDER BY id", uid, item_id))
	if not rows or #rows == 0 then
		return false, 0
	end

	local remain = count
	local ok = true
	for _, row in ipairs(rows) do
		if remain <= 0 then
			break
		end
		local have = to_number(row.count, 0)
		local take = math.min(have, remain)
		remain = remain - take

		if take >= have then
			if not mysql_call(string.format("DELETE FROM bag_items WHERE id=%d", row.id)) then
				ok = false
			end
		else
			if not mysql_call(string.format("UPDATE bag_items SET count=count-%d WHERE id=%d", take, row.id)) then
				ok = false
			end
		end
	end

	local used = count - remain
	if used <= 0 then
		return false, 0
	end

	invalidate_bag(user_id)

	-- Bag.proto：SCUseItem.count 是使用后的剩余总数量，不是本次使用量
	local sum = mysql_call(string.format(
		"SELECT COALESCE(SUM(count),0) AS n FROM bag_items WHERE user_id=%s AND item_id=%d",
		uid, item_id))
	local left = 0
	if sum and sum[1] then
		left = to_number(sum[1].n, 0)
	end

	skynet.error(string.format("使用道具: user=%s item=%d used=%d left=%d",
		user_id, item_id, used, left))
	return ok, left
end

-- 往背包加道具（按堆叠上限合并到已有格子，满了再开新格子）
-- 注意：不在这里清缓存，由调用方统一 invalidate_bag
local BAG_STACK_LIMIT = tonumber(skynet.getenv("bag_stack_limit") or "99")

local function add_item_rows(user_id, item_id, count)
	item_id = to_number(item_id, 0)
	count = to_number(count, 0)
	if item_id <= 0 or count <= 0 then
		return 0
	end

	local limit = math.max(1, BAG_STACK_LIMIT)
	local uid = quote(user_id)

	local rows = mysql_call(string.format(
		"SELECT id, count FROM bag_items WHERE user_id=%s AND item_id=%d ORDER BY id", uid, item_id))

	local remain = count
	for _, row in ipairs(rows or {}) do
		if remain <= 0 then
			break
		end
		local have = to_number(row.count, 0)
		if have < limit then
			local add = math.min(limit - have, remain)
			mysql_call(string.format("UPDATE bag_items SET count=count+%d WHERE id=%d", add, row.id))
			remain = remain - add
		end
	end

	while remain > 0 do
		local add = math.min(limit, remain)
		mysql_call(string.format(
			"INSERT INTO bag_items (user_id, item_id, count) VALUES (%s, %d, %d)", uid, item_id, add))
		remain = remain - add
	end

	return count - remain
end

-- 领取单封邮件奖励：置 status=2 + 发奖入包，已领取则不再重复发
-- 返回 rewards, bag
local function claim_mail_reward(user_id, mail_id)
	mail_id = to_number(mail_id, 0)
	if mail_id <= 0 then
		return nil, nil, "invalid mail id"
	end

	local uid = quote(user_id)
	local rows = mysql_call(string.format(
		"SELECT status FROM mails WHERE user_id=%s AND mail_id=%d", uid, mail_id))
	if not rows or #rows == 0 then
		return nil, nil, "mail not found"
	end

	if to_number(rows[1].status, 0) >= 2 then
		-- 已领取：不重复发奖，返回当前背包
		return {}, load_bag(user_id)
	end

	local reward_rows = mysql_call(string.format(
		"SELECT item_id, item_count FROM mail_rewards WHERE user_id=%s AND mail_id=%d", uid, mail_id)) or {}

	mysql_call(string.format(
		"UPDATE mails SET status=2 WHERE user_id=%s AND mail_id=%d", uid, mail_id))

	local rewards = {}
	for _, row in ipairs(reward_rows) do
		rewards[#rewards + 1] = {
			itemId = to_number(row.item_id, 0),
			count = to_number(row.item_count, 0),
		}
		add_item_rows(user_id, row.item_id, row.item_count)
	end

	invalidate_mails(user_id)
	invalidate_bag(user_id)

	skynet.error(string.format("领取邮件奖励: user=%s mail=%d, %d 种道具",
		user_id, mail_id, #rewards))
	return rewards, load_bag(user_id)
end

-- 一键领取：所有未领取且有奖励的邮件
local function claim_all_mail_rewards(user_id)
	local uid = quote(user_id)
	local rows = mysql_call(string.format(
		"SELECT mail_id FROM mails WHERE user_id=%s AND status<2 ORDER BY mail_id", uid)) or {}

	local rewards = {}
	if #rows > 0 then
		for _, row in ipairs(rows) do
			local mid = to_number(row.mail_id, 0)
			local reward_rows = mysql_call(string.format(
				"SELECT item_id, item_count FROM mail_rewards WHERE user_id=%s AND mail_id=%d", uid, mid)) or {}
			for _, r in ipairs(reward_rows) do
				rewards[#rewards + 1] = {
					itemId = to_number(r.item_id, 0),
					count = to_number(r.item_count, 0),
				}
				add_item_rows(user_id, r.item_id, r.item_count)
			end
		end

		mysql_call(string.format(
			"UPDATE mails SET status=2 WHERE user_id=%s AND status<2", uid))
		invalidate_mails(user_id)
		invalidate_bag(user_id)
	end

	skynet.error(string.format("一键领取邮件奖励: user=%s, %d 种道具", user_id, #rewards))
	return rewards, load_bag(user_id)
end

------------------------------------------------------------------
-- 服务
------------------------------------------------------------------
local handlers = {
	issue_token = issue_token,
	resolve_token = resolve_token,
	refresh_tokens = refresh_tokens,
	touch_online = touch_online,
	offline = offline,
	load_player = load_player,
	save_player = save_player,
	set_mail_status = set_mail_status,
	set_all_mail_status = set_all_mail_status,
	delete_mail = delete_mail,
	delete_all_mails = delete_all_mails,
	load_bag = load_bag,
	use_item = use_item,
	claim_mail_reward = claim_mail_reward,
	claim_all_mail_rewards = claim_all_mail_rewards,
	invalidate_player = invalidate_player,
	invalidate_mails = invalidate_mails,
	invalidate_bag = invalidate_bag,
	invalidate_all = invalidate_all,
}

skynet.start(function()
	-- 预热一个连接；失败也不影响启动，后续按需重试
	if not pcall(function() local c = mysql_pool:acquire(); mysql_pool:release(c) end) then
		skynet.error("启动时 MySQL 未就绪，将在首次查询时重试")
	end
	if not pcall(function() local c = redis_pool:acquire(); redis_pool:release(c) end) then
		skynet.error("启动时 Redis 未就绪，将在首次访问时重试")
	end
	skynet.error(string.format("数据层 store 已就绪（MySQL 池 %d / Redis 池 %d）",
		MYSQL_POOL_SIZE, REDIS_POOL_SIZE))

	skynet.dispatch("lua", function(_, _, cmd, ...)
		local f = handlers[cmd]
		if not f then
			skynet.error("未知的 store 命令: " .. tostring(cmd))
			skynet.ret(skynet.pack(nil, "unknown command"))
			return
		end

		local ok, result, err = pcall(f, ...)
		if not ok then
			skynet.error(string.format("store 执行 %s 异常: %s", tostring(cmd), tostring(result)))
			skynet.ret(skynet.pack(nil, tostring(result)))
		else
			skynet.ret(skynet.pack(result, err))
		end
	end)
end)