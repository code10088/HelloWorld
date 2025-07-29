@echo off
chcp 65001

echo 执行git
cd /d D:/HelloWorld
git checkout %1
git reset --hard %1
git pull

taskkill /f /im Unity.exe

"C:/Program Files/Unity 6000.0.25f1/Editor/Unity.exe" ^
-quit ^
-batchmode ^
-projectPath "D:/HelloWorld/HelloWorld" ^
-logFile - ^
-executeMethod %2 ^
--platform:%3 ^
--appversion:%4 ^
--development:%5 ^
--debug:%6