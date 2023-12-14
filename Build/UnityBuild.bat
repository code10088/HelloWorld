@echo off
chcp 65001

echo 执行git
cd /d D:/HelloWorld
git checkout %1
git reset --hard %1
git pull

taskkill /f /im Unity.exe

"C:/Program Files/Unity2021.3.25/Editor/Unity.exe" ^
-quit ^
-batchmode ^
-projectPath "D:/HelloWorld/HelloWorld" ^
-logFile "D:/HelloWorld/Build/build.log" ^
-executeMethod %2 ^
--version:%3 ^
--release:%4