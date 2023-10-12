@echo off

echo 执行git
cd D:\HelloWorld
git checkout %1
git reset --hard %1
git pull

echo 执行打包
"C:/Program Files/Unity2021.3.25/Editor/Unity.exe" ^
-quit ^
-batchmode ^
-projectPath "D:/HelloWorld/HelloWorld" ^
-logFile "D:/HelloWorld/Build/build.log" ^
-executeMethod %2 ^
--path:D:\HelloWorld\Build\HelloWorld.apk ^
--version:%3