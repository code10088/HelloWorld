using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

[assembly: Preserve]

namespace TTSDK
{
    public delegate void GetFontDataDelegate(IntPtr pBuffer,int length);
    public class TTWebGLInterface
    {
#if (UNITY_WEBPLAYER || UNITY_WEBGL)
        //以下接口为Web使用，用于调用JS代码。
        [method: Preserve]
        [DllImport("__Internal")]
        public static extern void unityCallJs(string msg);
        
        [method: Preserve]
        [DllImport("__Internal")]
        public static extern string unityCallJsSync(string msg);
        
        [method: Preserve]
        [DllImport("__Internal")]
        public static extern bool h5HasAPI(string apiName);
        
        [method: Preserve]
        [DllImport("__Internal")]
        public static extern string unityMixCallJs(string msg);
        
        [DllImport("__Internal")]
        public static extern void StarkGetSystemFont(GetFontDataDelegate onFontData);
#else
        public static void unityCallJs(string msg)
        {
        }

        public static string unityCallJsSync(string msg)
        {
            return "";
        }

        public static bool h5HasAPI(string apiName)
        {
            return false;
        }

        public static string unityMixCallJs(string msg)
        {
            return "";
        }
        
        public static void StarkGetSystemFont(GetFontDataDelegate onFontData)
        {
            onFontData?.Invoke(IntPtr.Zero, 0);
        }
#endif
    }
}