using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace TTSDK
{
    public class TTGraphics
    {
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TT_SetPreferredDevicePixelRatioPercent(int dprPct);
#endif
        
        /// <summary>
        /// 设置设备像素比
        /// </summary>
        /// <param name="dpr"></param>
        public static void SetPreferredDevicePixelRatio(float dpr)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var dprPct = dpr * 100;
            TT_SetPreferredDevicePixelRatioPercent((int)dprPct);
#else
            Debug.LogWarning($"SetPreferredDevicePixelRatio({dpr}) is not supported on current platform.");
#endif
        }
    }
}