-- 手写二进制协议（临时版本，后续改为 proto 生成）
--
-- 帧格式（与 Unity 客户端 TcpChannel 完全一致，小端）：
--   [int32 bodyLength][uint16 messageId][payload]
--   bodyLength = 2 + #payload
--
-- 字符串： [uint16 length][utf8 bytes]
-- 整数：   int32 / uint32 / int64，全部小端
local M = {}

M.ID = {
	CSSession         = 0,
	CSAuth            = 1,
	CSHeart           = 2,
	CSMail            = 100,
	CSReadMail        = 101,
	CSGetMailReward   = 102,
	CSGetMailAllReward= 103,
	CSDeleteMail      = 104,
	CSDeleteAllMail   = 105,
	CSBag             = 106,
	CSUseItem         = 107,

	SCSession         = 10000,
	SCAuth            = 10001,
	SCHeart           = 10002,
	SCPlayerData      = 10003,
	SCMail            = 10100,
	SCGetMailReward   = 10102,
	SCGetMailAllReward= 10103,
	SCDeleteMail      = 10104,
	SCBag             = 10106,
	SCUseItem         = 10107,
}

------------------------------------------------------------------
-- 写
------------------------------------------------------------------
local Writer = {}
Writer.__index = Writer

function M.writer()
	return setmetatable({ parts = {} }, Writer)
end

function Writer:i32(v)
	self.parts[#self.parts + 1] = string.pack("<i4", v)
	return self
end

function Writer:u32(v)
	self.parts[#self.parts + 1] = string.pack("<I4", v)
	return self
end

function Writer:i64(v)
	self.parts[#self.parts + 1] = string.pack("<i8", v)
	return self
end

function Writer:u16(v)
	self.parts[#self.parts + 1] = string.pack("<I2", v)
	return self
end

function Writer:str(s)
	s = s or ""
	self.parts[#self.parts + 1] = string.pack("<I2", #s) .. s
	return self
end

function Writer:build()
	return table.concat(self.parts)
end

------------------------------------------------------------------
-- 读
------------------------------------------------------------------
local Reader = {}
Reader.__index = Reader

function M.reader(data, pos)
	return setmetatable({ data = data, pos = pos or 1 }, Reader)
end

function Reader:i32()
	local v
	v, self.pos = string.unpack("<i4", self.data, self.pos)
	return v
end

function Reader:u32()
	local v
	v, self.pos = string.unpack("<I4", self.data, self.pos)
	return v
end

function Reader:i64()
	local v
	v, self.pos = string.unpack("<i8", self.data, self.pos)
	return v
end

function Reader:u16()
	local v
	v, self.pos = string.unpack("<I2", self.data, self.pos)
	return v
end

function Reader:str()
	local len
	len, self.pos = string.unpack("<I2", self.data, self.pos)
	local s = self.data:sub(self.pos, self.pos + len - 1)
	self.pos = self.pos + len
	return s
end

function Reader:remaining()
	return #self.data - self.pos + 1
end

------------------------------------------------------------------
-- 帧
------------------------------------------------------------------
function M.frame(id, payload)
	local body = string.pack("<I2", id) .. (payload or "")
	return string.pack("<i4", #body) .. body
end

-- KCP 帧：u16 messageId + payload（没有长度前缀，靠 KCP 的消息边界）
function M.kcp_frame(id, payload)
	return string.pack("<I2", id) .. (payload or "")
end

function M.parse_frame(body)
	local id = string.unpack("<I2", body, 1)
	return id, M.reader(body, 3)
end

return M