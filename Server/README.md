# 服务端部署说明（腾讯云 Ubuntu 22.04 / 2C2G）

对应客户端代码：`HelloWorld/Assets/Scripts/SubScripts`

## 0. 先看清客户端到底连什么

| 项目 | 代码位置 | 结论 |
| --- | --- | --- |
| 传输协议 | `GameStart.cs:64` `Create<TcpChannel>` | **只走 TCP，不用 KCP**。`AIServerTest.txt` 里 KCP 那段现在不需要部署 |
| 登录接口 | `AccountInfo.cs:100` `{LoginUrl}auth/login` | `https://112.126.84.17/auth/login` |
| 刷新接口 | `AccountInfo.cs:87` `{LoginUrl}auth/refresh` | `https://112.126.84.17/auth/refresh` |
| 游戏服地址 | 登录响应里的 `host` / `port` | 必须返回公网 `112.126.84.17` 和 `8088` |
| 游戏服协议 | `TcpChannel.cs` + `NetMsgHandle_Auto.cs` | `[int32 长度][uint16 id][payload]`，小端 |
| 鉴权 | `AccountInfo.cs:233` | 连接成功后发 `CSAuth(1)`，等 `SCAuth(10001) code=1` |
| 进主场景 | `PlayerData.cs:25` | 再收到 `SCPlayerData(10003)` 才触发 `RefreshPlayerData` |

所以这台服务器要跑两个进程：

```
客户端 --HTTPS 443--> nginx --HTTP 127.0.0.1:5080--> LoginServer   （只做登录，返回游戏服地址）
客户端 --TCP 8088---------------------------------> GameServer     （AIServerTest.txt 的 TCP 版）
```

仓库里已经准备好可编译的代码：

```
Server/GameServer/          TCP 游戏服（AIServerTest.txt 的 TCP 段，支持多客户端）
Server/LoginServer/         /auth/login、/auth/refresh
Server/deploy/              systemd、nginx、TCP 验证脚本
```

本机已用 `dotnet build` 编译并做过端到端测试：登录返回 `Content-Length: 164`，TCP 返回 `SCAuth(10001) code=1` + `SCPlayerData(10003)`。

---

## 1. 腾讯云安全组 / 防火墙

控制台 → 轻量应用服务器或 CVM → 防火墙，放行入站：

| 端口 | 协议 | 用途 |
| --- | --- | --- |
| 22 | TCP | SSH（建议只对你的 IP 开放） |
| 80 | TCP | 申请/续期证书、HTTP 跳转 |
| 443 | TCP | 登录接口 HTTPS |
| 8088 | TCP | 游戏服 |

> 只有改用 KCP 才需要放行 8088/UDP。

如果系统里开了 ufw：

```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 8088/tcp
sudo ufw enable
```

2G 内存建议加 2G swap（可选）：

```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

---

## 2. 在 Windows 上发布 Linux 程序

项目目标框架是 `net8.0`（已编译通过）。在仓库根目录 `D:\HelloWorld` 执行：

```powershell
dotnet publish Server\GameServer  -c Release -r linux-x64 --self-contained true -o Server\publish\game
dotnet publish Server\LoginServer -c Release -r linux-x64 --self-contained true -o Server\publish\login
```

自包含发布不需要在服务器上另装 .NET 运行时；两个目录加起来约 150MB，2G 内存完全够。

上传：

```powershell
ssh root@112.126.84.17 "mkdir -p /opt/aiserver"
scp -r Server\publish\game  root@112.126.84.17:/opt/aiserver/
scp -r Server\publish\login root@112.126.84.17:/opt/aiserver/
scp -r Server\deploy root@112.126.84.17:/opt/aiserver/
```

如果你更想用框架依赖方式，就在服务器执行 `apt install aspnetcore-runtime-8.0`，发布时去掉
`--self-contained true`，此时 service 里的 `ExecStart` 要改成
`/usr/bin/dotnet /opt/aiserver/game/GameServer.dll`。

---

## 3. 创建运行用户和 systemd 服务

```bash
sudo useradd -r -s /usr/sbin/nologin aiserver
sudo chown -R aiserver:aiserver /opt/aiserver
sudo chmod +x /opt/aiserver/game/GameServer /opt/aiserver/login/LoginServer

