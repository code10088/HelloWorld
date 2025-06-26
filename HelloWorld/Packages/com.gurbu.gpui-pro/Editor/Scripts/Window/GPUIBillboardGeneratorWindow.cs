// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIBillboardGeneratorWindow : GPUIPreviewSceneWindow
    {
        private GameObject _originalGO;

        private GameObject _originalGOPreview;

        private Mesh _previewMesh;
        private Material _previewMaterial;
        private GPUIBillboard _billboard;
        private bool _isBillboardPreview;

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Show Billboard Generator", validate = false, priority = 511)]
        public static GPUIBillboardGeneratorWindow ShowWindow()
        {
            if (Application.isPlaying)
                return null;
            if (SceneView.lastActiveSceneView == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find Scene View!");
                return null;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                GPUIBillboardGeneratorWindow window = GetWindow<GPUIBillboardGeneratorWindow>(false, "GPUI Billboard Generator", true);
                window.minSize = new Vector2(400, 560);

                window.Initialize();

                return window;
            }
            return null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isScrollable = true;
        }

        public void SetBillboard(GPUIBillboard billboard)
        {
            _billboard = billboard;
            _originalGO = _billboard.prefabObject;
            _isBillboardPreview = true;
            GeneratePreview();
        }

        protected override void DrawContents()
        {
            GPUIEditorTextUtility.GPUIText gpuiText;
            if (IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("gpuiBillboardGeneratorWindow", out gpuiText))
                EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
            if (_originalGO == null)
            {
                EditorGUILayout.HelpBox("Set a prefab to the Original GO field to preview the billboard.", MessageType.Info);
                EditorGUILayout.Space(10);
            }

            EditorGUI.BeginDisabledGroup(_isBillboardPreview);
            _originalGO = (GameObject)EditorGUILayout.ObjectField("Original GO", _originalGO, typeof(GameObject), false);
            if(_isBillboardPreview)
                EditorGUILayout.ObjectField("Billboard Asset", _billboard, typeof(GPUIBillboard), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);

            if (_originalGO != null && _billboard != null)
            {
                _billboard.atlasResolution = (GPUIBillboard.GPUIBillboardResolution)EditorGUILayout.EnumPopup("Resolution", _billboard.atlasResolution);
                _billboard.frameCount = EditorGUILayout.IntSlider("Frame Count", _billboard.frameCount, 1, 16);

                _billboard.cutoffOverride = EditorGUILayout.Slider("Billboard CutOff", _billboard.cutoffOverride, 0f, 1f);
                _billboard.brightness = EditorGUILayout.Slider("Brightness", _billboard.brightness, 0f, 1f);
                _billboard.normalStrength = EditorGUILayout.Slider("Normal Strength", _billboard.normalStrength, 0f, 1f);
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    _billboard.billboardShaderType = (GPUIBillboard.GPUIBillboardShaderType)EditorGUILayout.EnumPopup("Shader Type", _billboard.billboardShaderType);

                EditorGUILayout.LabelField("Quad Size: " + _billboard.quadSize + " Division: " + (_billboard.quadSize.x / _billboard.quadSize.y));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Albedo Texture", _billboard.albedoAtlasRT, typeof(Texture), false);
                EditorGUILayout.ObjectField("Normal Texture", _billboard.normalAtlasRT, typeof(Texture), false);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(10);

            if (IsShowHelpText() && GPUIEditorTextUtility.TryGetGPUIText("gpuiBillboardGeneratorButtons", out gpuiText))
                EditorGUILayout.HelpBox(gpuiText.helpText, MessageType.Info);
        }

        protected override void DrawAdditionalButtons()
        {
            base.DrawAdditionalButtons();

            GPUIEditorUtility.DrawColoredButton(new GUIContent("Save Standalone"), GPUIEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero, () => SaveBillboard(true));
            EditorGUILayout.Space(10);
        }

        protected override GameObject GeneratePreview()
        {
            Dispose();

            if (_originalGO != null)
            {
                if (_billboard == null)
                    _billboard = GPUIBillboardUtility.GenerateBillboardData(_originalGO);

                _originalGOPreview = Instantiate(_originalGO, new Vector3(-_billboard.quadSize.x / 2f, _billboard.yPivotOffset, 0), Quaternion.identity);
                _originalGOPreview.hideFlags = HideFlags.DontSave;
                _originalGOPreview.transform.localScale = Vector3.one;

                GPUIBillboardUtility.GenerateBillboard(_billboard);

                _previewGO = GenerateBillboardPreview(_billboard, out _previewMesh, out _previewMaterial);
            }

            return _previewGO;
        }

        public static GameObject GenerateBillboardPreview(GPUIBillboard billboard, out Mesh previewMesh, out Material previewMaterial)
        {
            GameObject previewGO = new GameObject(billboard.prefabObject.name + "_Billboard", typeof(MeshFilter), typeof(MeshRenderer));
            previewGO.transform.position = new Vector3(billboard.quadSize.x / 2f, billboard.yPivotOffset, 0);
            previewGO.hideFlags = HideFlags.DontSave;

            previewMesh = GPUIBillboardUtility.GenerateQuadMesh(billboard);
            previewMaterial = GPUIBillboardUtility.CreateBillboardMaterial(billboard);

            previewGO.GetComponent<MeshFilter>().mesh = previewMesh;
            MeshRenderer previewMeshRenderer = previewGO.GetComponent<MeshRenderer>();
            previewMeshRenderer.material = previewMaterial;
            previewMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return previewGO;
        }

        protected override void Dispose()
        {
            base.Dispose();

            if (_originalGOPreview != null)
                DestroyImmediate(_originalGOPreview);
            _originalGOPreview = null;

            if (_previewMesh != null)
                DestroyImmediate(_previewMesh);
            if (_previewMaterial != null)
                DestroyImmediate(_previewMaterial);

            if (_billboard != null)
            {
                GPUITextureUtility.DestroyRenderTexture(_billboard.albedoAtlasRT);
                GPUITextureUtility.DestroyRenderTexture(_billboard.normalAtlasRT);
                if (_billboard.prefabObject != _originalGO)
                    _billboard = null;
            }
        }

        protected override void OnSave()
        {
            SaveBillboard(false);
        }

        private void SaveBillboard(bool isStandalone)
        {
            if (_isBillboardPreview)
            {
                SaveBillboardToFolder(Path.GetDirectoryName(AssetDatabase.GetAssetPath(_billboard)), isStandalone);
                return;
            }

            _errorText = null;
            string folderPath = EditorUtility.SaveFolderPanel("Save Billboard", Path.GetDirectoryName(AssetDatabase.GetAssetPath(_originalGO)), "");
            if (folderPath.Length != 0)
            {
                folderPath = folderPath.Replace(Application.dataPath, "Assets");
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    string folderSuffix = "/" + _originalGO.name + "_Billboard";
                    if (!folderPath.EndsWith(folderSuffix))
                        folderPath += folderSuffix;

                    SaveBillboardToFolder(folderPath, isStandalone);
                }
                else
                {
                    _errorText = "Please select a folder within the project to Save Billboard Assets.";
                }
            }
        }

        private void SaveBillboardToFolder(string folderPath, bool isStandalone)
        {
            GPUIBillboardUtility.SaveBillboardAsAsset(_billboard);

            if (isStandalone)
            {
                _previewMesh.SaveAsAsset(folderPath + "/", _originalGO.name + "_Mesh.mesh");

                _previewMaterial.SetTexture("_AlbedoAtlas", _billboard.albedoAtlasTexture);
                _previewMaterial.SetTexture("_NormalAtlas", _billboard.normalAtlasTexture);
                _previewMaterial.SaveAsAsset(folderPath + "/", _originalGO.name + "_Material.mat");

                _previewGO.GetComponent<MeshFilter>().mesh = _previewMesh;
                MeshRenderer previewMeshRenderer = _previewGO.GetComponent<MeshRenderer>();
                previewMeshRenderer.material = _previewMaterial;
                _previewGO.transform.position = Vector3.zero;
                _previewGO.transform.rotation = Quaternion.identity;
                _previewGO.transform.localScale = Vector3.one;
                _previewGO.SaveAsAsset(folderPath + "/", _originalGO.name + "_Billboard.prefab");
            }

            _previewMesh = null; // To avoid destroy
            _previewMaterial = null; // To avoid destroy
            Debug.Log(GPUIConstants.LOG_PREFIX + "Billboard assets saved at: " + folderPath, AssetDatabase.LoadAssetAtPath(folderPath, typeof(DefaultAsset)));
            SetBillboard(_billboard);
            GeneratePreview();
            GUIUtility.ExitGUI();
            //this.Close();
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#Billboard_Generator_Window";
        }

        public override string GetTitleText()
        {
            return "GPUI Billboard Generator";
        }
    }
}