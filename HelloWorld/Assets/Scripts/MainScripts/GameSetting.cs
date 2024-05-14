using UnityEngine;

public class GameSetting : Singletion<GameSetting>, SingletionInterface
{
    public static float updateTimeSliceS = 1.0f / 60;
    public static int updateTimeSliceMS = 1000 / 60;
    public static int gcTimeIntervalS = 600;//GC间隔
    public static int threadLimit = 6;
    public static int httpLimit = 3;//小于threadLimit
    public static int downloadLimit = 5;//小于threadLimit
    public static int writeLimit = 3;//小于threadLimit
    public static int recycleTimeS = 30;
    public static float recycleTimeMaxS = 120;
    public static int retryTime = 3;
    public static int timeoutS = 10;//秒
    public static string HotUpdateConfigPath = "Assets/ZRes/GameConfig/HotUpdateConfig.txt";

    //cdn
    private string cdn = "https://assets-1321503079.cos.ap-beijing.myqcloud.com";
    private string cdnPlatform;
    private string cdnVersion;
    public string CDNPlatform => cdnPlatform;
    public string CDNVersion => cdnVersion;

    public void Init()
    {
#if UNITY_WEBGL
        cdnPlatform = $"{cdn}/WebGL/";
        cdnVersion = $"{cdn}/WebGL/{Application.version}/";
#elif UNITY_ANDROID
        cdnPlatform = $"{cdn}/Android/";
        cdnVersion = $"{cdn}/Android/{Application.version}/";
#elif UNITY_IOS
        cdnPlatform = $"{cdn}/iOS/";
        cdnVersion = $"{cdn}/iOS/{Application.version}/";
#endif
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