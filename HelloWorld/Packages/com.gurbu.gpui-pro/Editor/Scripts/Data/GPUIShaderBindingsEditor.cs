// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIShaderBindings))]
    public class GPUIShaderBindingsEditor : GPUIEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            GPUIShaderUtility.CheckForShaderModifications();
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("shaderInstances")));
            contentElement.Add(GPUIEditorUtility.CreateGPUIHelpBox("shaderBindingsInfo", null, null, HelpBoxMessageType.Info));
        }

        public override string GetTitleText()
        {
            return "GPUI Shader Bindings";
        }
    }
}
