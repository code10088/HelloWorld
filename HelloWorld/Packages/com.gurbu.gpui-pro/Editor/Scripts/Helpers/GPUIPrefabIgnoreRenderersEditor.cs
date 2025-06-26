// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIPrefabIgnoreRenderers))]
    public class GPUIPrefabIgnoreRenderersEditor : GPUIEditor
    {
        public override void DrawContentGUI(VisualElement contentElement)
        {
            contentElement.Add(GPUIEditorUtility.CreateGPUIHelpBox("prefabIgnoreRenderersInfo", null, null, HelpBoxMessageType.Info));
        }

        public override string GetWikiURLParams() => "title=GPU_Instancer_Pro:GettingStarted#GPUI_Prefab_Ignore_Renderers";

        public override string GetTitleText() => "GPUI Prefab Ignore Renderers";
    }
}
