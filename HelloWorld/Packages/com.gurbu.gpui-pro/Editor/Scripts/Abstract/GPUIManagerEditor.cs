// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace GPUInstancerPro
{
    public abstract class GPUIManagerEditor : GPUIEditor
    {
        protected GPUIManager _gpuiManager;

        protected VisualTreeAsset _managerUITemplate;
        protected VisualElement _managerHelpBoxesVE;
        protected VisualElement _managerSettingsContentVE;
        protected Foldout _managerAdvancedSettingsFoldout;
        protected VisualElement _registeredInstancesElement;
        protected Foldout _registeredInstancesFoldout;
        protected VisualElement _prototypesVE;
        protected VisualElement _prototypesContentVE;
        protected VisualElement _prototypeSettingsVE;
        protected VisualElement _prototypeButtonsVE;
        protected Foldout _prototypeSettingsFoldout;
        protected VisualElement _prototypeSettingsContentVE;
        protected VisualElement _actionButtonsVE;
        protected VisualElement _prototypeAdvancedActionsFoldoutVE;
        protected VisualElement _statisticsVE;
        protected ListView _statisticsListView;
        protected Foldout _profileFoldout;
        protected List<VisualElement> _previewVEs;
        protected ToolbarButton _statisticsTB;
        protected ToolbarButton _prototypesTB;
        protected VisualElement _prePrototypeButtonsVE;
        protected Toggle _textModeToggle;
        protected Label _statisticsCountsLabel;
        protected Toggle _showVisibilityToggle;
        protected Label _statisticsSummaryDrawCalls;
        protected Label _statisticsSummaryInstanceCount;
        protected Label _statisticsSummaryVerts;
        protected Label _statisticsSummaryVisibleCount;
        protected Label _statisticsSummaryVisibleShadowCount;
        protected Label _prototypesErrorCountLabel;
        protected Label _statisticsErrorCountLabel;
        protected VisualElement _statisticsTabWarningsVE;

        protected List<Button> _prototypeButtons;
        protected bool _isValidDrag;
        protected bool _isAttachPrefabComponent;
        protected bool _isDisablePrototypeType;

        protected SerializedProperty _prototypesSP;

        protected int _selectedCount;
        protected int _selectedIndex0;

        protected int _pickerControlID = -1;
        protected int _pickerOverwrite = -1;
        protected string _pickerObjectWarningCode = "prefabTypeWarning";
        protected bool _pickerAcceptModelPrefab;
        protected bool _pickerAcceptSkinnedMeshRenderer;

        protected bool _disableAddPrototypes;
        protected bool _disableRemovePrototypes;
        protected bool _disableDefaultRenderingWhenDisabledOption;
        protected bool _disableRenderInEditModeOption;
        protected bool _showStatisticsCountsLabel;

        private bool _billboardSettingsFoldoutValue;

        protected List<GPUIStatisticsElement> _statisticsElements;
        private Action<GPUIDataBuffer<GPUIVisibilityData>> _statisticsDataCallback;
        protected List<GPUIStatisticsError> _statisticsErrorCodes;
        protected struct GPUIStatisticsError
        {
            public int errorCode;
            public UnityEngine.Object targetObject;
            public UnityAction fixAction;
        }

        public static readonly Vector2 PREVIEW_ICON_SIZE = new Vector2(60, 60);
        private static readonly List<string> RENDER_MODE_CHOICES = new List<string>()
        {
            "Play and Edit Mode",
            "Play Mode Only",
            "Edit Mode Only"
        };

        public static Action<GameObject, bool> OnEnableGPUSkinningModifiedCallback;
        public static Action<GPUIManager, VisualElement, List<GPUIHelpBox>, Action> OnDrawGPUSkinningSettings;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiManager = target as GPUIManager;
            if (_gpuiManager != null )
            {
                if (!Application.isPlaying && GPUIRenderingSystem.IsActive)
                {
                    GPUIShaderBindings.Instance.ClearEmptyShaderInstances();
                    GPUIRenderingSystem.Instance.MaterialProvider.ClearFailedShaderConversions(); // In edit mode recheck failed shader conversions in case the shader is fixed
                }
                _gpuiManager.CheckPrototypeChanges();
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
            _managerUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIManagerUI.uxml");

            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                if (_gpuiManager.editor_selectedPrototypeIndexes[i] > _gpuiManager.GetPrototypeCount())
                {
                    _gpuiManager.editor_selectedPrototypeIndexes.RemoveAt(i);
                    i--;
                }
            }

            _prototypesSP = serializedObject.FindProperty("_prototypes");

            _statisticsDataCallback = UpdateStatisticsDataCallback;
            _statisticsErrorCodes = new List<GPUIStatisticsError>();

            // Render previews delayed otherwise Unity might not load some stuff and cause errors (e.g. Volumes)
            // Also cant render during UI draw, causes data loss
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update += CreatePreviews;

#if GPUI_CROWD
            _pickerAcceptSkinnedMeshRenderer = true;
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _gpuiManager = null;
            _prototypeButtons = null;
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update -= UpdateStatisticsData;
        }

        //private double _lastPreviewCompleteTime;
        //private const float _previewUpdateSeconds = 1f;
        protected void CreatePreviews() 
        {
            //double timeSinceStartup = EditorApplication.timeSinceStartup;
            //if (timeSinceStartup - _lastPreviewCompleteTime < _previewUpdateSeconds)
            //    return;
            bool completedPreviews = true;
            GPUIManager manager = _gpuiManager;
            bool hasPreviewVE = _previewVEs != null;
            if (manager != null)
            {
                GPUIPreviewCache.ClearEmptyPreviews();
                GPUIPreviewDrawer previewDrawer = new();
                for (int i = 0; i < manager.GetPrototypeCount(); i++)
                {
                    completedPreviews &= TryGetPreviewForPrototype(i, previewDrawer, out Texture2D prev);
                    if (hasPreviewVE && _previewVEs.Count > i && _previewVEs[i] != null)
                        _previewVEs[i].style.backgroundImage = prev;
                }
                previewDrawer.Cleanup();
            }

            if (completedPreviews)
            {
                //_lastPreviewCompleteTime = timeSinceStartup;
                //if (Application.isPlaying)
                    EditorApplication.update -= CreatePreviews;
            }
        }

        protected virtual bool TryGetPreviewForPrototype(int prototypeIndex, GPUIPreviewDrawer previewDrawer, out Texture2D preview)
        {
            preview = null;
            if (_gpuiManager == null)
                return false;
            GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
            int key = prototype.GetHashCode() + _gpuiManager.GetRendererGroupID(prototypeIndex);
            if (!GPUIPreviewCache.TryGetPreview(key, out preview) && previewDrawer != null)
            {
                if (previewDrawer.TryGetPreviewForPrototype(prototype, PREVIEW_ICON_SIZE, null, out preview))
                {
                    GPUIPreviewCache.AddPreview(key, preview);
                    return true;
                }
                return false;
            }

            return true;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            if (_gpuiManager == null)
                return;
            VisualElement rootElement = new();
            _managerUITemplate.CloneTree(rootElement);
            rootElement.Bind(serializedObject);
            contentElement.Add(rootElement);

            _managerHelpBoxesVE = rootElement.Q("ManagerHelpBoxes");
            _managerHelpBoxesVE.Clear();
            _managerHelpBoxesVE.Add(new IMGUIContainer(ShowManagerHelpBoxes));
            if (GPUIRuntimeSettings.Instance.Unsupported_Unity_Version)
                _managerHelpBoxesVE.Add(new GPUIHelpBox($"<b><size=12>Unsupported Unity Version</size></b>\nThe Unity version <b>{Application.unityVersion}</b> is not officially supported. Please use one of the following supported versions:\n • <b>2022.3 LTS</b> (2022.3.32 or later)\n • <b>6000 LTS</b> (6000.0.23 or later)", HelpBoxMessageType.Error));

            _managerSettingsContentVE = rootElement.Q("ManagerSettingsContent");
            _managerAdvancedSettingsFoldout = rootElement.Q<Foldout>("ManagerAdvancedSettingsFoldout");
            _registeredInstancesElement = rootElement.Q("RegisteredInstancesElement");
            _registeredInstancesFoldout = rootElement.Q<Foldout>("RegisteredInstancesFoldout");
            _prototypesVE = rootElement.Q("PrototypesElement");
            _prototypesTB = rootElement.Q<ToolbarButton>("PrototypesToolbarButton");
            _statisticsTB = rootElement.Q<ToolbarButton>("StatisticsToolbarButton");
            _prePrototypeButtonsVE = rootElement.Q("PrePrototypeButtons");
            _textModeToggle = rootElement.Q<Toggle>("TextModeToggle");
            _prototypesContentVE = rootElement.Q("PrototypesVE");
            _prototypeSettingsVE = _prototypesContentVE.Q("PrototypeSettings");
            _prototypeSettingsFoldout = _prototypeSettingsVE.Q<Foldout>("PrototypeSettingsFoldout");
            _prototypeSettingsContentVE = _prototypeSettingsVE.Q("PrototypeSettingsContent");
            _prototypeButtonsVE = _prototypesContentVE.Q("PrototypeButtons");
            _actionButtonsVE = _prototypeSettingsVE.Q("ActionButtons");
            _prototypeAdvancedActionsFoldoutVE = _prototypeSettingsVE.Q("PrototypeAdvancedActionsFoldout");
            _statisticsVE = rootElement.Q("StatisticsTabElement");
            _statisticsListView = _statisticsVE.Q<ListView>("StatisticsListView");
            _statisticsCountsLabel = _statisticsVE.Q<Label>("StatisticsCountsLabel");
            _showVisibilityToggle = rootElement.Q<Toggle>("ShowVisibilityDataToggle");
            _statisticsSummaryDrawCalls = _statisticsVE.Q<Label>("StatisticsSummaryDrawCalls");
            _statisticsSummaryInstanceCount = _statisticsVE.Q<Label>("StatisticsSummaryInstanceCount");
            _statisticsSummaryVerts = _statisticsVE.Q<Label>("StatisticsSummaryVerts");
            _statisticsSummaryVisibleCount = _statisticsVE.Q<Label>("StatisticsSummaryVisibleCount");
            _statisticsSummaryVisibleShadowCount = _statisticsVE.Q<Label>("StatisticsSummaryVisibleShadowCount");
            _prototypesErrorCountLabel = rootElement.Q<Label>("PrototypesErrorCountLabel");
            _statisticsErrorCountLabel = rootElement.Q<Label>("StatisticsErrorCountLabel");
            _statisticsTabWarningsVE = rootElement.Q("StatisticsTabWarnings");

            _helpBoxes.Add(_statisticsVE.Q<GPUIHelpBox>("StatisticsTabHelpBox"));

            Foldout managerSettingsFoldout = rootElement.Q<Foldout>("ManagerSettingsFoldout");
            managerSettingsFoldout.value = GPUIRenderingSystem.Editor_GetUIStoredValue(_gpuiManager, KEY_UISV_isManagerSettingsFoldoutCollapsed) == 0;
            managerSettingsFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == managerSettingsFoldout) GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiManager, KEY_UISV_isManagerSettingsFoldoutCollapsed, evt.newValue ? 0 : 1); });

            _registeredInstancesElement.SetVisible(IsDrawRegisteredInstances());
            if (IsDrawRegisteredInstances())
            {
                _registeredInstancesFoldout.value = GPUIRenderingSystem.Editor_GetUIStoredValue(_gpuiManager, KEY_UISV_isRegisteredInstancesFoldoutCollapsed) == 0;
                _registeredInstancesFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == _registeredInstancesFoldout) GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiManager, KEY_UISV_isRegisteredInstancesFoldoutCollapsed, evt.newValue ? 0 : 1); });
            }

            _prototypesTB.RemoveFromClassList("unity-disabled");
            _prototypesTB.clicked += OnToolbarButtonClicked;
            _statisticsTB.clicked += OnToolbarButtonClicked;
            int activeToolbarIndex = GPUIRenderingSystem.Editor_GetUIStoredValue(_gpuiManager, KEY_UISV_activeToolbarIndex);
            OnToolbarButtonClicked();
            if (activeToolbarIndex > 0)
                OnToolbarButtonClicked();

            _showVisibilityToggle.RegisterValueChangedCallback((val) =>
            {
                UpdateStatisticsDataCallback(null);
                DrawPrototypeButtons();
            });

            _statisticsListView.makeItem = () => new GPUIStatisticsElement();
            _statisticsListView.bindItem = OnStatisticsLVBindItem;
            _statisticsListView.unbindItem = OnStatisticsLVUnbindItem;
            _statisticsListView.selectedIndicesChanged += (list) =>
            {
                _gpuiManager.editor_selectedPrototypeIndexes = new(list);
                DrawPrototypeButtons();
            };
            _statisticsListView.fixedItemHeight = 60;

            Button removeButton = _actionButtonsVE.Q<Button>("RemoveButton");
            removeButton.RegisterCallback<MouseUpEvent>(ev => RemovePrototype());
            removeButton.SetVisible(!_disableRemovePrototypes);

            _actionButtonsVE.SetVisible(!Application.isPlaying);
            bool isDrawAdvancedActions = !Application.isPlaying && HasPrototypeAdvancedActions();
            _prototypeAdvancedActionsFoldoutVE.SetVisible(isDrawAdvancedActions);

            _textModeToggle.BindProperty(serializedObject.FindProperty("editor_isTextMode"));
            _textModeToggle.RegisterValueChangedCallback((val) => { DrawPrototypeButtons(); });

            rootElement.Add(new IMGUIContainer(HandlePickerObjectSelection));

            DrawManagerSettings();
            DrawPrototypeButtons();
            if (isDrawAdvancedActions)
                _prototypeAdvancedActionsFoldoutVE.Add(new IMGUIContainer(DrawAdvancedActions));
        }

        protected virtual void OnStatisticsLVBindItem(VisualElement element, int index)
        {
            if (_gpuiManager == null)
                return;
            GPUIStatisticsElement e = element as GPUIStatisticsElement;
            GPUIPrototype p = _gpuiManager.GetPrototype(index);
            e.SetData(p, null, _gpuiManager.GetRenderKey(index), _showVisibilityToggle.value);
            GPUIPreviewDrawer previewDrawer = new();
            if (TryGetPreviewForPrototype(index, previewDrawer, out Texture2D icon))
                e.icon = icon;
            previewDrawer.Cleanup();
            _statisticsElements ??= new List<GPUIStatisticsElement>();
            if (!_statisticsElements.Contains(e))
                _statisticsElements.Add(e);
        }

        protected virtual void OnStatisticsLVUnbindItem(VisualElement element, int index)
        {
            if (_gpuiManager == null)
                return;
            GPUIStatisticsElement e = element as GPUIStatisticsElement;
            if (_statisticsElements != null && _statisticsElements.Contains(e))
                _statisticsElements.Remove(e);
        }

        private void OnToolbarButtonClicked()
        {
            if (_prototypesTB.enabledSelf) // Prototypes Tab
            {
                _prototypesTB.SetToolbarButtonActive(true);
                _statisticsTB.SetToolbarButtonActive(false);

                _prototypesVE.SetVisible(true);
                _statisticsVE.SetVisible(false);
                _textModeToggle.parent.SetVisible(true);
                _showVisibilityToggle.parent.SetVisible(false);

                _showVisibilityToggle.value = false;
                EditorApplication.update -= UpdateStatisticsData;
                GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiManager, KEY_UISV_activeToolbarIndex, 0);
            }
            else // Statistics Tab
            {
                _prototypesTB.SetToolbarButtonActive(false);
                _statisticsTB.SetToolbarButtonActive(true);

                _prototypesVE.SetVisible(false);
                _statisticsVE.SetVisible(true);
                _textModeToggle.parent.SetVisible(false);
                _showVisibilityToggle.parent.SetVisible(/*_gpuiManager.IsInitialized*/true);

                UpdateStatisticsDataCallback(null);
                DrawStatisticsSummary();
                EditorApplication.update -= UpdateStatisticsData;
                EditorApplication.update += UpdateStatisticsData;
                GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiManager, KEY_UISV_activeToolbarIndex, 1);
            }
            DrawPrototypeButtons();
        }

        private bool _visibilityDataRequested;
        protected virtual void UpdateStatisticsData()
        {
            if (!_statisticsTB.enabledSelf && _statisticsListView != null && GPUIRenderingSystem.IsActive)
            {
                if (!_showVisibilityToggle.value || _visibilityDataRequested) return;

                GPUICameraData cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetSceneViewCameraData();
                if (Application.isPlaying)
                    cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetFirstValue();
                if (cameraData != null)
                    _visibilityDataRequested = cameraData.GetVisibilityBuffer().AsyncDataRequest(_statisticsDataCallback, false);
                //_statisticsListView.Rebuild();
            }
            else
                EditorApplication.update -= UpdateStatisticsData;
        }

        protected virtual void UpdateStatisticsDataCallback(GPUIDataBuffer<GPUIVisibilityData> buffer)
        {
            _visibilityDataRequested = false;
            DrawStatisticsSummary();
            if (_statisticsElements == null)
                return;
            foreach (GPUIStatisticsElement statistics in _statisticsElements)
                statistics.UpdateVisibilityData(_showVisibilityToggle.value);
        }

        protected virtual void DrawStatisticsSummary()
        {
            if (_gpuiManager == null)
                return;

            uint drawCalls = 0;
            uint shadowDrawCalls = 0;
            int instanceCount = 0;
            long verts = 0;
            uint visibleCount = 0;
            uint visibleShadowCount = 0;

            _statisticsSummaryVerts.parent.SetVisible(_showVisibilityToggle.value);

            GPUICameraData cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetSceneViewCameraData();
            if (Application.isPlaying)
                cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetFirstValue();
            if (_gpuiManager.IsInitialized && cameraData != null)
            {
                int prototypeCount = _gpuiManager.GetPrototypeCount();
                for (int i = 0; i < prototypeCount; i++)
                {
                    if (!GPUIRenderingSystem.TryGetRenderSourceGroup(_gpuiManager.GetRenderKey(i), out GPUIRenderSourceGroup rsg))
                        continue;
                    if (rsg.lodRenderStatistics == null || rsg.Profile == null)
                        continue;
                    bool hasShadow = rsg.Profile.isShadowCasting;
                    instanceCount += rsg.InstanceCount;

                    if (_showVisibilityToggle.value)
                    {
                        var visibilityData = cameraData.GetVisibilityBuffer().GetRequestedData();
                        if (visibilityData.IsCreated && cameraData.TryGetVisibilityBufferIndex(rsg, out int visibilityBufferIndex))
                        {
                            for (int l = 0; l < rsg.lodRenderStatistics.Length; l++)
                            {
                                if (rsg.InstanceCount == 0) continue;

                                uint visibleLODCount = visibilityData[visibilityBufferIndex + l].visibleCount;
                                visibleCount += visibleLODCount;
                                drawCalls += rsg.lodRenderStatistics[l].drawCount;
                                verts += visibleLODCount * (int)rsg.lodRenderStatistics[l].vertexCount;

                                if (hasShadow)
                                {
                                    visibleLODCount = visibilityData[visibilityBufferIndex + rsg.lodRenderStatistics.Length + l].visibleCount;
                                    visibleShadowCount += visibleLODCount;
                                    shadowDrawCalls += rsg.lodRenderStatistics[l].shadowDrawCount;
                                    verts += visibleLODCount * rsg.lodRenderStatistics[l].shadowVertexCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int l = 0; l < rsg.lodRenderStatistics.Length; l++)
                        {
                            if (rsg.InstanceCount > 0)
                            {
                                drawCalls += rsg.lodRenderStatistics[l].drawCount;
                                shadowDrawCalls += rsg.lodRenderStatistics[l].shadowDrawCount;
                            }
                        }
                    }
                }
            }

            _statisticsSummaryDrawCalls.text = "Draw calls: " + (drawCalls + shadowDrawCalls) + (shadowDrawCalls == 0 ? "" : " <size=10>[S: " + shadowDrawCalls+"]</size>");
            _statisticsSummaryInstanceCount.text = "Instance c.: " + instanceCount.FormatNumberWithSuffix();

            if (_showVisibilityToggle.value)
            {
                _statisticsSummaryVerts.text = "Verts: " + verts.FormatNumberWithSuffix();
                _statisticsSummaryVisibleCount.text = "Visible: " + visibleCount.FormatNumberWithSuffix();
                _statisticsSummaryVisibleShadowCount.text = "V. Shadow: " + visibleShadowCount.FormatNumberWithSuffix();
            }
        }

        private void ShowManagerHelpBoxes()
        {
            if (Application.isPlaying)
            {
                if (GPUIRenderingSystem.IsActive && GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Count > 0 && GPUIRenderingSystem.Instance.CameraDataProvider.Count == 0)
                {
                    GPUIEditorUtility.DrawIMGUIErrorMessage(-101, _gpuiManager);
                }
            }
        }

        #region Manager Settings

        protected virtual void DrawManagerSettings()
        {
            _managerSettingsContentVE.Clear();
            _gpuiManager.IsValid(false);

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("isDontDestroyOnLoad")));
            DrawDefaultProfiles();
            if (!_disableDefaultRenderingWhenDisabledOption)
                _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("isEnableDefaultRenderingWhenDisabled")));

            if (_gpuiManager.CanRenderInEditMode() && !_disableRenderInEditModeOption && !Application.isPlaying)
            {
                GPUIEditorTextUtility.TryGetGPUIText("managerRenderMode", out GPUIEditorTextUtility.GPUIText gpuiText);
                PopupField<string> renderModePopup = new PopupField<string>(gpuiText.title, RENDER_MODE_CHOICES, GetRenderModeValue())
                {
                    tooltip = gpuiText.tooltip
                };
                renderModePopup.RegisterValueChangedCallback(OnRenderModeValueChanged);
                GPUIEditorUtility.RegisterResizeLabelEvent(renderModePopup);
                _managerSettingsContentVE.Add(renderModePopup);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _managerSettingsContentVE);
            }

            GPUIEditorUtility.DrawErrorMessage(_managerSettingsContentVE, _gpuiManager.errorCode, null, _gpuiManager.errorFixAction != null ? () => { _gpuiManager.errorFixAction.Invoke(); serializedObject.Update(); DrawManagerSettings(); } : null);

            DrawManagerAdvancedSettings();
        }

        private int GetRenderModeValue()
        {
            bool disablePlayModeRendering = serializedObject.FindProperty("_disablePlayModeRendering").boolValue;
            bool isRenderInEditMode = serializedObject.FindProperty("editor_isRenderInEditMode").boolValue;

            if (!disablePlayModeRendering && isRenderInEditMode)
                return 0;
            else if (!disablePlayModeRendering && !isRenderInEditMode)
                return 1;
            else if (disablePlayModeRendering && isRenderInEditMode)
                return 2;
            return 0;
        }

        private void OnRenderModeValueChanged(ChangeEvent<string> evt)
        {
            SerializedProperty disablePlayModeRenderingSP = serializedObject.FindProperty("_disablePlayModeRendering");
            SerializedProperty isRenderInEditModeSP = serializedObject.FindProperty("editor_isRenderInEditMode");
            bool isRenderInEditMode = isRenderInEditModeSP.boolValue;
            if (evt.newValue == RENDER_MODE_CHOICES[0])
            {
                disablePlayModeRenderingSP.boolValue = false;
                isRenderInEditModeSP.boolValue = true;
            }
            else if (evt.newValue == RENDER_MODE_CHOICES[1])
            {
                disablePlayModeRenderingSP.boolValue = false;
                isRenderInEditModeSP.boolValue = false;
            }
            else if (evt.newValue == RENDER_MODE_CHOICES[2])
            {
                disablePlayModeRenderingSP.boolValue = true;
                isRenderInEditModeSP.boolValue = true;
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            if (!isRenderInEditMode && isRenderInEditModeSP.boolValue)
                _gpuiManager.Initialize();
            else if (isRenderInEditMode && !isRenderInEditModeSP.boolValue)
                _gpuiManager.Dispose();
            DrawManagerSettings();
        }

        protected virtual void DrawDefaultProfiles()
        {
            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("defaultProfile")));
        }

        #endregion Manager Settings

        #region Prototype Buttons

        protected virtual void DrawPrototypeButtons()
        {
            if (_gpuiManager == null)
                return;
            int prototypeCount = _gpuiManager.GetPrototypeCount();

            DrawRegisteredInstances();

            _prePrototypeButtonsVE.Clear();
            if (_gpuiManager.IsInitialized && prototypeCount > 0)
            {
                GPUIEditorTextUtility.TryGetGPUIText("regenerateRenderersButton", out GPUIEditorTextUtility.GPUIText gpuiText);
                Button regenerateRenderersButton = new()
                {
                    text = gpuiText.title,
                    focusable = false,
                    tooltip = gpuiText.tooltip
                };
                regenerateRenderersButton.AddToClassList("gpui-pre-prototype-button");
                regenerateRenderersButton.RegisterCallback<ClickEvent>((evt) => { GPUIRenderingSystem.RegenerateRenderers(); });
                _prePrototypeButtonsVE.Add(regenerateRenderersButton);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _prePrototypeButtonsVE);
            }

            _statisticsErrorCodes.Clear();
            _statisticsTabWarningsVE.Clear();
            if (!_statisticsTB.enabledSelf)
                DrawStatisticsSummary();
            if (_showVisibilityToggle.value)
                GPUIEditorUtility.DrawErrorMessage(_statisticsTabWarningsVE, -102, null, null);
            _statisticsCountsLabel.SetVisible(_gpuiManager.IsInitialized || _showStatisticsCountsLabel);

            _statisticsListView.itemsSource = new string[_gpuiManager.GetPrototypeCount()];
            _statisticsListView.SetSelectionWithoutNotify(_gpuiManager.editor_selectedPrototypeIndexes);
            _statisticsListView.Rebuild();

            _prototypeButtonsVE.Clear();

            _prototypeButtons = new();
            _previewVEs = new();
            _gpuiManager.CheckPrototypeChanges();

            AnalyzeManager();
            int prototypeErrorCount = 0;
            for (int i = 0; i < prototypeCount; i++)
            {
                GPUIPrototype prototype = _gpuiManager.GetPrototype(i);
                int index = i;
                Button prototypeButton = new()
                {
                    focusable = false,
                    tooltip = prototype.ToString()
                };
                prototypeButton.AddToClassList("gpui-prototype-button" + (_gpuiManager.editor_isTextMode ? "-textMode" : ""));
                prototypeButton.RegisterCallback<ClickEvent>((evt) => SetPrototypeSelected(evt, index));
                prototypeButton.RegisterCallback<MouseDownEvent>((evt) => OnPrototypeButtonMouseDown(evt, index));
                _prototypeButtonsVE.Add(prototypeButton);
                if (prototype.errorCode != 0)
                {
                    if (_gpuiManager.editor_selectedPrototypeIndexes.Contains(index))
                        prototypeButton.AddToClassList("gpui-prototype-button-error-selected");
                    else
                        prototypeButton.AddToClassList("gpui-prototype-button-error");
                    prototypeErrorCount++;
                }
                else if (_gpuiManager.editor_selectedPrototypeIndexes.Contains(index))
                    prototypeButton.AddToClassList("gpui-prototype-button-selected");

                if (_gpuiManager.editor_isTextMode)
                {
                    Label textVE = new(prototype.ToString());
                    prototypeButton.Add(textVE);
                }
                else
                {
                    VisualElement previewVE = new();
                    TryGetPreviewForPrototype(i, null, out Texture2D prev);
                    previewVE.style.backgroundImage = prev;
                    previewVE.AddToClassList("gpui-prototype-button-preview");
                    _previewVEs.Add(previewVE);
                    prototypeButton.Add(previewVE);

                    string prototypeName = prototype.ToString();
                    Label textVE = new(prototypeName);
                    textVE.AddToClassList("gpui-prototype-button-label");
                    prototypeButton.Add(textVE);
                }
                _prototypeButtons.Add(prototypeButton);

                AnalyzePrototype(i);
            }

            if (!_disableAddPrototypes)
            {
                VisualElement addPrototypeVE = new VisualElement();

                Button addPrototypeButton = new()
                {
                    text = _gpuiManager.editor_isTextMode ? "<size=12>Add </size><size=8>Click/Drop</size>": "<size=12>Add</size>\n<size=8>Click/Drop</size>",
                    focusable = false
                };
                addPrototypeButton.AddToClassList(_gpuiManager.editor_isTextMode ? "gpui-prototype-add-button-textMode" : "gpui-prototype-add-button");
                addPrototypeButton.RegisterCallback<DragPerformEvent>(OnAddButtonDragPerformEvent);
                addPrototypeButton.RegisterCallback<DragEnterEvent>(OnAddButtonDragEnterEvent);
                addPrototypeButton.RegisterCallback<DragLeaveEvent>(OnAddButtonDragLeaveEvent);
                addPrototypeButton.RegisterCallback<DragUpdatedEvent>(OnAddButtonDragUpdatedEvent);
                addPrototypeButton.RegisterCallback<DragExitedEvent>(OnAddButtonDragExitedEvent);
                addPrototypeButton.RegisterCallback<ClickEvent>(OnAddButtonClickEvent);

                //Button addMultiPrototypeButton = new()
                //{
                //    text = "<size=10>Multi. Add</size>",
                //    focusable = false
                //};
                //addMultiPrototypeButton.AddToClassList("gpui-prototype-multiadd-button");
                //addMultiPrototypeButton.RegisterCallback<ClickEvent>(OnAddMultiButtonClickEvent);

                addPrototypeVE.Add(addPrototypeButton);
                //addPrototypeVE.Add(addMultiPrototypeButton);
                addPrototypeVE.SetVisible(!Application.isPlaying);

                _prototypeButtonsVE.Add(addPrototypeVE);
            }

            if (!_statisticsTB.enabledSelf)
            {
                foreach (GPUIStatisticsError error in _statisticsErrorCodes)
                    GPUIEditorUtility.DrawErrorMessage(_statisticsTabWarningsVE, error.errorCode, error.targetObject, error.fixAction);
            }

            if (prototypeErrorCount > 0)
            {
                _prototypesErrorCountLabel.text = prototypeErrorCount.ToString();
                _prototypesErrorCountLabel.parent.SetVisible(true);
            }
            else
                _prototypesErrorCountLabel.parent.SetVisible(false);

            if (_statisticsErrorCodes.Count > 0)
            {
                _statisticsErrorCountLabel.text = _statisticsErrorCodes.Count.ToString();
                _statisticsErrorCountLabel.parent.SetVisible(true);
            }
            else
                _statisticsErrorCountLabel.parent.SetVisible(false);

            DrawPrototypeSettings();
        }

        protected virtual void AnalyzeManager() { }

        protected virtual void AnalyzePrototype(int prototypeIndex) { }

        protected bool ContainsStatisticsErrorCode(int errorCode)
        {
            foreach (var item in _statisticsErrorCodes)
            {
                if (item.errorCode == errorCode)
                    return true;
            }
            return false;
        }

        private void OnAddMultiButtonClickEvent(ClickEvent evt)
        {
            _pickerControlID = GUIUtility.GetControlID(FocusType.Passive) + 100;
            GPUIMultiAddWindow.ShowWindow(Vector2.zero, this);
        }

        protected virtual void OnAddButtonClickEvent(ClickEvent evt)
        {
            //_pickerControlID = GUIUtility.GetControlID(FocusType.Passive) + 100;
            //ShowObjectPicker();
            GPUIObjectSelectorWindow.ShowWindow("t:prefab", true, _pickerAcceptModelPrefab, true, _pickerAcceptSkinnedMeshRenderer, OnObjectsSelected);
        }

        protected virtual void OnObjectsSelected(List<UnityEngine.Object> objects)
        {
            if (objects != null)
            {
                foreach (var o in objects)
                    AddPickerObject(o);
            }
        }

        public virtual void ShowObjectPicker()
        {
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", _pickerControlID);
        }

        public void HandlePickerObjectSelection()
        {
            if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == _pickerControlID)
                {
                    AddPickerObject(EditorGUIUtility.GetObjectPickerObject(), _pickerOverwrite);
                }
                _pickerControlID = -1;
                _pickerOverwrite = -1;
            }
        }

        public virtual bool AddPickerObject(UnityEngine.Object pickerObject, int overwriteIndex = -1)
        {
            if (pickerObject == null)
                return false;

            if (!_gpuiManager.CanAddObjectAsPrototype(pickerObject)) // Already added
                return false;

            int prototypeIndex = 0;
            serializedObject.ApplyModifiedProperties();

            if (pickerObject is GameObject)
            {
                if (!GPUIEditorUtility.IsPrefabAsset(pickerObject, out GameObject prefabObject, _pickerObjectWarningCode, _pickerAcceptModelPrefab))
                    return false;

                if (!_gpuiManager.CanAddObjectAsPrototype(prefabObject)) // Already added
                    return false;

                if (_isAttachPrefabComponent)
                    AttachPrefabComponent(prefabObject);

                Undo.RecordObject(_gpuiManager, "Add prototype");

                if (overwriteIndex >= 0)
                {
                    prototypeIndex = overwriteIndex;
                    _gpuiManager.GetPrototype(prototypeIndex).prefabObject = prefabObject;
                }
                else
                {
                    prototypeIndex = _gpuiManager.GetPrototypeCount();
                    _gpuiManager.AddPrototype(new GPUIPrototype(prefabObject, _gpuiManager.GetDefaultProfile(prefabObject)));
                }
            }
            else if (pickerObject is GPUILODGroupData lodGroupData)
            {
                Undo.RecordObject(_gpuiManager, "Add prototype");

                if (overwriteIndex >= 0)
                {
                    prototypeIndex = overwriteIndex;
                    _gpuiManager.GetPrototype(prototypeIndex).gpuiLODGroupData = lodGroupData;
                }
                else
                {
                    prototypeIndex = _gpuiManager.GetPrototypeCount();
                    _gpuiManager.AddPrototype(new GPUIPrototype(lodGroupData, _gpuiManager.GetDefaultProfile()));
                }
            }

            serializedObject.Update();
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update += CreatePreviews;
            DrawPrototypeButtons();
            if (prototypeIndex >= 0)
                SetPrototypeSelected(null, prototypeIndex);

            return true;
        }

        protected virtual void AttachPrefabComponent(GameObject prefabObject) { }

        protected virtual void RevertAllPrefabComponents() { }

        private bool HasPrototypeError(int prototypeIndex)
        {
            GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
            return prototype != null && prototype.errorCode != 0;
        }

        private bool IsNullPrototype(int prototypeIndex)
        {
            return _gpuiManager.GetPrototype(prototypeIndex) == null;
        }

        private void SetPrototypeSelectedCallback()
        {
            DrawPrototypeSettings();
            _statisticsListView.SetSelectionWithoutNotify(_gpuiManager.editor_selectedPrototypeIndexes);
        }

        protected virtual void SetPrototypeSelected(ClickEvent evt, int index)
        {
            GPUIEditorUtility.SetPrototypeSelected(evt, index, _gpuiManager.GetPrototypeCount(), _gpuiManager.editor_selectedPrototypeIndexes, _prototypeButtons, DrawPrototypeButtons, HasPrototypeError, IsNullPrototype, SetPrototypeSelectedCallback);
        }

        protected virtual void OnPrototypeButtonMouseDown(MouseDownEvent evt, int index)
        {
            if (evt != null && evt.button == (int)MouseButton.RightMouse)
            {
                GenericMenu genericMenu = new GenericMenu();
                genericMenu.AddItem(new GUIContent("Remove Prototype"), false, () => { RemovePrototype(index); });
                genericMenu.ShowAsContext();
            }
        }

        #region Drag/Drop

        private void OnAddButtonDragExitedEvent(DragExitedEvent evt)
        {
            _isValidDrag = false;
        }

        private void OnAddButtonDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (_isValidDrag)
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnAddButtonDragLeaveEvent(DragLeaveEvent evt)
        {
            _isValidDrag = false;
        }

        private void OnAddButtonDragEnterEvent(DragEnterEvent evt)
        {
            _isValidDrag = false;
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                UnityEngine.Object obj = DragAndDrop.objectReferences[0];
                _isValidDrag = _gpuiManager.CanAddObjectAsPrototype(obj);
            }
        }

        private void OnAddButtonDragPerformEvent(DragPerformEvent evt)
        {
            if (_isValidDrag)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    AddPickerObject(DragAndDrop.objectReferences[i]);
                }
            }
        }

        #endregion Drag/Drop

        public virtual bool HasPrototypeAdvancedActions()
        {
            return false;
        }

        public virtual bool HasManagerAdvancedSettings()
        {
            return false;
        }

        protected virtual void DrawAdvancedActions() { }

        protected virtual void DrawManagerAdvancedSettings()
        {
            _managerAdvancedSettingsFoldout.Clear();
            _managerAdvancedSettingsFoldout.SetVisible(HasManagerAdvancedSettings());
        }

        protected virtual void DrawRegisteredInstances()
        {
            if (!IsDrawRegisteredInstances())
                return;
            _registeredInstancesFoldout.Clear();
            for (int i = 0; i < _gpuiManager.GetPrototypeCount(); i++)
            {
                if (i > 0)
                {
                    VisualElement border = new VisualElement();
                    border.style.borderBottomColor = Color.black;
                    border.style.borderBottomWidth = 1;
                    _registeredInstancesFoldout.Add(border);
                }

                VisualElement container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.alignContent = Align.Center;
                container.style.height = 25;
                container.style.maxHeight = 25;
                container.style.paddingLeft = 8;

                int prototypeIndex = i;
                container.Add(new IMGUIContainer(() => DrawIMGUIRegisteredInstanceCount(prototypeIndex)));

                Label label = new Label(_gpuiManager.GetPrototype(i).ToString());
                label.style.flexShrink = 1;
                label.style.flexGrow = 1;
                label.style.height = new Length(100, LengthUnit.Percent);
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                container.Add(label);

                _registeredInstancesFoldout.Add(container);
            }
        }

        private void DrawIMGUIRegisteredInstanceCount(int prototypeIndex)
        {
            EditorGUILayout.LabelField(_gpuiManager.GetRegisteredInstanceCount(prototypeIndex).FormatNumberWithSuffix(), GUILayout.Height(25), GUILayout.Width(50), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        }

        protected virtual bool IsDrawRegisteredInstances()
        {
            return _gpuiManager.IsInitialized;
        }

        #endregion Prototype Buttons

        #region Prototype Settings

        protected virtual void DrawPrototypeSettings()
        {
            if (_gpuiManager == null)
                return;
            _prototypeSettingsContentVE.Clear();
            _selectedCount = _gpuiManager.editor_selectedPrototypeIndexes.Count;
            if (_selectedCount == 0 || _gpuiManager.GetPrototypeCount() == 0)
            {
                _prototypeSettingsVE.SetVisible(false);
                return;
            }
            else
                _prototypeSettingsVE.SetVisible(true);

            _selectedIndex0 = _gpuiManager.editor_selectedPrototypeIndexes[0];
            if (_selectedCount == 1)
                _prototypeSettingsFoldout.text = _gpuiManager.GetPrototype(_selectedIndex0).ToString() + " Settings";
            else
                _prototypeSettingsFoldout.text = "Prototype Settings";

            BeginDrawPrototypeSettings();

            DrawBillboardFoldout();

            EndDrawPrototypeSettings();
        }

        protected void DrawBillboardFoldout()
        {
            if (_selectedCount == 0 || !SupportsBillboardGeneration())
                return;

            Foldout billboardSettingsFoldout = GPUIEditorUtility.DrawBoxContainer(_prototypeSettingsContentVE, "isGenerateBillboard", _billboardSettingsFoldoutValue, _helpBoxes);
            billboardSettingsFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == billboardSettingsFoldout) _billboardSettingsFoldoutValue = evt.newValue; });

            Toggle isGenerateBillboardToggle = new Toggle();
            VisualElement isGenerateBillboardVE = DrawMultiField(isGenerateBillboardToggle, _prototypesSP, "isGenerateBillboard", false);
            isGenerateBillboardToggle.label = "";
            isGenerateBillboardVE.style.position = Position.Absolute;
            isGenerateBillboardVE.style.marginLeft = GPUIEditorConstants.LABEL_WIDTH;
            isGenerateBillboardVE.style.marginTop = 5;
            billboardSettingsFoldout.parent.Add(isGenerateBillboardVE);

            bool generateBillboardValue = isGenerateBillboardToggle.value;
            if (Application.isPlaying)
                isGenerateBillboardToggle.SetEnabled(false);
            else
            {
                isGenerateBillboardToggle.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue != generateBillboardValue)
                    {
                        generateBillboardValue = evt.newValue;
                        if (GPUIRenderingSystem.IsActive)
                        {
                            for (int i = 0; i < _selectedCount; i++)
                            {
                                GPUIRenderingSystem.Instance.LODGroupDataProvider.RegenerateLODGroupData(_gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]));
                            }
                        }

                        if (!evt.newValue)
                        {
                            if (EditorUtility.DisplayDialog("Delete Billboard", "Do you wish to delete the generated billboard textures and settings?", "Delete", "No"))
                            {
                                for (int i = 0; i < _selectedCount; i++)
                                {
                                    GPUIPrototype prototype = _gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]);
                                    GPUIBillboardUtility.DeleteBillboard(prototype.billboardAsset);
                                    prototype.billboardAsset = null;
                                }
                            }
                        }
                        else if (evt.newValue)
                        {
                            for (int i = 0; i < _selectedCount; i++)
                            {
                                _gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]).GenerateBillboard(false);
                            }
                        }
                        _billboardSettingsFoldoutValue = generateBillboardValue;
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                        DrawPrototypeSettings();
                    }
                });
            }

            if (generateBillboardValue)
            {
                bool isBillboardReplaceLODCulled = false;
                GPUIPrototype prototype0 = _gpuiManager.GetPrototype(_selectedIndex0);
                if (_selectedCount > 1 || (prototype0.prefabObject != null && prototype0.prefabObject.HasComponent<LODGroup>()))
                {
                    Toggle isBillboardReplaceLODCulledToggle = new Toggle();
                    billboardSettingsFoldout.Add(DrawMultiField(isBillboardReplaceLODCulledToggle, _prototypesSP, "isBillboardReplaceLODCulled"));
                    isBillboardReplaceLODCulledToggle.RegisterValueChangedCallback(OnBillboardDistanceValueChanged);
                    isBillboardReplaceLODCulled = isBillboardReplaceLODCulledToggle.value && !isBillboardReplaceLODCulledToggle.showMixedValue;
                }
                Slider billboardDistanceSlider = new Slider(0f, 1f);
                billboardDistanceSlider.showInputField = true;
                VisualElement billboardDistanceVE = DrawMultiField(billboardDistanceSlider, _prototypesSP, "billboardDistance");
                billboardSettingsFoldout.Add(billboardDistanceVE);
                billboardDistanceVE.name = "BillboardDistancePF";
                billboardDistanceVE.SetVisible(!isBillboardReplaceLODCulled || _selectedCount > 1);
                billboardDistanceSlider.RegisterValueChangedCallback(OnBillboardDistanceValueChanged);

                if (_selectedCount == 1)
                {
                    VisualElement billboardAssetVE = new();
                    billboardAssetVE.style.marginTop = 5;
                    billboardSettingsFoldout.Add(billboardAssetVE);
                    Foldout billboardFoldout = new()
                    {
                        value = false,
                        text = "Billboard Asset"
                    };
                    float billboardTopMargin = 0;
                    billboardFoldout.style.marginTop = billboardTopMargin;
                    billboardAssetVE.Add(billboardFoldout);

                    VisualElement billboardBox = new();
                    billboardBox.style.position = Position.Absolute;
                    billboardBox.style.top = billboardTopMargin;
                    billboardBox.style.right = 0;
                    billboardBox.style.left = GPUIEditorConstants.LABEL_WIDTH - 7;
                    billboardBox.style.flexDirection = FlexDirection.Row;
                    billboardBox.style.flexGrow = 1;
                    billboardAssetVE.Add(billboardBox);

                    UnityEngine.Object billboardAsset = _prototypesSP.GetArrayElementAtIndex(_selectedIndex0).FindPropertyRelative("billboardAsset").objectReferenceValue;

                    ObjectField of = new();
                    of.objectType = typeof(GPUIBillboard);
                    of.style.flexShrink = 1;
                    of.style.flexGrow = 1;
                    of.value = billboardAsset;
                    of.RegisterValueChangedCallback((evt) =>
                    {
                        if (!Application.isPlaying)
                        {
                            SetPrototypeBillboardAsset(evt.newValue);
                            DrawBillboardFoldout(billboardFoldout, evt.newValue);
                        }
                        else
                            of.value = billboardAsset;
                    });
                    of.SetEnabled(!Application.isPlaying);
                    billboardBox.Add(of);

                    DrawBillboardFoldout(billboardFoldout, billboardAsset);
                }
            }
        }

        private void OnBillboardDistanceValueChanged(EventBase evt)
        {
            if (GPUIRenderingSystem.IsActive)
            {
                for (int i = 0; i < _selectedCount; i++)
                {
                    GPUIRenderingSystem.Instance.LODGroupDataProvider.RegenerateLODGroupData(_gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]));
                }
            }
        }

        private void CreateNewProfile()
        {
            GPUIPrototype prototype = _gpuiManager.GetPrototype(_selectedIndex0);
            GPUIProfile newProfile = GPUIProfile.CreateNewProfile(prototype.ToString(), prototype.profile);
            OnNewProfileCreated(newProfile);
            SetPrototypeProfile(newProfile);

            DrawPrototypeSettings();

            EditorGUIUtility.PingObject(newProfile);
        }

        protected virtual void OnNewProfileCreated(GPUIProfile newProfile) { }

        protected virtual void SetPrototypeProfile(UnityEngine.Object newProfile)
        {
            Undo.RecordObject(_gpuiManager, "Profile Changed");
            for (int i = 0; i < _selectedCount; i++)
            {
                _prototypesSP.GetArrayElementAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[i]).FindPropertyRelative("profile").objectReferenceValue = newProfile;
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            if (_gpuiManager.IsInitialized) // To recreate renderers with new profiles
                _gpuiManager.Initialize();
        }

        protected virtual void SetPrototypeBillboardAsset(UnityEngine.Object newBillboardAsset)
        {
            _prototypesSP.GetArrayElementAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[0]).FindPropertyRelative("billboardAsset").objectReferenceValue = newBillboardAsset;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        protected virtual void BeginDrawPrototypeSettings()
        {
            SerializedProperty prototypeSP = _prototypesSP.GetArrayElementAtIndex(_selectedIndex0);

            Toggle isPrototypeEnabledToggle = _prototypeSettingsVE.Q<Toggle>("PrototypeIsEnabledToggle");
            if (isPrototypeEnabledToggle != null)
                _prototypeSettingsVE.Remove(isPrototypeEnabledToggle);
            //while (_prototypeSettingsVE.childCount > 4)
            //{
            //    _prototypeSettingsVE.RemoveAt(4);
            //}
            isPrototypeEnabledToggle = new Toggle();
            isPrototypeEnabledToggle.name = "PrototypeIsEnabledToggle";
            isPrototypeEnabledToggle.AddToClassList("gpui-is-enabled-toggle");
            _prototypeSettingsVE.Add(DrawMultiField(isPrototypeEnabledToggle, _prototypesSP, "isEnabled", false));
            isPrototypeEnabledToggle.RegisterValueChangedCallback(OnPrototypeEnabledStatusChanged);

            if (_selectedCount != 1)
                return;

            GPUIPrototype prototype = _gpuiManager.GetPrototype(_selectedIndex0);
            GPUIEditorUtility.DrawErrorMessage(_prototypeSettingsContentVE, prototype.errorCode, prototype.prefabObject, prototype.errorFixAction != null ? () => { prototype.errorFixAction.Invoke(); DrawPrototypeButtons(); } : null);

            DrawPrototypeTypeAndObjects(prototypeSP);
        }

        protected virtual void DrawPrototypeTypeAndObjects(SerializedProperty prototypeSP)
        {
            VisualElement prototypeVE = DrawSerializedProperty(prototypeSP, "prototypeTypeAndObjects", out _);
            _prototypeSettingsContentVE.Add(prototypeVE);
        }

        private void OnPrototypeEnabledStatusChanged(ChangeEvent<bool> evt)
        {
            if (_gpuiManager == null)
                return;
            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                Debug.Assert(evt.newValue == _gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]).isEnabled, "Enabled value not equal.");
                _gpuiManager.OnPrototypeEnabledStatusChanged(_gpuiManager.editor_selectedPrototypeIndexes[i], evt.newValue);
            }
        }

        protected virtual void EndDrawPrototypeSettings()
        {
            #region Profile Settings
            VisualElement profileVE = new();
            profileVE.style.marginTop = 5;
            _prototypeSettingsContentVE.Add(profileVE);
            if (_profileFoldout == null)
            {
                _profileFoldout = new()
                {
                    value = GPUIRenderingSystem.Editor_GetUIStoredValue(_gpuiManager, KEY_UISV_isProfileSettingsFoldoutExpanded) == 1,
                    text = "Profile"
                };
                _profileFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == _profileFoldout) GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiManager, KEY_UISV_isProfileSettingsFoldoutExpanded, evt.newValue ? 1 : 0); });
            }
            float profileTopMargin = 10;
            _profileFoldout.style.marginTop = profileTopMargin;
            profileVE.Add(_profileFoldout);

            VisualElement profileBox = new();
            profileBox.style.position = Position.Absolute;
            profileBox.style.top = profileTopMargin;
            profileBox.style.right = 0;
            profileBox.style.left = GPUIEditorConstants.LABEL_WIDTH;
            profileBox.style.flexDirection = FlexDirection.Row;
            profileVE.Add(profileBox);
            if (GPUIEditorTextUtility.TryGetGPUIText("profile", out var gpuiText))
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, profileVE);

            UnityEngine.Object profile0 = _prototypesSP.GetArrayElementAtIndex(_selectedIndex0).FindPropertyRelative("profile").objectReferenceValue;
            bool isProfileMixed = false;
            for (int i = 1; i < _selectedCount; i++)
            {
                if (_prototypesSP.GetArrayElementAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[i]).FindPropertyRelative("profile").objectReferenceValue != profile0)
                    isProfileMixed = true;
            }
            ObjectField of = new();
            of.objectType = typeof(GPUIProfile);
            //of.style.width = new Length(70, LengthUnit.Percent);
            of.style.flexGrow = 1;
            of.style.flexShrink = 1;
            if (!isProfileMixed)
                of.value = profile0;
            of.showMixedValue = isProfileMixed;
            of.RegisterValueChangedCallback((evt) =>
            {
                SetPrototypeProfile(evt.newValue);
                DrawProfileFoldout(evt.newValue);
            });
            profileBox.Add(of);
            of.SetEnabled(!Application.isPlaying);

            if (!Application.isPlaying)
            {
                Button createProfileButton = new(CreateNewProfile);
                createProfileButton.text = "<b>+</b>Create";
                createProfileButton.enableRichText = true;
                createProfileButton.style.marginLeft = 10;
                createProfileButton.style.backgroundColor = GPUIEditorConstants.Colors.green;
                createProfileButton.style.color = Color.white;
                createProfileButton.focusable = false;
                profileBox.Add(createProfileButton);
            }

            if (!isProfileMixed)
                DrawProfileFoldout(profile0);
            #endregion Profile Settings
        }

        protected virtual void DrawProfileFoldout(UnityEngine.Object profileObject)
        {
            _profileFoldout.Clear();
            if (profileObject != null)
            {
                GPUIProfileEditor.DrawContentGUI(_profileFoldout, new SerializedObject(profileObject), _helpBoxes);

                _profileFoldout.Add(DrawSerializedProperty(serializedObject.FindProperty("editor_isRollbackRuntimeProfileChanges"), out PropertyField editor_isRollbackRuntimeProfileChangesPF));
                editor_isRollbackRuntimeProfileChangesPF.SetEnabled(!Application.isPlaying);
            }
        }

        protected virtual void DrawBillboardFoldout(Foldout billboardFoldout, UnityEngine.Object billboardAsset)
        {
            billboardFoldout.Clear();
            if (billboardAsset != null)
                GPUIBillboardEditor.DrawContentGUI(billboardFoldout, new SerializedObject(billboardAsset), _helpBoxes);
        }

        protected virtual void RemovePrototype()
        {
            if (_gpuiManager.editor_selectedPrototypeIndexes.Count == 0 || _gpuiManager.GetPrototypeCount() == 0)
                return;

            string selectedPrototypesText = "";
            int c = 0;
            foreach (int i in _gpuiManager.editor_selectedPrototypeIndexes)
            {
                selectedPrototypesText += "\n\"" + _gpuiManager.GetPrototype(i) + "\"";
                c++;
                if (c > 5)
                {
                    selectedPrototypesText += "\n...";
                    break;
                }
            }

            if (EditorUtility.DisplayDialog("Remove Confirmation", "Are you sure you want to remove the prototype from prototype list?" + selectedPrototypesText, "Remove From List", "Cancel"))
            {
                Undo.RecordObject(_gpuiManager, "Remove Prototype");
                _gpuiManager.editor_selectedPrototypeIndexes.Sort();
                for (int i = _gpuiManager.editor_selectedPrototypeIndexes.Count - 1; i >= 0; i--)
                    _gpuiManager.RemovePrototypeAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[i]);
                _gpuiManager.editor_selectedPrototypeIndexes.Clear();
                DrawPrototypeButtons();
            }
        }

        protected virtual void RemovePrototype(int index)
        {
            if (_gpuiManager.GetPrototypeCount() <= index)
                return;

            string selectedPrototypesText = "\n\"" + _gpuiManager.GetPrototype(index) + "\"";

            if (EditorUtility.DisplayDialog("Remove Confirmation", "Are you sure you want to remove the prototype from prototype list?" + selectedPrototypesText, "Remove From List", "Cancel"))
            {
                Undo.RecordObject(_gpuiManager, "Remove Prototype");
                _gpuiManager.RemovePrototypeAtIndex(index);
                _gpuiManager.editor_selectedPrototypeIndexes.Clear();
                DrawPrototypeButtons();
            }
        }

        public virtual bool SupportsBillboardGeneration()
        {
            return false;
        }

        #endregion Prototype Settings

        public VisualElement DrawMultiField<T>(BaseField<T> field, SerializedProperty arrayProp, string subPropPath, bool addHelpText = true)
        {
            return DrawMultiField(field, arrayProp, _gpuiManager.editor_selectedPrototypeIndexes, subPropPath, addHelpText);
        }

        public GPUIManager GetManager()
        {
            return _gpuiManager;
        }
    }
}
