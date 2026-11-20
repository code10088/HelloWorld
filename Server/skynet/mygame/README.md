# mygame —— skynet 服务器（MySQL + Redis）

把现有 Unity 客户端用到的服务搬到 skynet 上，**协议和接口保持不变**，客户端不需要改代码：

- 游戏服：TCP 二进制协议（原 `Server/GameServer`）
- 登录服：HTTP `/auth/login`、`/auth/refresh`（原 `Server/LoginServer`）
- 数据层：MySQL 持久化 + Redis 缓存/token，带连接池
- 登录安全：authCode 校验（白名单 / 签名 / mock 三种模式）

## 架构

```text
                     ┌──────────────┐
  HTTPS 443 ────────►│  nginx       │
                     └──────┬───────┘
                            │ http 127.0.0.1:5080
                     ┌──────▼───────┐        ┌─────────┐
                     │ loginserver  │───────►│  Redis  │
                     │ authCode 校验 │        │  6379   │
                     └──────────────┘        └────▲────┘
                                                  │
                     ┌──────────────┐             │
  TCP 8088 ─────────►│  gameserver  │──┐          │
  KCP(UDP) 8088 ────►│              │  │   ┌──────┴──────┐
                     └──────────────┘  │   ┌──────┴──────┐
                                       └──►│    store    │
                                           │ MySQL/Redis │
                                           │   连接池     │
                                           └──────┬──────┘
                                                  │
                                           ┌──────▼──────┐
                                           │    MySQL    │
                                           │    3306     │
                                           └─────────────┘
```

`store.lua` 是唯一持有数据库连接的服务，其他服务通过 `skynet.call` 访问。
MySQL / Redis 各有一个连接池，skynet 的驱动在等响应时会 yield，同一个服务内的
多个请求会交错，所以每次访问都要独占一个连接。

## 目录

```text
mygame/
├─ config                         skynet 配置（端口、auth、MySQL/Redis、缓存 TTL）
├─ start-db.bat                   启动本地 MySQL + Redis
├─ sql/
│  ├─ init.sql                    建库建表 + 种子数据（可重复执行）
│  ├─ verify.sql                  查表内容
│  └─ migrate-bag.sql             背包增量迁移（已有环境用这个，别重跑 init.sql）
├─ test-login.ps1                 登录服测试（含 authCode 校验、token 轮换）
├─ test-bag.ps1                   背包测试（发放/使用/持久化/不重复发放）
├─ test-mailreward.ps1            邮件奖励入包测试（领取/重复领取/堆叠合并）
├─ test-auth-sign.ps1             sign 模式签名校验测试
├─ test-gameserver.ps1            游戏服测试（含持久化、缓存、并发）
├─ tools/
│  └─ kcp-client.c                KCP 测试客户端（C，握手/鉴权/背包/心跳/丢包）
└─ service/
   ├─ server_main.lua             启动器：store → gameserver → loginserver
   ├─ store.lua                   MySQL + Redis 数据层（连接池 + 缓存）
   ├─ auth.lua                    authCode 校验
   ├─ gameserver.lua              连接管理 + 消息分发 + 邮件逻辑
   ├─ loginserver.lua             HTTP 登录服
   └─ protocol.lua                手写二进制协议（消息 ID、读写、封帧）
```

## 启动

1. 启动 MySQL + Redis（便携版在 `D:\HelloWorld\Server`）：

```bat
D:\HelloWorld\Server\skynet\mygame\start-db.bat
```

2. 首次初始化数据库：

```powershell
D:\HelloWorld\Server\mysql\bin\mysql.exe -u root --skip-password --default-character-set=utf8mb4 < D:\HelloWorld\Server\skynet\mygame\sql\init.sql
```

3. 启动 skynet：

```bat
D:\HelloWorld\Server\skynet\run-server.bat
```

启动后：

```text
游戏服   TCP  0.0.0.0:8088
登录服   HTTP 127.0.0.1:5080
```

## authCode 校验

`service/auth.lua` 支持三种模式，由 `config` 的 `auth_mode` 决定：

