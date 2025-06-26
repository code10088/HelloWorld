// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIRuntimeSettings))]
    public class GPUIRuntimeSettingsEditor : GPUIEditor
    {
        public delegate void OnDrawExtensionRuntimeSettingsDelegate(VisualElement rootElement);
        public static OnDrawExtensionRuntimeSettingsDelegate OnDrawExtensionRuntimeSettings;

        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawContentGUI(contentElement, serializedObject, _helpBoxes);
        }

        public static void DrawContentGUI(VisualElement contentElement, SerializedObject serializedObject, List<GPUIHelpBox> helpBoxes)
        {
            VisualElement renderPipelineVE = new VisualElement();
            renderPipelineVE.Add(new Label(
                " Graphics API:\t\t\t" + GPUIRuntimeSettings.Instance.GraphicsDeviceType
                + "\n Render Pipeline:\t\t\t" + GPUIRuntimeSettings.Instance.RenderPipeline
                + "\n Texture Max. Size:\t\t" + GPUIRuntimeSettings.Instance.TextureMaxSize.ToString("#,0") 
                + "\n Compute Thread Count:\t" + GPUIRuntimeSettings.Instance.ComputeThreadCount.ToString("#,0")
                + "\n Max Buffer Size:\t\t\t" + GPUIRuntimeSettings.Instance.MaxBufferSize.ToString("#,0")
                + "\n Allow Shader Buffers:\t\t" + !GPUIRuntimeSettings.Instance.DisableShaderBuffers 
                + "\n Allow Occlusion Culling:\t" + !GPUIRuntimeSettings.Instance.DisableOcclusionCulling
                ));
            renderPipelineVE.SetEnabled(false);
            renderPipelineVE.style.marginBottom = 5;
            contentElement.Add(renderPipelineVE);

            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("occlusionCullingCondition"), helpBoxes));
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("occlusionCullingMode"), helpBoxes));
            //contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("_disableShaderBuffers"), helpBoxes)); // Requires ShaderSettings modification to enable GPUI_NO_BUFFER
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("cameraLoadingType"), helpBoxes));
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("instancingBoundsSize"), helpBoxes));
            if (GPUIRuntimeSettings.Instance.IsHDRP)
                contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("defaultHDRPShadowDistance"), helpBoxes));

#if GPUI_ADDRESSABLES
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("loadShadersFromAddressables"), helpBoxes));
            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("loadResourcesFromAddressables"), helpBoxes));
#endif

            VisualElement billboardAssetsVE = DrawSerializedProperty(serializedObject.FindProperty("billboardAssets"), helpBoxes);
            contentElement.Add(billboardAssetsVE);

            OnDrawExtensionRuntimeSettings?.Invoke(contentElement);

            if (Application.isPlaying)
                contentElement.SetEnabled(false);
        }

        public override string GetTitleText()
        {
            return "GPUI Runtime Settings";
        }
    }

    [CustomPropertyDrawer(typeof(GPUIManagerDefaults))]
    public class GPUIManagerDefaultsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            Label label = new Label("<b>" + GPUIUtility.CamelToTitleCase(property.FindPropertyRelative("managerTypeName").stringValue.Replace("GPUI", "")) + "</b>");
            label.enableRichText = true;
            container.Add(label);
            container.Add(GPUIEditorUtility.DrawSerializedProperty(property.FindPropertyRelative("defaultProfileOverride")));
            return container;
        }
    }
}
