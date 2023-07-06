using UnityEngine;

public class GameDebug
{
    public static bool showLog = true;
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