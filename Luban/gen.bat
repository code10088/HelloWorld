set LUBAN_DLL=Tools\Luban\Luban.dll

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin  ^
    --conf luban.conf ^
    -x outputCodeDir=Client/OutCodes ^
    -x outputDataDir=Client/OutBytes ^
 
pause