sudo cp /opt/aiserver/deploy/aiserver-game.service  /etc/systemd/system/
sudo cp /opt/aiserver/deploy/aiserver-login.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now aiserver-game aiserver-login
```

检查：

```bash
systemctl status aiserver-game aiserver-login --no-pager
journalctl -u aiserver-game -f
ss -lntp | grep -E '8088|5080'
```

应该看到 `0.0.0.0:8088` 和 `127.0.0.1:5080`。

> `aiserver-login.service` 里的 `GAME_HOST` 必须是 `112.126.84.17`，
> 不能写 `127.0.0.1` 或内网 IP，否则客户端拿到地址后连不上。

---

## 4. HTTPS：这一步是“能否正常登录”的关键

客户端 `HttpManager` 没有设置任何证书回调，**自签名证书一定会被拒绝**（表现为
`data == null` → 提示 10008/10012，然后回登录界面）。

所以只有两条路：

- **保留 https** → 必须有一张客户端信任的证书。没有域名也可以，用 Let's Encrypt 的 IP 证书。
- **改成 http** → 完全不需要证书，但要改客户端，且 Android 9+ 还要允许明文流量。

### 方案 A：域名 + Let's Encrypt（推荐，长期最省心）

1. 域名 A 记录指向 `112.126.84.17`；国内服务器用域名访问需要备案。
2. 安装 nginx 和 certbot：

```bash
sudo apt update
sudo apt install -y nginx
sudo snap install --classic certbot
sudo ln -sf /snap/bin/certbot /usr/bin/certbot
```

3. 先放一个最小站点让 80 端口可用（见下面的引导配置），然后：

```bash
sudo certbot --nginx -d game.example.com
```

4. 把 `GameSetting.cs` 的 `LoginUrl` 改成 `https://game.example.com/`。

> 纯 IP 直连不需要备案；一旦用域名访问国内服务器，就需要 ICP 备案。

### 方案 B：没有域名，直接给 IP 申请证书

Let's Encrypt 从 2026 年起支持 IPv4/IPv6 证书，但**强制使用 `shortlived` 配置，
有效期 160 小时（约 6 天）**。需要 Certbot 5.4+，Ubuntu 22.04 仓库里的旧版不行，
必须用 snap 安装。

**第 1 步：装新版 certbot 和 nginx**

```bash
sudo apt update && sudo apt install -y nginx
sudo snap install --classic certbot
sudo ln -sf /snap/bin/certbot /usr/bin/certbot
certbot --version          # 确认 >= 5.4
```

**第 2 步：先用只有 80 端口的引导配置**

证书还没签出来时，不能启用带 443 的正式配置，否则 `nginx -t` 会报证书文件不存在。
仓库里已经准备了引导配置：

```bash
sudo mkdir -p /var/www/html
sudo cp /opt/aiserver/deploy/nginx-bootstrap-http.conf /etc/nginx/sites-available/aiserver
sudo ln -sf /etc/nginx/sites-available/aiserver /etc/nginx/sites-enabled/aiserver
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx

# 在你自己电脑上确认 80 端口可达，应返回 aiserver bootstrap
curl http://112.126.84.17/
```

**第 3 步：先用 staging 试签，避免浪费正式额度**

```bash
sudo certbot certonly --staging \
  --preferred-profile shortlived \
  --webroot -w /var/www/html \
  --ip-address 112.126.84.17 \
  --cert-name ip-112-126-84-17
```

**第 4 步：staging 成功后，去掉 `--staging` 正式签**

```bash
sudo certbot certonly \
  --preferred-profile shortlived \
  --webroot -w /var/www/html \
  --ip-address 112.126.84.17 \
  --cert-name ip-112-126-84-17
```

证书位置：`/etc/letsencrypt/live/ip-112-126-84-17/fullchain.pem` 和 `privkey.pem`。

注意：

- IP 证书只能用 `webroot` / `standalone` / `manual` 校验，nginx 插件**不会**自动改配置，
  443 那段要自己写（仓库的 `nginx-aiserver.conf` 已经写好）。
- 同一 IP 每 7 天最多 5 张正式证书，所以先 staging。
- 校验走 `http://112.126.84.17/.well-known/acme-challenge/...`，**80 端口必须对公网开放**。

**第 5 步：换成正式 nginx 配置**

```bash
sudo cp /opt/aiserver/deploy/nginx-aiserver.conf /etc/nginx/sites-available/aiserver
sudo nano /etc/nginx/sites-available/aiserver
```

把 `server_name` 改成 `112.126.84.17;`，证书路径改成：

```
ssl_certificate     /etc/letsencrypt/live/ip-112-126-84-17/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/ip-112-126-84-17/privkey.pem;
```

然后：

```bash
sudo nginx -t && sudo systemctl reload nginx
```

**第 6 步：自动续期（6 天证书的关键）**

