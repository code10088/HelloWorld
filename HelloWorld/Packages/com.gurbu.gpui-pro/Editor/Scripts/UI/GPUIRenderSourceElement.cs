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
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class GPUIRenderSourceElement : VisualElement
    {
        public GPUIRenderSource RenderSource { get; private set; }

        private ObjectField _sourceObjectField;
        private IntegerField _rsKeyField;
        private Label _startIndexLabel;
        private Label _bufferSizeLabel;

        public GPUIRenderSourceElement()
        {
            VisualElement rootElement = new();
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIDebuggerRenderSourceUI.uxml");
            template.CloneTree(rootElement);
            Add(rootElement);

            _sourceObjectField = rootElement.Q<ObjectField>("SourceObjectField");
            _rsKeyField = rootElement.Q<IntegerField>("RSKey");
            _startIndexLabel = rootElement.Q<Label>("SourceStartIndexValue");
            _bufferSizeLabel = rootElement.Q<Label>("SourceBufferSizeValue");

            _sourceObjectField.SetEnabled(false);
        }

        public void SetData(GPUIRenderSource renderSource)
        {
            RenderSource = renderSource;

            _sourceObjectField.value = renderSource.source;

            UpdateData();
        }

        public void UpdateData()
        {
            _rsKeyField.value = RenderSource.Key;
            _startIndexLabel.text = RenderSource.bufferStartIndex.FormatNumberWithSuffix();
            _bufferSizeLabel.text = RenderSource.bufferSize.FormatNumberWithSuffix();
        }
    }
}