| 模式 | 行为 | 用途 |
| --- | --- | --- |
| `whitelist`（默认） | authCode 必须出现在 `auth_whitelist` 里 | 测试服，现有客户端发 `"1"` 可直接登录 |
| `sign` | `authCode = "<userId>.<expire>.<md5(userId.."."..expire.."."..secret)>"` | 有服务端签发能力的场景 |
| `mock` | 任意 authCode 都通过 | 本地调试 |

校验失败返回：

```json
{"code":0,"message":"authCode 不在白名单"}
```

**接真实平台**：Google 见下面的小节；微信 / Apple / 抖音等还没接，在 `service/auth.lua` 里按 `platform` 加分支。

`test-auth-sign.ps1` 覆盖了 sign 模式的合法签名、错误签名、过期、格式错误、不同 userId。

### 平台回调直传

客户端把 SDK 原始回调串整个当 `authCode` 上报，服务端自己取需要的字段：

| platform | 客户端上报 | 服务端取值 |
| --- | --- | --- |
| 0 QQ | `0\|openid\|access_token\|expires_in` | `userId = "q_" .. openid` |
| 1 Google | `0\|id_token` | `userId = "g_" .. sub` |

解析规则：服务端按 `ret|payload` 拆开，只处理第一个 `|` 之后的部分；**不带 `|` 的串**（如 `"1"`）仍走上面的 `auth_mode`，所以老的白名单测试不受影响。编辑器桩 `0\|0` / `0\|0\|0\|0` 分别落到白名单 `test-user-1` 和 `q_0`。

**QQ**

- 目前**不校验 access_token**，直接信任客户端上报的 openid
- 上线前改成 `GET https://graph.qq.com/oauth2.0/me?access_token=<token>&unionid=1`，校验返回的 `client_id == 1906400845` 且 `openid` 与上报一致

**Google**

- 解 payload 取 `sub`
- 校验 `aud == google_client_id`、`iss` 是 `accounts.google.com`、`exp` 未过期
- 目前**不验签**，上线前改成 `GET https://oauth2.googleapis.com/tokeninfo?id_token=<token>`

> 两个平台的正式校验都需要 https（`ltls.so`），而当前 skynet 构建没编它。补齐之前，任何客户端都能伪造 openid / JWT 冒充别人，仅限内测。

新用户第一次登录时 `store.lua` 的 `create_player` 会自动建号（`player_id = MAX + 1`，从 `10000` 起）。

## 数据库

库名 `helloworld`，账号 `hw / hw123456`（`mysql_native_password`，skynet 的驱动只支持这个插件）。

| 表 | 说明 |
| --- | --- |
| `players` | `user_id` 主键，`player_id / nick_name / avatar / level / register_time` |
| `mails` | `(user_id, mail_id)` 唯一，`type / status / title / content / create_time` |
| `mail_rewards` | `(user_id, mail_id, item_id)` 唯一，`item_count` |
| `bag_items` | 道具格子，`(user_id, item_id)` 索引，一行一个堆叠 |
| `bag_equips` | 装备实例，`uid` 自增就是客户端看到的装备唯一 ID |
| `player_flags` | 玩家级开关（如 `bag_init`：是否已发放过背包演示数据） |

种子数据：`test-user-1` / `TestPlayer`，2 封邮件，邮件 1 带 `item=1 x10`。

## Redis 键

| Key | 类型 | TTL | 用途 |
| --- | --- | --- | --- |
| `token:<access>` | string | `token_ttl`(24h) | 游戏服校验 token → userId |
| `refresh:<refresh>` | string | `token_ttl` | 刷新用，**使用一次后删除** |
| `online:<userId>` | string | 120s | 在线标记，心跳每 30s 续期，断线立即删除 |
| `player:<userId>` | hash | `player_cache_ttl`(300s) | 玩家字段缓存 |
| `mails:<userId>` | string | `mail_cache_ttl`(60s) | 邮件列表缓存（`skynet.packstring` 序列化） |
| `bag:<userId>` | string | `bag_cache_ttl`(60s) | 背包缓存（`skynet.packstring` 序列化） |

## 缓存策略

读：cache-aside。先查 Redis，未命中查 MySQL 并回填。

写：delete-after-write。任何写操作成功后**删除对应缓存键**，下次读自然回源。

| 操作 | 失效的键 |
| --- | --- |
| `save_player`（改昵称/头像/等级） | `player:<uid>` |
| 已读 / 领取 / 删除邮件 | `mails:<uid>` |
| `invalidate_all` | 两个键都删 |