snap 版 certbot 自带 `snap.certbot.renew.timer`，但续期后必须让 nginx 重新加载证书：

```bash
sudo mkdir -p /etc/letsencrypt/renewal-hooks/deploy
sudo tee /etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh >/dev/null <<'EOF'
#!/bin/sh
systemctl reload nginx
EOF
sudo chmod +x /etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh

sudo systemctl list-timers | grep -i certbot
sudo certbot renew --dry-run
```

证书 160 小时有效，正常情况下第 4 天左右自动续。建议再加一条监控：

```bash
sudo certbot certificates          # 看 Expiry Date
```

**第 7 步：确认证书正确**

```bash
echo | openssl s_client -connect 112.126.84.17:443 \
  -servername 112.126.84.17 2>/dev/null \
  | openssl x509 -noout -issuer -dates -ext subjectAltName
```

应看到 `Issuer: C=US, O=Let's Encrypt` 和 `IP Address:112.126.84.17`。
证书没问题的话，`GameSetting.LoginUrl` 可以继续用 `https://112.126.84.17/`，客户端零改动。

> 如果不想每 6 天折腾续期，也可以找商业 CA 买 IP 证书（通常 1 年，价格较高），
> 但中小项目一般用上面的 Let's Encrypt 就够了。

### 方案 C：不用证书，改成 HTTP

把 `GameSetting.cs` 的 `LoginUrl` 改成 `http://112.126.84.17/`，nginx 只监听 80。

- **编辑器 / PC**：直接可用，不需要任何证书。
- **Android**：项目 `targetSdkVersion = 30`，Android 9+ 默认禁止明文 HTTP。需要自定义清单
  `Assets/Plugins/Android/AndroidManifest.xml`，在 `<application>` 上加：

```xml
<application
    android:usesCleartextTraffic="true"
    ... >
```

- **WebGL**：https 页面里请求 http 会被浏览器按混合内容拦截。
- **微信/抖音小游戏**：要求 https + 已备案域名白名单，**不能直接用 IP**。

所以 HTTP 只适合临时验证，长期还是建议 A 或 B。

### 方案 D：自签证书 + 客户端放行（仅内测）

不想用公共 CA、也不想改 http，可以自签，但必须改客户端信任逻辑。

服务器生成自签证书：

```bash
sudo mkdir -p /etc/ssl/aiserver
sudo openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
  -keyout /etc/ssl/aiserver/aiserver.key \
  -out /etc/ssl/aiserver/aiserver.crt \
  -subj "/CN=112.126.84.17" \
  -addext "subjectAltName=IP:112.126.84.17"
```

nginx 的 `ssl_certificate` / `ssl_certificate_key` 指到这两个文件。

客户端在 `HttpManager.HttpItem.Request` 调用 `hwr.GetResponse()` 之前加（需
`using System.Net.Security;`）：

```csharp
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) =>
{
    if (errors == SslPolicyErrors.None) return true;

    // 只放行测试服 IP，不要全局 return true
    return sender is HttpWebRequest req && req.RequestUri.Host == "112.126.84.17";
};
```

限制：WebGL 的 TLS 由浏览器控制，无法这样放行；iOS 还可能受 ATS 限制。
**这只适合内测，正式上线不要用。**

### nginx 配置

```bash
sudo cp /opt/aiserver/deploy/nginx-aiserver.conf /etc/nginx/sites-available/aiserver
sudo ln -s /etc/nginx/sites-available/aiserver /etc/nginx/sites-enabled/aiserver
sudo rm -f /etc/nginx/sites-enabled/default
sudo nano /etc/nginx/sites-available/aiserver   # 替换 server_name 和证书路径
sudo nginx -t && sudo systemctl reload nginx
```

- 方案 A：`server_name` 和证书路径都用域名；
- 方案 B：`server_name 112.126.84.17;`，证书路径用 `ip-112-126-84-17`；
- 方案 C：只保留 80 的 server 块，删掉 443 的；
- 方案 D：证书路径指向 `/etc/ssl/aiserver/`。

---

## 5. 客户端需要改的地方

`HelloWorld/Assets/Scripts/MainScripts/GameSetting.cs:23`：

```csharp
"https://112.126.84.17/";          // 方案 B/D 保持这样
"https://game.example.com/";       // 方案 A
"http://112.126.84.17/";           // 方案 C
```

注意 `GameSetting.cs` 在 **MainScripts（AOT 程序集）**，改完必须重新出包，热更不会生效。

其它不用改：

