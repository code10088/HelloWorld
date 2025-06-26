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
    public class GPUIDebuggerEditorWindow : GPUIEditorWindow
    {
        //private GPUIRenderingSystemEditor _renderingSystemEditor;
        
        private VisualTreeAsset _debuggerUITemplate;
        private VisualElement _topHelpTexts;
        private VisualElement _debuggerTopButtons;
        private VisualElement _topButtonsHelpTexts;
        private VisualElement _debuggerBottomButtons;
        private VisualElement _bottomButtonsHelpTexts;
        private VisualElement _camerasHelpTexts;
        private VisualElement _prototypesListHelpTexts;
        private ListView _camerasListView;
        private ListView _prototypesListView;
        private VisualElement _showVisibilityWarning;
        private bool _isShowVisibilityWarningVisible;

        protected List<GPUICameraStatisticsElement> _cameraElements;
        protected List<GPUIRenderSourceGroupElement> _prototypesElements;

        private bool _requireCameraListUpdate;
        private bool _requirePrototypeListUpdate;

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Show Debugger Window", validate = false, priority = 501)]
        private static void OpenWindow()
        {
            GPUIDebuggerEditorWindow wnd = GetWindow<GPUIDebuggerEditorWindow>("GPUI Debugger");
            wnd.minSize = new Vector2(300, 300);
            wnd.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _debuggerUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIDebuggerUI.uxml");
            _isScrollable = true;

            EditorApplication.update -= DebuggerEditorUpdate;
            EditorApplication.update += DebuggerEditorUpdate;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= DebuggerEditorUpdate;
        }

        private void DebuggerEditorUpdate()
        {
            if (_cameraElements == null)
                return;
            bool isAnyShowVisibility = false;
            foreach (var cameraElement in _cameraElements)
            {
                if (cameraElement != null && cameraElement.ShowVisibilityData)
                {
                    cameraElement.UpdateData();
                    isAnyShowVisibility = true;
                    if (!_isShowVisibilityWarningVisible && _showVisibilityWarning != null)
                    {
                        _showVisibilityWarning.SetVisible(true);
                        _isShowVisibilityWarningVisible = true;
                    }
                }
            }
            if (!isAnyShowVisibility && _isShowVisibilityWarningVisible && _showVisibilityWarning != null)
            {
                _showVisibilityWarning.SetVisible(false);
                _isShowVisibilityWarningVisible = false;
            }
        }

        private void UpdateDebuggerData()
        {
            if (!GPUIRenderingSystem.IsActive || _camerasListView == null || _prototypesListView == null)
                return;

            if (GetCameraCount() != _camerasListView.itemsSource.Count)
                _requireCameraListUpdate = true;
            if (GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Count != _prototypesListView.itemsSource.Count)
                _requirePrototypeListUpdate = true;


            if (!_requireCameraListUpdate && _cameraElements != null)
            {
                for (int i = 0; i < _cameraElements.Count; i++)
                {
                    var e = _cameraElements[i];
                    if (e == null || !GPUIRenderingSystem.Instance.CameraDataProvider.ContainsValue(e.CameraData))
                    {
                        _cameraElements.RemoveAt(i);
                        _requireCameraListUpdate = true;
                        break;
                    }
                    if (!e.ShowVisibilityData)
                        e.UpdateData();
                }
            }

            if (!_requirePrototypeListUpdate && _prototypesElements != null)
            {
                for (int i = 0; i < _prototypesElements.Count; i++)
                {
                    var e = _prototypesElements[i];
                    if (e == null || !GPUIRenderingSystem.Instance.RenderSourceGroupProvider.ContainsValue(e.RenderSourceGroup))
                    {
                        _prototypesElements.RemoveAt(i);
                        _requirePrototypeListUpdate = true;
                        break;
                    }
                    e.UpdateData();
                }
            }

            if (Event.current.type != EventType.Layout)
            {
                if (_requireCameraListUpdate)
                {
                    _cameraElements = null;
                    _camerasListView.itemsSource = new string[GetCameraCount()];
                    _camerasListView.Rebuild();
                    _requireCameraListUpdate = false;
                }
                if (_requirePrototypeListUpdate)
                {
                    _prototypesElements = null;
                    _prototypesListView.itemsSource = new string[GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Count];
                    _prototypesListView.Rebuild();
                    _requirePrototypeListUpdate = false;
                }
            }
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            if (_debuggerUITemplate == null)
                return;
            //contentElement.Add(new IMGUIContainer(() => DrawGPUIRenderingSystemEditor()));
            if (!GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.InitializeRenderingSystem();

            VisualElement rootElement = new();
            _debuggerUITemplate.CloneTree(rootElement);
            //rootElement.style.flexGrow = 1;
            rootElement.Add(new IMGUIContainer(UpdateDebuggerData));
            contentElement.Add(rootElement);

            _topHelpTexts = rootElement.Q("TopHelpText");
            _debuggerTopButtons = rootElement.Q("DebuggerTopButtons");
            _topButtonsHelpTexts = rootElement.Q("TopButtonsHelpText");
            _topHelpTexts = rootElement.Q("TopHelpText");
            _debuggerBottomButtons = rootElement.Q("DebuggerBottomButtons");
            _bottomButtonsHelpTexts = rootElement.Q("BottomButtonsHelpText");
            _camerasListView = rootElement.Q<ListView>("CamerasListView");
            _prototypesListView = rootElement.Q<ListView>("PrototypesListView");
            _camerasHelpTexts = rootElement.Q("CamerasHelpText");
            _prototypesListHelpTexts = rootElement.Q("PrototypesListHelpText");

            DrawDebugger();
        }

        private void DrawDebugger()
        {
            if (!GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.InitializeRenderingSystem();
            _topHelpTexts.Clear();
            GPUIEditorTextUtility.TryGetGPUIText("gpuiDebuggerWindow", out GPUIEditorTextUtility.GPUIText gpuiText);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _topHelpTexts);

            DrawDebuggerButtons();

            _camerasListView.makeItem = () => new GPUICameraStatisticsElement();
            _camerasListView.bindItem = OnCamerasLVBindItem;
            _camerasListView.unbindItem = OnCamerasLVUnbindItem;
            //_camerasListView.selectedIndicesChanged += (list) => { };
            _camerasListView.itemsSource = new string[GetCameraCount()];
            _camerasListView.Rebuild();

            _prototypesListView.makeItem = () => new GPUIRenderSourceGroupElement();
            _prototypesListView.bindItem = OnPrototypesLVBindItem;
            _prototypesListView.unbindItem = OnPrototypesLVUnbindItem;
            //_prototypesListView.selectedIndicesChanged += (list) => { };
            _prototypesListView.itemsSource = new string[GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Count];
            _prototypesListView.Rebuild();

            _camerasHelpTexts.Clear();
            GPUIEditorTextUtility.TryGetGPUIText("gpuiDebuggerCameras", out gpuiText);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _camerasHelpTexts);

            _prototypesListHelpTexts.Clear();
            GPUIEditorTextUtility.TryGetGPUIText("gpuiDebuggerPrototypes", out gpuiText);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _prototypesListHelpTexts);

//#if GPUIPRO_DEVMODE
            ObjectField activeRuntimeSettings = new ObjectField("Active Runtime Settings");
            activeRuntimeSettings.value = GPUIRuntimeSettings.Instance;
            activeRuntimeSettings.SetEnabled(false);
            activeRuntimeSettings.style.marginTop = 10;
            _prototypesListHelpTexts.Add(activeRuntimeSettings);
//#endif
        }

        private void DrawDebuggerButtons()
        {
            _debuggerTopButtons.Clear();

            GPUIEditorTextUtility.GPUIText gpuiText;
            Button button;

//#if GPUIPRO_DEVMODE
            button = new Button(DrawDebugger);
            button.text = "Refresh";
            button.focusable = false;
            button.AddToClassList("gpui-debugger-button");
            button.style.backgroundColor = GPUIEditorConstants.Colors.darkBlue;
            _debuggerTopButtons.Add(button);
//#endif

            GPUIEditorTextUtility.TryGetGPUIText("regenerateRenderersButton", out gpuiText);
            button = new Button(GPUIRenderingSystem.RegenerateRenderers);
            button.text = gpuiText.title;
            button.focusable = false;
            button.AddToClassList("gpui-debugger-button");
            button.style.backgroundColor = GPUIEditorConstants.Colors.darkBlue;
            _debuggerTopButtons.Add(button);

            _topButtonsHelpTexts.Clear();
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _topButtonsHelpTexts);
            _showVisibilityWarning = GPUIEditorUtility.CreateGPUIHelpBox(-102, null, null, HelpBoxMessageType.Warning);
            _showVisibilityWarning.SetVisible(false);
            _isShowVisibilityWarningVisible = false;
            _topButtonsHelpTexts.Add(_showVisibilityWarning);

            _debuggerBottomButtons.Clear();
            GPUIEditorTextUtility.TryGetGPUIText("disposeAllButton", out gpuiText);
            button = new Button(GPUIRenderingSystem.ResetRenderingSystem);
            button.text = gpuiText.title;
            button.focusable = false;
            button.AddToClassList("gpui-debugger-button");
            button.style.backgroundColor = GPUIEditorConstants.Colors.darkRed;
            _debuggerBottomButtons.Add(button);

            _bottomButtonsHelpTexts.Clear();
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _bottomButtonsHelpTexts);
        }

        protected virtual void OnCamerasLVBindItem(VisualElement element, int index)
        {
            if (!GPUIRenderingSystem.IsActive || GetCameraCount() < index)
                return;
            GPUICameraStatisticsElement e = element as GPUICameraStatisticsElement;
            GPUICameraData cameraData;
            cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetValueAtIndexWithEditModeCameras(index);
            if (cameraData == null)
                return;
            e.SetData(cameraData);
            _cameraElements ??= new List<GPUICameraStatisticsElement>();
            if (!_cameraElements.Contains(e))
                _cameraElements.Add(e);
        }

        protected virtual void OnCamerasLVUnbindItem(VisualElement element, int index)
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUICameraStatisticsElement e = element as GPUICameraStatisticsElement;
            if (_cameraElements != null && _cameraElements.Contains(e))
                _cameraElements.Remove(e);
        }

        protected virtual void OnPrototypesLVBindItem(VisualElement element, int index)
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIRenderSourceGroupElement e = element as GPUIRenderSourceGroupElement;
            GPUIRenderSourceGroup rsg = GPUIRenderingSystem.Instance.RenderSourceGroupProvider.GetValueAtIndex(index);
            if (rsg == null || rsg.RenderSources == null)
                return;
            e.SetData(rsg);
            TryGetPreviewForRenderSourceGroup(rsg, out Texture2D preview);
            e.icon = preview;
            _prototypesElements ??= new List<GPUIRenderSourceGroupElement>();
            if (!_prototypesElements.Contains(e))
                _prototypesElements.Add(e);
        }

        protected virtual void OnPrototypesLVUnbindItem(VisualElement element, int index)
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIRenderSourceGroupElement e = element as GPUIRenderSourceGroupElement;
            if (_prototypesElements != null && _prototypesElements.Contains(e))
                _prototypesElements.Remove(e);
        }

        protected virtual bool TryGetPreviewForRenderSourceGroup(GPUIRenderSourceGroup rsg, out Texture2D preview)
        {
            preview = null;
            if (rsg == null)
                return false;
            var lgd = rsg.LODGroupData;
            if (lgd != null && lgd.prototype != null)
            {
                int key = lgd.prototype.GetHashCode() + rsg.GroupID;
                if (!GPUIPreviewCache.TryGetPreview(key, out preview))
                {
                    GPUIPreviewDrawer previewDrawer = new();
                    if (previewDrawer.TryGetPreviewForPrototype(lgd.prototype, GPUIManagerEditor.PREVIEW_ICON_SIZE, null, out preview))
                    {
                        GPUIPreviewCache.AddPreview(key, preview);
                        return true;
                    }
                    return false;
                }
            }

            return true;
        }

        private int GetCameraCount()
        {
            return GPUIRenderingSystem.Instance.CameraDataProvider.CountWithEditModeCameras;
        }

        //private void DrawGPUIRenderingSystemEditor()
        //{
        //    if (!GPUIRenderingSystem.IsActive)
        //    {
        //        EditorGUILayout.LabelField("Rendering is inactive.");
        //        return;
        //    }
        //    if (_renderingSystemEditor == null)
        //        _renderingSystemEditor = (GPUIRenderingSystemEditor)Editor.CreateEditor(GPUIRenderingSystem.Instance);
        //    if (_renderingSystemEditor != null)
        //        _renderingSystemEditor.DrawRenderingSystem(this);
        //    else
        //        EditorGUILayout.LabelField("Rendering is inactive.");
        //}

        public override string GetTitleText()
        {
            return "GPUI Debugger";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Debugger_Window";
        }
    }
}
