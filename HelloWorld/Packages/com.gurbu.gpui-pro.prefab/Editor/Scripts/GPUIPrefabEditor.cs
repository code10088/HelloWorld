// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [CustomEditor(typeof(GPUIPrefab))]
    public class GPUIPrefabEditor : GPUIEditor
    {
        private GPUIPrefab _gpuiPrefab;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiPrefab = target as GPUIPrefab;
        }

        public override void DrawIMGUIContainer()
        {
//#if GPUIPRO_DEVMODE
            EditorGUI.BeginDisabledGroup(true);
            DrawIMGUISerializedProperty(serializedObject.FindProperty("_prefabID"));
            var childOptionalRenderersSP = serializedObject.FindProperty("childOptionalRenderers");
            if (childOptionalRenderersSP.arraySize > 0)
                DrawIMGUISerializedProperty(childOptionalRenderersSP);
            EditorGUI.EndDisabledGroup();
//#endif
            if (_gpuiPrefab.IsInstanced)
            {
                EditorGUILayout.LabelField("Instancing is active.");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Registered Manager", _gpuiPrefab.registeredManager, typeof(GPUIPrefabManager), true);
                EditorGUILayout.IntField("Render Key", _gpuiPrefab.renderKey);
                EditorGUILayout.IntField("Buffer Index", _gpuiPrefab.bufferIndex);
#if GPUIPRO_DEVMODE
                if (childOptionalRenderersSP.arraySize > 0)
                    EditorGUILayout.TextField("Optional Renderer Status", GPUIUtility.UintToBinaryString(_gpuiPrefab.optionalRendererStatus, childOptionalRenderersSP.arraySize));
#endif
                EditorGUI.EndDisabledGroup();
            }
            else if (Application.isPlaying && !GPUIPrefabUtility.IsPrefabAsset(_gpuiPrefab.gameObject, out _, false))
                EditorGUILayout.LabelField("Instancing has not been initialized.");
        }

        public override string GetTitleText()
        {
            return "GPUI Prefab";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Prefab";
        }
    }
}
