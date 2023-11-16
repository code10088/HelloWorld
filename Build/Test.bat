@echo off
chcp 65001

echo 1
set path=D:\HelloWorld\Build\%date:~3,4%%date:~8,2%%date:~11,2%_%time:~0,2%%time:~3,2%%time:~6,2%
if %1==1 (echo %path%)