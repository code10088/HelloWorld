@echo off

set ADB=%1
set DEVICE_ID=%2
set APP_ID=%3
set CLEAN_TYPE=%4
set APP_HOST=%5
set SC_FOLDER_NAME=%6


if not exist %ADB% (
    set ADB=adb.exe
)

echo ADB: %ADB%
echo DEVICE_ID: %DEVICE_ID%
echo APP_ID: %APP_ID%
echo APP_HOST: %APP_HOST%
echo CLEAN_TYPE: %CLEAN_TYPE%

if "%APP_HOST%" == "tt" (
    set PACKAGE_NAME=com.ss.android.article.news
) else if "%APP_HOST%" == "tt_lite" (
    set PACKAGE_NAME=com.ss.android.article.lite
) else if "%APP_HOST%" == "douyin" (
    set PACKAGE_NAME=com.ss.android.ugc.aweme
) else if "%APP_HOST%" == "douyin_lite" (
    set PACKAGE_NAME=com.ss.android.ugc.aweme.lite
) else if "%APP_HOST%" == "dongchedi" (
    set PACKAGE_NAME=com.ss.android.auto
) else if "%APP_HOST%" == "momoyu" (
    set PACKAGE_NAME=com.playgame.havefun
) else (
    echo Error: invalid app_host: %APP_HOST%
    exit 1
)
%ADB% -s %DEVICE_ID% shell am force-stop %PACKAGE_NAME%
timeout 1 > nul
if "%CLEAN_TYPE%" == "preview" (
    %ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/preview
    %ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/Unity/preview
) else if "%CLEAN_TYPE%" == "latest" (
    %ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/latest
    %ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/Unity/latest
) else if "%CLEAN_TYPE%" == "all" (
    %ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%
) else (
    echo Error: invalid clean_type: %CLEAN_TYPE%
    exit 1
)

%ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/local
%ADB% -s %DEVICE_ID% shell rm -rf /sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/Unity/local
echo clean cache done.