- `GameStart.cs` 默认仍然是 `TcpChannel`；要测 KCP 就在 Player Settings 加 `USE_KCP` 宏
  （代码里已经用 `#if USE_KCP` 切换），或者直接把分支改成 `Create<KcpChannel>`；
- 登录服返回的 `host` = `112.126.84.17`，`port` = `"8088"`。

KCP 服务端已经部署为 `aiserver-game-kcp`（UDP 8088），详见第 10 节。

---

## 6. 验证顺序（建议逐条做）

### 6.1 服务器本机

```bash
curl -sS -X POST http://127.0.0.1:5080/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"platform":0,"authCode":"1","deviceId":"t","clientVersion":"1.2.0"}'
```

应返回：

```json
{"code":1,"accessToken":"test-access-token","refreshToken":"test-refresh-token",
 "userId":"test-user-1","serverId":"test-server-1","host":"112.126.84.17","port":"8088"}
```

再走 nginx：

```bash
curl -sS -X POST https://112.126.84.17/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"platform":0,"authCode":"1","deviceId":"t","clientVersion":"1.2.0"}'
```

### 6.2 你自己的电脑

```powershell
Test-NetConnection 112.126.84.17 -Port 8088
powershell -ExecutionPolicy Bypass -File Server\deploy\verify-tcp.ps1 112.126.84.17 8088
```

看到 `鉴权帧正确：SCAuth(10001) code=1` 就说明 TCP 通了。

### 6.3 客户端

登录点击“QQ 登录”（`UILogin.cs` 里写死 `authCode = "1"`），日志顺序应为：

```
HTTP 200 /auth/login
连接服务器成功
收到 TCP 消息 id=1
收到 TCP 消息 id=10001
收到 TCP 消息 id=10003
```

`SCPlayerData` 收到后才会关掉加载界面进主场景。

---

## 7. 常见问题对照

| 现象 | 原因 |
| --- | --- |
| 一直停在热更界面 | 热更走的是腾讯 COS CDN（`GameSetting.CDN`），和这台服务器无关，先确认 CDN 上有当前版本的资源 |
| 提示网络异常 / 10008 / 10012 | HTTPS 证书不被信任；或 443 没通；或 nginx 没代理到 5080 |
| 登录成功但马上“网络异常” | 登录响应里 `host` 是内网 IP/127.0.0.1，或 8088 没放行 |
| 登录后卡在加载界面 | 游戏服只回了 `SCAuth`，没回 `SCPlayerData(10003)`；确认部署的是本仓库 `Server/GameServer` |
| 客户端解析登录 JSON 出错 | 响应不能是 chunked，必须有 `Content-Length`；`port` 必须是字符串 `"8088"`，不能是数字 |
| 服务器日志中文乱码 | 用 `journalctl` 看；Windows 下 `*>` 重定向到文件会按本地编码写，不影响 Linux |
| 进程启动即退出 | `journalctl -u aiserver-game -n 50`；多为端口占用或缺可执行权限 |

### 客户端一个已知小隐患

`HttpManager.HttpItem.Request` 用的是：

```csharp
result = new byte[hwb.ContentLength];
stream.Read(result, 0, result.Length);   // 只读一次，不保证读满
```

服务端 JSON 很小（164 字节），通常一次就读完；但如果以后响应变大或网络分片，
可能出现 JSON 尾部为 0 导致解析失败。要彻底稳，建议改成循环读满：

```csharp
result = new byte[hwb.ContentLength];
int offset = 0;
while (offset < result.Length)
{
    int n = stream.Read(result, offset, result.Length - offset);
    if (n <= 0) break;
    offset += n;
}
```

---

## 9. 本次实际部署记录（2026-11-11）

| 项 | 值 |
| --- | --- |
| 云厂商 / 地域 | 阿里云 / `cn-beijing` |
| 实例 | `i-2ze34rkx4y8okqrwjl2k`（Ubuntu 22.04.5，x86_64） |
| EIP | `112.126.84.17`（`eip-2zeig98o5wadsl3epx4l1`） |
| 登录服 | `127.0.0.1:5080`，systemd `aiserver-login` |
| 游戏服（TCP） | `0.0.0.0:8088`，systemd `aiserver-game` |
| 游戏服（KCP） | `0.0.0.0:8088/UDP`，systemd `aiserver-game-kcp` |
| nginx | 1.18，站点 `/etc/nginx/sites-available/aiserver`（来自 `nginx-aiserver-ip.conf`） |
| 证书 | `/etc/letsencrypt/live/ip-112-126-84-17/`，IP 证书，有效期 160 小时 |
| 到期时间 | 2026-11-17 18:30 UTC |
| 续期 | `snap.certbot.renew.timer` + `/etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh` |
| 安全组 | 80/TCP、443/TCP、8088/TCP、**8088/UDP** 均已放行 |

