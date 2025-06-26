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
    public partial class GPUIRenderSourceGroupElement : VisualElement
    {
        public GPUIRenderSourceGroup RenderSourceGroup { get; private set; }

        private VisualElement _prototypeIconVE;
        private ObjectField _prefabField;
        private ObjectField _lgdField;
        private IntegerField _rsgKeyField;
        private Label _bufferSizeLabel;
        private Label _instanceCountLabel;
        private ListView _sourcesListView;
        private Button _rsgActionsButton;

        private List<GPUIRenderSourceElement> _renderSourceElements;

        public Texture2D icon
        {
            set
            {
                if (_prototypeIconVE != null)
                    _prototypeIconVE.style.backgroundImage = value;
            }
        }

        public GPUIRenderSourceGroupElement()
        {
            VisualElement rootElement = new();
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIDebuggerRenderGroupUI.uxml");
            template.CloneTree(rootElement);
            Add(rootElement);

            _prototypeIconVE = rootElement.Q("PrototypeIcon");
            _prefabField = rootElement.Q<ObjectField>("PrefabField");
            _lgdField = rootElement.Q<ObjectField>("LGDField");
            _rsgKeyField = rootElement.Q<IntegerField>("RSGKey");
            _bufferSizeLabel = rootElement.Q<Label>("BufferSizeValue");
            _instanceCountLabel = rootElement.Q<Label>("InstanceCountValue");
            _sourcesListView = rootElement.Q<ListView>("SourcesListView");
            _rsgActionsButton = rootElement.Q<Button>("RSGActionsButton");

            _prefabField.SetEnabled(false);
            _lgdField.SetEnabled(false);
            //_rsgKeyField.SetEnabled(false);
        }

        public void SetData(GPUIRenderSourceGroup renderSourceGroup)
        {
            RenderSourceGroup = renderSourceGroup;

            _rsgKeyField.value = renderSourceGroup.Key;

            _lgdField.SetVisible(true);
            _prefabField.SetVisible(true);
            var lgd = renderSourceGroup.LODGroupData;
            if (lgd != null)
            {
                _lgdField.value = lgd;
                if (lgd.prototype != null && lgd.prototype.prefabObject != null)
                    _prefabField.value = lgd.prototype.prefabObject;
                else
                    _prefabField.SetVisible(false);
            }
            else
            {
                _lgdField.SetVisible(false);
                _prefabField.SetVisible(false);
            }

            _sourcesListView.makeItem = () => new GPUIRenderSourceElement();
            _sourcesListView.bindItem = OnRenderSourceLVBindItem;
            _sourcesListView.unbindItem = OnRenderSourceLVUnbindItem;
            _sourcesListView.selectedIndicesChanged += (list) => { };
            _sourcesListView.itemsSource = new string[renderSourceGroup.RenderSources.Count];
            _sourcesListView.Rebuild();

            _rsgActionsButton.clicked -= ShowActionsMenu;
            _rsgActionsButton.clicked += ShowActionsMenu;

            UpdateData();
        }

        public void UpdateData()
        {
            _rsgKeyField.value = RenderSourceGroup.Key;
            _bufferSizeLabel.text = RenderSourceGroup.BufferSize.FormatNumberWithSuffix();
            _instanceCountLabel.text = RenderSourceGroup.InstanceCount.FormatNumberWithSuffix();

            _lgdField.value = RenderSourceGroup.LODGroupData;

            if (_renderSourceElements != null)
            {
                foreach (var e in _renderSourceElements)
                    e.UpdateData();
            }
        }

        private void OnRenderSourceLVBindItem(VisualElement element, int index)
        {
            if (RenderSourceGroup.RenderSources == null || RenderSourceGroup.RenderSources.Count <= index)
                return;
            GPUIRenderSource renderSource = RenderSourceGroup.RenderSources[index];
            if (renderSource == null || renderSource.source == null)
                return;

            GPUIRenderSourceElement e = element as GPUIRenderSourceElement;
            e.SetData(renderSource);
            _renderSourceElements ??= new();
            if (!_renderSourceElements.Contains(e))
                _renderSourceElements.Add(e);
        }

        private void OnRenderSourceLVUnbindItem(VisualElement element, int index)
        {
            GPUIRenderSourceElement e = element as GPUIRenderSourceElement;
            if (_renderSourceElements != null && _renderSourceElements.Contains(e))
                _renderSourceElements.Remove(e);
        }

        private void ShowActionsMenu()
        {
            GPUIEditorUtility.ShowRSGDebugActionsMenu(RenderSourceGroup);
        }
    }
}