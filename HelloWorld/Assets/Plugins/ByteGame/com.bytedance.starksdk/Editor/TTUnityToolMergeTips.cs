using System;
using UnityEditor;

namespace TTSDK
{
    /// <summary>
    /// 
    /// </summary>
    [InitializeOnLoad]
    public static class TTUnityToolMergeTips
    {
        private const string KeyRemoveOldTips = "ttsdk.remove-old-tool.tips";
        
        static TTUnityToolMergeTips()
        {
            if (EditorPrefs.HasKey(KeyRemoveOldTips))
                return;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "starksdk_tools")
                {
                    EditorUtility.DisplayDialog("提示", "自 TTSDK 6.2.0 开始，已包含 StarkSDKTools 所有功能, 原 StarkSDKTools 已经废弃，请通过 BGDT 管理窗口删除。", "Ok");
                    break;
                }
            }
        }
    }
}