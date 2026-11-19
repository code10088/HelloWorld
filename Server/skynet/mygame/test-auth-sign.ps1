$ErrorActionPreference = 'Stop'
$target = '127.0.0.1'; $port = 5080; $pass = 0; $fail = 0

function Check([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { $script:pass++; Write-Host ("  [PASS] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Green }
    else     { $script:fail++; Write-Host ("  [FAIL] {0}{1}" -f $name, $(if ($detail) { "  ($detail)" } else { '' })) -ForegroundColor Red }
}

function Post-Login([string]$authCode) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.NoDelay = $true
    $c.Connect($target, $port)
    $s = $c.GetStream(); $s.ReadTimeout = 5000
    $body = '{"platform":0,"authCode":"' + $authCode + '","deviceId":"t","clientVersion":"1.2.0"}'
    $bb = [Text.Encoding]::UTF8.GetBytes($body)
    $req = "POST /auth/login HTTP/1.1`r`nHost: ${target}:${port}`r`nConnection: close`r`nContent-Type: application/json`r`nContent-Length: $($bb.Length)`r`n`r`n"
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

function Md5Hex([string]$s) {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $h = $md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($s))
        return (($h | ForEach-Object { $_.ToString('x2') }) -join '')
    } finally { $md5.Dispose() }
}

$secret = 'dev-secret-change-me'
$uid = 'test-user-1'

Write-Host "`n=== sign 模式 authCode 校验 ===`n" -ForegroundColor Cyan

# 1. 合法签名
$expire = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + 300
$code = "$uid.$expire." + (Md5Hex "$uid.$expire.$secret")
$r = Post-Login $code
Check '合法签名 code=1' ($r.code -eq 1) "code=$($r.code)"
Check 'userId 来自 authCode' ($r.userId -eq $uid) "userId=$($r.userId)"
Check '下发了 token' ($r.accessToken -match '^[0-9a-f]{32}$') "token=$($r.accessToken)"

# 2. 错误签名
$badCode = "$uid.$expire." + (Md5Hex "$uid.$expire.wrong-secret")
$r = Post-Login $badCode
Check '错误签名 code=0' ($r.code -eq 0) "code=$($r.code)"

# 3. 过期
$oldExpire = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() - 10
$oldCode = "$uid.$oldExpire." + (Md5Hex "$uid.$oldExpire.$secret")
$r = Post-Login $oldCode
Check '过期 authCode code=0' ($r.code -eq 0) "code=$($r.code)"

# 4. 格式错误
$r = Post-Login 'not-a-valid-code'
Check '格式错误 code=0' ($r.code -eq 0) "code=$($r.code)"

# 5. 另一个 userId
$uid2 = 'user-2'
$expire2 = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + 300
$code2 = "$uid2.$expire2." + (Md5Hex "$uid2.$expire2.$secret")
$r = Post-Login $code2
Check '签名里的 userId 生效' ($r.code -eq 1 -and $r.userId -eq 'user-2') "code=$($r.code) userId=$($r.userId)"

Write-Host ""
Write-Host ("结果: PASS={0}  FAIL={1}" -f $pass, $fail) -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
if ($fail -ne 0) { exit 1 }