-- 启动器：先拉起数据层，再启动游戏服和登录服
-- 配置文件里 start = "server_main"
local skynet = require "skynet"

skynet.start(function()
	skynet.error("正在启动服务...")
	skynet.uniqueservice("store")
	skynet.newservice("gameserver")
	skynet.newservice("loginserver")
	skynet.error("服务已全部启动")
	skynet.exit()
end)