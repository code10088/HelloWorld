-- 游戏服（skynet 版）
-- 协议与 Server/GameServer（C#）一致，客户端无需改动。
-- 玩家和邮件来自 store 服务（MySQL + Redis），拿到内存副本后按操作回写。
local skynet = require "skynet"
local socket = require "skynet.socket"
local proto = require "protocol"

local ID = proto.ID
local MAX_BODY = 16 * 1024

local store   -- store 服务地址，启动时填充

------------------------------------------------------------------
-- 默认数据（DB 不可用时的兜底）
------------------------------------------------------------------
local function now_ms()
	return math.floor(skynet.time() * 1000)
end

local function default_session()
	return {
		player = {
			playerId = 10001,
			nickName = "TestPlayer",
			avatar = "avatar_01",
			level = 1,
			registerTime = os.time(),
		},
		mails = {
			{
				id = 1, type = 0, status = 0, time = os.time(),
				title = "测试邮件1", content = "欢迎来到游戏",
				rewards = { { itemId = 1, count = 10 } },
			},
			{
				id = 2, type = 0, status = 0, time = os.time(),
				title = "测试邮件2", content = "这是一封测试邮件",
				rewards = {},
			},
		},
	}
end

------------------------------------------------------------------
-- 编码
------------------------------------------------------------------
local function write_mail_list(mails)
	local w = proto.writer()
	w:i32(#mails)
	for _, mail in ipairs(mails) do
		w:u32(mail.id)
		w:i32(mail.type or 0)
		w:u32(mail.status)
		w:i64(mail.time or os.time())
		w:str(mail.title)
		w:str(mail.content)
		w:i32(#mail.rewards)
		for _, reward in ipairs(mail.rewards) do
			w:u32(reward.itemId)
			w:u32(reward.count)
		end
	end
	return w:build()
end

-- 背包列表编码（SCBag 和 SCPlayerData 共用）
local function write_bag_into(w, bag)
	bag = bag or { items = {}, equips = {} }

	w:i32(#bag.items)
	for _, item in ipairs(bag.items) do
		w:u32(item.itemId)
		w:u32(item.count)
	end
	w:i32(#bag.equips)
	for _, equip in ipairs(bag.equips) do
		w:i64(equip.uid)
		w:u32(equip.equipId)
		w:u32(equip.level)
		w:u32(equip.bind)
	end
end

local function write_bag(bag)
	local w = proto.writer()
	write_bag_into(w, bag)
	return w:build()
end

-- SCPlayerData：Player.proto 里新增了 items / equips
local function write_player_data(player, bag)
	local w = proto.writer()
	w:u32(player.playerId)
	w:str(player.nickName)
	w:str(player.avatar)
	w:u32(player.level)
	w:i64(player.registerTime)
	w:u32(0)                     -- mail
	write_bag_into(w, bag)
	return w:build()
end

------------------------------------------------------------------
-- 业务
------------------------------------------------------------------
local function find_mail(mails, mail_id)
	for _, mail in ipairs(mails) do
		if mail.id == mail_id then
			return mail
		end
	end
end

local function send(fd, id, payload)
	socket.write(fd, proto.frame(id, payload))
end

local function handle_auth(fd, session, token)
	local user_id = skynet.call(store, "lua", "resolve_token", token)
	if not user_id then
		user_id = "test-user-1"
		skynet.error(string.format("token 无效（%s），回退到测试账号 %s", tostring(token), user_id))
	end

	local data, err = skynet.call(store, "lua", "load_player", user_id)
	if not data then
		skynet.error(string.format("载入玩家失败（%s），使用默认数据", tostring(err)))
		local fallback = default_session()
		session.user_id = user_id
		session.player = fallback.player
		session.mails = fallback.mails
	else
		session.user_id = user_id
		session.player = data.player
		session.mails = data.mails
	end

	session.last_touch = os.time()

	-- 首登时这里会触发背包演示数据发放
	local bag = skynet.call(store, "lua", "load_bag", session.user_id)
	if not bag then
		bag = { items = {}, equips = {} }
	end

	local w = proto.writer()
	w:i32(1)                 -- code: 1=成功
	w:i64(now_ms())          -- 服务器时间
	send(fd, ID.SCAuth, w:build())
	send(fd, ID.SCPlayerData, write_player_data(session.player, bag))

	skynet.error(string.format("玩家已登录: %s, 邮件 %d 封", user_id, #session.mails))
end

local function handle_message(fd, session, id, r)
	if id == ID.CSAuth then
		handle_auth(fd, session, r:str())

	elseif id == ID.CSHeart then
		if session.user_id then
			local now = os.time()
			if now - (session.last_touch or 0) >= 30 then
				session.last_touch = now
				skynet.call(store, "lua", "touch_online", session.user_id)
			end
		end
		local w = proto.writer()
		w:i64(now_ms())
		send(fd, ID.SCHeart, w:build())

	elseif id == ID.CSMail then
		send(fd, ID.SCMail, write_mail_list(session.mails))

	elseif id == ID.CSReadMail then
		local count = r:i32()
		local ids = {}
		for _ = 1, count do
			ids[#ids + 1] = r:u32()
		end
		for _, mail_id in ipairs(ids) do
			local mail = find_mail(session.mails, mail_id)
			if mail and mail.status == 0 then
				mail.status = 1
			end
		end
		if session.user_id then
			skynet.call(store, "lua", "set_mail_status", session.user_id, ids, 1)
		end
		send(fd, ID.SCMail, write_mail_list(session.mails))

	elseif id == ID.CSGetMailReward then
		local mail_id = r:u32()
		local mail = find_mail(session.mails, mail_id)
		if mail then
			mail.status = 2
		end

		-- 置 status=2 + 奖励入包（重复领取不会重复发）
		local rewards, bag
		if session.user_id then
			rewards, bag = skynet.call(store, "lua", "claim_mail_reward", session.user_id, mail_id)
		end

		local w = proto.writer()
		w:i32(rewards and 0 or 1)   -- 0=成功
		w:u32(mail_id)
		send(fd, ID.SCGetMailReward, w:build())

		-- 主动推一次背包，客户端不用再请求
		if bag then
			send(fd, ID.SCBag, write_bag(bag))
		end

	elseif id == ID.CSGetMailAllReward then
		for _, mail in ipairs(session.mails) do
			if mail.status < 2 then
				mail.status = 2
			end
		end

		local _, bag
		if session.user_id then
			_, bag = skynet.call(store, "lua", "claim_all_mail_rewards", session.user_id)
		end

		send(fd, ID.SCGetMailAllReward, "")
		if bag then
			send(fd, ID.SCBag, write_bag(bag))
		end

	elseif id == ID.CSDeleteMail then
		local mail_id = r:u32()
		for i, mail in ipairs(session.mails) do
			if mail.id == mail_id then
				table.remove(session.mails, i)
				break
			end
		end
		if session.user_id then
			skynet.call(store, "lua", "delete_mail", session.user_id, mail_id)
		end
		local w = proto.writer()
		w:i32(0)                 -- 0=成功
		w:u32(mail_id)
		send(fd, ID.SCDeleteMail, w:build())

	elseif id == ID.CSDeleteAllMail then
		for i = #session.mails, 1, -1 do
			session.mails[i] = nil
		end
		if session.user_id then
			skynet.call(store, "lua", "delete_all_mails", session.user_id)
		end
		local w = proto.writer()
		w:i32(0)                 -- 空邮件列表
		send(fd, ID.SCMail, w:build())

	elseif id == ID.CSBag then
		local bag
		if session.user_id then
			bag = skynet.call(store, "lua", "load_bag", session.user_id)
		end
		if not bag then
			bag = { items = {}, equips = {} }
		end
		send(fd, ID.SCBag, write_bag(bag))

	elseif id == ID.CSUseItem then
		local item_id = r:u32()
		local count = r:u32()

		local ok, left = false, 0
		if session.user_id then
			ok, left = skynet.call(store, "lua", "use_item", session.user_id, item_id, count)
		end

		local w = proto.writer()
		w:i32(ok and 1 or 0)     -- 1=成功
		w:u32(item_id)
		w:u32(left or 0)         -- 使用后剩余数量（Bag.proto 定义）
		send(fd, ID.SCUseItem, w:build())

	else
		skynet.error(string.format("未处理的消息 id=%d", id))
	end
end

------------------------------------------------------------------
-- 连接
------------------------------------------------------------------
local function handle_connection(fd, addr)
	socket.start(fd)
	skynet.error(string.format("客户端已连接: %s (fd=%d)", tostring(addr), fd))

	local session = {}

	while true do
		local head = socket.read(fd, 4)
		if not head then
			break
		end

		local len = string.unpack("<i4", head)
		if len < 2 or len > MAX_BODY then
			skynet.error(string.format("[%s] 非法包长: %d", tostring(addr), len))
			break
		end

		local body = socket.read(fd, len)
		if not body then
			break
		end

		local ok, err = pcall(function()
			local id, r = proto.parse_frame(body)
			if id ~= ID.CSHeart then
				skynet.error(string.format("[%s] 收到消息 id=%d, len=%d", tostring(addr), id, len))
			end
			handle_message(fd, session, id, r)
		end)

		if not ok then
			skynet.error(string.format("[%s] 处理异常: %s", tostring(addr), tostring(err)))
			break
		end
	end

	if session.user_id then
		pcall(skynet.call, store, "lua", "offline", session.user_id)
	end

	socket.close(fd)
	skynet.error(string.format("客户端已断开: %s (fd=%d)", tostring(addr), fd))
end

------------------------------------------------------------------
-- 启动
------------------------------------------------------------------
skynet.start(function()
	store = skynet.uniqueservice("store")

	local port = tonumber(skynet.getenv("game_port") or "8088")
	local listen_id, ip = socket.listen("0.0.0.0", port)
	skynet.error(string.format(
		"游戏服已启动: TCP %s:%d (listen fd=%d)", tostring(ip), port, listen_id))

	socket.start(listen_id, function(fd, addr)
		skynet.fork(handle_connection, fd, addr)
	end)
end)