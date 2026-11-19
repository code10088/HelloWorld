$ErrorActionPreference = 'Stop'

$target = '127.0.0.1'
$port = 5080
$pass = 0; $fail = 0

function Check([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { $script:pass++; Write-Host ("  [PASS] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Green }
    else     { $script:fail++; Write-Host ("  [FAIL] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Red }
}

function Http-Request([string]$method, [string]$path, [string]$body) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $port)
    $s = $c.GetStream(); $s.ReadTimeout = 5000

    $utf8 = [Text.Encoding]::UTF8
    $bodyBytes = if ($body) { $utf8.GetBytes($body) } else { @() }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("$method $path HTTP/1.1`r`n")
    [void]$sb.Append("Host: ${target}:${port}`r`n")
    [void]$sb.Append("Connection: close`r`n")
    if ($body) {
        [void]$sb.Append("Content-Type: application/json`r`n")
        [void]$sb.Append("Content-Length: $($bodyBytes.Length)`r`n")
    }
    [void]$sb.Append("`r`n")

    $head = $utf8.GetBytes($sb.ToString())
    $s.Write($head, 0, $head.Length)
    if ($bodyBytes.Length -gt 0) { $s.Write($bodyBytes, 0, $bodyBytes.Length) }
    $s.Flush()

    $ms = New-Object System.IO.MemoryStream
    $buf = New-Object byte[] 4096
    while (($n = $s.Read($buf, 0, $buf.Length)) -gt 0) { $ms.Write($buf, 0, $n) }
    $c.Close()

    $raw = $utf8.GetString($ms.ToArray())
    $split = $raw.IndexOf("`r`n`r`n")
    $headText = if ($split -ge 0) { $raw.Substring(0, $split) } else { $raw }
    $bodyText = if ($split -ge 0) { $raw.Substring($split + 4) } else { '' }

    $lines = $headText -split "`r`n"
    $status = 0
    if ($lines[0] -match '^HTTP/\d\.\d\s+(\d+)') { $status = [int]$Matches[1] }
    $headers = @{}
    for ($i = 1; $i -lt $lines.Count; $i++) {
        $idx = $lines[$i].IndexOf(':')
        if ($idx -gt 0) { $headers[$lines[$i].Substring(0, $idx).Trim().ToLower()] = $lines[$i].Substring($idx + 1).Trim() }
    }
    return @{ status = $status; headers = $headers; body = $bodyText }
}

function Login([string]$authCode) {
    $body = '{"platform":0,"authCode":"' + $authCode + '","deviceId":"test-device","clientVersion":"1.2.0"}'
    $r = Http-Request 'POST' '/auth/login' $body
    return ($r.body | ConvertFrom-Json)
}

Write-Host "`n=== skynet login server test -> ${target}:${port} ===`n" -ForegroundColor Cyan

# 1. health
Write-Host '[1] GET /health'
$r = Http-Request 'GET' '/health' $null
Check 'status=200' ($r.status -eq 200) "status=$($r.status)"
Check 'body=ok' ($r.body -eq 'ok') "body=$($r.body)"

# 2. 合法 authCode（白名单 "1"）
Write-Host '[2] POST /auth/login（authCode=1，白名单内）'
$r = Http-Request 'POST' '/auth/login' '{"platform":0,"authCode":"1","deviceId":"test-device","clientVersion":"1.2.0"}'
Check 'status=200' ($r.status -eq 200) "status=$($r.status)"
Check 'content-type=application/json' ($r.headers['content-type'] -like 'application/json*') "ct=$($r.headers['content-type'])"
$bodyLen = [Text.Encoding]::UTF8.GetByteCount($r.body)
Check 'content-length 与实际一致' ([int]$r.headers['content-length'] -eq $bodyLen) "header=$($r.headers['content-length']) actual=$bodyLen"

$auth = $r.body | ConvertFrom-Json
Check 'code=1' ($auth.code -eq 1) "code=$($auth.code)"
Check 'accessToken 是 32 位 hex' ($auth.accessToken -match '^[0-9a-f]{32}$') "token=$($auth.accessToken)"
Check 'refreshToken 是 32 位 hex' ($auth.refreshToken -match '^[0-9a-f]{32}$') "token=$($auth.refreshToken)"
Check 'userId 正确' ($auth.userId -eq 'test-user-1') "userId=$($auth.userId)"
Check 'host/port 正确' ($auth.host -eq '127.0.0.1' -and $auth.port -is [string] -and $auth.port -eq '8088') "host=$($auth.host) port=$($auth.port)"

# 3. 非法 authCode
Write-Host '[3] POST /auth/login（authCode 不在白名单）'
$bad = Login 'not-allowed'
Check 'code=0（拒绝登录）' ($bad.code -eq 0) "code=$($bad.code)"
Check '没有下发 token' (-not $bad.accessToken) "accessToken=$($bad.accessToken)"
Check '带错误信息' ([bool]$bad.message) "message=$($bad.message)"

# 4. 空 authCode
Write-Host '[4] POST /auth/login（authCode 为空）'
$empty = Login ''
Check 'code=0（拒绝登录）' ($empty.code -eq 0) "code=$($empty.code)"

# 5. 两次登录 token 不同
Write-Host '[5] 再登录一次，token 应不同'
$auth2 = Login '1'
Check '两次登录 accessToken 不同' ($auth.accessToken -ne $auth2.accessToken)
Check 'userId 一致' ($auth.userId -eq $auth2.userId)

# 6. refresh
Write-Host '[6] POST /auth/refresh（有效 refreshToken）'
$r = Http-Request 'POST' '/auth/refresh' ('{"refreshToken":"' + $auth.refreshToken + '"}')
$j3 = $r.body | ConvertFrom-Json
Check 'status=200' ($r.status -eq 200) "status=$($r.status)"
Check 'code=1' ($j3.code -eq 1) "code=$($j3.code)"
Check '换发了新的 accessToken' ($j3.accessToken -match '^[0-9a-f]{32}$' -and $j3.accessToken -ne $auth.accessToken) "new=$($j3.accessToken)"
Check '换发了新的 refreshToken' ($j3.refreshToken -match '^[0-9a-f]{32}$' -and $j3.refreshToken -ne $auth.refreshToken) "new=$($j3.refreshToken)"
Check 'refresh 后 userId 保持' ($j3.userId -eq $auth.userId) "userId=$($j3.userId)"

# 7. refresh token 只能用一次
Write-Host '[7] 复用旧的 refreshToken（应失败）'
$r = Http-Request 'POST' '/auth/refresh' ('{"refreshToken":"' + $auth.refreshToken + '"}')
$j4 = $r.body | ConvertFrom-Json
Check '旧 refreshToken 复用返回 code=0' ($j4.code -eq 0) "code=$($j4.code)"

# 8. 方法错误
Write-Host '[8] GET /auth/login（应 405）'
$r = Http-Request 'GET' '/auth/login' $null
Check 'status=405' ($r.status -eq 405) "status=$($r.status)"

# 9. 404
Write-Host '[9] GET /nope（应 404）'
$r = Http-Request 'GET' '/nope' $null
Check 'status=404' ($r.status -eq 404) "status=$($r.status)"

# 10. 连续请求
Write-Host '[10] 连续 20 次 /health'
$okCount = 0
for ($i = 0; $i -lt 20; $i++) {
    $r = Http-Request 'GET' '/health' $null
    if ($r.status -eq 200 -and $r.body -eq 'ok') { $okCount++ }
}
Check '20/20 成功' ($okCount -eq 20) "ok=$okCount"

Write-Host ""
Write-Host ("结果: PASS={0}  FAIL={1}" -f $pass, $fail) -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
if ($fail -ne 0) { exit 1 }