日志里可以直接看到：

```text
玩家缓存未命中，已从 MySQL 载入: test-user-1
邮件缓存未命中，已从 MySQL 载入: test-user-1 (2 封)
玩家缓存命中: test-user-1
邮件缓存命中: test-user-1 (0 封)
玩家已更新并清除缓存: test-user-1
```

## 配置（`mygame/config`）

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `game_port` | `8088` | 游戏服端口，也是登录服下发的端口 |
| `game_kcp_port` | `8088` | KCP(UDP) 端口，默认与 `game_port` 同号（TCP/UDP 可共用同一端口号） |
| `kcp_timeout` | `30` | KCP 会话超时（秒），超过这么久没有 UDP 输入就判定掉线 |
| `login_host` / `login_port` | `127.0.0.1` / `5080` | 登录服监听地址 |
| `game_host` | `127.0.0.1` | **下发给客户端的游戏服地址**，部署时改公网 IP/域名 |
| `user_id` | `test-user-1` | whitelist/mock 模式登录的账号 |
| `auth_mode` | `whitelist` | `whitelist` / `sign` / `mock` |
| `auth_whitelist` | `1,test` | 白名单 authCode，逗号分隔 |
| `auth_secret` | `dev-secret-change-me` | sign 模式密钥 |
| `mysql_*` | `127.0.0.1:3306/helloworld` | 账号 `hw / hw123456` |
| `mysql_pool_size` | `4` | MySQL 连接池大小 |
| `redis_*` / `redis_pool_size` | `127.0.0.1:6379` / `4` | Redis 连接池 |
| `token_ttl` | `86400` | token 有效期（秒） |
| `player_cache_ttl` | `300` | 玩家缓存有效期 |
| `mail_cache_ttl` | `60` | 邮件缓存有效期 |
| `bag_cache_ttl` | `60` | 背包缓存有效期 |
| `bag_stack_limit` | `99` | 服务端合并道具时的堆叠上限 |

## 数据流

**登录**

```text
POST /auth/login
  → auth.verify(authCode, platform)   -- 白名单 / 签名 / mock
  → store.issue_token(userId)         -- Redis: token / refresh / online
  → 返回 { code, accessToken, refreshToken, userId, serverId, host, port }
```

**进游戏**

```text
CSAuth(accessToken)
  → store.resolve_token → Redis GET token:<token>
  → 查不到则回退到配置账号 test-user-1（兼容旧 token）
  → store.load_player
       player：Redis HGETALL，未命中查 MySQL 回填
       mails ：Redis GET，未命中查 MySQL 回填
  → store.load_bag（首次登录在这里发演示数据）
  → SCAuth + SCPlayerData
       SCPlayerData = 玩家字段 + registerTime + mail + **items[] + equips[]**
       （Player.proto 已带背包快照，客户端不需要再单独发 CSBag 拿首屏数据）
```

**邮件**

```text
CSMail          → 返回内存副本
CSReadMail      → 内存置 status=1 + MySQL UPDATE + DEL mails:<uid>
CSGetMailReward → store.claim_mail_reward
                   置 status=2（重复领取不再发奖）
                   mail_rewards 里的道具按 bag_stack_limit 合并进 bag_items
                   DEL mails:<uid> + DEL bag:<uid>
                   → SCGetMailReward + **SCBag 主动推送**
CSGetMailAll…   → store.claim_all_mail_rewards（同上，批量）
                   → SCGetMailAllReward + **SCBag 主动推送**
CSDeleteMail    → 内存删除 + MySQL DELETE + DEL mails:<uid>
CSDeleteAllMail → 内存清空 + MySQL DELETE + DEL mails:<uid>
```

**背包**

```text
CSBag
  → store.load_bag
       首次：player_flags 无 bag_init → 发放演示数据（9×5、10×3、装备 1/2）并打标记
       Redis GET bag:<uid>，未命中查 bag_items / bag_equips 回填
  → SCBag { items: [itemId,count...], equips: [uid,equipId,level,bind...] }

CSUseItem { itemId, count }
  → store.use_item：按格子顺序扣减，清零删行，DEL bag:<uid>
  → SCUseItem { code: 1成功 / 0失败, itemId, count: **使用后剩余总数量** }
       Bag.proto 定义：count = 当前道具剩余数量（不是本次使用量）
```

