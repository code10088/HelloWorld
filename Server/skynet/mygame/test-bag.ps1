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

# ---------- 工具 ----------
function Reset-Bag {
    $sql = @"
USE helloworld;
DELETE FROM bag_items WHERE user_id='test-user-1';
DELETE FROM bag_equips WHERE user_id='test-user-1';
DELETE FROM player_flags WHERE user_id='test-user-1';
"@
    $f = "$env:TEMP\bag-reset.sql"
    [System.IO.File]::WriteAllText($f, $sql, (New-Object System.Text.UTF8Encoding($false)))
    $p = Start-Process -FilePath $MysqlExe -ArgumentList "-u","root","--skip-password","--default-character-set=utf8mb4" `
        -RedirectStandardInput $f -Wait -PassThru -WindowStyle Hidden
    & $RedisCli -h 127.0.0.1 -p 6379 del "bag:test-user-1" | Out-Null
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
$script:pos = 0
function R-I32([byte[]]$b) { $v = [BitConverter]::ToInt32($b, $script:pos); $script:pos += 4; $v }
function R-U32([byte[]]$b) { $v = [BitConverter]::ToUInt32($b, $script:pos); $script:pos += 4; $v }
function R-I64([byte[]]$b) { $v = [BitConverter]::ToInt64($b, $script:pos); $script:pos += 8; $v }

function Parse-Bag([byte[]]$b, [int]$startPos = 2) {
    $script:pos = $startPos
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

function Connect-Auth([string]$token) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $gamePort)
    $s = $c.GetStream(); $s.ReadTimeout = 8000
    $tb = [Text.Encoding]::UTF8.GetBytes($token)
    $payload = [byte[]](@([BitConverter]::GetBytes([uint16]$tb.Length)) + $tb)
    Send-Frame $s 1 $payload
    $f1 = Read-Frame $s
    $f2 = Read-Frame $s
    if ($f1.id -ne 10001 -or $f2.id -ne 10003) { throw "auth failed ($($f1.id)/$($f2.id))" }

    # 解析 SCPlayerData：BasePlayerData + registerTime + mail + items + equips
    $script:pos = 2
    R-U32 $f2.body | Out-Null                                  # playerId
    $nl = [BitConverter]::ToUInt16($f2.body, $script:pos); $script:pos += 2 + $nl   # nickName
    $al = [BitConverter]::ToUInt16($f2.body, $script:pos); $script:pos += 2 + $al   # avatar
    R-U32 $f2.body | Out-Null                                  # level
    R-I64 $f2.body | Out-Null                                  # registerTime
    R-U32 $f2.body | Out-Null                                  # mail
    $playerBag = Parse-Bag $f2.body $script:pos

    return @{ client = $c; stream = $s; playerBag = $playerBag }
}

function Get-Bag([hashtable]$conn) {
    Send-Frame $conn.stream 106 $null
    $f = Read-Frame $conn.stream
    if ($f.id -ne 10106) { throw "unexpected id $($f.id)" }
    return Parse-Bag $f.body
}

function Use-Item([hashtable]$conn, [uint32]$itemId, [uint32]$count) {
    Send-Frame $conn.stream 107 ([byte[]](@([BitConverter]::GetBytes($itemId)) + [BitConverter]::GetBytes($count)))
    $f = Read-Frame $conn.stream
    if ($f.id -ne 10107) { throw "unexpected id $($f.id)" }
    $script:pos = 2
    return @{ code = (R-I32 $f.body); itemId = (R-U32 $f.body); count = (R-U32 $f.body) }
}

Write-Host "`n=== 背包功能测试 -> ${target}:${gamePort} ===`n" -ForegroundColor Cyan

Write-Host '[0] 重置背包数据'
Check '重置成功' (Reset-Bag)

Write-Host '[1] 登录并鉴权'
$auth = Http-Post '/auth/login' '{"platform":0,"authCode":"1","deviceId":"bag-test","clientVersion":"1.2.0"}'
Check '拿到 token' ($auth.code -eq 1 -and $auth.accessToken) "code=$($auth.code)"
$conn = Connect-Auth $auth.accessToken

Write-Host '[2] SCPlayerData 自带背包（首次登录发放演示数据）'
$bag = $conn.playerBag
Check 'SCPlayerData 携带背包' ($bag.items.Count -eq 2 -and $bag.equips.Count -eq 2) "items=$($bag.items.Count) equips=$($bag.equips.Count)"
Check '道具 2 种' ($bag.items.Count -eq 2) "count=$($bag.items.Count)"
Check '回血丹(9) x5' ($bag.items[0].itemId -eq 9 -and $bag.items[0].count -eq 5) "item=$($bag.items[0].itemId) count=$($bag.items[0].count)"
Check '经验丹(10) x3' ($bag.items[1].itemId -eq 10 -and $bag.items[1].count -eq 3) "item=$($bag.items[1].itemId) count=$($bag.items[1].count)"
Check '装备 2 件' ($bag.equips.Count -eq 2) "count=$($bag.equips.Count)"
Check '天元笔(1) / 紫晶靴(2)' ($bag.equips[0].equipId -eq 1 -and $bag.equips[1].equipId -eq 2) "e1=$($bag.equips[0].equipId) e2=$($bag.equips[1].equipId)"
Check '装备 uid 有效' ($bag.equips[0].uid -gt 0 -and $bag.equips[1].uid -gt 0) "uid1=$($bag.equips[0].uid) uid2=$($bag.equips[1].uid)"
Check '装备默认 level=1 bind=0' ($bag.equips[0].level -eq 1 -and $bag.equips[0].bind -eq 0) "lv=$($bag.equips[0].level) bind=$($bag.equips[0].bind)"

$bag2 = Get-Bag $conn
Check 'CSBag 与 SCPlayerData 一致' ($bag2.items.Count -eq $bag.items.Count -and $bag2.equips.Count -eq $bag.equips.Count) "items=$($bag2.items.Count) equips=$($bag2.equips.Count)"

Write-Host '[3] 使用回血丹 x2（SCUseItem.count = 剩余数量）'
$r = Use-Item $conn 9 2
Check 'code=1 成功' ($r.code -eq 1) "code=$($r.code)"
Check '剩余 3 个' ($r.itemId -eq 9 -and $r.count -eq 3) "item=$($r.itemId) count=$($r.count)"
$bag = Get-Bag $conn
Check '回血丹剩 3' ($bag.items[0].itemId -eq 9 -and $bag.items[0].count -eq 3) "count=$($bag.items[0].count)"

Write-Host '[4] 超量使用回血丹 x99'
$r = Use-Item $conn 9 99
Check 'code=1 成功' ($r.code -eq 1) "code=$($r.code)"
Check '剩余 0 个' ($r.count -eq 0) "count=$($r.count)"
$bag = Get-Bag $conn
Check '回血丹已用完并移除' ($bag.items.Count -eq 1 -and $bag.items[0].itemId -eq 10) "count=$($bag.items.Count)"

Write-Host '[5] 异常参数'
$r = Use-Item $conn 10 0
Check 'count=0 返回失败' ($r.code -eq 0 -and $r.count -eq 0) "code=$($r.code) count=$($r.count)"
$r = Use-Item $conn 999 1
Check '不存在道具返回失败' ($r.code -eq 0) "code=$($r.code)"

Write-Host '[6] 重连验证持久化 & 不重复发放'
$conn.client.Close()
$conn = Connect-Auth $auth.accessToken
$bag = Get-Bag $conn
Check '重连后经验丹 x3 还在' ($bag.items.Count -eq 1 -and $bag.items[0].itemId -eq 10 -and $bag.items[0].count -eq 3) "items=$($bag.items.Count)"
Check '回血丹没有被重新发放' (-not ($bag.items | Where-Object { $_.itemId -eq 9 })) ""
Check '装备仍是 2 件' ($bag.equips.Count -eq 2) "count=$($bag.equips.Count)"

Write-Host '[7] 用光经验丹后重连'
$r = Use-Item $conn 10 3
Check 'code=1 成功' ($r.code -eq 1) "code=$($r.code)"
$conn.client.Close()
$conn = Connect-Auth $auth.accessToken
$bag = Get-Bag $conn
Check '背包道具为空' ($bag.items.Count -eq 0) "count=$($bag.items.Count)"
Check '装备不受影响' ($bag.equips.Count -eq 2) "count=$($bag.equips.Count)"

$conn.client.Close()

Write-Host ""
Write-Host ("结果: PASS={0}  FAIL={1}" -f $pass, $fail) -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
if ($fail -ne 0) { exit 1 }