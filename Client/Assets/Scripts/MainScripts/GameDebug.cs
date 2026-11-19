using UnityEngine;

public class GameDebug
{
#if Debug
    public static bool GDebug = true;
#else
    public static bool GDebug = false;
#endif
    public static void Log(object message)
    {
        if (GDebug) Debug.Log(message);
    }
    public static void LogError(object message)
    {
        if (GDebug) Debug.LogError(message);
    }
    public static void LogWarning(object message)
    {
        if (GDebug) Debug.LogWarning(message);
    }
    public static void LogFormat(string message, object[] obj)
    {
        if (GDebug) Debug.LogFormat(message, obj);
    }
}