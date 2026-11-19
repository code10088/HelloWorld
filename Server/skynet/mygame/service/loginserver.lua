-- 登录服（skynet 版）
--   GET  /health
--   POST /auth/login    body: {"platform":0,"authCode":"1","deviceId":"...","clientVersion":"1.2.0"}
--   POST /auth/refresh  body: {"refreshToken":"..."}
--
-- authCode 由 auth.lua 校验；通过后按校验出的 userId 签发 token：
--   token:<access>    -> userId   (TTL token_ttl)
--   refresh:<refresh> -> userId   (TTL token_ttl，使用一次后作废)
local skynet = require "skynet"
local socket = require "skynet.socket"
local httpd = require "http.httpd"
local sockethelper = require "http.sockethelper"
local urllib = require "http.url"
local auth = require "auth"

local listen_host = skynet.getenv("login_host") or "127.0.0.1"
local listen_port = tonumber(skynet.getenv("login_port") or "5080")
local game_host   = skynet.getenv("game_host") or "127.0.0.1"
local game_port   = skynet.getenv("game_port") or "8088"
local server_id   = skynet.getenv("server_id") or "test-server-1"

local MAX_BODY = 8192

local store

------------------------------------------------------------------
-- JSON
------------------------------------------------------------------
local function esc(s)
	return (tostring(s):gsub('[%z\1-\31\\"]', function(c)
		if c == '"' then
			return '\\"'
		elseif c == '\\' then
			return '\\\\'
		end
		return string.format("\\u%04x", c:byte())
	end))
end

local function auth_json(tokens, user_id)
	return string.format(
		'{"code":1,"accessToken":"%s","refreshToken":"%s","userId":"%s","serverId":"%s","host":"%s","port":"%s"}',
		esc(tokens.access), esc(tokens.refresh), esc(user_id), esc(server_id),
		esc(game_host), esc(game_port))
end

local function error_json(code, message)
	if message then
		return string.format('{"code":%d,"message":"%s"}', code, esc(message))
	end
	return string.format('{"code":%d}', code)
end

------------------------------------------------------------------
-- HTTP
------------------------------------------------------------------
local function respond(write, code, body, content_type)
	return httpd.write_response(write, code, body, {
		["content-type"] = content_type or "application/json; charset=utf-8",
		["connection"] = "close",
	})
end

local function handle_login(write, body)
	local platform = tonumber(body and body:match('"platform"%s*:%s*(%-?%d+)') or "0") or 0
	local auth_code = body and body:match('"authCode"%s*:%s*"([^"]*)"') or ""

	local user_id, err = auth.verify(auth_code, platform)
	if not user_id then
		skynet.error(string.format("authCode 校验失败: %s (code=%s)", tostring(err), tostring(auth_code)))
		respond(write, 200, error_json(0, err))
		return
	end

	local tokens = skynet.call(store, "lua", "issue_token", user_id)
	if not tokens then
		skynet.error("签发 token 失败")
		respond(write, 200, error_json(0, "token 签发失败"))
		return
	end

	skynet.error(string.format("登录成功: user=%s, auth_mode=%s", user_id, auth.mode()))
	respond(write, 200, auth_json(tokens, user_id))
end

local function handle_refresh(write, body)
	local refresh_token = body and body:match('"refreshToken"%s*:%s*"([^"]*)"')
	local tokens, user_id = nil, nil

	if refresh_token then
		tokens, user_id = skynet.call(store, "lua", "refresh_tokens", refresh_token)
	end

	if not tokens then
		skynet.error("refreshToken 无效或已过期")
		respond(write, 200, error_json(0, "refreshToken 无效或已过期"))
		return
	end

	respond(write, 200, auth_json(tokens, user_id))
end

local function handle(fd)
	socket.start(fd)

	local read = sockethelper.readfunc(fd)
	local write = sockethelper.writefunc(fd)

	local code, url, method, header, body = httpd.read_request(read, MAX_BODY)

	if not code then
		socket.close(fd)
		return
	end

	if code ~= 200 then
		respond(write, code, error_json(code))
		socket.close(fd)
		return
	end

	local path = urllib.parse(url)

	if path == "/health" then
		respond(write, 200, "ok", "text/plain; charset=utf-8")

	elseif path == "/auth/login" then
		if method ~= "POST" then
			respond(write, 405, error_json(405))
		else
			skynet.error(string.format("%s body: %s", path, body or ""))
			handle_login(write, body)
		end

	elseif path == "/auth/refresh" then
		if method ~= "POST" then
			respond(write, 405, error_json(405))
		else
			skynet.error(string.format("%s body: %s", path, body or ""))
			handle_refresh(write, body)
		end

	else
		respond(write, 404, error_json(404))
	end

	socket.close(fd)
end

------------------------------------------------------------------
-- 启动
------------------------------------------------------------------
skynet.start(function()
	store = skynet.uniqueservice("store")

	local listen_id, ip = socket.listen(listen_host, listen_port)
	skynet.error(string.format(
		"登录服已启动: HTTP %s:%d (listen fd=%d), 下发游戏服 %s:%s, auth_mode=%s",
		tostring(ip), listen_port, listen_id, game_host, tostring(game_port), auth.mode()))

	socket.start(listen_id, function(fd, addr)
		skynet.fork(handle, fd)
	end)
end)