> 道具没有格子索引，顺序由 `bag_items.id` 决定；装备 `uid` 就是 `bag_equips.uid`（自增）。
> 客户端只做展示副本，扣减以服务端返回的 `count` 为准。

**断线**

```text
连接关闭 → store.offline(userId) → DEL online:<uid>
```

## 测试

```powershell
powershell -ExecutionPolicy Bypass -File D:\HelloWorld\Server\skynet\mygame\test-login.ps1
powershell -ExecutionPolicy Bypass -File D:\HelloWorld\Server\skynet\mygame\test-auth-sign.ps1   # 需临时切 auth_mode=sign
powershell -ExecutionPolicy Bypass -File D:\HelloWorld\Server\skynet\mygame\test-gameserver.ps1 127.0.0.1 8088 5080
powershell -ExecutionPolicy Bypass -File D:\HelloWorld\Server\skynet\mygame\test-bag.ps1 127.0.0.1 8088 5080
powershell -ExecutionPolicy Bypass -File D:\HelloWorld\Server\skynet\mygame\test-mailreward.ps1 127.0.0.1 8088 5080
```

- `test-login.ps1`：health、合法/非法/空 authCode、token 是 32 位 hex、两次登录不同、refresh 轮换、userId 保持、旧 refresh 复用失败、405、404、连续 20 次。
- `test-auth-sign.ps1`：合法签名、错误签名、过期、格式错误、签名里的 userId 生效。
- `test-gameserver.ps1`：重置库 → 登录拿 token → 鉴权 → 心跳 → 邮件全流程 → 重连验证持久化 → 无效 token 回退 → **5 个并发连接**（连接池）→ 恢复种子数据。
- `test-bag.ps1`：重置背包 → 首登自动发放演示数据（并校验 SCPlayerData 携带背包）→ 使用道具（普通/超量/异常参数，校验 SCUseItem.count 为剩余数量）→ 重连验证持久化 → 确认不会重复发放。
- `test-mailreward.ps1`：领取邮件奖励入包 → 重复领取不重复发 → 多封同道具合并堆叠 → 邮件状态置 2 → 重连持久化。

### KCP 通道测试（C 客户端）

```bash
# 编译（Windows / MSYS2）
gcc -O2 -o kcp-client.exe kcp-client.c ../../3rd/kcp/ikcp.c -I../../3rd/kcp -lws2_32 -lwinmm
# 编译（Linux）
gcc -O2 -o kcp-client kcp-client.c ../../3rd/kcp/ikcp.c -I../../3rd/kcp

# 正常链路
./kcp-client 127.0.0.1 8088 <accessToken>
# 20% 丢包下验证重传
./kcp-client 127.0.0.1 8088 <accessToken> --drop 20
# 伪造 token 应被拒（conv=0）
./kcp-client 127.0.0.1 8088 fake-token --expect-bad-token
# 延迟采样（CSHeart 往返，30 次）
./kcp-client 112.126.84.17 8088 <accessToken> --latency 30
```

## 传输协议（TCP / KCP）

客户端根据 `GameStart.cs` 选择的 `IChannel` 决定用哪种传输，两种连接跑同一套业务逻辑。

```text
TCP 帧：  [i32 bodyLength][u16 messageId][payload]     // bodyLength = 2 + #payload
KCP 帧：  [u16 messageId][payload]                    // 靠 KCP 消息边界，没有长度前缀
```

KCP 握手（客户端 `KcpChannel` 已实现，服务端在 `gameserver.lua` 的 UDP 回调里处理）：

```text
请求（裸 UDP 包）: u16 0x0000(CSSession) | u16 tokenLen | token | i32 version
响应（裸 UDP 包）: u16 10000(SCSession) | i32 1 | u32 conv      // conv == 0 表示 token 无效
之后：全部走 KCP，conv 用于区分会话
```

KCP 参数必须与客户端一致：

```text
NoDelay(1, 5, 2, 1)     窗口 32 / 128    MTU 1400    消息模式（stream = 0）
```

