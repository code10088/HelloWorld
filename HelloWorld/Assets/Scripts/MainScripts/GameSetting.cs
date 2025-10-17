using UnityEngine;
using UnityEngine.Rendering;

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

    private static string CDN =>
#if Debug
        "http://192.168.7.148";
#else
        "https://assets-1321503079.cos.ap-beijing.myqcloud.com";
#endif
    public static string CDNPlatform =>
#if UNITY_ANDROID
        $"{CDN}/Android";
#elif UNITY_IOS
        $"{CDN}/iOS";
#elif UNITY_WEBGL
        $"{CDN}/WebGL";
#else
        string.Empty;
#endif
    public static string CDNVersion => $"{CDNPlatform}/{Application.version}/";

    public static string AppName =>
#if UNITY_ANDROID
        "HelloWorld.apk";
#elif UNITY_IOS
        "HelloWorld.app";
#else
        string.Empty;
#endif


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void StopSplashScreen()
    {
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void InitObfuzSecret()
    {
        var ta = Resources.Load<TextAsset>("StaticSecretKey");
        Obfuz.EncryptionService<Obfuz.DefaultStaticEncryptionScope>.Encryptor = new Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine(ta.bytes);
        Resources.UnloadAsset(ta);
        ta = Resources.Load<TextAsset>("DynamicSecretKey");
        Obfuz.EncryptionService<Obfuz.DefaultDynamicEncryptionScope>.Encryptor = new Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine(ta.bytes);
        Resources.UnloadAsset(ta);
    }
}