#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK;
#endif

public static class GamePlayerPrefs
{
    public static void SetInt(string key, int value)
    {
#if UNITY_EDITOR
        UnityEngine.PlayerPrefs.SetInt(key, value);
        UnityEngine.PlayerPrefs.Save();
#elif WEIXINMINIGAME
        WX.StorageSetIntSync(key, value);
#elif DOUYINMINIGAME
        TT.PlayerPrefs.SetInt(key, value);
#else
        UnityEngine.PlayerPrefs.SetInt(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static int GetInt(string key, int defaultValue = 0)
    {
#if UNITY_EDITOR
        return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
#elif WEIXINMINIGAME
        return WX.StorageGetIntSync(key, defaultValue);
#elif DOUYINMINIGAME
        return TT.PlayerPrefs.GetInt(key, defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
#endif
    }
    public static void SetFloat(string key, float value)
    {
#if UNITY_EDITOR
        UnityEngine.PlayerPrefs.SetFloat(key, value);
        UnityEngine.PlayerPrefs.Save();
#elif WEIXINMINIGAME
        WX.StorageSetFloatSync(key,value);
#elif DOUYINMINIGAME
        TT.PlayerPrefs.SetFloat(key, value);
#else
        UnityEngine.PlayerPrefs.SetFloat(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static float GetFloat(string key, float defaultValue = 0)
    {
#if UNITY_EDITOR
        return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
#elif WEIXINMINIGAME
        return WX.StorageGetFloatSync(key, defaultValue);
#elif DOUYINMINIGAME
        return TT.PlayerPrefs.GetFloat(key, defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }
    public static void SetString(string key, string value)
    {
#if UNITY_EDITOR
        UnityEngine.PlayerPrefs.SetString(key, value);
        UnityEngine.PlayerPrefs.Save();
#elif WEIXINMINIGAME
        WX.StorageSetStringSync(key,value);
#elif DOUYINMINIGAME
        TT.PlayerPrefs.SetString(key, value);
#else
        UnityEngine.PlayerPrefs.SetString(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static string GetString(string key, string defaultValue = "")
    {
#if UNITY_EDITOR
        return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
#elif WEIXINMINIGAME
        return WX.StorageGetStringSync(key,defaultValue);
#elif DOUYINMINIGAME
        return TT.PlayerPrefs.GetString(key, defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
#endif
    }
}