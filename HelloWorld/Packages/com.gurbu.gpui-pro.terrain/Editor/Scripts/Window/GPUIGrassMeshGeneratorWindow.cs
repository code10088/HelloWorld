// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    public class GPUIGrassMeshGeneratorWindow : GPUIPreviewSceneWindow
    {
        private Texture2D _grassTexture;
        private int _seed;
        private Color _grassColor = new Color(0.1f, 0.6f, 0);
        private int _meshCount = 1;
        private Vector2 _minMaxWidth = Vector2.one;
        private Vector2 _minMaxHeight = Vector2.one;
        private float _spread = 0.2f;

        private PatchMode _patchMode;

        private QuadMode _quadMode;

        private Vector2 _quadFlowerMinMaxBend = new Vector2(10, 30);

        private int _bladeSegmentCount = 2;
        private AnimationCurve _bladeBendCurve;
        private Vector2 _bladeMinMaxBend = new Vector2(1, 1);
        private Vector2 _bladeMinMaxBendLowerAmount = new Vector2(0.2f, 0.2f);
        private AnimationCurve _bladeWidthCurve;

        private Mesh[] _patchMeshes;
        private Material _grassMaterial;

        private bool _setVertexColors;

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Show Grass Mesh Generator", validate = false, priority = 521)]
        public static void ShowWindow()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find Scene View!");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {

                GPUIGrassMeshGeneratorWindow window = EditorWindow.GetWindow<GPUIGrassMeshGeneratorWindow>(false, "GPUI Grass Mesh Generator", true);
                window.minSize = new Vector2(400, 560);
                window.Initialize();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isScrollable = true;
        }

        protected override void DrawContents()
        {
            if (IsShowHelpText())
            {
                EditorGUILayout.HelpBox("The Grass Mesh Generator tool facilitates the creation of custom meshes for texture detail prototypes managed by the Detail Manager. This feature enables users to tailor the appearance of grass and other textured details to suit their specific project requirements.", MessageType.Info);
            }

            EditorGUILayout.HelpBox("You can also move child transforms from the Hierarchy window before saving. But when values are changed the transforms will reset.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            Rect bRect = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(false));
            GPUIEditorUtility.DrawColoredButton(new GUIContent("Random Seed"), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Bold, bRect, () => _seed = UnityEngine.Random.Range(1, 1000));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            _grassTexture = (Texture2D)EditorGUILayout.ObjectField("Grass Texture", _grassTexture, typeof(Texture2D), true);
            _grassColor = EditorGUILayout.ColorField("Grass Color", _grassColor);
            _meshCount = EditorGUILayout.IntSlider("Count", _meshCount, 1, 10);

            DrawMinMaxField("Min-Max Width", ref _minMaxWidth, 0f, 3f);
            DrawMinMaxField("Min-Max Height", ref _minMaxHeight, 0f, 3f);

            _setVertexColors = EditorGUILayout.Toggle("Vertex Colors", _setVertexColors);

            _patchMode = (PatchMode)EditorGUILayout.EnumPopup("Patch Mode", _patchMode);

            if (_patchMode == PatchMode.Quad)
            {
                _quadMode = (QuadMode)EditorGUILayout.EnumPopup("Quad Mode", _quadMode);

                if (_quadMode == QuadMode.Flower)
                {
                    DrawMinMaxField("Min-Max Bend", ref _quadFlowerMinMaxBend, 0f, 90f);
                    _spread = EditorGUILayout.FloatField("Spread", _spread);
                }
            }
            else if (_patchMode == PatchMode.Blade)
            {
                _bladeSegmentCount = EditorGUILayout.IntSlider("Segment Count", _bladeSegmentCount, 1, 10);
                _bladeBendCurve = EditorGUILayout.CurveField("Blade Bend Curve", _bladeBendCurve);
                DrawMinMaxField("Min-Max Bend", ref _bladeMinMaxBend, 0f, 1f);
                DrawMinMaxField("Bend Lower Amount", ref _bladeMinMaxBendLowerAmount, 0f, 2f);
                _bladeWidthCurve = EditorGUILayout.CurveField("Blade Width Curve", _bladeWidthCurve);
                _spread = EditorGUILayout.FloatField("Spread", _spread);
            }
        }

        protected override GameObject GeneratePreview()
        {
            if (_grassTexture == null)
                _grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GPUIConstants.GetDefaultUserDataPath() + "Demos/Core/_SharedResources/Textures/Nature/GrassTuft.png");
                
            Dispose();

            if (_bladeWidthCurve == null)
                _bladeWidthCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.15f, 0f, 0f), new Keyframe(1, 0.10f, 0f, 0f) });

            if (_bladeBendCurve == null)
                _bladeBendCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0f, 1f, 0f), new Keyframe(1, 0.5f, 1f, 0f) });

            _grassMaterial = new Material(GPUITerrainConstants.DefaultDetailMaterial);
            _grassMaterial.SetColor("_Color", new Color(0.05f, 0.6f, 0, 1));

            _previewGO = new GameObject("Grass Patch");
            _previewGO.hideFlags = HideFlags.DontSave;

            if (_patchMode == PatchMode.Quad)
            {
                _grassMaterial.SetTexture("_MainTex", _grassTexture);
                _grassMaterial.SetColor("_DryColor", _grassColor);
                _grassMaterial.SetColor("_HealthyColor", _grassColor);
                _grassMaterial.SetFloat("_Cutoff", 0.5f);
                GenerateQuadChildren();
            }
            else
            {
                _grassMaterial.SetTexture("_BaseMap", null);
                _grassMaterial.SetColor("_DryColor", _grassColor);
                _grassMaterial.SetColor("_HealthyColor", _grassColor);
                _grassMaterial.SetFloat("_Cutoff", 0);
                GenerateBladeChildren();
            }

            return _previewGO;
        }

        private void GenerateQuadChildren()
        {
            UnityEngine.Random.InitState(_seed);
            _patchMeshes = new Mesh[_meshCount];
            for (int i = 0; i < _meshCount; i++)
            {
                float randomSize = UnityEngine.Random.Range(0, 1f);
                _patchMeshes[i] = GPUIUtility.GenerateQuadMesh(_minMaxWidth.x + ((_minMaxWidth.y - _minMaxWidth.x) * randomSize), _minMaxHeight.x + ((_minMaxHeight.y - _minMaxHeight.x) * randomSize), null, true, 0, 0, _setVertexColors);
                GameObject quadGO = new GameObject("Quad " + i);
                quadGO.transform.SetParent(_previewGO.transform);
                MeshRenderer mr = quadGO.AddComponent<MeshRenderer>();
                MeshFilter mf = quadGO.AddComponent<MeshFilter>();
                mf.mesh = _patchMeshes[i];
                mr.material = _grassMaterial;

                if (_quadMode == QuadMode.Cross)
                    quadGO.transform.localRotation = Quaternion.Euler(0, (180f / _meshCount) * i, 0);
                else if (_quadMode == QuadMode.Flower)
                {
                    Vector2 randomPos = UnityEngine.Random.insideUnitCircle * _spread;
                    quadGO.transform.localPosition = new Vector3(randomPos.x, 0, randomPos.y);
                    quadGO.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(_quadFlowerMinMaxBend.x, _quadFlowerMinMaxBend.y), (360f / _meshCount) * i, 0);
                }
            }
        }

        private void GenerateBladeChildren()
        {
            UnityEngine.Random.InitState(_seed);
            _patchMeshes = new Mesh[_meshCount];
            for (int i = 0; i < _meshCount; i++)
            {
                float randomSize = UnityEngine.Random.Range(0, 1f);
                float bendMultiplier = UnityEngine.Random.Range(_bladeMinMaxBend.x, _bladeMinMaxBend.y);
                float lowerAmount = UnityEngine.Random.Range(_bladeMinMaxBendLowerAmount.x, _bladeMinMaxBendLowerAmount.y);
                _patchMeshes[i] = GPUITerrainUtility.GenerateBladeMesh(new Vector2(_minMaxWidth.x + ((_minMaxWidth.y - _minMaxWidth.x) * randomSize), _minMaxHeight.x + ((_minMaxHeight.y - _minMaxHeight.x) * randomSize)), _bladeSegmentCount, bendMultiplier, lowerAmount, _bladeBendCurve, _bladeWidthCurve);
                GameObject bladeGO = new GameObject("Blade " + i);
                bladeGO.transform.SetParent(_previewGO.transform);

                Vector2 randomPos = UnityEngine.Random.insideUnitCircle * _spread;
                bladeGO.transform.localPosition = new Vector3(randomPos.x, 0, randomPos.y);
                bladeGO.transform.localRotation = Quaternion.Euler(0, (360f / _meshCount) * i, 0);

                MeshRenderer mr = bladeGO.AddComponent<MeshRenderer>();
                MeshFilter mf = bladeGO.AddComponent<MeshFilter>();
                mf.mesh = _patchMeshes[i];
                mr.material = _grassMaterial;
            }
        }

        protected override void Dispose()
        {
            base.Dispose();

            if (_grassMaterial != null)
                DestroyImmediate(_grassMaterial);
            if (_patchMeshes != null)
            {
                for (int i = 0; i < _patchMeshes.Length; i++)
                {
                    if (_patchMeshes[i])
                        DestroyImmediate(_patchMeshes[i]);
                }
            }
            _patchMeshes = null;
        }

        protected override void OnSave()
        {
            string path = EditorUtility.SaveFilePanel(
               "Save Grass Mesh",
               Application.dataPath,
               "GrassMesh.mesh",
               "mesh");

            if (path.Length != 0)
            {
                path = path.Replace(Application.dataPath, "Assets");
                string directoryPath = System.IO.Path.GetDirectoryName(path);
                if (AssetDatabase.IsValidFolder(directoryPath))
                {
                    CombineInstance[] combineInstances = new CombineInstance[_previewGO.transform.childCount];
                    for (int i = 0; i < _previewGO.transform.childCount; i++)
                    {
                        Transform quad = _previewGO.transform.GetChild(i);
                        combineInstances[i] = new CombineInstance()
                        {
                            transform = quad.localToWorldMatrix,
                            mesh = quad.GetComponent<MeshFilter>().sharedMesh
                        };
                    }
                    Mesh resultMesh = new Mesh();
                    resultMesh.CombineMeshes(combineInstances, true, true, false);
                    AssetDatabase.CreateAsset(resultMesh, path);
                    AssetDatabase.SaveAssets();
                    Debug.Log(GPUIConstants.LOG_PREFIX + "Mesh saved at: " + path, resultMesh);

                    _errorText = null;
                    this.Close();
                }
                else
                {
                    _errorText = "Please select a folder within the project to Save Grass Mesh.";
                }
            }
        }

        private static void DrawMinMaxField(string label, ref Vector2 minMaxValue, float sliderMin, float sliderMax)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.MinMaxSlider(label, ref minMaxValue.x, ref minMaxValue.y, sliderMin, sliderMax);
            minMaxValue.x = EditorGUI.FloatField(GUILayoutUtility.GetRect(40, 20, GUILayout.ExpandWidth(false)), minMaxValue.x);
            minMaxValue.y = EditorGUI.FloatField(GUILayoutUtility.GetRect(40, 20, GUILayout.ExpandWidth(false)), minMaxValue.y);
            EditorGUILayout.EndHorizontal();
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#Grass_Mesh_Generator_Window";
        }

        public override string GetTitleText()
        {
            return "GPUI Grass Mesh Generator";
        }

        public enum PatchMode
        {
            Quad = 0,
            Blade = 1
        }

        public enum QuadMode
        {
            Cross = 0,
            Flower = 1
        }
    }
}