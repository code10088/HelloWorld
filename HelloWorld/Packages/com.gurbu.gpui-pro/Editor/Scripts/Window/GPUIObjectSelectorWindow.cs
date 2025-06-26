// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;
using UnityEngine.Events;
using UnityEditor.UIElements;

namespace GPUInstancerPro
{
    public class GPUIObjectSelectorWindow : GPUIEditorWindow
    {
        public List<UnityEngine.Object> selectedObjects;
        private string _searchText = "";
        private List<UnityEngine.Object> _objectList;
        private List<UnityEngine.Object> _filteredList;
        private Dictionary<UnityEngine.Object, int> _instanceCounts;

        private string _filter;
        private bool _showInstanceCounts;
        private bool _allowModelPrefabs;
        private bool _allowMultiSelect;
        private bool _allowSkinnedMeshRenderer;
        private bool _allowPackagesFolder;

        private Button _selectButton;
        private Button _cancelButton;
        private ToolbarSearchField _searchField;
        private ListView _listView;
        private Label _instanceCountTitleLabel;
        private Foldout _prefabPreviewFoldout;
        private IMGUIContainer _prefabPreviewIMGUI;
        private Label _prefabPreviewLabel;
        private Button _prefabPreviewButton;
        private Toggle _packagesToggle;


        private UnityAction<List<UnityEngine.Object>> _prefabSelectionAction;

        private KeyValuePair<UnityEngine.Object, Editor> _objectEditor;

        //[MenuItem("Tools/GPU Instancer Pro/Utilities/Show Object Selector", validate = false, priority = 521)]
        //public static void ShowWindow()
        //{
        //    ShowWindow(true, true, true, false, null);
        //}

        public static void ShowWindow(string filter, bool showInstanceCounts, bool allowModelPrefabs, bool allowMultiSelect, bool allowSkinnedMeshRenderer, UnityAction<List<UnityEngine.Object>> prefabSelectionAction)
        {
            GPUIObjectSelectorWindow window = GetWindow<GPUIObjectSelectorWindow>(true, "Prefab Selector", true);
            window._filter = filter;
            window.minSize = new Vector2(400, 400);
            window._showInstanceCounts = showInstanceCounts;
            window._allowModelPrefabs = allowModelPrefabs;
            window._allowMultiSelect = allowMultiSelect;
            window._prefabSelectionAction = prefabSelectionAction;
            window._allowSkinnedMeshRenderer = allowSkinnedMeshRenderer;
            window._allowPackagesFolder = false;
            window.Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectedObjects = new();
        }

        private void Initialize()
        {
            selectedObjects = new();
            _instanceCountTitleLabel.SetVisible(_showInstanceCounts);
            if (_showInstanceCounts)
                _instanceCounts = new();
            _listView.selectionType = _allowMultiSelect ? SelectionType.Multiple : SelectionType.Single;
            _objectList = FindObjects();
            DrawObjectList();
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            VisualTreeAsset editorUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIObjectSelectorUI.uxml");
            VisualElement ve = editorUITemplate.Instantiate();
            contentElement.style.flexShrink = 1;
            contentElement.style.flexGrow = 1;
            ve.style.height = new Length(100, LengthUnit.Percent);
            contentElement.Add(ve);

            _selectButton = ve.Q<Button>("SelectButton");
            _cancelButton = ve.Q<Button>("CancelButton");
            _searchField = ve.Q<ToolbarSearchField>("ObjectSearchField");
            _listView = ve.Q<ListView>("PrefabListView");
            _instanceCountTitleLabel = ve.Q<Label>("InstanceCountTitleLabel");
            _prefabPreviewFoldout = ve.Q<Foldout>("PrefabPreviewFoldout");
            _prefabPreviewIMGUI = ve.Q<IMGUIContainer>("PrefabPreviewIMGUI");
            _prefabPreviewLabel = ve.Q<Label>("PrefabPreviewLabel");
            _prefabPreviewButton = ve.Q<Button>("PingButton");
            _packagesToggle = ve.Q<Toggle>("PackagesToggle");

            _prefabPreviewLabel.text = "";
            _prefabPreviewFoldout.SetVisible(false);
            _prefabPreviewIMGUI.onGUIHandler = DrawPreview;
            _prefabPreviewButton.clicked += () => { if (selectedObjects != null && selectedObjects.Count > 0) EditorGUIUtility.PingObject(selectedObjects[selectedObjects.Count - 1]); };

            _listView.itemsSource = null;
            _listView.makeItem = () => new GPUIObjectSelectorElement();
            _listView.bindItem = (element, index) =>
            {
                GPUIObjectSelectorElement selector = element as GPUIObjectSelectorElement;
                UnityEngine.Object o = _filteredList[index];
                selector.SetObject(o);
                if (_showInstanceCounts && _instanceCounts != null && _instanceCounts.TryGetValue(o, out int instanceCount))
                    selector.countText = instanceCount.ToString();

                var clickable2 = new Clickable(OnItemDoubleClick);
                clickable2.activators.Clear();
                clickable2.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
                selector.AddManipulator(clickable2);
            };
            _listView.selectedIndicesChanged -= OnSelectedObjectsChanged;
            _listView.selectedIndicesChanged += OnSelectedObjectsChanged;

            _selectButton.clicked += OnPrefabsSelected;
            _selectButton.SetEnabled(false);
            _selectButton.text = GetSelectString();
            _cancelButton.clicked += Close;
            _searchField.RegisterValueChangedCallback(OnSearchTextChanged);

            _packagesToggle.RegisterValueChangedCallback(OnPackagesToggleChanged);

            DrawObjectList();
        }

