// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    //[CreateAssetMenu(menuName = "GPU Instancer Pro/Path Locator")]
    public class GPUIPathLocator : ScriptableObject
    {
    }

    [CustomEditor(typeof(GPUIPathLocator))]
    public class GPUIPathLocatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIStyle style = new GUIStyle("Label");
            style.richText = true;
            style.wordWrap = true;
            GUILayout.Label("<size=20>Do not delete or edit this file.</size>\n\nThis file will be used to locate the GPUInstancerPro folder where the generated assets such as settings and profiles will be kept.", style);

            string path = AssetDatabase.GetAssetPath(target);
            string currentGuid = AssetDatabase.AssetPathToGUID(path);
            if (GPUIConstants.PATH_LOCATOR_GUID != currentGuid)
            {
                GUILayout.Label("\n\n<color=red><size=14><b>File GUID does not match the expected GUID!</b></size></color>", style);
                GUILayout.Label("\n" + GPUIConstants.PATH_LOCATOR_GUID + " [Expected GUID]\n" + currentGuid + " [Current GUID]", style);
            }
        }
    }
}
