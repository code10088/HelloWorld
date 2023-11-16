@echo off
chcp 65001

echo 执行git
cd /d D:/HelloWorld
git checkout %1
git reset --hard %1
git pull

taskkill /f /im Unity.exe

set path=D:\HelloWorld\Build\%date:~3,4%%date:~8,2%%date:~11,2%_%time:~0,2%%time:~3,2%%time:~6,2%
if %2==BuildEditor.BuildPlayer (md %path%)

"C:/Program Files/Unity2021.3.25/Editor/Unity.exe" ^
-quit ^
-batchmode ^
-projectPath "D:/HelloWorld/HelloWorld" ^
-logFile "D:/HelloWorld/Build/build.log" ^
-executeMethod %2 ^
--path:%path%\HelloWorld.apk ^
--version:%3 ^
--develop:%4