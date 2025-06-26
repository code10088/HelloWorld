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
    public class GPUITerrainEditor : GPUIEditor
    {
        private GPUITerrain _gpuiTerrain;
        private bool _detailRenderTexturesFoldout = true;
        protected bool _isBoundsEditable = false;
        protected bool _isBakedDetailTexturesVisible = true;

        protected VisualTreeAsset _terrainUITemplate;
        protected VisualElement _terrainHelpBoxesVE;
        protected ToolbarButton _terrainSettingsTB;
        protected ToolbarButton _terrainDataTB;
        protected ToolbarButton _terrainDebugTB;
        protected VisualElement _terrainContentVE;
        protected VisualElement _terrainActionButtonsVE;
        protected VisualElement _terrainActionButtonsHelpVE;
        protected VisualElement _terrainDataVE;
        protected VisualElement _terrainDebugVE;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiTerrain = target as GPUITerrain;
            _terrainUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUITerrainEditorConstants.GetUIPath() + "GPUITerrainUI.uxml");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _gpuiTerrain = null;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            if (_gpuiTerrain == null)
                return;

            VisualElement rootElement = new();
            _terrainUITemplate.CloneTree(rootElement);
            rootElement.Bind(serializedObject);
            contentElement.Add(rootElement);

            _terrainHelpBoxesVE = rootElement.Q("TerrainHelpBoxes");
            DrawTerrainHelpBoxes();

            _terrainSettingsTB = rootElement.Q<ToolbarButton>("TerrainSettingsToolbarButton");
            _terrainDataTB = rootElement.Q<ToolbarButton>("TerrainDataToolbarButton");
            _terrainDebugTB = rootElement.Q<ToolbarButton>("TerrainDebugToolbarButton");
            _terrainContentVE = rootElement.Q("TerrainContentElement");
            _terrainActionButtonsVE = rootElement.Q("TerrainActionButtons");
            _terrainActionButtonsHelpVE = rootElement.Q("TerrainActionButtonsHelp");

            int activeToolbarIndex = GPUIRenderingSystem.Editor_GetUIStoredValue(_gpuiTerrain, KEY_UISV_activeToolbarIndex);
            activeToolbarIndex = Mathf.Clamp(activeToolbarIndex, 0, 2);
            if (activeToolbarIndex == 1 && !HasGPUITerrainData())
                activeToolbarIndex = 0;
            _terrainSettingsTB.clicked += () => OnTerrainToolbarButtonClicked(0);
            _terrainDataTB.clicked += () => OnTerrainToolbarButtonClicked(1);
            _terrainDebugTB.clicked += () => OnTerrainToolbarButtonClicked(2);

            if (_gpuiTerrain.gameObject.HasComponent<GPUITerrain.GPUITerrainPaintingProxy>())
            {
                OnTerrainToolbarButtonClicked(2);
                _terrainSettingsTB.SetVisible(false);
                _terrainDataTB.SetVisible(false);
                return;
            }
            OnTerrainToolbarButtonClicked(activeToolbarIndex);

            _terrainDataTB.SetVisible(HasGPUITerrainData());
        }

        protected virtual void DrawTerrainHelpBoxes()
        {
            _terrainHelpBoxesVE.Clear();
        }

        protected virtual void OnTerrainToolbarButtonClicked(int index)
        {
            GPUIRenderingSystem.Editor_SetUIStoredValue(_gpuiTerrain, KEY_UISV_activeToolbarIndex, index);
            _terrainSettingsTB.SetToolbarButtonActive(false);
            _terrainDataTB.SetToolbarButtonActive(false);
            _terrainDebugTB.SetToolbarButtonActive(false);
            switch (index)
            {
                case 0:
                    _terrainSettingsTB.SetToolbarButtonActive(true);
                    DrawTerrainSettings();
                    break;
                case 1:
                    _terrainDataTB.SetToolbarButtonActive(true);
                    DrawTerrainData();
                    break;
                case 2:
                    _terrainDebugTB.SetToolbarButtonActive(true);
                    DrawTerrainDebug();
                    break;
            }
            if (!Application.isPlaying && !_gpuiTerrain.gameObject.HasComponent<GPUITerrain.GPUITerrainPaintingProxy>())
                DrawActionButtons();
        }

        protected virtual void DrawTerrainSettings()
        {
            _terrainContentVE.Clear();
            _terrainContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("isAutoFindTreeManager")));
            _terrainContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("isAutoFindDetailManager")));
            _terrainContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("terrainHolesSampleMode")));
            if (_isBoundsEditable)
            {
                _terrainContentVE.Add(DrawSerializedProperty(serializedObject.FindProperty("_bounds"), out PropertyField boundsPF));
                if (!Application.isPlaying)
                {
                    Button recalculateBoundsButton = new Button(() => _gpuiTerrain.SetTerrainBounds(true))
                    {
                        text = "Recalculate Bounds"
                    };
                    recalculateBoundsButton.focusable = false;
                    _terrainContentVE.Add(recalculateBoundsButton);
                    boundsPF.RegisterValueChangedCallbackDelayed((e) => { _gpuiTerrain.RequireTreeUpdate(); _gpuiTerrain.RequireDetailUpdate(); });
                }
                else
                    boundsPF.SetEnabled(false);
            }

            if (_isBakedDetailTexturesVisible)
            {
                int bakedDetailTextureCount = _gpuiTerrain.GetBakedDetailTextureCount();
                if (bakedDetailTextureCount > 0 && _gpuiTerrain.IsBakedDetailTextures())
                {
                    VisualElement bakedDetailTexturesVE = new VisualElement();
                    bakedDetailTexturesVE.style.paddingTop = 5;
                    bakedDetailTexturesVE.style.paddingBottom = 5;
                    _terrainContentVE.Add(bakedDetailTexturesVE);
                    DrawBakedDetailTexturesFoldout(bakedDetailTexturesVE);
                }
            }
        }

        protected virtual void DrawBakedDetailTexturesFoldout(VisualElement bakedDetailTexturesVE)
        {
            bakedDetailTexturesVE.Clear();
            SerializedProperty isCustomBakedDetailTexturesSP = serializedObject.FindProperty("_isCustomBakedDetailTextures");
            bool isAllowCustomBakedDetailTextures = true;
            if (isCustomBakedDetailTexturesSP != null)
            {
                bakedDetailTexturesVE.Add(DrawSerializedProperty(isCustomBakedDetailTexturesSP, out PropertyField isCustomBakedDetailTexturesPF));
                isCustomBakedDetailTexturesPF.RegisterValueChangedCallbackDelayed((e) => DrawBakedDetailTexturesFoldout(bakedDetailTexturesVE));
                isAllowCustomBakedDetailTextures = isCustomBakedDetailTexturesSP.boolValue;
            }

            bakedDetailTexturesVE.Add(new IMGUIContainer(DrawIMGUIBakedDetailTextures));
        }

        private void DrawIMGUIBakedDetailTextures()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bakedDetailTextures"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                if (_gpuiTerrain.IsDetailDensityTexturesLoaded)
                    _gpuiTerrain.CreateDetailTextures();
            }
        }

        protected virtual void DrawTerrainData()
        {
            _terrainContentVE.Clear();
        }

        protected virtual void DrawTerrainDebug()
        {
            _terrainContentVE.Clear();
            IMGUIContainer iMGUIContainer = new IMGUIContainer(DrawTerrainDebugIMGUI);
            _terrainContentVE.Add(iMGUIContainer);
        }

        protected virtual void DrawTerrainDebugIMGUI()
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GPUIEditorConstants.LABEL_WIDTH;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Heightmap Texture", _gpuiTerrain.GetHeightmapTexture(), typeof(RenderTexture), false);
            EditorGUILayout.ObjectField("Holes Texture", _gpuiTerrain.GetHolesTexture(), typeof(RenderTexture), false);

            int detailTextureCount = _gpuiTerrain.GetDetailTextureCount();
            if (detailTextureCount > 0)
            {
                EditorGUILayout.Space(5);
                _detailRenderTexturesFoldout = EditorGUILayout.Foldout(_detailRenderTexturesFoldout, "Detail Render Textures", true);
                if (_detailRenderTexturesFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    for (int i = 0; i < detailTextureCount; i++)
                    {
                        EditorGUILayout.ObjectField("Layer " + i, _gpuiTerrain.GetDetailDensityTexture(i), typeof(RenderTexture), false);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space(5);
                    EditorGUI.indentLevel--;
                }
            }

#if GPUIPRO_DEVMODE
            EditorGUILayout.BoundsField("World Bounds", _gpuiTerrain.GetTerrainWorldBounds());
            EditorGUILayout.Vector3Field("Terrain Bottom Left Position", _gpuiTerrain.GetPosition());
#endif

            if (_gpuiTerrain.TreeManager != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.ObjectField("Tree Manager", _gpuiTerrain.TreeManager, typeof(GPUIDetailManager), false);
                string treePrototypeIndexes = _gpuiTerrain.GetTreePrototypeIndexesToString();
                if (!string.IsNullOrEmpty(treePrototypeIndexes))
                    EditorGUILayout.TextField("Tree Prototype Indexes", treePrototypeIndexes);
            }
            if (_gpuiTerrain.DetailManager != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.ObjectField("Detail Manager", _gpuiTerrain.DetailManager, typeof(GPUIDetailManager), false);
                string detailPrototypeIndexes = _gpuiTerrain.GetDetailPrototypeIndexesToString();
                if (!string.IsNullOrEmpty(detailPrototypeIndexes))
                    EditorGUILayout.TextField("Detail Prototype Indexes", detailPrototypeIndexes);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.labelWidth = labelWidth;
        }

        protected virtual void DrawActionButtons()
        {
            _terrainActionButtonsVE.Clear();
            _terrainActionButtonsHelpVE.Clear();
            GPUIEditorTextUtility.GPUIText gpuiText;
            GPUIEditorTextUtility.TryGetGPUIText("reloadTerrainDataButton", out gpuiText);
            Button reloadTerrainDataButton = new Button(_gpuiTerrain.ReloadTerrainData)
            {
                text = gpuiText.title,
                tooltip = gpuiText.tooltip
            };
            reloadTerrainDataButton.AddToClassList("gpui-action-button");
            reloadTerrainDataButton.focusable = false;
            _terrainActionButtonsVE.Add(reloadTerrainDataButton);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _terrainActionButtonsHelpVE);

            bool hasDetailPrototypes = _gpuiTerrain.DetailPrototypes != null && _gpuiTerrain.DetailPrototypes.Length > 0;

            if (_gpuiTerrain.DetailManager != null && hasDetailPrototypes && !_gpuiTerrain.IsDetailDensityTexturesLoaded)
                _gpuiTerrain.CreateDetailTextures();

            if (hasDetailPrototypes && !_gpuiTerrain.IsDetailDensityTexturesLoaded)
            {
                GPUIEditorTextUtility.TryGetGPUIText("createDetailRenderTexturesButton", out gpuiText);
                Button createDetailRTButton = new Button(() => { _gpuiTerrain.CreateDetailTextures(); DrawActionButtons(); })
                {
                    text = gpuiText.title,
                    tooltip = gpuiText.tooltip
                };
                createDetailRTButton.style.backgroundColor = GPUIEditorConstants.Colors.blue;
                createDetailRTButton.AddToClassList("gpui-action-button");
                createDetailRTButton.focusable = false;
                _terrainActionButtonsVE.Add(createDetailRTButton);
                GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _terrainActionButtonsHelpVE);
            }
        }

        public virtual bool HasGPUITerrainData()
        {
            return false;
        }

        public override string GetTitleText()
        {
            return "GPUI Terrain";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Terrain";
        }
    }
}