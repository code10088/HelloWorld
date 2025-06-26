// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUICamera))]
    public class GPUICameraEditor : GPUIEditor
    {
        private GPUICamera _gpuiCamera;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiCamera = target as GPUICamera;
        }

        public override void DrawIMGUIContainer()
        {
            GPUICameraData cameraData = _gpuiCamera.GetCameraData();
            SerializedProperty enableOcclusionCullingSP = serializedObject.FindProperty("_enableOcclusionCulling");
            EditorGUI.BeginChangeCheck();
            DrawIMGUISerializedProperty(enableOcclusionCullingSP);
            if (enableOcclusionCullingSP.boolValue)
            {
                if (GPUIRuntimeSettings.Instance.DisableOcclusionCulling)
                    GPUIEditorUtility.DrawIMGUIHelpText("Current platform does not support occlusion culling. " + SystemInfo.graphicsDeviceType + " " + GPUIRuntimeSettings.Instance.RenderPipeline, MessageType.Warning);
                else if (GPUIRuntimeSettings.Instance.occlusionCullingCondition == GPUIOcclusionCullingCondition.Never || (GPUIRuntimeSettings.Instance.occlusionCullingCondition == GPUIOcclusionCullingCondition.IfDepthAvailable && !_gpuiCamera.GetCamera().IsDepthTextureAvailable()))
                    GPUIEditorUtility.DrawIMGUIHelpText("Occlusion culling feature is disabled project wide in GPUI Runtime Settings.", MessageType.Warning);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();

                if (Application.isPlaying && cameraData != null)
                {
                    if (enableOcclusionCullingSP.boolValue)
                        cameraData.autoInitializeOcclusionCulling = true;
                    else
                    {
                        cameraData.autoInitializeOcclusionCulling = false;
                        cameraData.DisableOcclusionCulling();
                    }
                }
            }

            if (enableOcclusionCullingSP.boolValue)
            {
                SerializedProperty dynamicOcclusionOffsetIntensitySP = serializedObject.FindProperty("_dynamicOcclusionOffsetIntensity");
                EditorGUI.BeginChangeCheck();
                DrawIMGUISerializedProperty(dynamicOcclusionOffsetIntensitySP);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    if (Application.isPlaying)
                        _gpuiCamera.SetDynamicOcclusionOffsetIntensity(dynamicOcclusionOffsetIntensitySP.floatValue);
                }

                if (Application.isPlaying && cameraData != null && cameraData.OcclusionCullingData != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField("HiZ Depth Texture", cameraData.OcclusionCullingData.HiZDepthTexture, typeof(UnityEngine.Object), true); // Using Texture type slows down editor
#if GPUIPRO_DEVMODE
                    if (cameraData.OcclusionCullingData.CameraDepthTexture != null)
                        EditorGUILayout.ObjectField("Unity Depth Texture", cameraData.OcclusionCullingData.CameraDepthTexture, typeof(UnityEngine.Object), true);
                    EditorGUILayout.TextField("Occlusion C. Mode", cameraData.OcclusionCullingData.ActiveCullingMode.ToString());
#endif
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Camera";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Camera";
        }
    }
}