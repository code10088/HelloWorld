using UnityEngine;

public static class GameSetting
{
    public static float updateTimeSliceS = 1.0f / 60;
    public static int updateTimeSliceMS = 1000 / 60;
    public static int gcTimeIntervalS = 600;//GC间隔
    public static int threadLimit = 6;
    public static int httpLimit = 3;//小于threadLimit
    public static int downloadLimit = 5;//小于threadLimit
    public static int writeLimit = 3;//小于threadLimit
    public static int recycleTimeS = 30;
    public static float recycleTimeMinS = 10;
    public static float recycleTimeMaxS = 120;
    public static int retryTime = 3;
    public static int timeoutS = 10;//秒
    public static string HotUpdateConfigPath = "Assets/ZRes/GameConfig/HotUpdateConfig.txt";

    private static string CDN
    {
        get
        {
#if DEBUG
            return "https://assets-1321503079.cos.ap-beijing.myqcloud.com";
#else
            return "https://assets-1321503079.cos.ap-beijing.myqcloud.com";
#endif
        }
    }
    public static string CDNPlatform
    {
        get
        {
#if UNITY_WEBGL
            return $"{CDN}/WebGL";
#elif UNITY_ANDROID
            return $"{CDN}/Android";
#elif UNITY_IOS
            return $"{CDN}/iOS";
#endif
        }
    }
    public static string CDNVersion
    {
        get
        {
#if UNITY_WEBGL
            return $"{CDNPlatform}/{Application.version}/";
#elif UNITY_ANDROID
            return $"{CDNPlatform}/{Application.version}/";
#elif UNITY_IOS
            return $"{CDNPlatform}/{Application.version}/";
#endif
        }
    }

    public static string AppName =>
#if UNITY_ANDROID
        "HelloWorld.apk";
#elif UNITY_IOS
        "HelloWorld.app";
#else
        string.Empty;
#endif
}