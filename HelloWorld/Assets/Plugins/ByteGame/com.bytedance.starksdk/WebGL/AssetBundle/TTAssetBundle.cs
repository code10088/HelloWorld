using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace TTSDK
{
    public class TTAssetBundle
    {

        public static Dictionary<AssetBundle, string> bundle2path = new Dictionary<AssetBundle, string>();

        public static bool isAbfsReady = CheckReady();
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "StarkAbfsCheckReady")]
        public static extern bool CheckReady();
#else
        public static bool CheckReady()
        {
            return false;
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "StarkAbfsRegisterAssetBundleUrl")]
        public static extern void RegisterAssetBundleUrl(string path);
#else
        public static void RegisterAssetBundleUrl(string path) {
            
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "StarkAbfsUnregisterAssetBundleUrl")]
        public static extern void UnregisterAssetBundleUrl(string path);
#else
        public static void UnregisterAssetBundleUrl(string path) {
            
        }
#endif
        
        public static UnityWebRequest GetAssetBundle(string uri) => GetAssetBundle(uri, 0U);

        public static UnityWebRequest GetAssetBundle(string uri, uint crc)
        {
            if (isAbfsReady)
                RegisterAssetBundleUrl(uri);
            
            return new UnityWebRequest(uri, "GET", new DownloadHandlerTTAssetBundle(uri, crc), null);
        }

    }
}