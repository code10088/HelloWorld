@echo off
setlocal
"%~dp0NetMsgGen\publish\NetMsgGen.exe" --proto "%~dp0..\Proto" --out "%~dp0Out" --config "%~dp0..\Proto\AConfig.txt" %*
exit /b %ERRORLEVEL%