@echo off

set ADB=%1
set DEVICE_ID=%2
set APK_PATH=%3
set APP_HOST=%4
set SC_FOLDER_NAME=%5

if not exist %ADB% (
    set ADB=adb.exe
)

if not exist %APK_PATH% (
    echo Error: %APK_PATH% not exists
    exit 1
)

echo ADB: %ADB%
echo DEVICE_ID: %DEVICE_ID%
echo APK_PATH: %APK_PATH%
echo APP_HOST: %APP_HOST%
echo SC_FOLDER_NAME: %SC_FOLDER_NAME%

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

set ANDROID_LOCAL_PATH=/sdcard/Android/data/%PACKAGE_NAME%/files/%SC_FOLDER_NAME%/local/main.apk
echo push %APK_PATH% to %ANDROID_LOCAL_PATH%
%ADB% -s %DEVICE_ID% push %APK_PATH% %ANDROID_LOCAL_PATH%
