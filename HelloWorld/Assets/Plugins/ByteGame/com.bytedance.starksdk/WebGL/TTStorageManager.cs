using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace TTSDK
{
    public static class TTStorage
    {
        public static void SetIntSync(string key, int value)
        {
            StarkStorageSetIntSync(key, value);
        }

        public static int GetIntSync(string key, int defaultValue)
        {
            return StarkStorageGetIntSync(key, defaultValue);
        }

        public static void SetFloatSync(string key, float value)
        {
            StarkStorageSetFloatSync(key, value);
        }

        public static float GetFloatSync(string key, float defaultValue)
        {
            return StarkStorageGetFloatSync(key, defaultValue);
        }

        public static void SetStringSync(string key, string value)
        {
            StarkStorageSetStringSync(key, value);
        }

        public static string GetStringSync(string key, string defaultValue)
        {
            return StarkStorageGetStringSync(key, defaultValue);
        }

        public static bool HasKeySync(string key)
        {
            return StarkStorageHasKeySync(key);
        }

        public static void DeleteKeySync(string key)
        {
            StarkStorageDeleteKeySync(key);
        }

        public static void DeleteAllSync()
        {
            StarkStorageDeleteAllSync();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkStorageSetIntSync(string key, int value);
#else
        static void StarkStorageSetIntSync(string key, int value)
        {
            UnityEngine.PlayerPrefs.SetInt(key, value);
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int StarkStorageGetIntSync(string key, int defaultValue);
#else
        static int StarkStorageGetIntSync(string key, int defaultValue)
        {
            return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkStorageSetStringSync(string key, string value);
#else
        static void StarkStorageSetStringSync(string key, string value)
        {
            UnityEngine.PlayerPrefs.SetString(key, value);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string StarkStorageGetStringSync(string key, string defaultValue);
#else
        static string StarkStorageGetStringSync(string key, string defaultValue)
        {
            return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkStorageSetFloatSync(string key, float value);
#else
        static void StarkStorageSetFloatSync(string key, float value)
        {
            UnityEngine.PlayerPrefs.SetFloat(key, value);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern float StarkStorageGetFloatSync(string key, float defaultValue);
#else
        static float StarkStorageGetFloatSync(string key, float defaultValue)
        {
            return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkStorageDeleteAllSync();
#else
        static void StarkStorageDeleteAllSync()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkStorageDeleteKeySync(string key);
#else
        static void StarkStorageDeleteKeySync(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(key);
        }
#endif


#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool StarkStorageHasKeySync(string key);
#else
        static bool StarkStorageHasKeySync(string key)
        {
            return UnityEngine.PlayerPrefs.HasKey(key);
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [Preserve]
        [DllImport("__Internal")]
        private static extern void StarkPointerStringify();
#endif
    }
}