@echo off

set ADB=%1
set DEVICE_ID=%2
set APP_ID=%3
set RUNTIME_ENV=%4
set APP_HOST=%5

if "%APP_HOST%" == "" (
    echo "Usage: %0 adb_path device_id app_id runtime_env app_host(tt, douyin, douyin_lite)"
    exit 1
)

if not exist %ADB% (
    set ADB=adb.exe
)

echo ADB: %ADB%
echo DEVICE_ID: %DEVICE_ID%
echo APP_ID: %APP_ID%
echo APP_HOST: %APP_HOST%
echo RUNTIME_ENV: %RUNTIME_ENV%

if "%APP_HOST%" == "tt" (
    echo "launch toutiao uc game"
    %ADB% -s %DEVICE_ID% shell am force-stop com.ss.android.article.news
    timeout 1 > nul
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "snssdk143://microgame?app_id=%APP_ID%'&'version=v2'&'scene=011004'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=b27744e"
)else if "%APP_HOST%" == "tt_lite" (
    echo "launch toutiao lite uc game"
    %ADB% -s %DEVICE_ID% shell am force-stop com.ss.android.article.lite
    timeout 1 > nul
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "snssdk35://microgame?app_id=%APP_ID%'&'version=v2'&'scene=011004'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=b27744e"
) else if "%APP_HOST%" == "douyin" (
    echo "launch douyin uc game"
    %ADB% -s %DEVICE_ID% shell am force-stop com.ss.android.ugc.aweme
    timeout 1 > nul
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "snssdk1128://microgame?app_id=%APP_ID%'&'version=v2'&'scene=011004'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=b27744e"
) else if "%APP_HOST%" == "douyin_lite" (
    echo "launch douyin_lite uc game"
    %ADB% -s %DEVICE_ID% shell am force-stop com.ss.android.ugc.aweme.lite
    timeout 1 > nul
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "snssdk2329://microgame?app_id=%APP_ID%'&'version=v2'&'scene=011004'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=b27744e"
) else if "%APP_HOST%" == "dongchedi" (
    echo "launch dongchedi uc game"
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "snssdk36://microgame?app_id=%APP_ID%'&'version=v2'&'scene=011004'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=b27744e"
) else if "%APP_HOST%" == "momoyu" (
    echo "launch momoyu uc game"
    %ADB% -s %DEVICE_ID% shell am force-stop com.playgame.havefun
    timeout 1 > nul
    %ADB% -s %DEVICE_ID% shell am start -a android.intent.action.VIEW -d "sslocal://microgame?version=v2'&'app_id=%APP_ID%'&'scene=011007'&'version_type=%RUNTIME_ENV%'&'bdp_log=%%7B%%22launch_from%%22%%3A%%22stark_sdk_tools%%22%%7D'&'tech_type=7'&'bdpsum=5a29d84"
) else (
    echo Error: invalid app_host: %APP_HOST%
    exit 1
)

echo launch game completed
