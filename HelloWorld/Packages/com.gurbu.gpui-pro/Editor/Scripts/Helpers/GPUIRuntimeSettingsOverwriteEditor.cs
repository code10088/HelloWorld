// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIRuntimeSettingsOverwrite))]
    public class GPUIRuntimeSettingsOverwriteEditor : GPUIEditor
    {
        private GPUIRuntimeSettingsOverwrite _runtimeSettingsOverwrite;
        private VisualElement _contentElement;

        protected override void OnEnable()
        {
            base.OnEnable();

            _runtimeSettingsOverwrite = target as GPUIRuntimeSettingsOverwrite;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            if (_runtimeSettingsOverwrite == null)
                return;
            _contentElement = contentElement;
            DrawContents();
        }

        private void DrawContents()
        {
            _contentElement.Clear();
            _contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("runtimeSettingsOverwrite"), out PropertyField runtimeSettingsOverwritePF));
            runtimeSettingsOverwritePF.RegisterValueChangedCallbackDelayed((evt) => DrawContents());
            if (_runtimeSettingsOverwrite.runtimeSettingsOverwrite != null)
            {
                GPUIRuntimeSettingsEditor.DrawContentGUI(_contentElement, new SerializedObject(_runtimeSettingsOverwrite.runtimeSettingsOverwrite), _helpBoxes);
            }
            else
            {
                Button createButton = new Button(OnCreateButtonClicked);
                createButton.text = "Create";
                _contentElement.Add(createButton);
            }
        }

        private void OnCreateButtonClicked()
        {
            serializedObject.ApplyModifiedProperties();
            GPUIRuntimeSettings runtimeSettings = GPUIRuntimeSettings.CreateInstance<GPUIRuntimeSettings>();
            runtimeSettings.SaveAsAsset();
            _runtimeSettingsOverwrite.runtimeSettingsOverwrite = runtimeSettings;
            serializedObject.Update();
            DrawContents();
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPU_Instancer_Pro_Runtime_Settings";
        }

        public override string GetTitleText()
        {
            return "GPUI Runtime Settings";
        }
    }
}
