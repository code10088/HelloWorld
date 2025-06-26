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
    public partial class GPUIStatisticsElement : VisualElement
    { 
        public static readonly string ussClassName = "gpui-statistics-element";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string countUssClassName = ussClassName + "__count";
        public static readonly string lgdUssClassName = ussClassName + "__lgd";
        public static readonly string visibilityDataUssClassName = ussClassName + "__visibilityData";
        public static readonly string visibilityDataLODUssClassName = ussClassName + "__visibilityData-lod";

        private VisualElement _iconVE;
        private Label _titleLabel;
        private Label _countLabel;
        private ObjectField _lgdField;
        private VisualElement _detailsVE;
        private VisualElement _visibilityDataVE;
        private Label _drawCallsLabel;
        private Label _vertsLabel;
        private Button _actionsButton;

        private int _renderKey;
        private int _lodCount;
        private Label[] _lodLabels;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string title
        {
            get
            {
                return _titleLabel.text;
            }
            set
            {
                _titleLabel.text = value;
            }
        }

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string countText
        {
            get
            {
                return _countLabel.text;
            }
            set
            {
                _countLabel.text = value;
            }
        }

        public Texture2D icon
        {
            set
            {
                _iconVE.style.backgroundImage = value;
            }
        }

        private bool _showVisibilityData;

        public GPUIStatisticsElement()
        {
            VisualElement rootElement = new();
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIStatisticsElementUI.uxml");
            template.CloneTree(rootElement);
            Add(rootElement);            

            _iconVE = rootElement.Q("IconVE");
            _titleLabel = rootElement.Q<Label>("TitleLabel");
            _countLabel = rootElement.Q<Label>("CountLabel");
            _lgdField = rootElement.Q<ObjectField>("LGDObjectField");
            _detailsVE = rootElement.Q("DetailsVE");
            _drawCallsLabel = rootElement.Q<Label>("DrawCallsLabel");
            _vertsLabel = rootElement.Q<Label>("VertsLabel");
            _visibilityDataVE = rootElement.Q("VisibilityDataVE");
            _actionsButton = rootElement.Q<Button>("ActionsButton");

            _lgdField.SetEnabled(false);
            _actionsButton.clicked -= ShowActionsMenu;
            _actionsButton.clicked += ShowActionsMenu;
            _actionsButton.SetVisible(false);
        }

        public void SetData(GPUIPrototype prototype, string countText, int renderKey, bool showVisibilityData)
        {
            this.title = prototype.ToString();
            this._renderKey = renderKey;

            if (GPUIRenderingSystem.TryGetLODGroupData(prototype, out GPUILODGroupData lodGroupData))
                _lodCount = lodGroupData.Length;
            else
                _lodCount = prototype.GetLODCount();

            _lodLabels = new Label[_lodCount];
            _visibilityDataVE.Clear();
            VisualElement ve = null;
            int divider = 1;
            if (_lodCount > 2)
                divider++;
            if (_lodCount > 4)
                divider++;
            if (_lodCount > 6)
                divider++;
            for (int i = 0; i < _lodCount; i++)
            {
                if (i % divider == 0)
                {
                    ve = new VisualElement();
                    ve.AddToClassList(visibilityDataLODUssClassName);
                    _visibilityDataVE.Add(ve);
                }

                Label lodVD = new Label("");
                lodVD.enableRichText = true;
                lodVD.style.width = 75;
                lodVD.style.unityTextAlign = TextAnchor.LowerLeft;
                lodVD.style.fontSize = 12;
                ve.Add(lodVD);
                _lodLabels[i] = lodVD;
            }

            _showVisibilityData = showVisibilityData;
            _delayCounter = 0;
            EditorApplication.update -= DelayedUpdate;
            EditorApplication.update += DelayedUpdate;
            UpdateVisibilityData(showVisibilityData);
            if (!string.IsNullOrEmpty(countText))
                this.countText = countText;
        }

        private int _delayCounter = 0;
        private void DelayedUpdate()
        {
            _delayCounter++;
            if (_delayCounter > 2)
            {
                EditorApplication.update -= DelayedUpdate;
                UpdateVisibilityData(_showVisibilityData);
            }
        }

        public void UpdateVisibilityData(bool showVisibilityData)
        {
            _showVisibilityData = showVisibilityData;
            _lgdField.SetVisible(false);
            _visibilityDataVE.SetVisible(false);
            _detailsVE.SetVisible(false);
            _vertsLabel.SetVisible(false);
            _actionsButton.SetVisible(false);
            if (_renderKey != 0)
            {
                if (GPUIRenderingSystem.TryGetRenderSourceGroup(_renderKey, out GPUIRenderSourceGroup rsg))
                {
                    _actionsButton.SetVisible(true);
                    GPUILODGroupData lgd = rsg.LODGroupData;
                    if (lgd == null) return;

                    _lgdField.value = lgd;
                    _lgdField.SetVisible(true);
                    countText = rsg.InstanceCount.FormatNumberWithSuffix();

                    if (rsg.lodRenderStatistics == null || lgd.Length != rsg.lodRenderStatistics.Length) return;
                    
                    int drawCallCount = 0;
                    int shadowDC = 0;
                    if (showVisibilityData)
                    {
                        long vertCount = 0;

                        GPUICameraData cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetSceneViewCameraData();
                        if (Application.isPlaying)
                            cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetFirstValue();

                        if (cameraData != null)
                        {
                            var visibilityArray = cameraData.GetVisibilityBuffer().GetRequestedData();
                            if (visibilityArray.IsCreated && cameraData.TryGetVisibilityBufferIndex(rsg, out int index))
                            {
                                for (int l = 0; l < _lodCount; l++)
                                {
                                    GPUIVisibilityData visibilityData = visibilityArray[index + l];
                                    long visibleCount = visibilityData.visibleCount;
                                    _lodLabels[l].text = "<size=10>LOD" + l + ": </size>" + visibleCount.FormatNumberWithSuffix();

                                    drawCallCount += (int)rsg.lodRenderStatistics[l].drawCount;
                                    shadowDC += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                                    vertCount += visibleCount * rsg.lodRenderStatistics[l].vertexCount;
                                    if (rsg.Profile != null && rsg.Profile.isShadowCasting)
                                        vertCount += visibilityArray[index + _lodCount + l].visibleCount * rsg.lodRenderStatistics[l].shadowVertexCount;
                                }
                            }
                        }

                        _visibilityDataVE.SetVisible(true);
                        _vertsLabel.text = "<size=10>Verts: </size>" + vertCount.FormatNumberWithSuffix();
                        _vertsLabel.SetVisible(true);
                    }
                    else
                    {
                        for (int l = 0; l < _lodCount; l++)
                        {
                            drawCallCount += (int)rsg.lodRenderStatistics[l].drawCount;
                            shadowDC += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                        }
                    }
                    _drawCallsLabel.text = "<size=10>Draw: </size>" + (drawCallCount + shadowDC) + (shadowDC == 0 ? "" : " <size=8>[S: </size><size=10>" + shadowDC + "</size><size=8>]</size>");
                    _detailsVE.SetVisible(true);
                }
            }
        }

        private void ShowActionsMenu()
        {
            if (!GPUIRenderingSystem.TryGetRenderSourceGroup(_renderKey, out var renderSourceGroup))
                return;
            GPUIEditorUtility.ShowRSGDebugActionsMenu(renderSourceGroup);
        }

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<GPUIStatisticsElement, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription _title = new UxmlStringAttributeDescription
            {
                name = "title",
                defaultValue = "Object Name"
            };

            private UxmlStringAttributeDescription _count = new UxmlStringAttributeDescription
            {
                name = "count",
                defaultValue = "1234"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                GPUIStatisticsElement element = ve as GPUIStatisticsElement;
                element.title = _title.GetValueFromBag(bag, cc);
                element.countText = _count.GetValueFromBag(bag, cc);
            }
        }
#endif
    }
}