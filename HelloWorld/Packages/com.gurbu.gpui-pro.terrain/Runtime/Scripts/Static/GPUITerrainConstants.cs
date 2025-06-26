// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    public static class GPUITerrainConstants
    {
        #region Paths & File Names

        public const string FILE_DEFAULT_DETAIL_TEXTURE_PROFILE = "GPUIDefaultDetailTextureProfile";
        public const string FILE_DEFAULT_DETAIL_PREFAB_PROFILE = "GPUIDefaultDetailPrefabProfile";
        public const string FILE_DEFAULT_TREE_PROFILE = "GPUIDefaultTreeProfile";
        public const string FILE_DEFAULT_DETAIL_MESH = "DefaultDetailMesh";
        public const string FILE_DEFAULT_DETAIL_MATERIAL = "DefaultDetailMaterial";
        public const string FILE_DEFAULT_DETAIL_MATERIAL_DESC = "DefaultDetailMaterialDesc";
        public const string NAME_SUFFIX_DETAILTEXTURE = "_GPUIDetailLayer_";

        private static string _packagesPath;
        public static string GetPackagesPath()
        {
            if (string.IsNullOrEmpty(_packagesPath))
                _packagesPath = "Packages/com.gurbu.gpui-pro.terrain/";
            return _packagesPath;
        }

        #endregion Paths & File Names

        #region Shaders
        public static readonly string SHADER_DEFAULT_DETAIL_Builtin = "GPUInstancerPro/Foliage";
        public static readonly string SHADER_DEFAULT_DETAIL_Lambert_Builtin = "GPUInstancerPro/FoliageLambert";
        public static readonly string SHADER_DEFAULT_DETAIL_URP = "GPUInstancerPro/Foliage_SG";
        public static readonly string SHADER_DEFAULT_DETAIL_HDRP = "GPUInstancerPro/Foliage_SG";
        #endregion Shaders

        #region Default Assets

        private static GPUIProfile _defaultDetailTextureProfile;
        public static GPUIProfile DefaultDetailTextureProfile
        {
            get
            {
                if (_defaultDetailTextureProfile == null)
                {
#if UNITY_EDITOR
                    _defaultDetailTextureProfile = AssetDatabase.LoadAssetAtPath<GPUIProfile>(GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_PROFILES + FILE_DEFAULT_DETAIL_TEXTURE_PROFILE + ".asset");
                    if (_defaultDetailTextureProfile == null)
                    {
#endif
                        _defaultDetailTextureProfile = ScriptableObject.CreateInstance<GPUIProfile>();
                        _defaultDetailTextureProfile.isShadowCasting = false;
                        _defaultDetailTextureProfile.isDistanceCulling = false;
                        _defaultDetailTextureProfile.isDefaultProfile = true;
#if UNITY_EDITOR
                    }
#endif
                }
                return _defaultDetailTextureProfile;
            }
        }

        private static GPUIProfile _defaultDetailPrefabProfile;
        public static GPUIProfile DefaultDetailPrefabProfile
        {
            get
            {
                if (_defaultDetailPrefabProfile == null)
                {
#if UNITY_EDITOR
                    _defaultDetailPrefabProfile = AssetDatabase.LoadAssetAtPath<GPUIProfile>(GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_PROFILES + FILE_DEFAULT_DETAIL_PREFAB_PROFILE + ".asset");
                    if (_defaultDetailPrefabProfile == null)
                    {
#endif
                        _defaultDetailPrefabProfile = ScriptableObject.CreateInstance<GPUIProfile>();
                        _defaultDetailPrefabProfile.isShadowCasting = true;
                        _defaultDetailPrefabProfile.isShadowFrustumCulling = true;
                        _defaultDetailPrefabProfile.isShadowOcclusionCulling = true;
                        _defaultDetailPrefabProfile.isDistanceCulling = false;
                        _defaultDetailPrefabProfile.isDefaultProfile = true;
#if UNITY_EDITOR
                    }
#endif
                }
                return _defaultDetailPrefabProfile;
            }
        }

        private static GPUIProfile _defaultTreeProfile;
        public static GPUIProfile DefaultTreeProfile
        {
            get
            {
                if (_defaultTreeProfile == null)
                {
#if UNITY_EDITOR
                    _defaultTreeProfile = AssetDatabase.LoadAssetAtPath<GPUIProfile>(GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_PROFILES + FILE_DEFAULT_TREE_PROFILE + ".asset");
                    if (_defaultTreeProfile == null)
                    {
#endif
                        _defaultTreeProfile = ScriptableObject.CreateInstance<GPUIProfile>();
                        _defaultTreeProfile.isShadowCasting = true;
                        _defaultTreeProfile.isLODCrossFade = true;
                        _defaultTreeProfile.isDistanceCulling = false;
                        _defaultTreeProfile.isDefaultProfile = true;
#if UNITY_EDITOR
                    }
#endif
                }
                return _defaultTreeProfile;
            }
        }

        private static Mesh _defaultDetailMesh;
        public static Mesh DefaultDetailMesh
        {
            get
            {
                if (_defaultDetailMesh == null)
                {
#if UNITY_EDITOR
                    _defaultDetailMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GPUIConstants.GetStandardAssetsPath() + FILE_DEFAULT_DETAIL_MESH + ".mesh");
                    if (_defaultDetailMesh == null)
                    {
#endif
                        _defaultDetailMesh = GPUITerrainUtility.CreateCrossQuadsMesh(FILE_DEFAULT_DETAIL_MESH, 1);
#if UNITY_EDITOR
                        _defaultDetailMesh.SaveAsAsset(GPUIConstants.GetStandardAssetsPath(), FILE_DEFAULT_DETAIL_MESH + ".mesh");
                    }
#endif
                }
                return _defaultDetailMesh;
            }
            set
            {
                if (value != null)
                    _defaultDetailMesh = value;
            }
        }

        private static Material _defaultDetailMaterial;
        public static Material DefaultDetailMaterial
        {
            get
            {
                if (_defaultDetailMaterial == null)
                {
#if UNITY_EDITOR
                    _defaultDetailMaterial = AssetDatabase.LoadAssetAtPath<Material>(GPUIConstants.GetStandardAssetsPath() + FILE_DEFAULT_DETAIL_MATERIAL + ".mat");
                    if (_defaultDetailMaterial == null)
                    {
#endif
                        Shader detailShader = GetDefaultDetailShader();
                        if (detailShader == null)
                            return new Material(GetDefaultDetailShaderFallback());
                        _defaultDetailMaterial = new Material(detailShader);
                        if (DefaultHealthyDryNoiseTexture != null)
                            _defaultDetailMaterial.SetTexture("_HealthyDryNoiseTexture", DefaultHealthyDryNoiseTexture);
                        if (DefaultNoiseNormal != null)
                            _defaultDetailMaterial.SetTexture("_WindWaveNormalTexture", DefaultNoiseNormal);
                        _defaultDetailMaterial.SetFloat("_WindWavesOn", 1);
                        if (QualitySettings.billboardsFaceCameraPosition)
                            _defaultDetailMaterial.EnableKeyword(GPUIConstants.Kw_BILLBOARD_FACE_CAMERA_POS);
                        else
                            _defaultDetailMaterial.DisableKeyword(GPUIConstants.Kw_BILLBOARD_FACE_CAMERA_POS);
#if UNITY_EDITOR
                        _defaultDetailMaterial.SaveAsAsset(GPUIConstants.GetStandardAssetsPath(), FILE_DEFAULT_DETAIL_MATERIAL + ".mat");
                    }
#endif
                }
                return _defaultDetailMaterial;
            }
        }

        public static Shader GetDefaultDetailShader()
        {
            switch (GPUIRuntimeSettings.Instance.RenderPipeline)
            {
                case GPUIRenderPipeline.URP:
                    return GPUIUtility.FindShader(SHADER_DEFAULT_DETAIL_URP);
                case GPUIRenderPipeline.HDRP:
                    return GPUIUtility.FindShader(SHADER_DEFAULT_DETAIL_HDRP);
                default:
                    return GPUIUtility.FindShader(SHADER_DEFAULT_DETAIL_Builtin);
            }
        }

        public static Shader GetDefaultDetailShaderFallback()
        {
            switch (GPUIRuntimeSettings.Instance.RenderPipeline)
            {
                case GPUIRenderPipeline.URP:
                    return GPUIUtility.FindShader("Universal Render Pipeline/Lit");
                case GPUIRenderPipeline.HDRP:
                    return GPUIUtility.FindShader("HDRP/Lit");
                default:
                    return GPUIUtility.FindShader("Standard");
            }
        }

        public static bool IsDefaultDetailShader(Shader shader)
        {
            return shader != null && shader.name.StartsWith(SHADER_DEFAULT_DETAIL_Builtin);
        }

        private static GPUIDetailMaterialDescription _defaultDetailMaterialDescription;
        public static GPUIDetailMaterialDescription DefaultDetailMaterialDescription
        {
            get
            {
                if (_defaultDetailMaterialDescription == null)
                {
#if UNITY_EDITOR
                    _defaultDetailMaterialDescription = AssetDatabase.LoadAssetAtPath<GPUIDetailMaterialDescription>(GPUIConstants.GetStandardAssetsPath() + FILE_DEFAULT_DETAIL_MATERIAL_DESC + ".asset");

                    if (_defaultDetailMaterialDescription == null)
                    {
#endif
                        _defaultDetailMaterialDescription = ScriptableObject.CreateInstance<GPUIDetailMaterialDescription>();
                        _defaultDetailMaterialDescription.SetDefaultValues();
#if UNITY_EDITOR
                        _defaultDetailMaterialDescription.SaveAsAsset(GPUIConstants.GetStandardAssetsPath(), FILE_DEFAULT_DETAIL_MATERIAL_DESC + ".asset");
                    }
#endif
                }
                return _defaultDetailMaterialDescription;
            }
        }

        private static Texture2D _defaultHealthyDryNoiseTexture;
        public static Texture2D DefaultHealthyDryNoiseTexture
        {
            get
            {
#if UNITY_EDITOR
                if (_defaultHealthyDryNoiseTexture == null)
                {
                    string noisePath = GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_TEXTURES + "Noise/";
                    _defaultHealthyDryNoiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(noisePath + "Fractal_Simplex_grayscale.png");
                }
#endif
                return _defaultHealthyDryNoiseTexture;
            }
        }

        private static Texture2D _defaultNoiseNormal;
        public static Texture2D DefaultNoiseNormal
        {
            get
            {
#if UNITY_EDITOR
                if (_defaultNoiseNormal == null)
                {
                    string noisePath = GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_TEXTURES + "Noise/";
                    _defaultNoiseNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(noisePath + "Fractal_Simplex_normal.png");
                }
#endif
                return _defaultNoiseNormal;
            }
        }

        #endregion Default Assets

        #region Compute Shaders

