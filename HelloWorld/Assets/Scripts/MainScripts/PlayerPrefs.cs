using WeChatWASM;

public static class PlayerPrefs
{
    public static void SetInt(string key, int value)
    {
        UnityEngine.PlayerPrefs.SetInt(key, value);
        UnityEngine.PlayerPrefs.Save();
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetIntSync(key, value);
#endif
    }
    public static int GetInt(string key, int defaultValue = 0)
    {
        return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetIntSync(key, defaultValue);
#endif
    }
    public static void SetFloat(string key, float value)
    {
        UnityEngine.PlayerPrefs.SetFloat(key, value);
        UnityEngine.PlayerPrefs.Save();
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetFloatSync(key,value);
#endif
    }
    public static float GetFloat(string key, float defaultValue = 0)
    {
        return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetFloatSync(key, defaultValue);
#endif
    }
    public static void SetString(string key, string value)
    {
        UnityEngine.PlayerPrefs.SetString(key, value);
        UnityEngine.PlayerPrefs.Save();
#if WeChatGame && !UNITY_EDITOR
        WX.StorageSetStringSync(key,value);
#endif
    }
    public static string GetString(string key, string defaultValue = "")
    {
        return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
#if WeChatGame && !UNITY_EDITOR
        return WX.StorageGetStringSync(key,defaultValue);
#endif
    }
}