实际签发命令：

```bash
/snap/bin/certbot certonly --non-interactive --agree-tos \
  --register-unsafely-without-email \
  --preferred-profile shortlived \
  --webroot -w /var/www/html \
  --ip-address 112.126.84.17 \
  --cert-name ip-112-126-84-17
```

续期验证：

```bash
/snap/bin/certbot renew --dry-run --no-random-sleep-on-renew \
  --cert-name ip-112-126-84-17
```

已从公网验证通过：

- `POST https://112.126.84.17/auth/login` → 200，`Content-Length: 167`
- `POST https://112.126.84.17/auth/refresh` → 200
- `http://112.126.84.17/` → 301 到 https
- `TCP 112.126.84.17:8088` → `SCAuth(10001) code=1` + `SCPlayerData(10003)`

客户端 `GameSetting.cs` 的 `LoginUrl` 仍然是 `https://112.126.84.17/`，
本次部署不需要改客户端、不需要重新出包。

维护提醒：

- 证书只有 6 天，**续期失败会导致登录突然不可用**；定期执行
  `/snap/bin/certbot certificates` 看到期时间。
- Workbench CLI 的 AK 属于 RAM 子用户，部署完成后建议禁用或轮换该 AK。
- 当前 8088/443 对 `0.0.0.0/0` 开放，且登录服不校验 token，仅适合内测。

---

## 8. 安全提醒

`LoginServer` 和 `GameServer` 都是**测试实现**：

- 不校验 `authCode`，任何请求都返回成功；
- 不校验 `accessToken`，谁都能连上 8088；
- 邮件等功能没有持久化，进程重启就还原。

只适合内测。对外长期开放前，至少要做：登录接口校验、token 校验、按连接限速、
把 22 端口限制到固定 IP，并给两个服务配置非 root 用户（上面的 service 已使用 `aiserver`）。

---

## 10. KCP 部署与测试

KCP 服务端已经作为独立进程部署，和 TCP 版**同时监听 8088**（一个是 TCP，一个是 UDP，不冲突）。

### 10.1 目录与项目

```text
Server/GameServerKcp/         KCP 游戏服，直接引用 Assets/Plugins/Kcp 源码
Server/deploy/aiserver-game-kcp.service
```

服务器上的路径：

```text
/opt/aiserver/game-kcp/       GameServerKcp
```

### 10.2 安全组

除了 TCP 的 80/443/8088，还需要放行：

| 端口 | 协议 | 授权对象 | 用途 |
| --- | --- | --- | --- |
| `8088/8088` | **UDP** | `0.0.0.0/0` | KCP |

> 注意：TCP 8088 和 UDP 8088 是两条独立规则，只开 TCP 不够。

### 10.3 服务管理

```bash
systemctl status aiserver-game-kcp
journalctl -u aiserver-game-kcp -f
systemctl restart aiserver-game-kcp
```

`aiserver-game-kcp` 和 `aiserver-game` 可以同时运行：

```bash
ss -lntp | grep 8088     # TCP: GameServer
ss -lnup | grep 8088     # UDP: GameServerKcp
```

### 10.4 客户端切换

`GameStart.cs` 已经改成：

```csharp
#if USE_KCP
        NetClient.Instance.Create<KcpChannel>(host, port);
#else
        NetClient.Instance.Create<TcpChannel>(host, port);
#endif
```

测 KCP 时在 **Project Settings → Player → Scripting Define Symbols** 加 `USE_KCP`，
删掉该宏就回到 TCP。改完需要重新出包（AOT 代码，热更不生效）。

### 10.5 测试

加 `USE_KCP` 出包后启动客户端，在服务器上看日志：

```bash
journalctl -u aiserver-game-kcp -f
```

正常应该依次出现：

```text
KCP 握手: token=..., version=1, endpoint=...
收到 KCP 消息 id=1, length=21
鉴权 token=...
```

能进主场景、邮件能正常刷新，就说明 KCP 通道是通的。

### 10.6 当前限制

`GameServerKcp` 是**单客户端**测试服务器：全局只有一个 `remote` / `kcp` 实例。
两个客户端同时连会互相覆盖。需要多客户端时，把 `remote`/`kcp` 改成
`ConcurrentDictionary<IPEndPoint, KcpSession>`，按 `RemoteEndPoint` 分发。`