服务端每 10ms 驱动一次所有会话（`update` → `flush` → `sendto`）；收到数据并处理完后会立即 `flush_now`（跳过 KCP interval 门控）+ `flush` 下发，避免多等一个 tick；超过 `kcp_timeout` 没有收到任何 UDP 包即判定掉线，走与 TCP 相同的下线清理。

> **为什么不能只调小 `interval` 让 `update` 自己发**：`ikcp_update` 只在 `current >= ts_flush` 时才真正 flush，而 skynet 的定时器最小粒度是 10ms。数据到达时若正好落在 interval 窗口内，`update` 不会发，响应要等下一个 tick。
> 实测（本机回环，同一客户端）：
>
> | 数据路径 | interval | avg | p50 |
> | --- | --- | --- | --- |
> | `update(now)` | 1ms | 19.5ms | 20.5ms |
> | `flush_now(now)` | 5ms | 10.1ms | 11.2ms |
>
> 所以低延迟必须靠数据路径上的强制 flush；`flush_now(now)` 会先刷新 `kcp->current`，避免 RTT 采样偏大。

> KCP 依赖 `luaclib/kcp.so`（`3rd/kcp` + `lualib-src/lkcp.c`）。缺这个模块时游戏服只开 TCP，日志会打印 `luaclib kcp 未加载`。

## 部署（线上 ECS）

| 项 | 值 |
| --- | --- |
| 服务器 | 阿里云 Ubuntu 22.04，`112.126.84.17` |
| 目录 | `/opt/aiserver/skynet`（运行用户 `aiserver`） |
| systemd | `skynet.service`（开机自启，`Restart=always`） |
| MySQL | 8.0.46，`127.0.0.1:3306`，库 `helloworld` |
| Redis | 6.0.16，`127.0.0.1:6379` |
| 登录服 | `127.0.0.1:5080`，由 nginx 反代 443 |
| 游戏服 | `0.0.0.0:8088`（TCP）+ `0.0.0.0:8088`（UDP/KCP） |
| 安全组 | 需要放行 **8088/TCP 和 8088/UDP**（UDP 漏放行时客户端握手超时） |
| 下发地址 | `game_host = "112.126.84.17"` |

### 打包（本地 → ECS）

本地改完并 **commit** 后，用 git 里已提交的内容生成包（`git archive` 只收录跟踪文件，`mygame/config`、编译产物、`msys64/mysql/redis` 都不会进来）：

```powershell
cd D:\HelloWorld
git archive --format=tar.gz --prefix=skynet/ `
  -o Server\skynet-linux.tar.gz HEAD:Server/skynet `
  -- . ":(exclude)mygame/config"
```

> 必须显式排除 `mygame/config`：它目前仍被 git 跟踪（含本机数据库密码），不排除会覆盖线上配置。
>
> 注意用 `HEAD:Server/skynet`：这样打出来的是**已提交**的版本；工作区里未提交的改动不会进包。

上传（`<instance-id>` 换成 ECS 实例 ID）：

```powershell
$wb = "$env:LOCALAPPDATA\Programs\workbench\workbench.exe"
& $wb upload Server\skynet-linux.tar.gz /tmp/skynet-linux.tar.gz -i <instance-id> -f
```

服务器上解压 + 编译 + 启动（**直接覆盖，不保留备份**）：

```bash
systemctl stop skynet

# 包里没有 mygame/config，直接解压不会碰到线上配置
tar -xzf /tmp/skynet-linux.tar.gz -C /opt/aiserver
chown -R aiserver:aiserver /opt/aiserver/skynet

cd /opt/aiserver/skynet
make clean && make linux -j4 MALLOC_STATICLIB= SKYNET_DEFINES=-DNOUSE_JEMALLOC
systemctl start skynet
systemctl is-active skynet
```

> 本项目**不做部署备份**：不创建 `skynet.bak.*`，也不复制 `mygame/config`。
> 覆盖式解压依赖两点前提：包的根目录固定是 `skynet/`，且 `mygame/config` 始终在打包时排除。
> 出问题时的恢复手段是重新打一次包再覆盖，而不是回滚目录。

### 首次部署（新机器 / 新目录）

包里的 `mygame/config` 是**排除**的（含数据库密码），所以新机器第一次部署要自己建：

```bash
cd /opt/aiserver/skynet/mygame
cp config.example config
vi config
```