        private void OnItemDoubleClick(EventBase eventBase)
        {
            OnPrefabsSelected();
        }

        private void OnPackagesToggleChanged(ChangeEvent<bool> evt)
        {
            _allowPackagesFolder = evt.newValue;
            Initialize();
        }

        private void DrawPreview()
        {
            if (selectedObjects != null && selectedObjects.Count > 0)
            {
                UnityEngine.Object lastObject = selectedObjects[selectedObjects.Count - 1];
                if (_prefabPreviewFoldout.value)
                {
                    if (_objectEditor.Key != lastObject)
                    {
                        if (_objectEditor.Value != null)
                            DestroyImmediate(_objectEditor.Value);
                        _objectEditor = new(lastObject, Editor.CreateEditor(lastObject));
                    }
                    _objectEditor.Value.OnPreviewGUI(GUILayoutUtility.GetRect(128, 128), GUIStyle.none);
                    _prefabPreviewLabel.text = lastObject.name + "\n" + AssetDatabase.GetAssetPath(lastObject);
                }

                _prefabPreviewFoldout.text = lastObject.name;
                _prefabPreviewFoldout.SetVisible(true);
            }
            else
                _prefabPreviewFoldout.SetVisible(false);
        }

        private void OnSelectedObjectsChanged(IEnumerable<int> enumerable)
        {
            selectedObjects.Clear();
            foreach (int index in enumerable)
                selectedObjects.Add(_filteredList[index]);
            DrawObjectList();
            _selectButton.SetEnabled(selectedObjects.Count > 0);
            _selectButton.text = GetSelectString();
            DrawPreview();
        }

        private void DrawObjectList()
        {
            if (_objectList == null || _prefabSelectionAction == null) return;

            _filteredList = FilterObjects(_objectList, _searchText, out List<int> selectedIndices);
            _listView.itemsSource = _filteredList;
            _listView.SetSelectionWithoutNotify(selectedIndices);
            _listView.RefreshItems();
            //_listView.SetSelection(selectedIndices);
            //if (selectedIndices.Count > 0)
            //{
            //    string selection = "";
            //    for (int i = 0; i < selectedIndices.Count; i++)
            //    {
            //        selection += selectedIndices[i] + " ";
            //    }
            //    Debug.Log(selection);
            //}
        }

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            _searchText = evt.newValue;
            DrawObjectList();
        }

        private void OnPrefabsSelected()
        {
            if (selectedObjects.Count > 0 && _prefabSelectionAction != null)
                _prefabSelectionAction.Invoke(selectedObjects);
            Close();
        }

        private List<UnityEngine.Object> FindObjects()
        {
            List<string> folders = new() { "Assets" };
            if (_allowPackagesFolder)
                folders.Add("Packages");
            string[] foldersArray = folders.ToArray();
            string[] guids = AssetDatabase.FindAssets(_filter, foldersArray);
            bool isPrefab = _filter.ToLower().Contains("t:prefab");
            if (_allowModelPrefabs && isPrefab)
            {
                string[] guids1 = guids;
                string[] guids2 = AssetDatabase.FindAssets("t:model", foldersArray);
                guids = new string[guids1.Length + guids2.Length];
                Array.Copy(guids1, guids, guids1.Length);
                Array.Copy(guids2, 0, guids, guids1.Length, guids2.Length);
            }
            List<UnityEngine.Object> objectList = new List<UnityEngine.Object>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (isPrefab)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null && (prefab.GetComponentInChildren<MeshRenderer>(true) != null || (_allowSkinnedMeshRenderer && prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)))
                    {
                        objectList.Add(prefab);
                        if (_showInstanceCounts && _instanceCounts != null)
                            _instanceCounts[prefab] = GPUIPrefabUtility.FindAllInstancesOfPrefab(prefab).Length;
                    }
                }
                else
                {
                    UnityEngine.Object o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (o != null)
                    {
                        objectList.Add(o);
                        _instanceCounts[o] = 0;
                    }
                }
            }
            objectList.Sort(ObjectListSort);

            return objectList;
        }

        private int ObjectListSort(UnityEngine.Object o1, UnityEngine.Object o2)
        {
            if (_showInstanceCounts && _instanceCounts != null)
            {
                int ic1 = _instanceCounts[o1];
                int ic2 = _instanceCounts[o2];
                if (ic1 != ic2)
                    return ic2.CompareTo(ic1);
            }
                
            return o1.name.CompareTo(o2.name);
        }

        private List<UnityEngine.Object> FilterObjects(List<UnityEngine.Object> prefabs, string search, out List<int> selectedIndices)
        {
            List<UnityEngine.Object> filteredList;
            if (string.IsNullOrEmpty(search))
                filteredList = prefabs;
            else
            {
                filteredList = new();
                search = search.ToLower();
                foreach (var prefab in prefabs)
                {
                    if (prefab.name.ToLower().Contains(search))
                        filteredList.Add(prefab);
                }
                foreach (var sp in selectedObjects)
                {
                    if (!filteredList.Contains(sp))
                        filteredList.Add(sp);
                }
            }
            selectedIndices = new List<int>();
            for (int i = 0; i < filteredList.Count; i++)
            {
                if (selectedObjects.Contains(filteredList[i]))
                    selectedIndices.Add(i);
            }

            return filteredList;
        }

        private string GetSelectString()
        {
            if (selectedObjects.Count <= 1)
                return "Select";

            return "Select [" + selectedObjects.Count + "]";
        }

        public override string GetTitleText()
        {
            return "GPUI Prefab Selector";
        }

        protected override bool IsDrawHeader()
        {
            return false;
        }
    }
}
