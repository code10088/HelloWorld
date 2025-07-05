#if TUANJIE_1_5_OR_NEWER
using UnityEditor.Build.Profile;
using UnityEngine;

namespace TTSDK.Tool
{
    class DouYinMiniGameSettings: MiniGameSettings
    {
        [SerializeField] public string appId = "";
        [SerializeField] public int wasmMemorySize = 128;
        [SerializeField] public string[] urlCacheList;
        [SerializeField] public string[] dontCacheFileNames;
        [SerializeField] public bool needCompress = true;
        [SerializeField] public bool profiling = false;
        [SerializeField] public bool iOSPerformancePlus = false;
        [SerializeField] public int orientation = (int)TTDeviceOrientation.Portrait;
        
        [SerializeField] public string CDN = "";
        [SerializeField] public string preloadFiles = "";
        [SerializeField] public string preloadDataListUrl = "";
        [SerializeField] public bool clearStreamingAssets = false;
        
        public DouYinMiniGameSettings(MiniGameSettingsEditor editor) : base(editor)
        {
        }
    }
}
#endif
