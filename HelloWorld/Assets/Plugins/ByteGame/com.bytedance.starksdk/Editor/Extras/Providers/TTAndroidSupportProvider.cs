#if UNITY_ANDROID
#define TT_ANDROID_BUILD_SUPPORTED
#endif

using UnityEngine;
#if TT_ANDROID_BUILD_SUPPORTED
using UnityEditor.Android;
#endif

namespace TTSDK.Tool
{
    public class TTAndroidSupportProvider : ITTAndroidSupportProvider
    {
        private const string NotSupportedTips = "小游戏 Native 方案构建需安装 Android Build Support 并切换至对应 Platform。";
        
        public string sdkRootPath
        {
            get
            {
#if TT_ANDROID_BUILD_SUPPORTED
                return AndroidExternalToolsSettings.sdkRootPath;
#else
                Debug.LogError(NotSupportedTips);
                return string.Empty;
#endif
            }
        }
        
        public string ndkRootPath
        {
            get
            {
#if TT_ANDROID_BUILD_SUPPORTED
                return AndroidExternalToolsSettings.ndkRootPath;
#else
                Debug.LogError(NotSupportedTips);
                return string.Empty;
#endif
            }
        }
        
        public string jdkRootPath
        {
            get
            {
#if TT_ANDROID_BUILD_SUPPORTED
                return AndroidExternalToolsSettings.jdkRootPath;
#else
                Debug.LogError(NotSupportedTips);
                return string.Empty;
#endif
            }
        }
        
        public string gradlePath
        {
            get
            {
#if TT_ANDROID_BUILD_SUPPORTED
                return AndroidExternalToolsSettings.gradlePath;
#else
                Debug.LogError(NotSupportedTips);
                return string.Empty;
#endif
            }
        }

    }
}