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
    [CustomEditor(typeof(GPUIDetailManager))]
    public class GPUIDetailManagerEditor : GPUITerrainManagerEditor<GPUIDetailPrototypeData>
    {
        private GPUIDetailManager _gpuiDetailManager;

        private GPUIHelpBox _detailUpdateDistanceWarning;
        private SerializedProperty _detailUpdateDistanceSP;

        public const string KEY_UISV_isDensityReduceFoldoutExpanded = "isDensityReduceFoldoutExpanded";
        public const string KEY_UISV_isDetailAdjustmentFoldoutExpanded = "isDetailAdjustmentFoldoutExpanded";
        public const string KEY_UISV_isDetailTerrainPropertiesFoldoutExpanded = "isDetailTerrainPropertiesFoldoutExpanded";
        public const string KEY_UISV_isDetailSubSettingsFoldoutExpanded = "isDetailSubSettingsFoldoutExpanded";
        public const string KEY_UISV_isDetailShaderPropertiesFoldoutExpanded = "isDetailShaderPropertiesFoldoutExpanded";
        public const string KEY_UISV_isDetailProceduralDensityFoldoutExpanded = "isDetailProceduralDensityFoldoutExpanded";

        protected override void OnEnable()
        {
            _gpuiDetailManager = target as GPUIDetailManager;
            base.OnEnable();
    }

        protected override bool TryGetPreviewForPrototype(int prototypeIndex, GPUIPreviewDrawer previewDrawer, out Texture2D preview)
        {
            GPUIDetailPrototypeData detailPrototypeData = _gpuiDetailManager.GetPrototypeData(prototypeIndex);
            if (detailPrototypeData != null && detailPrototypeData.detailTexture != null)
            {
                int key = _gpuiDetailManager.GetPrototype(prototypeIndex).GetHashCode() + _gpuiDetailManager.GetRendererGroupID(prototypeIndex);
                if (!GPUIPreviewCache.TryGetPreview(detailPrototypeData.detailTexture.GetInstanceID(), out preview) && previewDrawer != null)
                {
                    if (previewDrawer.TryGetPreviewForPrototype(null, PREVIEW_ICON_SIZE, detailPrototypeData.detailTexture, out preview))
                    {
                        GPUIPreviewCache.AddPreview(detailPrototypeData.detailTexture.GetInstanceID(), preview);
                        GPUIPreviewCache.AddPreview(key, preview);
                        return true;
                    }
                    return false;
                }
                else
                    GPUIPreviewCache.AddPreview(key, preview);
                return true;
            }
            return base.TryGetPreviewForPrototype(prototypeIndex, previewDrawer, out preview);
        }

        protected override void DrawManagerSettings()
        {
            base.DrawManagerSettings();

            VisualElement detailManagerSettingsVE = new VisualElement();
            _managerSettingsContentVE.Add(detailManagerSettingsVE);
            detailManagerSettingsVE.Add(DrawSerializedProperty(serializedObject.FindProperty("detailObjectDistance"), out PropertyField detailObjectDistancePF));
            detailObjectDistancePF.RegisterValueChangedCallbackDelayed(OnDetailManagerSettingsChanged);

            _detailUpdateDistanceSP = serializedObject.FindProperty("detailUpdateDistance");
            VisualElement detailUpdateDistanceVE = DrawSerializedProperty(_detailUpdateDistanceSP, out PropertyField updateDistancePF);
            detailManagerSettingsVE.Add(detailUpdateDistanceVE);
            updateDistancePF.RegisterValueChangedCallbackDelayed(OnDetailManagerSettingsChanged);
            _detailUpdateDistanceWarning = GPUIEditorUtility.CreateGPUIHelpBox(-405, null, null, HelpBoxMessageType.Warning);
            detailUpdateDistanceVE.Add(_detailUpdateDistanceWarning);
            if (_detailUpdateDistanceSP.floatValue != 0)
                _detailUpdateDistanceWarning.AddToClassList("gpui-hidden");

            detailManagerSettingsVE.Add(DrawSerializedProperty(serializedObject.FindProperty("healthyDryNoiseTexture"), out PropertyField healthyDryNoiseTexturePF));
            healthyDryNoiseTexturePF.RegisterValueChangedCallbackDelayed(OnDetailManagerSettingsChanged);
        }

        protected override void DrawDefaultProfiles()
        {
            base.DrawDefaultProfiles();

            _managerSettingsContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("defaultDetailTextureProfile")));
        }

        private void OnDetailManagerSettingsChanged(SerializedPropertyChangeEvent evt)
        {
            _gpuiDetailManager.OnPrototypePropertiesModified();

            if (_detailUpdateDistanceSP != null && _detailUpdateDistanceWarning != null)
            {
                if (_detailUpdateDistanceSP.floatValue > 0 && !_detailUpdateDistanceWarning.ClassListContains("gpui-hidden"))
                    _detailUpdateDistanceWarning.AddToClassList("gpui-hidden");
                else if (_detailUpdateDistanceSP.floatValue == 0 && _detailUpdateDistanceWarning.ClassListContains("gpui-hidden"))
                    _detailUpdateDistanceWarning.RemoveFromClassList("gpui-hidden");
            }
        }

        protected override void BeginDrawPrototypeSettings()
        {
            base.BeginDrawPrototypeSettings();

            SerializedProperty detailPrototypeDataSP = null;
            if (_selectedCount == 1)
            {
                SerializedProperty prototypeSP = _prototypesSP.GetArrayElementAtIndex(_selectedIndex0);
                detailPrototypeDataSP = _prototypeDataArraySP.GetArrayElementAtIndex(_selectedIndex0);
                GPUIPrototype prototype = _gpuiDetailManager.GetPrototype(_selectedIndex0);
                bool isTextureDetail = prototype != null && prototype.prototypeType == GPUIPrototypeType.MeshAndMaterial;
                bool isDefaultShader = isTextureDetail && prototype.prototypeMaterials != null && prototype.prototypeMaterials.Length == 1 && prototype.prototypeMaterials[0] != null && prototype.prototypeMaterials[0].shader != null && prototype.prototypeMaterials[0].shader.name.StartsWith("GPUInstancerPro/Foliage");
                bool isLambert = isDefaultShader && prototype.prototypeMaterials[0].shader.name.EndsWith("Lambert");

                //SerializedProperty detailTextureSP = detailPrototypeDataSP.FindPropertyRelative("detailTexture");
                //bool isTextureDetail = detailTextureSP.objectReferenceValue != null;

                if (isTextureDetail)
                {
                    SerializedProperty mpbDescriptionSP = detailPrototypeDataSP.FindPropertyRelative("mpbDescription");
                    VisualElement mpbDescriptionVE = DrawSerializedProperty(mpbDescriptionSP, out PropertyField mpbDescriptionPF);
                    _prototypeSettingsContentVE.Add(mpbDescriptionVE);
                    mpbDescriptionVE.style.marginBottom = 5;
                    mpbDescriptionPF.SetEnabled(!Application.isPlaying);

                    LayerField layerField = new LayerField();
                    _prototypeSettingsContentVE.Add(GPUIEditorUtility.DrawSerializedProperty(layerField, prototypeSP.FindPropertyRelative("layer"), "layer", _helpBoxes));
                    layerField.SetEnabled(!Application.isPlaying);
                }

                if (isDefaultShader)
                    DrawFoliageShaderSettings(detailPrototypeDataSP, isLambert);
            }

            DrawDensityReductionSettings();
            DrawDetailAdjustmentSettings();

            if (_selectedCount == 1)
                DrawTerrainPrototypeSettings(detailPrototypeDataSP);

            DrawProceduralDensitySettings();
        }

        private void OnDetailPropertiesChanged(EventBase evt)
        {
            EditorApplication.delayCall -= _gpuiDetailManager.OnPrototypePropertiesModified;
            EditorApplication.delayCall += _gpuiDetailManager.OnPrototypePropertiesModified; // Executing delayed, because the value is not applied immediately
            GPUIRenderingSystem.Editor_RenderEditModeCameras();
        }

        protected override void DrawPrototypeTypeAndObjects(SerializedProperty prototypeSP)
        {
            GPUIPrototype prototype = _gpuiDetailManager.GetPrototype(_selectedIndex0);
            bool isTextureDetail = prototype != null && prototype.prototypeType == GPUIPrototypeType.MeshAndMaterial;
            if (isTextureDetail)
            {
                SerializedProperty detailPrototypeDataSP = _prototypeDataArraySP.GetArrayElementAtIndex(_selectedIndex0); 
                _prototypeSettingsContentVE.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("detailTexture"), out var pf));
                pf.SetEnabled(false);
            }
            base.DrawPrototypeTypeAndObjects(prototypeSP);
        }

        public override bool HasPrototypeAdvancedActions()
        {
            return true;
        }

        public override bool HasManagerAdvancedSettings()
        {
            return true;
        }

        protected override void DrawAdvancedActions()
        {
            if (_selectedCount == 1)
            {
                SerializedProperty detailPrototypeDataSP = _prototypeDataArraySP.GetArrayElementAtIndex(_selectedIndex0);
                EditorGUI.BeginChangeCheck();
                DrawIMGUISerializedProperty(detailPrototypeDataSP.FindPropertyRelative("maxDetailInstanceCountPerUnit"));
                DrawIMGUISerializedProperty(detailPrototypeDataSP.FindPropertyRelative("initialBufferSize"));
                DrawIMGUISerializedProperty(detailPrototypeDataSP.FindPropertyRelative("detailExtraBufferSizePercentage"));
                DrawIMGUISerializedProperty(detailPrototypeDataSP.FindPropertyRelative("detailBufferSizePercentageDifferenceForReduction"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    _gpuiDetailManager.OnPrototypePropertiesModified();
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(5);

            base.DrawAdvancedActions();
        }

        protected override void DrawManagerAdvancedSettings()
        {
            base.DrawManagerAdvancedSettings();

            SerializedProperty disableAsyncDetailDataRequestSP = serializedObject.FindProperty("disableAsyncDetailDataRequest");
            _managerAdvancedSettingsFoldout.Add(DrawSerializedProperty(disableAsyncDetailDataRequestSP, out PropertyField disableAsyncDetailDataRequestPF));
            disableAsyncDetailDataRequestPF.RegisterValueChangedCallbackDelayed((evt) => DrawManagerSettings());

            bool advancedSettingsModified = disableAsyncDetailDataRequestSP.boolValue;
            if (advancedSettingsModified)
                GPUIEditorUtility.DrawGPUIHelpBox(_managerSettingsContentVE, 406, null, RevertAdvancedSettings, HelpBoxMessageType.Warning);
        }

        private void RevertAdvancedSettings()
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(_gpuiDetailManager, "Revert Advanced Settings");
            _gpuiDetailManager.disableAsyncDetailDataRequest = false;
            serializedObject.Update();
            DrawManagerSettings();
        }

        private void DrawDensityReductionSettings()
        {
            Foldout densityReduceFoldout = GPUIEditorUtility.DrawBoxContainerWithUIStoredValue(_prototypeSettingsContentVE, "isUseDensityReduction", _helpBoxes, _gpuiManager, KEY_UISV_isDensityReduceFoldoutExpanded);

            Toggle isUseDensityReductionToggle = new Toggle();
            VisualElement isUseDensityReductionVE = DrawMultiField(isUseDensityReductionToggle, _prototypeDataArraySP, "isUseDensityReduction", false);
            isUseDensityReductionToggle.label = "";
            isUseDensityReductionVE.style.position = Position.Absolute;
            isUseDensityReductionVE.style.marginLeft = 160;// EditorGUIUtility.labelWidth;
            isUseDensityReductionVE.style.marginTop = 5;
            densityReduceFoldout.parent.Add(isUseDensityReductionVE);
            bool isUseDensityReductionPreviousValue = isUseDensityReductionToggle.value;
            densityReduceFoldout.SetEnabled(isUseDensityReductionPreviousValue);
            isUseDensityReductionToggle.RegisterValueChangedCallback((evt) =>
            {
                if (isUseDensityReductionPreviousValue != evt.newValue)
                {
                    densityReduceFoldout.value = evt.newValue;
                    isUseDensityReductionPreviousValue = evt.newValue;
                }
                densityReduceFoldout.SetEnabled(evt.newValue);
                _gpuiDetailManager.OnPrototypePropertiesModified();
            });

            FloatField densityReduceDistanceF = new FloatField();
            densityReduceDistanceF.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            densityReduceFoldout.Add(DrawMultiField(densityReduceDistanceF, _prototypeDataArraySP, "densityReduceDistance"));

            Slider densityReduceMultiplierSlider = new Slider(1f, 128f);
            densityReduceMultiplierSlider.showInputField = true;
            densityReduceMultiplierSlider.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            densityReduceFoldout.Add(DrawMultiField(densityReduceMultiplierSlider, _prototypeDataArraySP, "densityReduceMultiplier"));

            Slider densityReduceMaxScaleSlider = new Slider(0f, 128f);
            densityReduceMaxScaleSlider.showInputField = true;
            densityReduceMaxScaleSlider.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            densityReduceFoldout.Add(DrawMultiField(densityReduceMaxScaleSlider, _prototypeDataArraySP, "densityReduceMaxScale"));

            Slider densityReduceHeightScaleSlider = new Slider(0f, 1f);
            densityReduceHeightScaleSlider.showInputField = true;
            densityReduceHeightScaleSlider.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            densityReduceFoldout.Add(DrawMultiField(densityReduceHeightScaleSlider, _prototypeDataArraySP, "densityReduceHeightScale"));
        }

        private void DrawDetailAdjustmentSettings()
        {
            Foldout detailAdjustmentFoldout = GPUIEditorUtility.DrawBoxContainerWithUIStoredValue(_prototypeSettingsContentVE, "terrainDetailAdjustments", _helpBoxes, _gpuiManager, KEY_UISV_isDetailAdjustmentFoldoutExpanded);

            Slider densityAdjustmentSlider = new Slider(0f, 16f);
            densityAdjustmentSlider.showInputField = true;
            densityAdjustmentSlider.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            detailAdjustmentFoldout.Add(DrawMultiField(densityAdjustmentSlider, _prototypeDataArraySP, "densityAdjustment"));

            Slider healthyDryScaleAdjustmentSlider = new Slider(-4f, 4f);
            healthyDryScaleAdjustmentSlider.showInputField = true;
            healthyDryScaleAdjustmentSlider.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            detailAdjustmentFoldout.Add(DrawMultiField(healthyDryScaleAdjustmentSlider, _prototypeDataArraySP, "healthyDryScaleAdjustment"));

            IntegerField noiseSeedAdjustmentF = new IntegerField();
            noiseSeedAdjustmentF.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            detailAdjustmentFoldout.Add(DrawMultiField(noiseSeedAdjustmentF, _prototypeDataArraySP, "noiseSeedAdjustment"));

            Slider noiseSpreadAdjustmentF = new Slider(0f, 4f);
            noiseSpreadAdjustmentF.showInputField = true;
            noiseSpreadAdjustmentF.RegisterValueChangedCallback(OnDetailPropertiesChanged);
            detailAdjustmentFoldout.Add(DrawMultiField(noiseSpreadAdjustmentF, _prototypeDataArraySP, "noiseSpreadAdjustment"));
        }

        private void DrawTerrainPrototypeSettings(SerializedProperty detailPrototypeDataSP)
        {
            if (detailPrototypeDataSP == null) return;
            GPUIDetailPrototypeData detailPrototypeData = _gpuiDetailManager.GetPrototypeData(_selectedIndex0);

            Foldout detailTerrainPropertiesFoldout = GPUIEditorUtility.DrawBoxContainerWithUIStoredValue(_prototypeSettingsContentVE, "detailTerrainProperties", _helpBoxes, _gpuiManager, KEY_UISV_isDetailTerrainPropertiesFoldoutExpanded);

            GPUIHelpBox detailTerrainPropertiesHelpBox = GPUIEditorUtility.CreateGPUIHelpBox("detailTerrainProperties", null, null, HelpBoxMessageType.Info);
            detailTerrainPropertiesFoldout.Add(detailTerrainPropertiesHelpBox);

            VisualElement detailTerrainPropertiesFoldoutChild = new VisualElement();
            detailTerrainPropertiesFoldout.Add(detailTerrainPropertiesFoldoutChild);
            detailTerrainPropertiesFoldoutChild.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("healthyColor")));
            detailTerrainPropertiesFoldoutChild.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("dryColor")));
            detailTerrainPropertiesFoldoutChild.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("noiseSpread")));
            detailTerrainPropertiesFoldoutChild.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("isBillboard")));


            if (detailPrototypeData != null && detailPrototypeData.GetSubSettingCount() > 0)
            {
                Foldout detailSubSettingsFoldout = GPUIEditorUtility.DrawBoxContainerWithUIStoredValue(detailTerrainPropertiesFoldout, "prototypeSubSettings", _helpBoxes, _gpuiManager, KEY_UISV_isDetailSubSettingsFoldoutExpanded);

                VisualElement detailSubSettingsFoldoutChild = new VisualElement();
                detailSubSettingsFoldout.Add(detailSubSettingsFoldoutChild);
                detailSubSettingsFoldoutChild.SetEnabled(Application.isPlaying);
                detailSubSettingsFoldout.text += "[" + detailPrototypeData.GetSubSettingCount() + "]";
                for (int i = 0; i < detailPrototypeData.GetSubSettingCount(); i++)
                {
                    GPUIDetailPrototypeData.GPUIDetailPrototypeSubSettings subSettings = detailPrototypeData.GetSubSettings(i);
                    Foldout subFoldout = GPUIEditorUtility.DrawBoxContainer(detailSubSettingsFoldoutChild, "Setting " + i);
                    int subSettingIndex = i;
                    subFoldout.Add(DrawField(new FloatField(), subSettings.minWidth, "minWidth", (evt) => { subSettings.minWidth = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));
                    subFoldout.Add(DrawField(new FloatField(), subSettings.maxWidth, "maxWidth", (evt) => { subSettings.maxWidth = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));
                    subFoldout.Add(DrawField(new FloatField(), subSettings.minHeight, "minHeight", (evt) => { subSettings.minHeight = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));
                    subFoldout.Add(DrawField(new FloatField(), subSettings.maxHeight, "maxHeight", (evt) => { subSettings.maxHeight = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));
                    subFoldout.Add(DrawField(new IntegerField(), subSettings.noiseSeed, "noiseSeed", (evt) => { subSettings.noiseSeed = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));
                    Slider alignToGroundSlider = new Slider(0f, 1f);
                    alignToGroundSlider.showInputField = true;
                    subFoldout.Add(DrawField(alignToGroundSlider, subSettings.alignToGround, "alignToGround", (evt) => { subSettings.alignToGround = evt.newValue; _gpuiDetailManager.OnPrototypePropertiesModified(); }));

#if GPUIPRO_DEVMODE
                    FloatField detailUniqueValueField = new FloatField("Detail Unique Value");
                    detailUniqueValueField.value = subSettings.GetUniqueValue();
                    subFoldout.Add(detailUniqueValueField);
#endif
                }
            }
            detailTerrainPropertiesFoldoutChild.RegisterCallbackDelayed<SerializedPropertyChangeEvent>(OnDetailPropertiesChanged);

            detailTerrainPropertiesFoldoutChild.SetEnabled(Application.isPlaying || _gpuiDetailManager.GetTerrainCount() == 0);
        }

        private void DrawFoliageShaderSettings(SerializedProperty detailPrototypeDataSP, bool isLambert)
        {
            Foldout detailShaderPropertiesFoldout = GPUIEditorUtility.DrawBoxContainerWithUIStoredValue(_prototypeSettingsContentVE, "detailShaderProperties", _helpBoxes, _gpuiManager, KEY_UISV_isDetailShaderPropertiesFoldoutExpanded);

            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("contrast")));
            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("healthyDryRatio")));
            if (!isLambert)
                detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("ambientOcclusion")));
            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("gradientPower")));

            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("windWaveTintColor")));
            //detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("noiseSpread")));

            SerializedProperty isOverrideHealthyDryNoiseTextureSP = detailPrototypeDataSP.FindPropertyRelative("isOverrideHealthyDryNoiseTexture");
            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(isOverrideHealthyDryNoiseTextureSP, out PropertyField isOverrideHealthyDryNoiseTexturePF));
            VisualElement healthyDryNoiseTextureVE = DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("healthyDryNoiseTexture"));
            healthyDryNoiseTextureVE.SetVisible(isOverrideHealthyDryNoiseTextureSP.boolValue);
            isOverrideHealthyDryNoiseTexturePF.RegisterValueChangeCallback((evt) => healthyDryNoiseTextureVE.SetVisible(isOverrideHealthyDryNoiseTextureSP.boolValue));
            detailShaderPropertiesFoldout.Add(healthyDryNoiseTextureVE);

            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("windIdleSway")));

            SerializedProperty windWavesOnSP = detailPrototypeDataSP.FindPropertyRelative("windWavesOn");
            detailShaderPropertiesFoldout.Add(DrawSerializedProperty(windWavesOnSP, out PropertyField windWavesOnPF));
            VisualElement windWaveSizeVE = DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("windWaveSize"));
            detailShaderPropertiesFoldout.Add(windWaveSizeVE);
            VisualElement windWaveTintVE = DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("windWaveTint"));
            detailShaderPropertiesFoldout.Add(windWaveTintVE);
            VisualElement windWaveSwayVE = DrawSerializedProperty(detailPrototypeDataSP.FindPropertyRelative("windWaveSway"));
            detailShaderPropertiesFoldout.Add(windWaveSwayVE);
            windWaveSizeVE.SetVisible(windWavesOnSP.boolValue);
            windWaveTintVE.SetVisible(windWavesOnSP.boolValue);
            windWaveSwayVE.SetVisible(windWavesOnSP.boolValue);
            windWavesOnPF.RegisterValueChangeCallback((evt) =>
            {
                windWaveSizeVE.SetVisible(windWavesOnSP.boolValue);
                windWaveTintVE.SetVisible(windWavesOnSP.boolValue);
                windWaveSwayVE.SetVisible(windWavesOnSP.boolValue);
            });

            detailShaderPropertiesFoldout.RegisterCallbackDelayed<SerializedPropertyChangeEvent>(OnDetailPropertiesChanged);
        }

        private void DrawProceduralDensitySettings()
        {
            GPUITerrainEditorConstants.DrawProceduralDensitySettings(_prototypeSettingsContentVE, _helpBoxes, _gpuiDetailManager, null, KEY_UISV_isDetailProceduralDensityFoldoutExpanded, _prototypeDataArraySP, _gpuiManager.editor_selectedPrototypeIndexes, DrawPrototypeSettings, "proceduralDensityData", (int index) => { return _gpuiDetailManager.GetPrototype(index).name; });
        }

        protected override void OnAddButtonClickEvent(ClickEvent evt)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Prefab"), false, () =>
            {
                GPUIObjectSelectorWindow.ShowWindow("t:prefab", false, _pickerAcceptModelPrefab, true, _pickerAcceptSkinnedMeshRenderer, OnObjectsSelected);
            });
            menu.AddItem(new GUIContent("Texture"), false, () =>
            {
                _pickerControlID = GUIUtility.GetControlID(FocusType.Passive) + 100;
                EditorGUIUtility.ShowObjectPicker<Texture>(null, false, null, _pickerControlID);
                //GPUIObjectSelectorWindow.ShowWindow("t:texture", true, _pickerAcceptModelPrefab, true, _pickerAcceptSkinnedMeshRenderer, OnObjectsSelected);
            });

            // display the menu
            menu.ShowAsContext();
        }

        public override bool AddPickerObject(UnityEngine.Object pickerObject, int overwriteIndex = -1)
        {
            if (pickerObject == null)
                return false;

            if (!_gpuiManager.CanAddObjectAsPrototype(pickerObject))
                return false;

            if (pickerObject is Texture2D texture)
            {
                Undo.RecordObject(_gpuiManager, "Add prototype");

                if (overwriteIndex >= 0)
                {
                    if (_gpuiDetailManager.GetPrototype(overwriteIndex).prototypeType != GPUIPrototypeType.MeshAndMaterial)
                        return false;
                    _gpuiDetailManager.GetPrototypeData(overwriteIndex).detailTexture = texture;
                }
                else
                {
                    _gpuiDetailManager.AddPrototype(new GPUIPrototype(GPUITerrainConstants.DefaultDetailMesh, new Material[] { GPUITerrainConstants.DefaultDetailMaterial }, _gpuiDetailManager.GetTexturePrototypeProfile()));
                    _gpuiDetailManager.CheckPrototypeChanges();
                    GPUIDetailPrototypeData detailPrototypeData = _gpuiDetailManager.GetPrototypeData(_gpuiDetailManager.GetPrototypeCount() - 1);
                    detailPrototypeData.detailTexture = texture;
                }
            }

            if (!base.AddPickerObject(pickerObject, overwriteIndex))
                return false;

            Undo.RecordObject(this, "Add prototype");

            if (_gpuiDetailManager.GetTerrainCount() > 0 && EditorUtility.DisplayDialog("Add Prototype to Terrains", "Do you wish to add \"" + pickerObject.name + "\" prototype to terrains?", "Yes", "No"))
            {
                _gpuiDetailManager.AddPrototypeToTerrains(pickerObject, overwriteIndex);
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
                bool removeFromTerrain = _gpuiDetailManager.GetTerrainCount() > 0 && EditorUtility.DisplayDialog("Remove Prototypes from Terrain", "Do you wish to remove the prototypes form the terrains?" + selectedPrototypesText, "Remove from Terrains", "No");
                for (int i = _gpuiManager.editor_selectedPrototypeIndexes.Count - 1; i >= 0; i--)
                    _gpuiDetailManager.RemoveDetailPrototypeAtIndex(_gpuiManager.editor_selectedPrototypeIndexes[i], removeFromTerrain);
                _gpuiManager.editor_selectedPrototypeIndexes.Clear();
                DrawPrototypeButtons();
            }
        }

        protected override void OnNewProfileCreated(GPUIProfile newProfile)
        {
            base.OnNewProfileCreated(newProfile);
            newProfile.isDistanceCulling = false;
        }

        public override string GetTitleText()
        {
            return "GPUI Detail Manager";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#The_Detail_Manager";
        }

        [MenuItem("Tools/GPU Instancer Pro/Add Detail Manager For Terrains", validate = false, priority = 121)]
        public static void ToolbarAddDetailManager()
        {
            GPUIDetailManager detailManager = FindFirstObjectByType<GPUIDetailManager>();
            GameObject go;
            if (detailManager == null)
            {
                go = new GameObject("GPUI Detail Manager");
                detailManager = go.AddComponent<GPUIDetailManager>();
                detailManager.AddTerrains(FindObjectsByType<GPUITerrain>(FindObjectsSortMode.None));
                List<Terrain> terrains = new(Terrain.activeTerrains);
                terrains.Sort(DetailPrototypeCountSort);
                detailManager.AddTerrains(terrains);
                detailManager.ResetPrototypesFromTerrains();
                Undo.RegisterCreatedObjectUndo(go, "Add GPUI Detail Manager");
            }
            else
                go = detailManager.gameObject;

            Selection.activeGameObject = go;
        }

        private static int DetailPrototypeCountSort(Terrain x, Terrain y)
        {
            if (x == null || y == null || x.terrainData == null || y.terrainData == null)
                return 0;
            int xPC = x.terrainData.detailPrototypes.Length;
            int yPC = y.terrainData.detailPrototypes.Length;
            return yPC.CompareTo(xPC);
        }
    }
}