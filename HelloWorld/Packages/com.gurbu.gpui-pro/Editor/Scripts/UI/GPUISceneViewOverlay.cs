// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;

namespace GPUInstancerPro
{
    [Overlay(typeof(SceneView), "GPUI Pro")]
    //[Icon("Assets/unity.png")]
    public class GPUISceneViewOverlay : ToolbarOverlay
    {
        GPUISceneViewOverlay() : base(GPUISceneViewOverlayRenderModeToggle.id, GPUISceneViewOverlayOcclusionCullingDebugToggle.id) { }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class GPUISceneViewOverlayRenderModeToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        public const string id = "GPUISceneViewOverlay/RenderModeToggle";
        private Texture2D OnIcon;
        private Texture2D OffIcon;

        public EditorWindow containerWindow { get; set; }

        public GPUISceneViewOverlayRenderModeToggle()
        {
            text = "GPUI";
            tooltip = "GPUI Pro, switch between Culled view and Full view.";
            style.paddingLeft = 4;

            try
            {
                OnIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleon").image as Texture2D;
                OffIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image as Texture2D;
            }
            catch(Exception e) { Debug.LogException(e); }

            SetIcon(value);
            this.RegisterValueChangedCallback(ChangeRenderMode);
            this.RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            value = GPUIRenderingSystem.Editor_ContainsSceneViewCameraData(GetActiveSceneViewCamera());
        }

        void ChangeRenderMode(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                GPUIRenderingSystem.Editor_AddSceneViewCameraData(GetActiveSceneViewCamera());
            else
                GPUIRenderingSystem.Editor_RemoveSceneViewCameraData(GetActiveSceneViewCamera());
            SetIcon(evt.newValue);
        }

        Camera GetActiveSceneViewCamera()
        {
            if (containerWindow is SceneView view)
                return view.camera;
            return null;
        }

        void SetIcon(bool value)
        {
            if (value)
                icon = OnIcon;
            else
                icon = OffIcon;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class GPUISceneViewOverlayOcclusionCullingDebugToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        public const string id = "GPUISceneViewOverlay/OcclusionCullingDebugToggle";
        private Texture2D OnIcon;
        private Texture2D OffIcon;

        public EditorWindow containerWindow { get; set; }

        public GPUISceneViewOverlayOcclusionCullingDebugToggle()
        {
            text = null;
            tooltip = "GPUI Pro, enable/disable HiZ Depth Texture preview.";
            value = false;
            style.maxWidth = 20;
            style.minWidth = 20;
            this.SetVisible(false);
            _isToggleVisible = false;
            _isPreviewActive = false;

            try
            {
                OnIcon = EditorGUIUtility.IconContent("DebuggerAttached").image as Texture2D;
                OffIcon = EditorGUIUtility.IconContent("DebuggerDisabled").image as Texture2D;
            }
            catch (Exception e) { Debug.LogException(e); }

            SetIcon(value);
            this.RegisterValueChangedCallback(ChangeDebugMode);
            this.RegisterCallback<AttachToPanelEvent>(OnAttach);
            this.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            value = _isPreviewActive;
            foreach (var item in Children())
            {
                item.style.paddingLeft = 2;
                item.style.paddingRight = 0;
                item.style.maxWidth = 20;
                item.style.minWidth = 20;
            }
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void ChangeDebugMode(ChangeEvent<bool> evt)
        {
            _isPreviewActive = value;
            SetIcon(evt.newValue);
        }

        void SetIcon(bool value)
        {
            if (value)
                icon = OnIcon;
            else
                icon = OffIcon;
        }

        private bool _isToggleVisible;
        private bool _isPreviewActive;
        private float _exposure = 5f;
        private float _scale = 1.0f;
        private int _mip = 0;
        private static readonly Color _backgroundColor = new Color(0, 0, 0, 0.7f);

        private void HideToggle()
        {
            if (_isToggleVisible)
            {
                this.SetVisible(false);
                _isToggleVisible = false;
            }
            if (_isPreviewActive)
                this.value = false;
        }

        private void OnSceneGUI(SceneView view)
        {
            if (!Application.isPlaying || containerWindow != view)
                return;
            GameObject activeGO = Selection.activeGameObject;
            if (activeGO == null || !activeGO.TryGetComponent(out GPUICamera gpuiCamera))
            {
                HideToggle();
                return;
            }
            GPUICameraData cameraData = gpuiCamera.GetCameraData();
            if (cameraData == null || cameraData.OcclusionCullingData == null || cameraData.OcclusionCullingData.HiZDepthTexture == null)
            {
                HideToggle();
                return;
            }
            _isToggleVisible = true;
            this.SetVisible(true);
            if (!_isPreviewActive)
                return;

            RenderTexture renderTexture = cameraData.OcclusionCullingData.HiZDepthTexture;

            float aspectRatio = (float)renderTexture.width / renderTexture.height;
            float previewWidth = 256 * _scale; // Base width scaled
            float previewHeight = previewWidth / aspectRatio;

            Rect previewArea = new Rect(10, 10, previewWidth + 10, previewHeight + 60);

            Handles.BeginGUI();
            EditorGUI.DrawRect(previewArea, _backgroundColor);
            GUILayout.BeginArea(previewArea);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Exposure:", GUILayout.Width(70));
            _exposure = GUILayout.HorizontalSlider(_exposure, 0.1f, 10.0f, GUILayout.Width(70));
            GUILayout.Label(_exposure.ToString("F1"), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mip:", GUILayout.Width(70));
            _mip = (int)GUILayout.HorizontalSlider(_mip, 0, cameraData.OcclusionCullingData.HiZMipLevels - 1, GUILayout.Width(70));
            GUILayout.Label(_mip.ToString(), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale:", GUILayout.Width(70));
            _scale = GUILayout.HorizontalSlider(_scale, 0.5f, 3.0f, GUILayout.Width(70));
            GUILayout.Label(_scale.ToString("F1"), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            EditorGUI.DrawPreviewTexture(previewRect, renderTexture, null, ScaleMode.ScaleToFit, 0, _mip, UnityEngine.Rendering.ColorWriteMask.Red, _exposure);

            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontStyle = FontStyle.Bold };
            string sizeText = Mathf.Max(renderTexture.width >> _mip, 1) + "x" + Mathf.Max(renderTexture.height >> _mip, 1);
            if(cameraData.OcclusionCullingData.HiZTextureSize.x != renderTexture.width)
                sizeText += " (" + Mathf.Max(cameraData.OcclusionCullingData.HiZTextureSize.x >> _mip, 1) + "x" + Mathf.Max(cameraData.OcclusionCullingData.HiZTextureSize.y >> _mip, 1) + ")";
            EditorGUI.LabelField(previewRect, sizeText, style);

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}