using UnityEngine;

namespace MainAssembly
{
    public class GameDebug
    {
        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }
        public static void LogFormat(string message, object[] obj)
        {
            Debug.LogFormat(message, obj);
        }
    }
}