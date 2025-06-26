// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.TerrainModule
{
    [CustomEditor(typeof(GPUITreeManager))]
    public class GPUITreeManagerEditor : GPUITerrainManagerEditor<GPUITreePrototypeData>
    {
        private GPUITreeManager _gpuiTreeManager;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiTreeManager = target as GPUITreeManager;
        }

        private void OnTreeValuesChanged<TEventType>(TEventType evt) where TEventType : EventBase<TEventType>, new()
        {
            if (!GPUIRenderingSystem.IsActive || !_gpuiTreeManager.IsInitialized)
                return;
            _gpuiTreeManager.Initialize();
        }

        protected override void DrawPrototypeButtons()
        {
            base.DrawPrototypeButtons();

            if (_gpuiTreeManager.GetPrototypeCount() == 0)
                return;

            if (!Application.isPlaying && HasPrototypeWithBillboard())
            {
                GPUIEditorTextUtility.TryGetGPUIText("regenerateBillboardsButton", out GPUIEditorTextUtility.GPUIText gpuiText);
                Button regenerateBillboardsButton = new()
                {
                    text = gpuiText.title,
                    focusable = false,
                    tooltip = gpuiText.tooltip
                };
                regenerateBillboardsButton.AddToClassList("gpui-pre-prototype-button");
                regenerateBillboardsButton.RegisterCallback<ClickEvent>(RegenerateBillboards);
                _prePrototypeButtonsVE.Add(regenerateBillboardsButton);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _prePrototypeButtonsVE);
            }
        }

        private bool HasPrototypeWithBillboard()
        {
            if (_gpuiTreeManager == null)
                return false;

            for (int i = 0; i < _gpuiTreeManager.GetPrototypeCount(); i++)
            {
                GPUIPrototype prototype = _gpuiTreeManager.GetPrototype(i);
                if (prototype != null && prototype.isGenerateBillboard)
                    return true;
            }

            return false;
        }

        private void RegenerateBillboards(ClickEvent evt)
        {
            for (int i = 0; i < _gpuiTreeManager.GetPrototypeCount(); i++)
            {
                GPUIPrototype prototype = _gpuiTreeManager.GetPrototype(i);
                if (!prototype.isGenerateBillboard)
                    continue;
                prototype.GenerateBillboard();
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.LODGroupDataProvider.RegenerateLODGroups();

            DrawPrototypeSettings();
        }

        protected override void BeginDrawPrototypeSettings()
        {
            base.BeginDrawPrototypeSettings();

            SerializedProperty treePrototypeDataSP = _prototypeDataArraySP.GetArrayElementAtIndex(_selectedIndex0);
            VisualElement treePrototypeSettingsVE = new VisualElement();
            _prototypeSettingsContentVE.Add(treePrototypeSettingsVE);
            if (Application.isPlaying)
                treePrototypeSettingsVE.SetEnabled(false);
            else
                treePrototypeSettingsVE.RegisterCallbackDelayed<ChangeEvent<bool>>(OnTreeValuesChanged);

            treePrototypeSettingsVE.Add(DrawMultiField(new Toggle(), _prototypeDataArraySP, "isApplyRotation"));
            treePrototypeSettingsVE.Add(DrawMultiField(new Toggle(), _prototypeDataArraySP, "isApplyPrefabScale"));
            treePrototypeSettingsVE.Add(DrawMultiField(new Toggle(), _prototypeDataArraySP, "isApplyHeight"));
        }

        public override bool HasManagerAdvancedSettings()
        {
            return true;
        }

        protected override void DrawManagerAdvancedSettings()
        {
            base.DrawManagerAdvancedSettings();

            _managerAdvancedSettingsFoldout.Add(DrawSerializedProperty(serializedObject.FindProperty("_enableTreeInstanceColors"), out PropertyField enableTreeInstanceColorsPF));
            if (Application.isPlaying)
            {
                enableTreeInstanceColorsPF.SetEnabled(false);
            }
            else
            {
                enableTreeInstanceColorsPF.RegisterValueChangedCallbackDelayed(OnTreeValuesChanged);
            }
        }

        public override bool AddPickerObject(UnityEngine.Object pickerObject, int overwriteIndex = -1)
        {
            if (!base.AddPickerObject(pickerObject, overwriteIndex))
                return false;

            Undo.RecordObject(this, "Add prototype");

            if (_gpuiTreeManager.GetTerrainCount() > 0 && EditorUtility.DisplayDialog("Add Prototype to Terrains", "Do you wish to add \"" + pickerObject.name + "\" prototype to terrains?", "Yes", "No"))
            {
                if (pickerObject is GameObject pickerGameObject)
                {
                    if (pickerGameObject.GetComponentInChildren<MeshRenderer>() == null)
                        return false;

                    _gpuiTreeManager.AddPrototypeToTerrains(pickerGameObject, overwriteIndex);
                }
            }
            return true;
        }

        protected override void RemovePrototype()
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
                bool removeFromTerrain = _gpuiTreeManager.GetTerrainCount() > 0 && EditorUtility.DisplayDialog("Remove Prototypes from Terrain", "Do you wish to remove the prototypes form the terrains?" + selectedPrototypesText, "Remove from Terrains", "No");
                for (int i = _gpuiManager.editor_selectedPrototypeIndexes.Count - 1; i >= 0; i--)
                    _gpuiTreeManager.RemoveTreePrototypeAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[i], removeFromTerrain);
                _gpuiManager.editor_selectedPrototypeIndexes.Clear();
                DrawPrototypeButtons();
            }
        }

        public override bool SupportsBillboardGeneration()
        {
            return true;
        }

        public override string GetTitleText()
        {
            return "GPUI Tree Manager";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#The_Tree_Manager";
        }

        [MenuItem("Tools/GPU Instancer Pro/Add Tree Manager For Terrains", validate = false, priority = 122)]
        public static void ToolbarAddTreeManager()
        {
            GPUITreeManager treeManager = FindFirstObjectByType<GPUITreeManager>();
            GameObject go;
            if (treeManager == null)
            {
                go = new GameObject("GPUI Tree Manager");
                treeManager = go.AddComponent<GPUITreeManager>();
                treeManager.AddTerrains(FindObjectsByType<GPUITerrain>(FindObjectsSortMode.None));
                List<Terrain> terrains = new(Terrain.activeTerrains);
                terrains.Sort(TreePrototypeCountSort);
                treeManager.AddTerrains(terrains);
                treeManager.ResetPrototypesFromTerrains();
                Undo.RegisterCreatedObjectUndo(go, "Add GPUI Tree Manager");
            }
            else
                go = treeManager.gameObject;

            Selection.activeGameObject = go;
        }

        private static int TreePrototypeCountSort(Terrain x, Terrain y)
        {
            if (x == null || y == null || x.terrainData == null || y.terrainData == null)
                return 0;
            int xPC = x.terrainData.treePrototypes.Length;
            int yPC = y.terrainData.treePrototypes.Length;
            return yPC.CompareTo(xPC);
        }
    }
}