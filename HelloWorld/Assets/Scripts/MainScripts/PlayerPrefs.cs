#if WeChatGame && !UNITY_EDITOR
using WeChatWASM;
#endif

public static class PlayerPrefs
{
    public static void SetInt(string key, int value)
    {
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetIntSync(key, value);
#else
        UnityEngine.PlayerPrefs.SetInt(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static int GetInt(string key, int defaultValue = 0)
    {
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetIntSync(key, defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
#endif
    }
    public static void SetFloat(string key, float value)
    {
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetFloatSync(key,value);
#else
        UnityEngine.PlayerPrefs.SetFloat(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static float GetFloat(string key, float defaultValue = 0)
    {
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetFloatSync(key, defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }
    public static void SetString(string key, string value)
    {
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetStringSync(key,value);
#else
        UnityEngine.PlayerPrefs.SetString(key, value);
        UnityEngine.PlayerPrefs.Save();
#endif
    }
    public static string GetString(string key, string defaultValue = "")
    {
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetStringSync(key,defaultValue);
#else
        return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
#endif
    }
}