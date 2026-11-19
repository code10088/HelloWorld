$ErrorActionPreference = 'Stop'

$target = if ($args.Count -ge 1) { $args[0] } else { '127.0.0.1' }
$gamePort = if ($args.Count -ge 2) { [int]$args[1] } else { 8088 }
$loginPort = if ($args.Count -ge 3) { [int]$args[2] } else { 5080 }

$MysqlExe = 'D:\HelloWorld\Server\mysql\bin\mysql.exe'
$RedisCli = 'D:\HelloWorld\Server\redis\redis-cli.exe'
$pass = 0; $fail = 0

function Check([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { $script:pass++; Write-Host ("  [PASS] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Green }
    else     { $script:fail++; Write-Host ("  [FAIL] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Red }
}

function Reset-All {
    $sql = @"
USE helloworld;
DELETE FROM bag_items WHERE user_id='test-user-1';
DELETE FROM bag_equips WHERE user_id='test-user-1';
DELETE FROM player_flags WHERE user_id='test-user-1';
DELETE FROM mails WHERE user_id='test-user-1';
DELETE FROM mail_rewards WHERE user_id='test-user-1';
INSERT INTO mails (user_id, mail_id, type, status, title, content, create_time) VALUES
 ('test-user-1', 1, 0, 0, '测试邮件1', '欢迎来到游戏', UNIX_TIMESTAMP()),
 ('test-user-1', 2, 0, 0, '测试邮件2', '这是一封测试邮件', UNIX_TIMESTAMP());
INSERT INTO mail_rewards (user_id, mail_id, item_id, item_count) VALUES
 ('test-user-1', 1, 1, 10),
 ('test-user-1', 2, 1, 5);
"@
    $f = "$env:TEMP\mailreward-reset.sql"
    [System.IO.File]::WriteAllText($f, $sql, (New-Object System.Text.UTF8Encoding($false)))
    $p = Start-Process -FilePath $MysqlExe -ArgumentList "-u","root","--skip-password","--default-character-set=utf8mb4" `
        -RedirectStandardInput $f -Wait -PassThru -WindowStyle Hidden
    & $RedisCli -h 127.0.0.1 -p 6379 del "bag:test-user-1" "mails:test-user-1" "player:test-user-1" | Out-Null
    return $p.ExitCode -eq 0
}

function Http-Post([string]$path, [string]$body) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $loginPort)
    $s = $c.GetStream(); $s.ReadTimeout = 5000
    $bb = [Text.Encoding]::UTF8.GetBytes($body)
    $req = "POST $path HTTP/1.1`r`nHost: ${target}:${loginPort}`r`nConnection: close`r`nContent-Type: application/json`r`nContent-Length: $($bb.Length)`r`n`r`n"
    $hb = [Text.Encoding]::UTF8.GetBytes($req)
    $s.Write($hb, 0, $hb.Length); $s.Write($bb, 0, $bb.Length); $s.Flush()
    $ms = New-Object System.IO.MemoryStream
    $buf = New-Object byte[] 4096
    while (($n = $s.Read($buf, 0, $buf.Length)) -gt 0) { $ms.Write($buf, 0, $n) }
    $c.Close()
    $raw = [Text.Encoding]::UTF8.GetString($ms.ToArray())
    $i = $raw.IndexOf("`r`n`r`n")
    return ($(if ($i -ge 0) { $raw.Substring($i + 4) } else { '' }) | ConvertFrom-Json)
}

function Read-Exact([System.IO.Stream]$s, [int]$n) {
    $b = New-Object byte[] $n; $o = 0
    while ($o -lt $n) { $r = $s.Read($b, $o, $n - $o); if ($r -le 0) { throw 'closed' }; $o += $r }
    return $b
}
function Send-Frame([System.IO.Stream]$s, [int]$id, [byte[]]$payload) {
    if ($null -eq $payload) { $payload = @() }
    $b = [byte[]](@([BitConverter]::GetBytes([uint16]$id)) + $payload)
    $s.Write([BitConverter]::GetBytes([int]$b.Length), 0, 4)
    $s.Write($b, 0, $b.Length); $s.Flush()
}
function Read-Frame([System.IO.Stream]$s) {
    $h = Read-Exact $s 4
    $len = [BitConverter]::ToInt32($h, 0)
    $b = Read-Exact $s $len
    return @{ id = [BitConverter]::ToUInt16($b, 0); body = $b }
}
$script:pos = 0
function R-I32([byte[]]$b) { $v = [BitConverter]::ToInt32($b, $script:pos); $script:pos += 4; $v }
function R-U32([byte[]]$b) { $v = [BitConverter]::ToUInt32($b, $script:pos); $script:pos += 4; $v }
function R-I64([byte[]]$b) { $v = [BitConverter]::ToInt64($b, $script:pos); $script:pos += 8; $v }

function Parse-Bag([byte[]]$b) {
    $script:pos = 2
    $items = @()
    $ic = R-I32 $b
    for ($i = 0; $i -lt $ic; $i++) { $items += @{ itemId = (R-U32 $b); count = (R-U32 $b) } }
    $equips = @()
    $ec = R-I32 $b
    for ($i = 0; $i -lt $ec; $i++) {
        $equips += @{ uid = (R-I64 $b); equipId = (R-U32 $b); level = (R-U32 $b); bind = (R-U32 $b) }
    }
    return @{ items = $items; equips = $equips }
}
function Parse-MailList([byte[]]$b) {
    $script:pos = 2
    $count = R-I32 $b
    $mails = @()
    for ($i = 0; $i -lt $count; $i++) {
        $m = @{}
        $m.id = R-U32 $b; $m.type = R-I32 $b; $m.status = R-U32 $b; R-I64 $b | Out-Null
        $tl = [BitConverter]::ToUInt16($b, $script:pos); $script:pos += 2
        $m.title = [Text.Encoding]::UTF8.GetString($b, $script:pos, $tl); $script:pos += $tl
        $cl = [BitConverter]::ToUInt16($b, $script:pos); $script:pos += 2
        $script:pos += $cl
        $rc = R-I32 $b
        for ($r = 0; $r -lt $rc; $r++) { R-U32 $b | Out-Null; R-U32 $b | Out-Null }
        $script:pos = $script:pos
        $mails += $m
    }
    return ,$mails
}

function Connect-Auth([string]$token) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $gamePort)
    $s = $c.GetStream(); $s.ReadTimeout = 8000
    $tb = [Text.Encoding]::UTF8.GetBytes($token)
    Send-Frame $s 1 ([byte[]](@([BitConverter]::GetBytes([uint16]$tb.Length)) + $tb))
    $f1 = Read-Frame $s
    $f2 = Read-Frame $s
    if ($f1.id -ne 10001 -or $f2.id -ne 10003) { throw "auth failed ($($f1.id)/$($f2.id))" }
    return @{ client = $c; stream = $s }
}

function Get-Bag([hashtable]$conn) {
    Send-Frame $conn.stream 106 $null
    $f = Read-Frame $conn.stream
    if ($f.id -ne 10106) { throw "unexpected id $($f.id)" }
    return Parse-Bag $f.body
}
function Read-BagPush([hashtable]$conn) {
    $f = Read-Frame $conn.stream
    if ($f.id -ne 10106) { throw "expected SCBag push, got $($f.id)" }
    return Parse-Bag $f.body
}
function ItemCount($bag, [uint32]$itemId) {
    $sum = 0
    foreach ($it in $bag.items) { if ($it.itemId -eq $itemId) { $sum += $it.count } }
    return $sum
}
function ItemStacks($bag, [uint32]$itemId) {
    return @($bag.items | Where-Object { $_.itemId -eq $itemId }).Count
}

Write-Host "`n=== 邮件奖励入包测试 -> ${target}:${gamePort} ===`n" -ForegroundColor Cyan

Write-Host '[0] 重置数据（邮件 + 背包）'
Check '重置成功' (Reset-All)

Write-Host '[1] 登录 + 鉴权'
$auth = Http-Post '/auth/login' '{"platform":0,"authCode":"1","deviceId":"mailreward","clientVersion":"1.2.0"}'
Check '拿到 token' ($auth.code -eq 1 -and $auth.accessToken) "code=$($auth.code)"
$conn = Connect-Auth $auth.accessToken

Write-Host '[2] 初始背包（演示数据）'
$bag = Get-Bag $conn
Check '回血丹 5 + 经验丹 3 + 装备 2' ($bag.items.Count -eq 2 -and $bag.equips.Count -eq 2) "items=$($bag.items.Count) equips=$($bag.equips.Count)"
Check '还没有道具 1（邮件奖励）' ((ItemCount $bag 1) -eq 0) "item1=$(ItemCount $bag 1)"

Write-Host '[3] 领取邮件 1（奖励 道具1 x10）'
Send-Frame $conn.stream 102 ([BitConverter]::GetBytes([uint32]1))
$f = Read-Frame $conn.stream
Check 'SCGetMailReward id=10102' ($f.id -eq 10102) "id=$($f.id)"
$script:pos = 2
$code = R-I32 $f.body; $mid = R-U32 $f.body
Check '领取成功 code=0' ($code -eq 0 -and $mid -eq 1) "code=$code mailId=$mid"

$bag = Read-BagPush $conn
Check '收到 SCBag 推送' $true
Check '道具1 到账 x10' ((ItemCount $bag 1) -eq 10) "item1=$(ItemCount $bag 1)"
Check '原有道具不受影响' ((ItemCount $bag 9) -eq 5 -and (ItemCount $bag 10) -eq 3) "9=$(ItemCount $bag 9) 10=$(ItemCount $bag 10)"

Write-Host '[4] 重复领取同一封（不应重复发奖）'
Send-Frame $conn.stream 102 ([BitConverter]::GetBytes([uint32]1))
$f = Read-Frame $conn.stream
$script:pos = 2
R-I32 $f.body | Out-Null; R-U32 $f.body | Out-Null
$bag = Read-BagPush $conn
Check '道具1 仍是 x10' ((ItemCount $bag 1) -eq 10) "item1=$(ItemCount $bag 1)"

Write-Host '[5] 领取邮件 2（奖励 道具1 x5，应合并）'
Send-Frame $conn.stream 102 ([BitConverter]::GetBytes([uint32]2))
$f = Read-Frame $conn.stream
$script:pos = 2
R-I32 $f.body | Out-Null; R-U32 $f.body | Out-Null
$bag = Read-BagPush $conn
Check '道具1 合并为 x15' ((ItemCount $bag 1) -eq 15) "item1=$(ItemCount $bag 1)"
Check '只有 1 个堆叠' ((ItemStacks $bag 1) -eq 1) "stacks=$(ItemStacks $bag 1)"

Write-Host '[6] 一键领取（此时已无可领）'
Send-Frame $conn.stream 103 $null
$f = Read-Frame $conn.stream
Check 'SCGetMailAllReward id=10103' ($f.id -eq 10103) "id=$($f.id)"
$bag = Read-BagPush $conn
Check '道具1 仍为 x15' ((ItemCount $bag 1) -eq 15) "item1=$(ItemCount $bag 1)"

Write-Host '[7] 邮件状态已置为已领取'
Send-Frame $conn.stream 100 $null
$mails = Parse-MailList (Read-Frame $conn.stream).body
Check '两封邮件 status=2' ($mails[0].status -eq 2 -and $mails[1].status -eq 2) "s1=$($mails[0].status) s2=$($mails[1].status)"

Write-Host '[8] 重连验证持久化'
$conn.client.Close()
$conn = Connect-Auth $auth.accessToken
$bag = Get-Bag $conn
Check '道具1 x15 仍在' ((ItemCount $bag 1) -eq 15) "item1=$(ItemCount $bag 1)"
Check '道具/装备数量正确' ($bag.items.Count -eq 3 -and $bag.equips.Count -eq 2) "items=$($bag.items.Count) equips=$($bag.equips.Count)"
$conn.client.Close()

Write-Host ""
Write-Host ("结果: PASS={0}  FAIL={1}" -f $pass, $fail) -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
if ($fail -ne 0) { exit 1 }