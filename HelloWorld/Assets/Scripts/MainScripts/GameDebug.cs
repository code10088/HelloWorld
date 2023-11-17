using UnityEngine;

public class GameDebug
{
#if RELEASE
    public static bool GDebug = false;
    public static bool showLog = false;
#else
    public static bool GDebug = true;
    public static bool showLog = true;
#endif
    public static void Log(object message)
    {
        if (showLog) Debug.Log(message);
    }
    public static void LogError(object message)
    {
        if (showLog) Debug.LogError(message);
    }
    public static void LogWarning(object message)
    {
        if (showLog) Debug.LogWarning(message);
    }
    public static void LogFormat(string message, object[] obj)
    {
        if (showLog) Debug.LogFormat(message, obj);
    }
}