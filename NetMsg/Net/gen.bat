@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

set "PROTOGEN_PATH=D:\HelloWorld\NetMsg\Net\protogen.exe"
set "PROTO_PATH=D:\HelloWorld\NetMsg\Proto"

set count=0
for /r %PROTO_PATH% %%f in (*.proto) do (
	echo [处理] %%f
    %PROTOGEN_PATH% %%~nxf --proto_path=%%~pf --csharp_out=OutCodes
)

echo [完成]
pause