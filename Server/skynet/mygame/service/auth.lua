-- authCode 校验
--
-- auth_mode：
--   whitelist（默认）authCode 必须在 auth_whitelist 里（逗号分隔），映射到配置的 user_id
--   sign             authCode = "<userId>.<expire>.<md5(userId.."."..expire.."."..secret)>"
--   mock             任意 authCode 都通过（仅本地调试）
--
-- platform：
--   0 QQ     authCode 走上面的 auth_mode
--   1 Google authCode 是 Google ID Token（JWT）
--
-- TODO 上线前：Google ID Token 目前只解 payload，没有验签，任何人可伪造。
--              改成调 https://oauth2.googleapis.com/tokeninfo?id_token=... 让 Google 验签，
--              需要先给 skynet 编出 ltls.so（https 支持）。
local skynet = require "skynet"
local md5 = require "md5"

local mode = skynet.getenv("auth_mode") or "whitelist"
local secret = skynet.getenv("auth_secret") or "dev-secret-change-me"
local default_user = skynet.getenv("user_id") or "test-user-1"
local google_client_id = skynet.getenv("google_client_id") or ""

local PLATFORM_GOOGLE = 1

local whitelist = {}
for code in (skynet.getenv("auth_whitelist") or ""):gmatch("[^,]+") do
	whitelist[code] = true
end

local M = {}

function M.mode()
	return mode
end

------------------------------------------------------------------
-- base64url
------------------------------------------------------------------
local b64chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
local b64index = {}
for i = 1, #b64chars do
	b64index[b64chars:sub(i, i)] = i - 1
end

local function b64url_decode(s)
	s = s:gsub("-", "+"):gsub("_", "/")
	s = s .. string.rep("=", (4 - #s % 4) % 4)

	local out = {}
	local buf, bits = 0, 0
	for i = 1, #s do
		local c = s:sub(i, i)
		if c ~= "=" then
			local v = b64index[c]
			if not v then
				return nil
			end
			buf = buf * 64 + v
			bits = bits + 6
			if bits >= 8 then
				bits = bits - 8
				out[#out + 1] = string.char(math.floor(buf / 2 ^ bits) % 256)
				buf = buf % 2 ^ bits
			end
		end
	end
	return table.concat(out)
end

------------------------------------------------------------------
-- Google ID Token
------------------------------------------------------------------
local google_issuer = {
	["accounts.google.com"] = true,
	["https://accounts.google.com"] = true,
}

local function verify_google(id_token)
	local payload = id_token:match("^[^.]+%.([^.]+)%.")
	local json = payload and b64url_decode(payload)
	if not json then
		return nil, "idToken 格式错误"
	end

	local aud = json:match('"aud"%s*:%s*"([^"]*)"')
	local iss = json:match('"iss"%s*:%s*"([^"]*)"')
	local sub = json:match('"sub"%s*:%s*"([^"]*)"')
	local exp = tonumber(json:match('"exp"%s*:%s*(%d+)') or "")

	if google_client_id == "" or aud ~= google_client_id then
		return nil, "idToken audience 不匹配"
	end
	if not google_issuer[iss] then
		return nil, "idToken issuer 不匹配"
	end
	if not exp or exp <= os.time() then
		return nil, "idToken 已过期"
	end
	if not sub or sub == "" then
		return nil, "idToken 缺少 sub"
	end

	return "g_" .. sub
end

------------------------------------------------------------------
-- 本地 authCode
------------------------------------------------------------------
local function verify_local(auth_code)
	if mode == "mock" then
		return default_user
	end

	if mode == "whitelist" then
		if auth_code == "" then
			return nil, "authCode 为空"
		end
		if not whitelist[auth_code] then
			return nil, "authCode 不在白名单"
		end
		return default_user
	end

	if mode == "sign" then
		local uid, expire, sign = auth_code:match("^(.-)%.(%d+)%.([0-9a-fA-F]+)$")
		if not uid or uid == "" then
			return nil, "authCode 格式错误"
		end
		if tonumber(expire) < os.time() then
			return nil, "authCode 已过期"
		end
		local expect = md5.sumhexa(uid .. "." .. expire .. "." .. secret)
		if expect ~= sign:lower() then
			return nil, "authCode 签名不匹配"
		end
		return uid
	end

	return nil, "未知 auth_mode: " .. tostring(mode)
end

-- 客户端上报的是 SDK 原始回调串：
--   QQ     "0|openid|access_token|expires_in"
--   Google "0|id_token"
function M.verify(auth_code, platform)
	auth_code = auth_code or ""

	local _, payload = auth_code:match("^(%-?%d+)|(.+)$")
	if not payload then
		return verify_local(auth_code)
	end

	if platform == PLATFORM_GOOGLE then
		if not payload:find("%.") then
			return verify_local(payload)
		end
		return verify_google(payload)
	end

	return "q_" .. payload:match("^([^|]+)")
end

-- 测试/工具用：生成一个 sign 模式下合法的 authCode
function M.sign(uid, ttl)
	local expire = os.time() + (ttl or 300)
	local s = md5.sumhexa(uid .. "." .. expire .. "." .. secret)
	return string.format("%s.%d.%s", uid, expire, s)
end

return M