#if UNITY_EDITOR
        /// <summary>
        /// Sometimes Unity does not import Compute Shader files correctly the first time when they have file references in other packages.
        /// So we check for compiler errors here and reimport the Compute Shaders.
        /// </summary>
        public static void CheckForComputeCompilerErrors()
        {
            if (GPUIUtility.ComputeShaderHasCompilerErrors(CS_TerrainDetailCapture)
                || GPUIUtility.ComputeShaderHasCompilerErrors(CS_VegetationGenerator)
                || GPUIUtility.ComputeShaderHasCompilerErrors(CS_TerrainTreeGenerator)
                || GPUIUtility.ComputeShaderHasCompilerErrors(CS_TerrainDetailDensityModifier)
                )
                ReimportComputeShaders();
        }

        public static void ReimportComputeShaders()
        {
            GPUIUtility.ReimportFilesInFolder(GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_COMPUTE, "*.hlsl");
            GPUIUtility.ReimportFilesInFolder(GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_COMPUTE, "*.compute");
        }
#endif

        public static readonly string FILE_CS_TerrainDetailCapture = "GPUITerrainDetailCaptureCS";
        private static ComputeShader _CS_TerrainDetailCapture;
        public static ComputeShader CS_TerrainDetailCapture
        {
            get
            {
                if (_CS_TerrainDetailCapture == null)
                    _CS_TerrainDetailCapture = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TerrainDetailCapture);
                return _CS_TerrainDetailCapture;
            }
        }

        public static readonly string FILE_CS_TerrainDetailCaptureFromInstanceTransforms = "GPUITerrainDetailCaptureFromInstanceTransformsCS";
        private static ComputeShader _CS_TerrainDetailCaptureFromInstanceTransforms;
        public static ComputeShader CS_TerrainDetailCaptureFromInstanceTransforms
        {
            get
            {
                if (_CS_TerrainDetailCaptureFromInstanceTransforms == null)
                    _CS_TerrainDetailCaptureFromInstanceTransforms = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TerrainDetailCaptureFromInstanceTransforms);
                return _CS_TerrainDetailCaptureFromInstanceTransforms;
            }
        }

        public static readonly string FILE_CS_VegetationGenerator = "GPUIVegetationGeneratorCS";
        private static ComputeShader _CS_VegetationGenerator;
        public static ComputeShader CS_VegetationGenerator
        {
            get
            {
                if (_CS_VegetationGenerator == null)
                    _CS_VegetationGenerator = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_VegetationGenerator);
                return _CS_VegetationGenerator;
            }
        }
        public static readonly string Kw_GPUI_DETAIL_DENSITY_REDUCE = "GPUI_DETAIL_DENSITY_REDUCE";
        public static readonly string Kw_GPUI_TERRAIN_HOLES = "GPUI_TERRAIN_HOLES";
        public static readonly string Kw_GPUI_TWO_CHANNEL_HEIGHTMAP = "GPUI_TWO_CHANNEL_HEIGHTMAP";

        public static readonly string FILE_CS_TerrainTreeGenerator = "GPUITerrainTreeGeneratorCS";
        private static ComputeShader _CS_TerrainTreeGenerator;
        public static ComputeShader CS_TerrainTreeGenerator
        {
            get
            {
                if (_CS_TerrainTreeGenerator == null)
                    _CS_TerrainTreeGenerator = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TerrainTreeGenerator);
                return _CS_TerrainTreeGenerator;
            }
        }
        public static readonly string Kw_GPUI_TREE_INSTANCE_COLOR = "GPUI_TREE_INSTANCE_COLOR";

        public static readonly string FILE_CS_TerrainDetailDensityModifier = "GPUITerrainDetailDensityModifierCS";
        private static ComputeShader _CS_TerrainDetailDensityModifier;
        public static ComputeShader CS_TerrainDetailDensityModifier
        {
            get
            {
                if (_CS_TerrainDetailDensityModifier == null)
                    _CS_TerrainDetailDensityModifier = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TerrainDetailDensityModifier);
                return _CS_TerrainDetailDensityModifier;
            }
        }

        #endregion Compute Shaders

        #region Shader Props

        public static readonly int PROP_terrainDetailTexture = Shader.PropertyToID("terrainDetailTexture");
        public static readonly int PROP_detailCounterBuffer = Shader.PropertyToID("detailCounterBuffer");
        public static readonly int PROP_terrainHoleTexture = Shader.PropertyToID("terrainHoleTexture");
        public static readonly int PROP_detailLayerBuffer = Shader.PropertyToID("detailLayerBuffer");
        public static readonly int PROP_detailResolution = Shader.PropertyToID("detailResolution");
        public static readonly int PROP_heightmapTexture = Shader.PropertyToID("heightmapTexture");
        public static readonly int PROP_terrainHeightmapResolution = Shader.PropertyToID("terrainHeightmapResolution");
        public static readonly int PROP_terrainPosition = Shader.PropertyToID("terrainPosition");
        public static readonly int PROP_terrainSize = Shader.PropertyToID("terrainSize");
        public static readonly int PROP_alphaMapTexture = Shader.PropertyToID("alphaMapTexture");
        public static readonly int PROP_alphamapResolution = Shader.PropertyToID("alphamapResolution");
        public static readonly int PROP_detailTextureSize = Shader.PropertyToID("detailTextureSize");
        public static readonly int PROP_heightmapTextureSize = Shader.PropertyToID("heightmapTextureSize");
        public static readonly int PROP_startPosition = Shader.PropertyToID("startPosition");
        public static readonly int PROP_cameraPos = Shader.PropertyToID("cameraPos");
        public static readonly int PROP_density = Shader.PropertyToID("density");
        public static readonly int PROP_detailObjectDistance = Shader.PropertyToID("detailObjectDistance");
        public static readonly int PROP_healthyDryNoiseTexture = Shader.PropertyToID("healthyDryNoiseTexture");
        public static readonly int PROP_gpuiTreeInstanceDataBuffer = Shader.PropertyToID("gpuiTreeInstanceDataBuffer");
        public static readonly int PROP_terrainPrototypeIndex = Shader.PropertyToID("terrainPrototypeIndex");

        public static readonly int PROP_treeData = Shader.PropertyToID("treeData");
        public static readonly int PROP_prefabScale = Shader.PropertyToID("prefabScale");
        public static readonly int PROP_applyPrefabScale = Shader.PropertyToID("applyPrefabScale");
        public static readonly int PROP_applyRotation = Shader.PropertyToID("applyRotation");
        public static readonly int PROP_applyHeight = Shader.PropertyToID("applyHeight");

        #endregion Shader Props

        public static RenderTextureFormat R8_RenderTextureFormat => GPUIRuntimeSettings.Instance.API_HAS_GUARANTEED_R8_SUPPORT? RenderTextureFormat.R8 : RenderTextureFormat.RFloat;
        public static RenderTextureFormat R16_RenderTextureFormat => GPUIRuntimeSettings.Instance.API_HAS_GUARANTEED_R8_SUPPORT ? RenderTextureFormat.R16 : RenderTextureFormat.RFloat;
    }
}