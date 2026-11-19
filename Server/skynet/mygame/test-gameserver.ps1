$ErrorActionPreference = 'Stop'

# 参数: <gameHost> <gamePort> [loginPort]
$target    = if ($args.Count -ge 2) { $args[0] } else { '127.0.0.1' }
$port      = if ($args.Count -ge 2) { [int]$args[1] } else { 8088 }
$loginPort = if ($args.Count -ge 3) { [int]$args[2] } else { 5080 }

$MysqlExe = 'D:\HelloWorld\Server\mysql\bin\mysql.exe'
$InitSql  = Join-Path $PSScriptRoot 'sql\init.sql'

$pass = 0; $fail = 0

function Check([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { $script:pass++; Write-Host ("  [PASS] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Green }
    else     { $script:fail++; Write-Host ("  [FAIL] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Red }
}

# ---------- 二进制工具 ----------
function B-I32([int]$v) { [BitConverter]::GetBytes([int]$v) }
function B-U32([long]$v) { [BitConverter]::GetBytes([uint32]$v) }
function B-Str([string]$s) {
    $b = [Text.Encoding]::UTF8.GetBytes($s)
    [byte[]](@([BitConverter]::GetBytes([uint16]$b.Length)) + $b)
}
$script:pos = 0
function R-I32([byte[]]$b) { $v = [BitConverter]::ToInt32($b, $script:pos); $script:pos += 4; $v }
function R-U32([byte[]]$b) { $v = [BitConverter]::ToUInt32($b, $script:pos); $script:pos += 4; $v }
function R-I64([byte[]]$b) { $v = [BitConverter]::ToInt64($b, $script:pos); $script:pos += 8; $v }
function R-Str([byte[]]$b) {
    $len = [BitConverter]::ToUInt16($b, $script:pos); $script:pos += 2
    $s = [Text.Encoding]::UTF8.GetString($b, $script:pos, $len); $script:pos += $len; $s
}

function Read-Exact([System.IO.Stream]$s, [int]$n) {
    $buf = New-Object byte[] $n; $off = 0
    while ($off -lt $n) { $r = $s.Read($buf, $off, $n - $off); if ($r -le 0) { throw '连接被关闭' }; $off += $r }
    return $buf
}
function Send-Frame([System.IO.Stream]$s, [int]$id, [byte[]]$payload) {
    if ($null -eq $payload) { $payload = @() }
    $body = [byte[]](@([BitConverter]::GetBytes([uint16]$id)) + $payload)
    $s.Write([BitConverter]::GetBytes([int]$body.Length), 0, 4)
    $s.Write($body, 0, $body.Length); $s.Flush()
}
function Read-Frame([System.IO.Stream]$s) {
    $head = Read-Exact $s 4
    $len = [BitConverter]::ToInt32($head, 0)
    $body = Read-Exact $s $len
    return @{ id = [BitConverter]::ToUInt16($body, 0); body = $body }
}
function Parse-MailList([byte[]]$b) {
    $script:pos = 2
    $count = R-I32 $b
    $mails = @()
    for ($i = 0; $i -lt $count; $i++) {
        $mail = @{}
        $mail.id = R-U32 $b; $mail.type = R-I32 $b; $mail.status = R-U32 $b; $mail.time = R-I64 $b
        $mail.title = R-Str $b; $mail.content = R-Str $b
        $rc = R-I32 $b
        $mail.rewards = @()
        for ($r = 0; $r -lt $rc; $r++) { $mail.rewards += @{ itemId = (R-U32 $b); count = (R-U32 $b) } }
        $mails += $mail
    }
    return ,$mails
}

function Http-Get([string]$path) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $loginPort)
    $s = $c.GetStream(); $s.ReadTimeout = 5000
    $req = "GET $path HTTP/1.1`r`nHost: ${target}:${loginPort}`r`nConnection: close`r`n`r`n"
    $bytes = [Text.Encoding]::UTF8.GetBytes($req)
    $s.Write($bytes, 0, $bytes.Length); $s.Flush()
    $ms = New-Object System.IO.MemoryStream
    $buf = New-Object byte[] 4096
    while (($n = $s.Read($buf, 0, $buf.Length)) -gt 0) { $ms.Write($buf, 0, $n) }
    $c.Close()
    return [Text.Encoding]::UTF8.GetString($ms.ToArray())
}

function Http-Post([string]$path, [string]$body) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $loginPort)
    $s = $c.GetStream(); $s.ReadTimeout = 5000
    $bodyBytes = [Text.Encoding]::UTF8.GetBytes($body)
    $req = "POST $path HTTP/1.1`r`nHost: ${target}:${loginPort}`r`nConnection: close`r`nContent-Type: application/json`r`nContent-Length: $($bodyBytes.Length)`r`n`r`n"
    $bytes = [Text.Encoding]::UTF8.GetBytes($req)
    $s.Write($bytes, 0, $bytes.Length)
    $s.Write($bodyBytes, 0, $bodyBytes.Length); $s.Flush()
    $ms = New-Object System.IO.MemoryStream
    $buf = New-Object byte[] 4096
    while (($n = $s.Read($buf, 0, $buf.Length)) -gt 0) { $ms.Write($buf, 0, $n) }
    $c.Close()
    $raw = [Text.Encoding]::UTF8.GetString($ms.ToArray())
    $idx = $raw.IndexOf("`r`n`r`n")
    if ($idx -ge 0) { return $raw.Substring($idx + 4) } else { return '' }
}

function Reset-Db() {
    # 1) schema + players
    $p = Start-Process -FilePath $MysqlExe -ArgumentList "-u","root","--skip-password","--default-character-set=utf8mb4" `
        -RedirectStandardInput $InitSql -RedirectStandardOutput "$env:TEMP\reset-db.out" -RedirectStandardError "$env:TEMP\reset-db.err" `
        -Wait -PassThru -WindowStyle Hidden
    if ($p.ExitCode -ne 0) { return $false }

    # 2) 邮件状态复位（init.sql 的 ON DUPLICATE 不会把 status 改回 0）
    $sql = @"
USE helloworld;
DELETE FROM mails WHERE user_id='test-user-1';
DELETE FROM mail_rewards WHERE user_id='test-user-1';
INSERT INTO mails (user_id, mail_id, type, status, title, content, create_time) VALUES
 ('test-user-1', 1, 0, 0, '测试邮件1', '欢迎来到游戏', UNIX_TIMESTAMP()),
 ('test-user-1', 2, 0, 0, '测试邮件2', '这是一封测试邮件', UNIX_TIMESTAMP());
INSERT INTO mail_rewards (user_id, mail_id, item_id, item_count) VALUES ('test-user-1', 1, 1, 10);
"@
    $f = "$env:TEMP\reset-mails.sql"
    [System.IO.File]::WriteAllText($f, $sql, (New-Object System.Text.UTF8Encoding($false)))
    $p2 = Start-Process -FilePath $MysqlExe -ArgumentList "-u","root","--skip-password","--default-character-set=utf8mb4" `
        -RedirectStandardInput $f -Wait -PassThru -WindowStyle Hidden

    # 3) 清缓存
    & 'D:\HelloWorld\Server\redis\redis-cli.exe' -h 127.0.0.1 -p 6379 del "mails:test-user-1" "player:test-user-1" | Out-Null

    return $p2.ExitCode -eq 0
}

function Get-Token() {
    $json = Http-Post '/auth/login' '{"platform":0,"authCode":"1","deviceId":"test","clientVersion":"1.2.0"}'
    return ($json | ConvertFrom-Json)
}

Write-Host "`n=== skynet game server test -> ${target}:${port} (login ${loginPort}) ===`n" -ForegroundColor Cyan

# ================= 0. 重置数据库 =================
Write-Host '[0] 重置 MySQL 测试数据'
Check 'init.sql 执行成功' (Reset-Db)

# ================= 1. 登录拿 token =================
Write-Host '[1] POST /auth/login 获取 token'
$auth = Get-Token
Check '拿到 accessToken' ($auth.accessToken -match '^[0-9a-f]{32}$') "token=$($auth.accessToken)"
Check '拿到 refreshToken' ($auth.refreshToken -match '^[0-9a-f]{32}$') "token=$($auth.refreshToken)"
Check 'accessToken 与 refreshToken 不同' ($auth.accessToken -ne $auth.refreshToken)

# ================= 2. 连接游戏服 =================
Write-Host '[2] CSAuth（用登录拿到的 token）'
$conn = New-Object System.Net.Sockets.TcpClient
$conn.NoDelay = $true
$conn.Connect($target, $port)
$stream = $conn.GetStream(); $stream.ReadTimeout = 5000

Send-Frame $stream 1 (B-Str $auth.accessToken)
$f = Read-Frame $stream
Check 'SCAuth id=10001' ($f.id -eq 10001) "id=$($f.id)"
$script:pos = 2
$code = R-I32 $f.body
$srvTime = R-I64 $f.body
Check 'SCAuth code=1' ($code -eq 1) "code=$code"
$nowMs = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
Check 'SCAuth 服务器时间有效' ([math]::Abs($srvTime - $nowMs) -lt 10000) "diff=$($srvTime - $nowMs)ms"

$f = Read-Frame $stream
Check 'SCPlayerData id=10003' ($f.id -eq 10003) "id=$($f.id)"
$script:pos = 2
$playerId = R-U32 $f.body
$nick = R-Str $f.body
$avatar = R-Str $f.body
$level = R-U32 $f.body
$regTime = R-I64 $f.body
$mailFlag = R-U32 $f.body
Check '玩家数据来自 MySQL' ($playerId -eq 10001 -and $nick -eq 'TestPlayer' -and $avatar -eq 'avatar_01' -and $level -eq 1) "playerId=$playerId nick=$nick avatar=$avatar lv=$level"

# ================= 3. 心跳 =================
Write-Host '[3] CSHeart x5'
$heartOk = $true
for ($i = 0; $i -lt 5; $i++) {
    $t0 = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    Send-Frame $stream 2 $null
    $f = Read-Frame $stream
    if ($f.id -ne 10002) { $heartOk = $false }
    $script:pos = 2
    $t = R-I64 $f.body
    if ([math]::Abs($t - $t0) -gt 5000) { $heartOk = $false }
}
Check 'SCHeart 5 次全部正确' $heartOk

# ================= 4. 邮件列表 =================
Write-Host '[4] CSMail'
Send-Frame $stream 100 $null
$f = Read-Frame $stream
Check 'SCMail id=10100' ($f.id -eq 10100) "id=$($f.id)"
$mails = Parse-MailList $f.body
Check '邮件数量=2（MySQL 种子数据）' ($mails.Count -eq 2) "count=$($mails.Count)"
Check '邮件1 奖励 item=1 x10' ($mails[0].rewards.Count -eq 1 -and $mails[0].rewards[0].itemId -eq 1 -and $mails[0].rewards[0].count -eq 10) "rewards=$($mails[0].rewards | ConvertTo-Json -Compress)"
Check '邮件1 标题正确（UTF-8）' ($mails[0].title -eq '测试邮件1') "title=$($mails[0].title)"

# ================= 5. 已读 =================
Write-Host '[5] CSReadMail([1])'
Send-Frame $stream 101 ([byte[]](@(B-I32 1) + (B-U32 1)))
$f = Read-Frame $stream
$mails = Parse-MailList $f.body
Check '邮件1 status=1（已读）' ($mails[0].status -eq 1) "status=$($mails[0].status)"
Check '邮件2 status=0（未读）' ($mails[1].status -eq 0) "status=$($mails[1].status)"

# ================= 6. 领取单封 =================
Write-Host '[6] CSGetMailReward(1)'
Send-Frame $stream 102 (B-U32 1)
$f = Read-Frame $stream
Check 'SCGetMailReward id=10102' ($f.id -eq 10102) "id=$($f.id)"
$script:pos = 2
$code = R-I32 $f.body
$mid = R-U32 $f.body
Check '领取返回 code=0 mailId=1' ($code -eq 0 -and $mid -eq 1) "code=$code mailId=$mid"

# 领取成功后服务端会主动推一次 SCBag
$push = Read-Frame $stream
Check '领取后收到 SCBag 推送' ($push.id -eq 10106) "id=$($push.id)"

Send-Frame $stream 100 $null
$mails = Parse-MailList (Read-Frame $stream).body
Check '邮件1 status=2（已领取）' ($mails[0].status -eq 2) "status=$($mails[0].status)"

# ================= 7. 领取全部 =================
Write-Host '[7] CSGetMailAllReward'
Send-Frame $stream 103 $null
$f = Read-Frame $stream
Check 'SCGetMailAllReward id=10103' ($f.id -eq 10103) "id=$($f.id)"

$push = Read-Frame $stream
Check '一键领取后收到 SCBag 推送' ($push.id -eq 10106) "id=$($push.id)"

Send-Frame $stream 100 $null
$mails = Parse-MailList (Read-Frame $stream).body
Check '全部邮件 status=2' ($mails[0].status -eq 2 -and $mails[1].status -eq 2) "s1=$($mails[0].status) s2=$($mails[1].status)"

# ================= 8. 删除单封 =================
Write-Host '[8] CSDeleteMail(2)'
Send-Frame $stream 104 (B-U32 2)
$f = Read-Frame $stream
Check 'SCDeleteMail id=10104' ($f.id -eq 10104) "id=$($f.id)"
Send-Frame $stream 100 $null
$mails = Parse-MailList (Read-Frame $stream).body
Check '删除后剩 1 封' ($mails.Count -eq 1 -and $mails[0].id -eq 1) "count=$($mails.Count)"

# ================= 9. 删除全部 =================
Write-Host '[9] CSDeleteAllMail'
Send-Frame $stream 105 $null
$f = Read-Frame $stream
Check 'CSDeleteAllMail 返回 SCMail' ($f.id -eq 10100) "id=$($f.id)"
$mails = Parse-MailList $f.body
Check '清空后 0 封' ($mails.Count -eq 0) "count=$($mails.Count)"
$conn.Close()

# ================= 10. 重连：数据持久化 =================
Write-Host '[10] 用同一个 token 重连（验证 MySQL 持久化）'
$conn2 = New-Object System.Net.Sockets.TcpClient
$conn2.NoDelay = $true
$conn2.Connect($target, $port)
$s2 = $conn2.GetStream(); $s2.ReadTimeout = 5000
Send-Frame $s2 1 (B-Str $auth.accessToken)
$f = Read-Frame $s2
$f = Read-Frame $s2
Send-Frame $s2 100 $null
$mails = Parse-MailList (Read-Frame $s2).body
Check '重连后仍是 0 封（删除已写库）' ($mails.Count -eq 0) "count=$($mails.Count)"
$conn2.Close()

# ================= 11. 无效 token =================
Write-Host '[11] 无效 token 回退测试账号'
$conn3 = New-Object System.Net.Sockets.TcpClient
$conn3.NoDelay = $true
$conn3.Connect($target, $port)
$s3 = $conn3.GetStream(); $s3.ReadTimeout = 5000
Send-Frame $s3 1 (B-Str 'invalid-token-xxx')
$f = Read-Frame $s3
Check '无效 token 也能走测试账号' ($f.id -eq 10001) "id=$($f.id)"
$conn3.Close()

# ================= 12. 并发连接（连接池） =================
Write-Host '[12] 5 个连接同时鉴权 + 拉邮件（连接池）'
$clients = @()
for ($i = 0; $i -lt 5; $i++) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $port)
    $st = $c.GetStream(); $st.ReadTimeout = 8000
    Send-Frame $st 1 (B-Str $auth.accessToken)
    $clients += @{ client = $c; stream = $st }
}

$allOk = $true
foreach ($cl in $clients) {
    $f1 = Read-Frame $cl.stream
    $f2 = Read-Frame $cl.stream
    if ($f1.id -ne 10001 -or $f2.id -ne 10003) { $allOk = $false }
}
foreach ($cl in $clients) { Send-Frame $cl.stream 100 $null }
foreach ($cl in $clients) {
    $m = Parse-MailList (Read-Frame $cl.stream).body
    if ($m.Count -ne 0) { $allOk = $false }
}
foreach ($cl in $clients) { $cl.client.Close() }
Check '5 个并发连接全部正常' $allOk

# 恢复种子数据，方便下次手工测试
Check '恢复种子数据' (Reset-Db)

Write-Host ""
Write-Host ("结果: PASS={0}  FAIL={1}" -f $pass, $fail) -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
if ($fail -ne 0) { exit 1 }