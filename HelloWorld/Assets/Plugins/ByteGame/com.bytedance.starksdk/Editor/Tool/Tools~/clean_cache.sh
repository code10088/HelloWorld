

ADB=$1
DEVICE_ID=$2
APP_ID=$3
CLEAN_TYPE=$4
APP_HOST=$5
SC_FOLDER_NAME=$6

if [ ! -f $ADB ]; then
    ADB = "adb"
fi

echo "ADB:" $ADB
echo "DEVICE_ID:" $DEVICE_ID
echo "APP_ID:" $APP_ID
echo "APP_HOST:" $APP_HOST
echo "CLEAN_TYPE:" $CLEAN_TYPE

if [ ! command -v $ADB &> /dev/null ]; then
    echo "Error: adb is not installed! \$ADB: $ADB" >&2
    exit 1
fi

if [ "$APP_HOST" = "tt" ]; then
    PACKAGE_NAME=com.ss.android.article.news
elif [ "$APP_HOST" = "tt_lite" ]; then
    PACKAGE_NAME=com.ss.android.article.lite
elif [ "$APP_HOST" = "douyin" ]; then
    PACKAGE_NAME=com.ss.android.ugc.aweme
elif [ "$APP_HOST" = "douyin_lite" ]; then
    PACKAGE_NAME=com.ss.android.ugc.aweme.lite
elif [ "$APP_HOST" = "dongchedi" ]; then
    PACKAGE_NAME=com.ss.android.auto
elif [ "$APP_HOST" = "momoyu" ]; then
    PACKAGE_NAME=com.playgame.havefun
else
    echo "Error: invalid app_host:" $APP_HOST
    exit 1
fi

if [ "$CLEAN_TYPE" = "preview" ]; then
  SUB_DIR=_ucfiles/preview
elif [ "$CLEAN_TYPE" = "latest" ]; then
  SUB_DIR=_ucfiles/latest
elif [ "$CLEAN_TYPE" = "all" ]; then
  SUB_DIR=_ucfiles
else
    echo "Error: invalid clean_type:" $CLEAN_TYPE
    exit 1
fi

$ADB -s $DEVICE_ID shell am force-stop ${PACKAGE_NAME}
sleep 1
$ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/local
$ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/Unity/local

if [ "$CLEAN_TYPE" = "preview" ]; then
  $ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/preview
  $ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/Unity/preview
elif [ "$CLEAN_TYPE" = "latest" ]; then
  $ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/latest
  $ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}/Unity/latest
elif [ "$CLEAN_TYPE" = "all" ]; then
  $ADB -s $DEVICE_ID shell rm -rf /sdcard/Android/data/${PACKAGE_NAME}/files/${SC_FOLDER_NAME}
fi
echo "clean cache done."
