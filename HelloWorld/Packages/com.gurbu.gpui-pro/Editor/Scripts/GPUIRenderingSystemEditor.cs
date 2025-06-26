// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIRenderingSystem))]
    public class GPUIRenderingSystemEditor : Editor
    {
        private GPUIRenderingSystem _renderingSystem;

        private Vector2 _scrollPos;
        private List<int> _foldedRSGList;

        private bool _showVisibilityData;
        //private uint[] _renderGroupData;
        private bool _camerasFoldout = true;
        private bool _prototypesFoldout = true;

        protected void OnEnable()
        {
            _renderingSystem = target as GPUIRenderingSystem;
            _foldedRSGList = new();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Do not add this component manually. It will be automatically created. GameObject containing this component will not be saved in your scenes and will persist between scenes. You can still access the information on this Inspector from the GPUI Debugger window.", MessageType.Warning);
            DrawRenderingSystem(null);
        }

        public void DrawRenderingSystem(GPUIDebuggerEditorWindow debuggerWindow)
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            _renderingSystem = GPUIRenderingSystem.Instance;

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
            EditorGUI.indentLevel++;

            GPUIEditorTextUtility.GPUIText gpuiText;
            if (debuggerWindow != null && debuggerWindow.IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("gpuiDebuggerWindow", out gpuiText))
            {
                EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
                EditorGUILayout.Space(10);
            }

            if (!Application.isPlaying)
            {
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Regenerate Renderers"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, GUILayoutUtility.GetRect(160, 20, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)), GPUIRenderingSystem.RegenerateRenderers);
                if (debuggerWindow != null && debuggerWindow.IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("regenerateRenderersButton", out gpuiText))
                    EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
                EditorGUILayout.Space(10);
            }

            if (Application.isPlaying)
            {
                _showVisibilityData = EditorGUILayout.Toggle("Show Visibility Data", _showVisibilityData);
                if (debuggerWindow != null && debuggerWindow.IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("showVisibilityData", out gpuiText))
                    EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
            }
            else _showVisibilityData = false;


            if (_renderingSystem.CameraDataProvider != null)
            {
                _camerasFoldout = EditorGUILayout.Foldout(_camerasFoldout, "Cameras [" + _renderingSystem.CameraDataProvider.Count + "]");
                if (_camerasFoldout)
                {
                    EditorGUI.indentLevel++;
                    int index = 0;
                    foreach (var cd in _renderingSystem.CameraDataProvider.Values)
                    {
                        DrawCameraData(cd, index);
                        index++;
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(10);
            }

            if (_renderingSystem.RenderSourceGroupProvider != null)
            {
                _prototypesFoldout = EditorGUILayout.Foldout(_prototypesFoldout, "Prototypes [" + _renderingSystem.RenderSourceGroupProvider.Count + "]");
                if (_prototypesFoldout)
                {
                    EditorGUI.indentLevel++;
                    foreach (var rsg in _renderingSystem.RenderSourceGroupProvider.Values)
                    {
                        DrawRenderSourceGroup(rsg);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(10);
            GPUIEditorUtility.DrawColoredButton(new GUIContent("Dispose All"), GPUIEditorConstants.Colors.lightRed, Color.white, FontStyle.Bold, GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)), GPUIRenderingSystem.ResetRenderingSystem);
            if (debuggerWindow != null && debuggerWindow.IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("disposeAllButton", out gpuiText))
                EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
            EditorGUILayout.Space(10);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(30);
            EditorGUILayout.EndScrollView();

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void DrawCameraData(GPUICameraData cameraData, int index)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("", cameraData.ActiveCamera, typeof(UnityEngine.Object), true);
            EditorGUI.EndDisabledGroup();
            if (Application.isPlaying)
                cameraData.renderToSceneView = EditorGUILayout.Toggle("Render to Scene View", cameraData.renderToSceneView);
            EditorGUI.BeginDisabledGroup(true);
            //if (cameraData.hiZDepthTexture != null)
            //    EditorGUILayout.ObjectField("HiZDepth", cameraData.hiZDepthTexture, typeof(UnityEngine.Texture), true);

            if (_showVisibilityData)
            {
                GPUIVisibilityData[] visibilityDataArray = cameraData.GetVisibilityDataArray();
                if (visibilityDataArray != null)
                {
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 300f;
                    EditorGUI.indentLevel++;
                    foreach (GPUIRenderSourceGroup rsg in _renderingSystem.RenderSourceGroupProvider.Values)
                    {
                        GPUILODGroupData lgd = rsg.LODGroupData;
                        if (lgd != null && cameraData.TryGetVisibilityBufferIndex(rsg, out int visibilityBufferIndex))
                        {
                            for (int l = 0; l < lgd.Length; l++)
                            {
                                DrawVisibilityData(rsg, lgd, visibilityDataArray, l, visibilityBufferIndex);
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUIUtility.labelWidth = labelWidth;
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawVisibilityData(GPUIRenderSourceGroup rsg, GPUILODGroupData lgd, GPUIVisibilityData[] visibilityDataArray, int l, int visibilityBufferIndex)
        {
            GPUIVisibilityData visibilityData = visibilityDataArray[visibilityBufferIndex + l];
            EditorGUILayout.IntField(lgd.ToString() + " LOD " + l + " Visible Count", (int)visibilityData.visibleCount);
            //if (rsg.profile.isShadowCasting)
            //{
            //    visibilityData = visibilityDataArray[visibilityBufferIndex + l + lgd.Length];
            //    EditorGUILayout.IntField(lgd.ToString() + " LOD " + l + " Shadow", (int)visibilityData.visibleCount);
            //    EditorGUILayout.IntField(lgd.ToString() + " LOD " + l + " Shadow Instance Count", (int)visibilityData.instanceCount);
            //}
        }

        private void DrawRenderSourceGroup(GPUIRenderSourceGroup rsg)
        {
            EditorGUI.BeginChangeCheck();
            bool foldout = EditorGUILayout.Foldout(!_foldedRSGList.Contains(rsg.Key), "");
            if (EditorGUI.EndChangeCheck())
            {
                if (foldout && _foldedRSGList.Contains(rsg.Key))
                    _foldedRSGList.Remove(rsg.Key);
                else if (!foldout && !_foldedRSGList.Contains(rsg.Key))
                    _foldedRSGList.Add(rsg.Key);
            }
            EditorGUI.BeginDisabledGroup(true);
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += 20;
            rect.width -= 20;
            GPUILODGroupData lodGroupData = rsg.LODGroupData;
            if (lodGroupData != null)
            {
                if (lodGroupData.prototype != null && lodGroupData.prototype.prefabObject != null)
                {
                    float halfWidth = rect.width / 2;
                    rect.width = halfWidth;
                    EditorGUI.ObjectField(rect, lodGroupData.prototype.prefabObject, typeof(UnityEngine.Object), false);
                    rect.x += halfWidth;
                    EditorGUI.ObjectField(rect, lodGroupData, typeof(UnityEngine.Object), true);
                }
                else
                    EditorGUI.ObjectField(rect, lodGroupData, typeof(UnityEngine.Object), true);
            }
            EditorGUILayout.LabelField("["+ rsg.Key + "] Buffer Size: " + rsg.BufferSize.ToString() + " Instance Count: " + rsg.InstanceCount.ToString());
            if (_showVisibilityData /*&& _renderGroupData != null*/)
            {
                //for (int i = 0; i < rsg.lodGroupData.Length; i++)
                //{
                //    EditorGUILayout.TextField("Visible LOD " + i, _renderGroupData[rsg.dataStartIndex + 1 + i].ToString());
                //}
            }
            if (foldout)
            {
                EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
                EditorGUI.indentLevel++;
                foreach (var rs in rsg.RenderSources)
                {
                    EditorGUILayout.ObjectField("[" + rs.Key + "] Source", rs.source, typeof(UnityEngine.Object), true);
                    //EditorGUILayout.TextField("Key", rs.Key.ToString());
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.TextField("Start Index", rs.bufferStartIndex.ToString());
                    EditorGUILayout.TextField("Buffer Size", rs.bufferSize.ToString());
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
