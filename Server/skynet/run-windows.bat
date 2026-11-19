@echo off
rem ---------------------------------------------------------------
rem  skynet on Windows launcher
rem  NOTE: the config path MUST use a backslash on Windows,
rem        because Lua's package.config separator is "\".
rem        "examples/config" will fail to resolve config.path.
rem ---------------------------------------------------------------
cd /d %~dp0
skynet.exe examples\config
pause