至少确认这几项：

| 配置项 | 说明 |
| --- | --- |
| `game_host` | 下发给客户端的游戏服地址（公网 IP / 域名，如 `112.126.84.17`） |
| `mysql_password` | 线上真实数据库密码 |
| `auth_secret` | `sign` 模式用的签名密钥 |
| `google_client_id` | Google 登录（`platform=1`）校验 ID Token 的 `aud`；**漏了会报 `idToken audience 不匹配`** |
| `auth_mode` / `auth_whitelist` | 测试环境 `whitelist`；正式环境用 `sign` 或接平台校验 |

> 已经在跑的机器做升级时**不用动** `config`：包里不含 `config`，覆盖解压不会影响它。
>
> `auth.lua`（Google 分支）和 `store.lua`（首次登录自动建号）都是代码，随包一起更新；只要包是用最新提交打的就不会漏。

验证：`journalctl -u skynet -n 30 --no-pager` 应看到

```text
数据层 store 已就绪（MySQL 池 4 / Redis 池 4）
游戏服已启动: TCP 0.0.0.0:8088
登录服已启动: HTTP 127.0.0.1:5080, 下发游戏服 <game_host>:8088
服务已全部启动
```

出问题时的处理：

```bash
# 本项目不保留部署备份，恢复手段 = 用目标版本重新打包 + 覆盖部署
systemctl stop skynet
tar -xzf /tmp/skynet-linux.tar.gz -C /opt/aiserver
cd /opt/aiserver/skynet && make clean && make linux -j4 MALLOC_STATICLIB= SKYNET_DEFINES=-DNOUSE_JEMALLOC
systemctl start skynet
```

> 实测 Workbench 的 `upload` 会破坏 `.sh` 内容（`/opt/aiserver` 被存成 `/opt/aiserve`），
> **不要用它上传 shell 脚本**；脚本直接在服务器上执行，或上传后先 `md5sum` / `cat` 核对。

Linux 编译（禁用 jemalloc，免去拉子模块）：

```bash
cd /opt/aiserver/skynet
make clean && make linux -j4 MALLOC_STATICLIB= SKYNET_DEFINES=-DNOUSE_JEMALLOC
```

运维：

```bash
systemctl status skynet
systemctl restart skynet
journalctl -u skynet -f
```

升级：按上面「打包（本地 → ECS）」生成包 → 解压覆盖 → 重新编译 → `systemctl restart skynet`。

数据库结构变更走增量脚本，例如背包：

```bash
mysql < /opt/aiserver/skynet/mygame/sql/migrate-bag.sql
```

> 已有环境**不要**重跑 `init.sql`：它会把被删掉的种子邮件重新插回去（`ON DUPLICATE` 只保护未删除的数据）。

> 注意：直接改数据库**不会**触发缓存失效。手工改完要
> `redis-cli del player:<uid> mails:<uid>`，否则得等 TTL（默认 300s / 60s）。

## 当前限制

- 玩家缓存是**读时回填 + 写时删除**，没有主动预热；冷启动后第一次登录会多一次查库；
- 邮件缓存的 key 是整体列表，单封改动会整表失效（数据量小无所谓，邮件多了要改成按封缓存）；
- `auth_whitelist` 适合测试，正式环境要么用 `sign`，要么接平台校验；
- 传输同时支持 TCP 与 KCP(UDP)，但客户端同一时刻只用其中一种（`GameStart.cs` 里选的 `IChannel`）；
- KCP 不做连接级鉴权之外的防重放：`conv` 随机分配 + 地址绑定，token 仍是唯一身份凭据；
- 单节点 `harbor = 0`；
- MySQL/Redis 都装在本机 `D:\HelloWorld\Server`（`mysql\`、`redis\`），重启电脑后需要重新执行 `start-db.bat`。

## 日志

`skynet.error` 输出到启动终端。重定向到文件：

```powershell
skynet.exe mygame\config > mygame\server.log 2>&1
```

关键日志：`数据层 store 已就绪（MySQL 池 4 / Redis 池 4）`、`登录成功: user=..., auth_mode=...`、
`玩家缓存命中/未命中`、`邮件缓存命中/未命中`、`玩家已更新并清除缓存`、`客户端已断开`。