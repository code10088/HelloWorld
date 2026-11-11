# Verify the game server TCP 8088 from Windows.
# Usage:
#   powershell -ExecutionPolicy Bypass -File verify-tcp.ps1 112.126.84.17 8088
param(
    [string]$HostName = '112.126.84.17',
    [int]$Port = 8088
)

$token = [Text.Encoding]::UTF8.GetBytes('test-access-token')
$body = New-Object System.Collections.Generic.List[byte]
$body.Add(1); $body.Add(0)                                      # id = 1 (CSAuth)
$body.Add([byte]($token.Length -band 0xFF))                     # token length, uint16 LE
$body.Add([byte](($token.Length -shr 8) -band 0xFF))
$body.AddRange($token)

$tcp = [Net.Sockets.TcpClient]::new($HostName, $Port)
$tcp.ReceiveTimeout = 5000
$ns = $tcp.GetStream()
$lenBytes = [BitConverter]::GetBytes([int]$body.Count)          # bodyLength, int32 LE
$ns.Write($lenBytes, 0, 4)
$ns.Write($body.ToArray(), 0, $body.Count)

$buf = New-Object byte[] 4096
$n = $ns.Read($buf, 0, $buf.Length)
$tcp.Close()

if ($n -le 0) {
    Write-Host 'No data received' -ForegroundColor Red
    exit 1
}

Write-Host ("Received {0} bytes:" -f $n)
Write-Host (($buf[0..($n - 1)] | ForEach-Object { $_.ToString('X2') }) -join ' ')

# First frame should be: 0E 00 00 00 | 11 27 (10001) | 01 00 00 00 (code=1)
if ($buf[4] -eq 0x11 -and $buf[5] -eq 0x27 -and $buf[6] -eq 0x01) {
    Write-Host 'OK: SCAuth(10001) code=1' -ForegroundColor Green
} else {
    Write-Host 'Unexpected frame; check TCP server and port' -ForegroundColor Yellow
    exit 1
}