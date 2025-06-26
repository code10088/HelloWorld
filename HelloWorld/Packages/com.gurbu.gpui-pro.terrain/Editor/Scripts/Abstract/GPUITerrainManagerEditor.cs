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
    public abstract class GPUITerrainManagerEditor<T> : GPUIManagerEditor where T : GPUIPrototypeData, new()
    {
        private GPUITerrainManager<T> _gpuiTerrainManager;
        private bool showEditModeAdditionalTerrains = true;
        protected SerializedProperty _prototypeDataArraySP;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiTerrainManager = target as GPUITerrainManager<T>;
            _pickerAcceptModelPrefab = true;
            //_disableAddPrototypes = true;
            //_disableRemovePrototypes = true;
            _prototypeDataArraySP = serializedObject.FindProperty("_prototypeDataArray");
        }

        protected override void DrawManagerSettings()
        {
            base.DrawManagerSettings();

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoAddActiveTerrainsOnInitialization")));
            CheckTerrainReferences();
            _managerSettingsContentVE.Add(new IMGUIContainer(DrawTerrainsProperty));

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoAddPrototypesBasedOnTerrains")));
            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_isAutoRemovePrototypesBasedOnTerrains")));
        }

        private void DrawTerrainsProperty()
        {
            if (_gpuiTerrainManager == null)
                return;
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUI.BeginChangeCheck();
            DrawIMGUISerializedProperty(serializedObject.FindProperty("_gpuiTerrains"));
            if (!Application.isPlaying && _gpuiTerrainManager.editor_EditModeAdditionalTerrains != null && _gpuiTerrainManager.editor_EditModeAdditionalTerrains.Count > 0)
                GPUIEditorUtility.DrawIMGUIList(ref showEditModeAdditionalTerrains, ref _gpuiTerrainManager.editor_EditModeAdditionalTerrains, "Edit Mode Temp. Terrains", false);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                DrawManagerSettings();
                DrawPrototypeButtons();
            }
            if (!Application.isPlaying)
            {
                Terrain[] activeTerrains = Terrain.activeTerrains;
                GPUITerrain[] gpuiTerrains = FindObjectsByType<GPUITerrain>(FindObjectsSortMode.None);
                if (!_gpuiTerrainManager.ContainsTerrains(activeTerrains) || !_gpuiTerrainManager.ContainsTerrains(gpuiTerrains))
                {
                    GPUIEditorUtility.DrawColoredButton(new GUIContent("Add Active Terrains"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Normal, Rect.zero, () =>
                    {
                        _gpuiTerrainManager.RemoveNullTerrains();
                        _gpuiTerrainManager.AddTerrains(activeTerrains);
                        _gpuiTerrainManager.AddTerrains(gpuiTerrains);
                    });
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawPrototypeButtons()
        {
            base.DrawPrototypeButtons();

            if (!Application.isPlaying && _gpuiTerrainManager.GetTerrainCount() > 0)
            {
                GPUIEditorTextUtility.TryGetGPUIText("terrainUpdatePrototypesButton", out GPUIEditorTextUtility.GPUIText gpuiText);
                Button loadPrototypesButton = new()
                {
                    text = gpuiText.title,
                    focusable = false,
                    tooltip = gpuiText.tooltip,
                };
                loadPrototypesButton.AddToClassList("gpui-pre-prototype-button");
                loadPrototypesButton.RegisterCallback<ClickEvent>((evt) => { _gpuiTerrainManager.ReloadTerrains(); _gpuiTerrainManager.ResetPrototypesFromTerrains(); DrawManagerSettings(); DrawPrototypeButtons(); });
                _prePrototypeButtonsVE.Add(loadPrototypesButton);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _prePrototypeButtonsVE);
            }
            CheckPerObjectMotionVectors();
        }

        private void CheckPerObjectMotionVectors()
        {
            if (!GPUIRuntimeSettings.Instance.IsBuiltInRP && _gpuiTerrainManager != null)
            {
                bool isPerObjectMotionEnabled = false;
                int prototypeCount = _gpuiTerrainManager.GetPrototypeCount();
                for (int p = 0; p < prototypeCount; p++)
                {
                    var prototype = _gpuiTerrainManager.GetPrototype(p);
                    if (prototype != null && prototype.profile != null && prototype.profile.enablePerObjectMotionVectors)
                    {
                        isPerObjectMotionEnabled = true;
                        break;
                    }
                }
                if (isPerObjectMotionEnabled)
                    GPUIEditorUtility.DrawGPUIHelpBox(_prePrototypeButtonsVE, 302, null, DisableProfilePerObjectMotion, HelpBoxMessageType.Warning);
            }
        }

        private void CheckTerrainReferences()
        {
            if (_gpuiTerrainManager == null)
                return;
            int terrainCount = _gpuiTerrainManager.GetTerrainCount();
            bool showPrefabWarning = false;
            for (int t = 0; t < terrainCount; t++)
            {
                var gpuiTerrain = _gpuiTerrainManager.GetTerrain(t);
                if (gpuiTerrain == null)
                    continue;
                if(gpuiTerrain.gameObject.scene == null || !gpuiTerrain.gameObject.scene.IsValid())
                {
                    showPrefabWarning = true;
                    break;
                }
            }
            if (showPrefabWarning)
                GPUIEditorUtility.DrawGPUIHelpBox(_managerSettingsContentVE, 303, null, RemovePrefabTerrainReferences, HelpBoxMessageType.Warning);
        }

        private void RemovePrefabTerrainReferences()
        {
            if (_gpuiTerrainManager == null)
                return;
            for (int t = 0; t < _gpuiTerrainManager.GetTerrainCount(); t++)
            {
                var gpuiTerrain = _gpuiTerrainManager.GetTerrain(t);
                if (gpuiTerrain == null)
                    continue;
                if (gpuiTerrain.gameObject.scene == null || !gpuiTerrain.gameObject.scene.IsValid())
                {
                    _gpuiTerrainManager.RemoveTerrain(gpuiTerrain);
                    t--;
                }
            }
            DrawManagerSettings();
            DrawPrototypeButtons();
        }

        private void DisableProfilePerObjectMotion()
        {
            int prototypeCount = _gpuiTerrainManager.GetPrototypeCount();
            for (int p = 0; p < prototypeCount; p++)
            {
                var prototype = _gpuiTerrainManager.GetPrototype(p);
                if (prototype != null && prototype.profile != null && prototype.profile.enablePerObjectMotionVectors)
                {
                    prototype.profile.enablePerObjectMotionVectors = false;
                    EditorUtility.SetDirty(prototype.profile);
                }
            }
            DrawPrototypeButtons();
        }

        //protected override void DrawPrototypeSettings()
        //{
        //    if (_gpuiTerrainManager.GetTerrainCount() == 0)
        //    {
        //        _prototypeSettingsContentVE.Clear();
        //        return;
        //    }

        //    base.DrawPrototypeSettings();
        //}
    }
}