@echo off
setlocal

set DOTNET_ROLL_FORWARD=Major
set NET_DIR=%~dp0
set MSG_ROOT=%~dp0..
set NET_MSG_GEN=%NET_DIR%NetMsgGen\publish\NetMsgGen.exe
set PROTO_DIR=%MSG_ROOT%\Proto
set OUT_DIR=%NET_DIR%Out
set CONFIG=%MSG_ROOT%\Proto\AConfig.txt

"%NET_MSG_GEN%" ^
    --proto "%PROTO_DIR%" ^
    --out "%OUT_DIR%" ^
    --config "%CONFIG%" ^
    --prune ^
    %*

exit /b %ERRORLEVEL%