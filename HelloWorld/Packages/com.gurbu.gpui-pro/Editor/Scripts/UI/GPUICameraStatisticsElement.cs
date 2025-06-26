// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class GPUICameraStatisticsElement : VisualElement
    {
        public GPUICameraData CameraData { get; private set; }
        public bool ShowVisibilityData => _showVisibilityToggle == null ? false : _showVisibilityToggle.value;

        private ObjectField _cameraField;
        private Toggle _showVisibilityToggle;
        private Label _renderToSceneViewLabel;
        private Toggle _renderToSceneViewToggle;
        private Label _drawCallsLabel;
        private Label _drawCallsShadowLabel;
        private Label _bufferSizeLabel;
        private Label _vertexCountLabel;
        private Label _visibleShadowCountLabel;
        private Label _visibleCountLabel;

        private int _drawCallCount = -1;
        private int _shadowDrawCallCount = -1;
        private int _bufferSize = -1;
        private const string LabelNA = "N/A";

        private Action<GPUIDataBuffer<GPUIVisibilityData>> _statisticsDataCallback;

        public GPUICameraStatisticsElement()
        {
            VisualElement rootElement = new();
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIDebuggerCameraUI.uxml");
            template.CloneTree(rootElement);
            Add(rootElement);

            _cameraField = rootElement.Q<ObjectField>("CameraField");
            _cameraField.SetEnabled(false);

            _showVisibilityToggle = rootElement.Q<Toggle>("ShowVisibilityToggle");
            _renderToSceneViewLabel = rootElement.Q<Label>("SceneViewRenderingLabel");
            _renderToSceneViewToggle = rootElement.Q<Toggle>("SceneViewRenderingToggle");
            _renderToSceneViewToggle.SetEnabled(Application.isPlaying);

            _drawCallsLabel = rootElement.Q<Label>("DrawCallsValue");
            _bufferSizeLabel = rootElement.Q<Label>("BufferSizeValue");
            _drawCallsShadowLabel = rootElement.Q<Label>("ShadowCastersValue");
            _vertexCountLabel = rootElement.Q<Label>("VertsValue");
            _visibleCountLabel = rootElement.Q<Label>("VisibleValue");
            _visibleShadowCountLabel = rootElement.Q<Label>("VisibleShadowValue");

            _showVisibilityToggle.RegisterValueChangedCallback(ShowVisibilityValueChanged);
            //_vertexCountLabel.parent.SetVisible(_showVisibilityToggle.value);
            //_visibleCountLabel.parent.SetVisible(_showVisibilityToggle.value);

            _statisticsDataCallback = VisibilityBufferReadback;

            _renderToSceneViewToggle.RegisterValueChangedCallback((evt) =>
            {
                if (CameraData != null) 
                    CameraData.renderToSceneView = evt.newValue;
            });
        }

        private void ShowVisibilityValueChanged(ChangeEvent<bool> evt)
        {
            //_vertexCountLabel.parent.SetVisible(evt.newValue);
            //_visibleCountLabel.parent.SetVisible(evt.newValue);
            _vertexCountLabel.text = LabelNA;
            _visibleCountLabel.text = LabelNA;
            _visibleShadowCountLabel.text = LabelNA;
        }

        public void SetData(GPUICameraData cameraData)
        {
            this.CameraData = cameraData;
            if (!Application.isPlaying || cameraData.ActiveCamera.cameraType == CameraType.SceneView)
            {
                cameraData.renderToSceneView = false;
                _renderToSceneViewToggle.value = false;
                _renderToSceneViewToggle.SetVisible(false);
                _renderToSceneViewLabel.SetVisible(false);
            }
            else
            {
                _renderToSceneViewToggle.value = cameraData.renderToSceneView;
                _renderToSceneViewToggle.SetVisible(true);
                _renderToSceneViewLabel.SetVisible(true);
            }
        }

        public void UpdateData()
        {
            if (CameraData == null)
            {
                _cameraField.value = null;
                _drawCallsLabel.text = LabelNA;
                _drawCallsShadowLabel.text = LabelNA;
                _bufferSizeLabel.text = LabelNA;
                _vertexCountLabel.text = LabelNA;
                _visibleCountLabel.text = LabelNA;
                _visibleShadowCountLabel.text = LabelNA;
                return;
            }
            _cameraField.value = CameraData.ActiveCamera;

            bool showVisibility = _showVisibilityToggle.value;

            int drawCallCount = 0;
            int shadowDrawCallCount = 0;
            int bufferSize = 0;
            foreach (var rsg in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                if (CameraData.TryGetVisibilityBufferIndex(rsg, out int visibilityBufferIndex) && rsg.lodRenderStatistics != null)
                {
                    for (int l = 0; l < rsg.lodRenderStatistics.Length; l++)
                    {
                        drawCallCount += (int)rsg.lodRenderStatistics[l].drawCount;
                        drawCallCount += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                        shadowDrawCallCount += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                    }
                    bufferSize += rsg.BufferSize;
                }
            }

            if (drawCallCount != _drawCallCount)
            {
                _drawCallsLabel.text = drawCallCount.FormatNumberWithSuffix();
                _drawCallCount = drawCallCount;
            }
            if (shadowDrawCallCount != _shadowDrawCallCount)
            {
                _drawCallsShadowLabel.text = shadowDrawCallCount.FormatNumberWithSuffix();
                _shadowDrawCallCount = shadowDrawCallCount;
            }
            if (bufferSize != _bufferSize)
            {
                _bufferSizeLabel.text = bufferSize.FormatNumberWithSuffix();
                _bufferSize = bufferSize;
            }

            if (showVisibility)
                CameraData.GetVisibilityBuffer().AsyncDataRequest(_statisticsDataCallback, false);
        }

        private void VisibilityBufferReadback(GPUIDataBuffer<GPUIVisibilityData> buffer)
        {
            if (!ShowVisibilityData)
                return;

            long totalVertexCount = 0;
            long totalVisibleCount = 0;
            long totalVisibleShadowCount = 0;
            NativeArray<GPUIVisibilityData> requestedData = buffer.GetRequestedData();
            if (!requestedData.IsCreated)
                return;
            foreach (var rsg in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                if (rsg.lodRenderStatistics == null || rsg.Profile == null || !CameraData.TryGetVisibilityBufferIndex(rsg, out int visibilityBufferIndex) || requestedData.Length <= visibilityBufferIndex) continue;

                int lodCount = rsg.lodRenderStatistics.Length;
                for (int l = 0; l < lodCount; l++)
                {
                    if (rsg.InstanceCount == 0) continue;

                    GPUIRenderStatistics renderStatistics = rsg.lodRenderStatistics[l];

                    GPUIVisibilityData visibilityData = requestedData[visibilityBufferIndex + l];
                    totalVisibleCount += visibilityData.visibleCount;

                    totalVertexCount += visibilityData.visibleCount * renderStatistics.vertexCount;
                    if (rsg.Profile.isShadowCasting)
                    {
                        GPUIVisibilityData shadowVisibilityData = requestedData[visibilityBufferIndex + lodCount + l];
                        totalVisibleShadowCount += shadowVisibilityData.visibleCount;
                        totalVertexCount += shadowVisibilityData.visibleCount * renderStatistics.shadowVertexCount;   
                    }
                }
            }
            _vertexCountLabel.text = totalVertexCount.FormatNumberWithSuffix();
            _visibleCountLabel.text = totalVisibleCount.FormatNumberWithSuffix();
            _visibleShadowCountLabel.text = totalVisibleShadowCount.FormatNumberWithSuffix();
        }
    }
}