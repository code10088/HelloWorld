// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIBillboard))]
    public class GPUIBillboardEditor : GPUIEditor
    {
        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawContentGUI(contentElement, serializedObject, _helpBoxes);
        }

        public static void DrawContentGUI(VisualElement contentElement, SerializedObject serializedObject, List<GPUIHelpBox> helpBoxes)
        {
            if (serializedObject.targetObject == null)
                return;

            GPUIBillboard billboardAsset = serializedObject.targetObject as GPUIBillboard;

            VisualElement container = new VisualElement();
            contentElement.Add(container);

            container.Add(DrawSerializedProperty(serializedObject.FindProperty("prefabObject"), helpBoxes, out PropertyField pf));
            pf.SetEnabled(false);
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("atlasResolution"), helpBoxes));
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("frameCount"), helpBoxes));
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("brightness"), helpBoxes));
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("cutoffOverride"), helpBoxes));
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("normalStrength"), helpBoxes));
            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                container.Add(DrawSerializedProperty(serializedObject.FindProperty("billboardShaderType"), helpBoxes));

            container.Add(DrawSerializedProperty(serializedObject.FindProperty("albedoAtlasTexture"), helpBoxes, out pf));
            pf.SetEnabled(false);
            container.Add(DrawSerializedProperty(serializedObject.FindProperty("normalAtlasTexture"), helpBoxes, out pf));
            pf.SetEnabled(false);
            GPUIEditorUtility.DrawGPUIHelpBox(container, -1101, null, null, HelpBoxMessageType.Info);

            VisualElement buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            container.Add(buttons);

            Button generateBillboardButton = new(() =>
            {
                GPUIBillboardUtility.GenerateBillboard(billboardAsset, true);
                GPUIRenderingSystem.RegenerateRenderers();
            });
            generateBillboardButton.text = "Regenerate";
            generateBillboardButton.enableRichText = true;
            generateBillboardButton.style.marginLeft = 10;
            generateBillboardButton.style.backgroundColor = GPUIEditorConstants.Colors.green;
            generateBillboardButton.style.color = Color.white;
            generateBillboardButton.style.flexGrow = 1;
            generateBillboardButton.focusable = false;
            buttons.Add(generateBillboardButton);

            if (!Application.isPlaying)
            {
                Button editBillboardButton = new(() => { OnPreviewButtonClickEvent(billboardAsset); });
                editBillboardButton.text = "Preview";
                editBillboardButton.enableRichText = true;
                editBillboardButton.style.marginLeft = 10;
                editBillboardButton.style.backgroundColor = GPUIEditorConstants.Colors.blue;
                editBillboardButton.style.color = Color.white;
                editBillboardButton.style.flexGrow = 0.5f;
                editBillboardButton.focusable = false;
                buttons.Add(editBillboardButton);
            }

            //container.SetEnabled(!Application.isPlaying);
        }

        private static void OnPreviewButtonClickEvent(GPUIBillboard billboardAsset)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Current Scene"), false, () =>
            {
                GameObject previewGO = GPUIBillboardGeneratorWindow.GenerateBillboardPreview(billboardAsset, out _, out _);
                Selection.activeGameObject = previewGO;
                SceneView.lastActiveSceneView.FrameSelected();
            });
            menu.AddItem(new GUIContent("Preview Scene"), false, () =>
            {
                GPUIBillboardGeneratorWindow w = GPUIBillboardGeneratorWindow.ShowWindow();
                if (w != null)
                    w.SetBillboard(billboardAsset);
            });

            // display the menu
            menu.ShowAsContext();
        }

        public override string GetTitleText()
        {
            return "GPUI Billboard";
        }
    }
}
