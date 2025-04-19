@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

set "PROTOGEN_PATH=.\protogen.exe"
set "PROTO_PATH=..\Proto"
set "OUTCODES_PATH=.\OutCodes"

rmdir /s /q %OUTCODES_PATH%
mkdir %OUTCODES_PATH%

for /r %PROTO_PATH% %%f in (*.proto) do (
	echo [处理] %%f
    %PROTOGEN_PATH% %%~nxf --proto_path=%%~pf --csharp_out=%OUTCODES_PATH%
)

echo [完成]
pause