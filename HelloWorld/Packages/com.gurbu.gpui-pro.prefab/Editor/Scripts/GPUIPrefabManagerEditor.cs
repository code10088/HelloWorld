// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.PrefabModule
{
    [CustomEditor(typeof(GPUIPrefabManager))]
    public class GPUIPrefabManagerEditor : GPUIManagerEditor
    {
        private GPUIPrefabManager _gpuiPrefabManager;
        private SerializedProperty _isFindInstancesAtInitializationPF;
        private bool _isFindInstancesAtInitializationValue;
        private bool _materialVariationsFoldoutValue;
        private bool _optionalRenderersFoldoutValue;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiPrefabManager = target as GPUIPrefabManager;

            _isAttachPrefabComponent = true;
            _isDisablePrototypeType = true;

            _isFindInstancesAtInitializationPF = serializedObject.FindProperty("isFindInstancesAtInitialization");

            OnCheckPrefabComponentsAll();
        }

        protected override void DrawManagerSettings()
        {
            base.DrawManagerSettings();

            _isFindInstancesAtInitializationValue = _isFindInstancesAtInitializationPF.boolValue;
            _managerSettingsContentVE.Add(DrawSerializedProperty(_isFindInstancesAtInitializationPF, out PropertyField isFindInstancesAtInitializationPF));
            isFindInstancesAtInitializationPF.RegisterValueChangeCallback(OnIsFindInstancesAtInitializationChanged);
            isFindInstancesAtInitializationPF.SetEnabled(!Application.isPlaying);
        }

        protected override void OnStatisticsLVBindItem(VisualElement element, int index)
        {
            if (Application.isPlaying || _isFindInstancesAtInitializationPF.boolValue)
                base.OnStatisticsLVBindItem(element, index);
            else
            {
                GPUIStatisticsElement e = element as GPUIStatisticsElement;
                GPUIPrototype p = _gpuiManager.GetPrototype(index);
                e.SetData(p, _gpuiPrefabManager.GetPrototypeData(index).GetRegisteredInstanceCount().ToString("N0"), _gpuiManager.GetRenderKey(index), _showVisibilityToggle.value);
                GPUIPreviewDrawer previewDrawer = new();
                if (TryGetPreviewForPrototype(index, previewDrawer, out Texture2D icon))
                    e.icon = icon;
                previewDrawer.Cleanup();
            }
        }

        private void OnIsFindInstancesAtInitializationChanged(SerializedPropertyChangeEvent evt)
        {
            if (Application.isPlaying || _isFindInstancesAtInitializationValue == _isFindInstancesAtInitializationPF.boolValue)
                return;

            _isFindInstancesAtInitializationValue = _isFindInstancesAtInitializationPF.boolValue;

            if (_isFindInstancesAtInitializationValue)
            {
                Undo.RecordObject(_gpuiPrefabManager.gameObject, "Remove registered instances");
                int count = _gpuiPrefabManager.GetPrototypeCount();
                for (int i = 0; i < count; i++)
                {
                    var prototypeData = _gpuiPrefabManager.GetPrototypeData(i);
                    prototypeData.registeredInstances = null;
                }
            }
            else
                RegisterInstancesInScene(_gpuiPrefabManager);
            _registeredInstancesElement.SetVisible(IsDrawRegisteredInstances());
            DrawPrototypeButtons();
        }

        protected override void DrawPrototypeButtons()
        {
            _showStatisticsCountsLabel = !_isFindInstancesAtInitializationPF.boolValue;
            base.DrawPrototypeButtons();

            if (!Application.isPlaying && !_isFindInstancesAtInitializationPF.boolValue)
            {
                Button registerInstancesButton = new()
                {
                    text = "Register Instances in Scene",
                    focusable = false
                };
                registerInstancesButton.AddToClassList("gpui-pre-prototype-button");
                registerInstancesButton.clicked += () => { RegisterInstancesInScene(_gpuiPrefabManager); };
                _prePrototypeButtonsVE.Add(registerInstancesButton);
            }
        }

        protected override void AnalyzeManager()
        {
            base.AnalyzeManager();
            if (!ContainsStatisticsErrorCode(-104) && _gpuiManager.GetPrototypeCount() > 20)
                _statisticsErrorCodes.Add(new GPUIStatisticsError()
                {
                    errorCode = -104, // too many prototypes
                });
        }

        protected override void AnalyzePrototype(int prototypeIndex)
        {
            base.AnalyzePrototype(prototypeIndex);

            if (GPUIRenderingSystem.TryGetRenderSourceGroup(_gpuiManager.GetRenderKey(prototypeIndex), out GPUIRenderSourceGroup rsg))
            {
                if (!ContainsStatisticsErrorCode(-103) && rsg.InstanceCount > 0 && rsg.InstanceCount < 50)
                    _statisticsErrorCodes.Add(new GPUIStatisticsError()
                    {
                        errorCode = -103, // low instance count
                        //targetObject = _gpuiManager.GetPrototype(prototypeIndex).prefabObject
                    });

                if (!ContainsStatisticsErrorCode(-105) && rsg.LODGroupData != null)
                {
                    int drawCount = rsg.LODGroupData.GetMeshMaterialCombinationCount();
                    if (drawCount > 20)
                    {
                        _statisticsErrorCodes.Add(new GPUIStatisticsError()
                        {
                            errorCode = -105, // container prefab
                            targetObject = _gpuiManager.GetPrototype(prototypeIndex).prefabObject
                        });
                    }
                }
            }
        }

        protected override void BeginDrawPrototypeSettings()
        {
            base.BeginDrawPrototypeSettings();

            VisualElement space = new VisualElement();
            space.style.marginTop = 5;
            _prototypeSettingsContentVE.Add(space);

            _prototypeSettingsContentVE.Add(DrawMultiField(new Toggle(), serializedObject.FindProperty("_prototypeDataArray"), "isAutoUpdateTransformData"));
            if (OnCheckPrefabComponents())
                _prototypeSettingsContentVE.Add(DrawMultiFieldWithValues(new Toggle(), GetAutoAddRemoveValues(), "isAutoAddRemove", true, OnAutoAddRemoveModified));
#if GPUI_CROWD
            if (OnEnableGPUSkinningModifiedCallback != null && HasSelectedPrototypesSkinnedMeshRenderers())
            {
                Toggle isEnableGPUSkinningToggle = new Toggle();
                _prototypeSettingsContentVE.Add(DrawMultiFieldWithValues(isEnableGPUSkinningToggle, GetEnableGPUSkinningValues(), "isEnableGPUSkinning", true, OnEnableGPUSkinningModified));
                isEnableGPUSkinningToggle.SetEnabled(!Application.isPlaying);
                OnDrawGPUSkinningSettings?.Invoke(_gpuiPrefabManager, _prototypeSettingsContentVE, _helpBoxes, DrawPrototypeSettings);
            }
#endif
            DrawMaterialVariationDefinitions();
            DrawOptionalRendererSettings();
        }

        #region Material Variations
        private void DrawMaterialVariationDefinitions()
        {
            if (_selectedCount != 1)
                return;

            GameObject prefabObject0 = _gpuiPrefabManager.GetPrototype(_selectedIndex0).prefabObject;
            GPUIMaterialVariationInstance[] materialVariationInstances = prefabObject0.GetComponents<GPUIMaterialVariationInstance>();

            int materialVariationCount = materialVariationInstances.Length;

            Foldout materialVariationsFoldout = GPUIEditorUtility.DrawBoxContainer(_prototypeSettingsContentVE, "materialVariations", _materialVariationsFoldoutValue, _helpBoxes, true);
            if (materialVariationCount > 0)
                materialVariationsFoldout.text += " [" + materialVariationCount + "]";
            materialVariationsFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == materialVariationsFoldout) _materialVariationsFoldoutValue = evt.newValue; });

            for ( int i = 0; i < materialVariationCount; i++ )
            {
                GPUIMaterialVariationInstance materialVariationInstance = materialVariationInstances[i];

                VisualElement horizontalVE = new VisualElement();
                horizontalVE.style.flexDirection = FlexDirection.Row;
                horizontalVE.SetEnabled(!Application.isPlaying);
                materialVariationsFoldout.Add(horizontalVE);

                Toggle enableToggle = new Toggle("");
                enableToggle.value = materialVariationInstance.enabled;
                enableToggle.style.marginTop = 0;
                enableToggle.style.marginBottom = 0;
                enableToggle.RegisterValueChangedCallback((evt) => OnEnableMaterialVariationClicked(evt.newValue, materialVariationInstance));
                horizontalVE.Add(enableToggle);

                ObjectField materialVariationDefinitionField = new ObjectField("");
                materialVariationDefinitionField.objectType = typeof(GPUIMaterialVariationDefinition);
                materialVariationDefinitionField.value = materialVariationInstance.variationDefinition;
                materialVariationDefinitionField.style.flexGrow = 1;
                materialVariationDefinitionField.style.flexShrink = 1;
                materialVariationDefinitionField.style.marginLeft = 15;
                materialVariationDefinitionField.style.marginRight = 10;
                materialVariationDefinitionField.RegisterValueChangedCallback((evt) => OnMaterialVariationDefinitionChanged((GPUIMaterialVariationDefinition)evt.newValue, materialVariationInstance));
                horizontalVE.Add(materialVariationDefinitionField);

                Button removeButton = new Button(() => OnRemoveMaterialVariationClicked(materialVariationInstance));
                removeButton.text = "Remove";
                removeButton.enableRichText = true;
                removeButton.style.backgroundColor = GPUIEditorConstants.Colors.darkRed;
                removeButton.style.color = Color.white;
                removeButton.focusable = false;
                horizontalVE.Add(removeButton);
            }

            Button addMaterialVariationButton = new Button(OnAddMaterialVariationClicked);
            addMaterialVariationButton.text = "<b>+</b>Create";
            addMaterialVariationButton.enableRichText = true;
            addMaterialVariationButton.style.backgroundColor = GPUIEditorConstants.Colors.green;
            addMaterialVariationButton.style.color = Color.white;
            addMaterialVariationButton.style.marginTop = 5;
            addMaterialVariationButton.style.marginBottom = 5;
            addMaterialVariationButton.focusable = false;
            addMaterialVariationButton.SetEnabled(!Application.isPlaying);
            materialVariationsFoldout.Add(addMaterialVariationButton);
        }

        private void OnEnableMaterialVariationClicked(bool enabled, GPUIMaterialVariationInstance materialVariationInstance)
        {
            materialVariationInstance.enabled = enabled;
            EditorUtility.SetDirty(materialVariationInstance);
            RevertMaterialVariationPrefabOverrides();
        }

        private void OnMaterialVariationDefinitionChanged(GPUIMaterialVariationDefinition newValue, GPUIMaterialVariationInstance materialVariationInstance)
        {
            materialVariationInstance.variationDefinition = newValue;
            EditorUtility.SetDirty(materialVariationInstance);
            RevertMaterialVariationPrefabOverrides();
        }

        private void RevertMaterialVariationPrefabOverrides()
        {
            GameObject prefabObject0 = _gpuiPrefabManager.GetPrototype(_selectedIndex0).prefabObject;
            GameObject[] prefabInstances = GPUIPrefabUtility.FindAllInstancesOfPrefab(prefabObject0);
            for (int i = 0; i < prefabInstances.Length; i++)
            {
                SerializedObject mVarInstanceSO = new SerializedObject(prefabInstances[i].GetComponent<GPUIMaterialVariationInstance>());
                PrefabUtility.RevertPropertyOverride(mVarInstanceSO.FindProperty("m_Enabled"), InteractionMode.AutomatedAction);
                PrefabUtility.RevertPropertyOverride(mVarInstanceSO.FindProperty("variationDefinition"), InteractionMode.AutomatedAction);
            }
            GPUIPrefabUtility.MergeAllPrefabInstances(prefabObject0);
        }

        private void OnAddMaterialVariationClicked()
        {
            GameObject prefabObject0 = _gpuiPrefabManager.GetPrototype(_selectedIndex0).prefabObject;

            GPUIMaterialVariationDefinition materialVariationDefinition = CreateInstance<GPUIMaterialVariationDefinition>();
            Renderer renderer = prefabObject0.GetComponentInChildren<Renderer>();
            if (renderer != null)
                materialVariationDefinition.material = renderer.sharedMaterial;

            string folderPath = prefabObject0.GetAssetFolderPath();
            string fileName = prefabObject0.name + "_GPUIVariationDefinition.asset";
            int defCount = 1;
            while (System.IO.File.Exists(folderPath + fileName))
            {
                defCount++;
                fileName = prefabObject0.name + "_GPUIVariationDefinition" + "_" + defCount + ".asset";
            }
            materialVariationDefinition.SaveAsAsset(folderPath, fileName);

            GPUIPrefabUtility.AddComponentToPrefab<GPUIMaterialVariationInstance>(prefabObject0);
            GPUIMaterialVariationInstance[] materialVariationInstances = prefabObject0.GetComponents<GPUIMaterialVariationInstance>();
            GPUIMaterialVariationInstance materialVariationInstance = materialVariationInstances[0];
            int index = 1;
            while (materialVariationInstance.variationDefinition != null && index < materialVariationInstances.Length)
            {
                materialVariationInstance = materialVariationInstances[index];
                index++;
            }
            materialVariationInstance.variationDefinition = materialVariationDefinition;
            EditorUtility.SetDirty(materialVariationInstance);
            GPUIPrefabUtility.MergeAllPrefabInstances(prefabObject0);

            DrawPrototypeSettings();
        }

        private void OnRemoveMaterialVariationClicked(GPUIMaterialVariationInstance materialVariationInstance)
        {
            if (materialVariationInstance.variationDefinition != null && EditorUtility.DisplayDialog("Remove Variation", "Do you wish to delete the Material Variation Definition?", "Delete Definition", "Remove From List"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(materialVariationInstance.variationDefinition));
            }
            DestroyImmediate(materialVariationInstance, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DrawPrototypeSettings();
        }
        #endregion Material Variations

        #region Optional Renderers
        private void DrawOptionalRendererSettings()
        {
            if (_selectedCount != 1)
                return;

            GameObject prefabObject0 = _gpuiPrefabManager.GetPrototype(_selectedIndex0).prefabObject;
            if (prefabObject0.HasComponent<LODGroup>())
                return;

            if (!prefabObject0.HasComponentInChildrenExceptParent<MeshRenderer>() && !prefabObject0.HasComponentInChildrenExceptParent<SkinnedMeshRenderer>())
                return;

            GPUIOptionalRenderer[] optionalRenderers = prefabObject0.GetComponentsInChildren<GPUIOptionalRenderer>();
            Foldout optionalRenderersFoldout = GPUIEditorUtility.DrawBoxContainer(_prototypeSettingsContentVE, "prefabManagerOptionalRenderers", _optionalRenderersFoldoutValue, _helpBoxes, true);
            if (optionalRenderers.Length > 0)
                optionalRenderersFoldout.text += " [" + optionalRenderers.Length + "]";
            optionalRenderersFoldout.RegisterValueChangedCallback((evt) => { if (evt.target == optionalRenderersFoldout) _optionalRenderersFoldoutValue = evt.newValue; });
            DrawOptionalRendererToggles(optionalRenderersFoldout, prefabObject0, prefabObject0.transform, optionalRenderersFoldout, 0);
        }

        private void DrawOptionalRendererToggles(Foldout optionalRenderersFoldout, GameObject prefabRoot, Transform parentTransform, VisualElement parentElement, int depth)
        {
            foreach (Transform childTransform in parentTransform)
            {
                if (!childTransform.GetComponent<MeshRenderer>() && !childTransform.GetComponent<SkinnedMeshRenderer>())
                    continue;
                VisualElement container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.marginLeft = depth * 15;
                Toggle toggle = new Toggle("")
                {
                    value = childTransform.HasComponent<GPUIOptionalRenderer>()
                };
                if (Application.isPlaying)
                    toggle.SetEnabled(false);
                else
                {
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue)
                            childTransform.gameObject.AddComponent<GPUIOptionalRenderer>();
                        else
                            DestroyImmediate(childTransform.GetComponent<GPUIOptionalRenderer>(), true);
                        prefabRoot.GetComponent<GPUIPrefab>().Reset();
                        GPUIPrefabUtility.MergeAllPrefabInstances(prefabRoot);
                        GPUIOptionalRenderer[] optionalRenderers = prefabRoot.GetComponentsInChildren<GPUIOptionalRenderer>();
                        optionalRenderersFoldout.text = "Optional Renderers [" + optionalRenderers.Length + "]";
                    });
                }
                container.Add(toggle);
                Label label = new Label(childTransform.name);
                label.style.paddingLeft = 10;
                container.Add(label);
                parentElement.Add(container);
                DrawOptionalRendererToggles(optionalRenderersFoldout, prefabRoot, childTransform, parentElement, depth + 1);
            }
        }
        #endregion Optional Renderers

        private void OnAutoAddRemoveModified(ChangeEvent<bool> evt)
        {
            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                if (evt.newValue)
                {
                    if (!prototype.prefabObject.HasComponent<GPUIPrefabAutoAddRemove>())
                    {
                        GPUIPrefabAutoAddRemove autoAddRemove = GPUIPrefabUtility.AddOrGetComponentToPrefab<GPUIPrefabAutoAddRemove>(prototype.prefabObject);
                        autoAddRemove.gpuiPrefab = prototype.prefabObject.GetComponent<GPUIPrefab>();
                        EditorUtility.SetDirty(prototype.prefabObject);
                    }
                }
                else
                {
                    if (prototype.prefabObject.HasComponent<GPUIPrefabAutoAddRemove>())
                        GPUIPrefabUtility.RemoveComponentFromPrefab<GPUIPrefabAutoAddRemove>(prototype.prefabObject);
                }
            }
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update += CreatePreviews;
        }

        protected virtual bool OnCheckPrefabComponents()
        {
            bool allHasPrefabComponent = true;
            for (int i = 0; i < _selectedCount; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                if (prototype.prefabObject == null)
                {
                    allHasPrefabComponent = false;
                    continue;
                }
                if (!prototype.prefabObject.TryGetComponent<GPUIPrefab>(out _))
                {
                    allHasPrefabComponent = false;
                    if (!Application.isPlaying)
                        AttachPrefabComponent(prototype.prefabObject);
                }
            }
            return allHasPrefabComponent;
        }

        protected virtual void OnCheckPrefabComponentsAll()
        {
            if (_gpuiManager == null)
                return;
            for (int i = 0; i < _gpuiManager.GetPrototypeCount(); i++)
            {
                GPUIPrototype prototype = _gpuiManager.GetPrototype(i);
                if (prototype == null || prototype.prefabObject == null)
                    continue;
                if (!prototype.prefabObject.TryGetComponent<GPUIPrefab>(out _))
                {
                    if (!Application.isPlaying)
                        AttachPrefabComponent(prototype.prefabObject);
                }
            }
        }

        protected override void AttachPrefabComponent(GameObject prefabObject)
        {
            GPUIPrefab prefabScript = prefabObject.GetComponent<GPUIPrefab>();
            if (prefabScript == null)
            {
                prefabScript = GPUIPrefabUtility.AddOrGetComponentToPrefab<GPUIPrefab>(prefabObject);
                if (!prefabObject.TryGetComponent<GPUIPrefab>(out _)) // If prefab component is removed from the variant manually.
                {
                    PrefabUtility.RevertRemovedComponent(prefabObject, prefabScript, InteractionMode.AutomatedAction);
                    PrefabUtility.SavePrefabAsset(prefabObject);
                }
            }
            if (prefabScript == null)
                return;
            EditorApplication.delayCall += () => { prefabScript.Reset(); RevertAllPrefabComponents(); };
        }

        protected override void RevertAllPrefabComponents()
        {
            GPUIPrefab[] gpuiPrefabs = FindObjectsByType<GPUIPrefab>(FindObjectsSortMode.None);
            foreach (var gpuiPrefab in gpuiPrefabs)
            {
                gpuiPrefab.GetPrefabID();
            }
        }

        private List<bool> GetAutoAddRemoveValues()
        {
            List<bool> values = new List<bool>();
            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                values.Add(prototype.prefabObject.HasComponent<GPUIPrefabAutoAddRemove>());
            }
            return values;
        }

        private bool HasSelectedPrototypesSkinnedMeshRenderers()
        {
            for (int i = 0; i < _selectedCount; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                if (prototype.prefabObject == null)
                    return false;
                if (!prototype.prefabObject.HasComponentInChildren<SkinnedMeshRenderer>())
                    return false;
            }
            return true;
        }

        private List<bool> GetEnableGPUSkinningValues()
        {
            List<bool> values = new List<bool>();
            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                values.Add(prototype.prefabObject.HasComponent<GPUISkinningBase>());
            }
            return values;
        }

        private void OnEnableGPUSkinningModified(ChangeEvent<bool> evt)
        {
            for (int i = 0; i < _gpuiManager.editor_selectedPrototypeIndexes.Count; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                if (prototype.prefabObject == null || !prototype.prefabObject.HasComponentInChildren<SkinnedMeshRenderer>())
                    continue;
                if (evt.newValue)
                {
                    if (!prototype.prefabObject.HasComponent<GPUISkinningBase>())
                    {
                        OnEnableGPUSkinningModifiedCallback.Invoke(prototype.prefabObject, true);
                        if (GPUIProfile.defaultGPUSkinningProfile != null && prototype.profile == GPUIProfile.DefaultProfile)
                            prototype.profile = GPUIProfile.defaultGPUSkinningProfile;
                        var prototypeData = _gpuiPrefabManager.GetPrototypeData(prototypeIndex);
                        prototypeData.isAutoUpdateTransformData = true;
                        EditorUtility.SetDirty(_gpuiManager);
                        serializedObject.Update();
                        DrawPrototypeSettings();
                    }
                }
                else
                {
                    if (prototype.prefabObject.HasComponent<GPUISkinningBase>())
                    {
                        OnEnableGPUSkinningModifiedCallback.Invoke(prototype.prefabObject, false);
                        DrawPrototypeSettings();
                    }
                }
            }
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update += CreatePreviews;
        }

        public override bool HasPrototypeAdvancedActions()
        {
            return true;
        }

        protected override void DrawAdvancedActions()
        {
            base.DrawAdvancedActions();

            if (_selectedCount > 0)
            {
                GPUIPrototype prototype = _gpuiManager.GetPrototype(_selectedIndex0);
                if (prototype.prefabObject.TryGetComponent(out GPUIPrefab gpuiPrefab))
                {
                    if (gpuiPrefab.IsRenderersDisabled)
                    {
                        GPUIEditorUtility.DrawColoredButton(new GUIContent("Enable Default Renderers"), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Bold, Rect.zero, SetRenderersEnabled);
                        EditorGUI.BeginChangeCheck();
                        SerializedProperty editor_enableEditModeRenderingSP = serializedObject.FindProperty("editor_enableEditModeRendering");
                        DrawIMGUISerializedProperty(editor_enableEditModeRenderingSP);
                        if (editor_enableEditModeRenderingSP.boolValue)
                            DrawIMGUISerializedProperty(serializedObject.FindProperty("editor_isReadPrefabTransformsEveryUpdate"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            serializedObject.Update();
                        }
                    }
                    else
                        GPUIEditorUtility.DrawColoredButton(new GUIContent("Disable Default Renderers"), GPUIEditorConstants.Colors.lightRed, Color.white, FontStyle.Bold, Rect.zero, SetRenderersEnabled);
                }
            }
        }

        private void SetRenderersEnabled()
        {
            GPUIPrototype prototype0 = _gpuiManager.GetPrototype(_selectedIndex0);
            GPUIPrefab gpuiPrefab;
            if (prototype0.prefabObject.TryGetComponent(out gpuiPrefab) && !gpuiPrefab.IsRenderersDisabled && !GPUIEditorUtility.DisplayDialog("disableDefaultRenderersWarning", true))
                return;
            for (int i = 0; i < _selectedCount; i++)
            {
                int prototypeIndex = _gpuiManager.editor_selectedPrototypeIndexes[i];
                GPUIPrototype prototype = _gpuiManager.GetPrototype(prototypeIndex);
                if (prototype.prefabObject.TryGetComponent(out gpuiPrefab))
                {
                    GameObject prefabContents = GPUIPrefabUtility.LoadPrefabContents(gpuiPrefab.gameObject);
                    _gpuiPrefabManager.SetPrefabInstanceRenderersEnabled(prefabContents.GetComponent<GPUIPrefab>(), gpuiPrefab.IsRenderersDisabled);
                    GPUIPrefabUtility.UnloadPrefabContents(gpuiPrefab.gameObject, prefabContents, true);
                    GPUIPreviewCache.RemovePreview(prototype.GetHashCode());
                }
            }
            EditorApplication.update -= CreatePreviews;
            EditorApplication.update += CreatePreviews;
        }

        public static void RegisterInstancesInScene(GPUIPrefabManager prefabManager)
        {
            Undo.RecordObject(prefabManager.gameObject, "Register instances in scene");
            int count = prefabManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                var prototypeData = prefabManager.GetPrototypeData(i);
                prototypeData.registeredInstances = new GPUIPrefabPrototypeData.GPUIPrefabInstances
                {
                    prefabInstances = GPUIPrefabUtility.FindAllInstancesOfPrefab(prefabManager.GetPrototype(i).prefabObject, false)
                };
            }
        }

        protected override bool IsDrawRegisteredInstances()
        {
            if (_isFindInstancesAtInitializationPF != null && !_isFindInstancesAtInitializationPF.boolValue)
                return true;
            return base.IsDrawRegisteredInstances();
        }

        public override bool SupportsBillboardGeneration()
        {
            for (int i = 0; i < _selectedCount; i++)
            {
                var prototype = _gpuiManager.GetPrototype(_gpuiManager.editor_selectedPrototypeIndexes[i]);
                if (prototype.prefabObject != null && prototype.prefabObject.HasComponent<GPUISkinningBase>())
                    return false;
            }
            return true;
        }

        public override string GetTitleText()
        {
            return "GPUI Prefab Manager";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#The_Prefab_Manager";
        }

        [MenuItem("Tools/GPU Instancer Pro/Add Prefab Manager", validate = false, priority = 111)]
        public static GPUIPrefabManager ToolbarAddPrefabManager()
        {
            GameObject go = new GameObject("GPUI Prefab Manager");
            GPUIPrefabManager prefabManager = go.AddComponent<GPUIPrefabManager>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Add GPUI Prefab Manager");

            return prefabManager;
        }
    }
}
