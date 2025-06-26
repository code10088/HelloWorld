// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public static class GPUIConstants
    {
        #region Paths & File Names

        public const string PATH_LOCATOR_GUID = "70144417e5298854fbabfec8cdd9eb41";
        public const string PATH_RESOURCES = "Resources/";
        public const string PATH_SETTINGS = "Settings/";
        public const string PATH_RUNTIME = "Runtime/";
        public const string PATH_PREFABS = "Prefabs/";
        public const string PATH_EDITOR = "Editor/";
        public const string PATH_SHADER = "Shaders/";
        public const string PATH_COMPUTE = "Compute/";
        public const string PATH_SHADER_INCLUDE = "Include/";
        public const string PATH_TEXTURES = "Textures/";
        public const string PATH_STANDARD_ASSETS = "StandardAssets/";
        public const string PATH_PROFILES = "Profiles/";
        public const string PATH_PROCEDURALDATA = "ProceduralData/";
        public const string PATH_EXTENSIONS = "Extensions/";

        public const string FILE_EDITOR_SETTINGS = "GPUIEditorSettings";
        public const string FILE_RUNTIME_SETTINGS = "GPUIRuntimeSettings";
        public const string FILE_SHADER_BINDINGS = "GPUIShaderBindings";
        public const string FILE_SHADER_VARIANT_COLLECTION = "GPUIShaderVariantCollection";
        public const string FILE_DEFAULT_PROFILE = "GPUIDefaultProfile";
        public const string FILE_PREVIEV_SCENE_ELEMENTS = "Preview/PreviewSceneElements";
        public const string FILE_CALIBRATION_FLOOR_MATERIAL = "Preview/CalibrationFloor";
        public const string FILE_DEBUGGER_CANVAS = "GPUIDebuggerCanvas";

        public const string FILE_CS_CameraVisibility = "GPUICameraVisibilityCS";
#if GPUI_XR
        public const string FILE_CS_CameraVisibilityXR = "GPUICameraVisibilityXRCS";
#endif
        public const string FILE_CS_CommandBufferUtility = "GPUICommandBufferUtilityCS";
        public const string FILE_CS_GraphicsBufferUtility = "GPUIGraphicsBufferUtilityCS";
        public const string FILE_CS_TextureUtility = "GPUITextureUtilityCS";
        public const string FILE_CS_HiZTextureCopy = "GPUIHiZTextureCopyCS";
        public const string FILE_CS_TextureReduce = "GPUITextureReduceCS";
        public const string FILE_CS_Billboard = "GPUIBillboardCS";
        public const string FILE_CS_BufferToTexture = "GPUIBufferToTextureCS";
        public const string FILE_CS_TransformModifications = "GPUITransformModificationsCS";
        public const string FILE_CS_OptionalRenderer = "GPUIOptionalRendererCS";
        public const string FILE_CS_LightProbeUtility = "GPUILightProbeUtilityCS";

        private const string _initialAssetsPath = "Assets/GPUInstancerPro/";

        /// <returns>GPUInstancerPro path. Default: Assets/GPUInstancerPro/</returns>
        public static string GetDefaultPath()
        {
#if UNITY_EDITOR
            string locatorPath = AssetDatabase.GUIDToAssetPath(PATH_LOCATOR_GUID);
            if (!string.IsNullOrEmpty(locatorPath))
                return locatorPath.Replace("Editor/GPUIPathLocator.asset", "");
#endif
            return _initialAssetsPath;
        }

        private static string _packagesPath;
        /// <returns>Packages/com.gurbu.gpui-pro/</returns>
        public static string GetPackagesPath()
        {
            if (string.IsNullOrEmpty(_packagesPath))
                _packagesPath = "Packages/com.gurbu.gpui-pro/";
            return _packagesPath;
        }

        /// <returns>GPUInstancerPro path. Default: Assets/GPUInstancerPro/</returns>
        public static string GetDefaultUserDataPath()
        {
            return GetDefaultPath();
        }

        public static string GetGeneratedShaderPath()
        {
            return GetDefaultUserDataPath() + PATH_SHADER;
        }

        public static string GetProfilesPath()
        {
            return GetDefaultUserDataPath() + PATH_PROFILES;
        }

        public static string GetStandardAssetsPath()
        {
            return GetDefaultUserDataPath() + PATH_STANDARD_ASSETS;
        }

        public static string GetProceduralDataPath()
        {
            return GetDefaultUserDataPath() + PATH_PROCEDURALDATA;
        }

        public static string GetExtensionsUserDataPath()
        {
            return GetDefaultPath() + PATH_EXTENSIONS;
        }

        #endregion Paths & File Names

        #region Default Assets

        public static Shader DefaultRPShader
        {
            get
            {
                switch (GPUIRuntimeSettings.Instance.RenderPipeline)
                {
                    case GPUIRenderPipeline.URP:
                        return Shader.Find("Universal Render Pipeline/Lit");
                    case GPUIRenderPipeline.HDRP:
                        return Shader.Find("HDRP/Lit");
                    default:
                        return Shader.Find("Standard");
                }
            }
        }

        private static GameObject _previewSceneElements;
        public static GameObject PreviewSceneElements
        {
            get
            {
#if UNITY_EDITOR
                if (_previewSceneElements == null)
                {
                    _previewSceneElements = AssetDatabase.LoadAssetAtPath<GameObject>(GetStandardAssetsPath() + FILE_PREVIEV_SCENE_ELEMENTS + ".prefab");
                    if (_previewSceneElements != null)
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(GetStandardAssetsPath() + FILE_CALIBRATION_FLOOR_MATERIAL + ".mat");
                        if (material != null)
                        {
                            Shader shader = DefaultRPShader;
                            if(material.shader != shader)
                            {
                                Undo.RecordObject(material, "Preview Material edit");
                                material.shader = shader;
                            }
                        }
                    }
                }
#endif
                return _previewSceneElements;
            }
        }

        #endregion Default Assets

        #region Shaders
        public static readonly string[] SHADERS_UNITY_BUILTIN =
        {
            "Standard",
            "Standard (Specular setup)",
            "Standard (Roughness setup)",
            "VertexLit",
            "Nature/SpeedTree",
            "Nature/Tree Creator Bark",
            "Hidden/Nature/Tree Creator Bark Optimized",
            "Nature/Tree Creator Leaves",
            "Hidden/Nature/Tree Creator Leaves Optimized",
            "Nature/Tree Creator Leaves Fast",
            "Hidden/Nature/Tree Creator Leaves Fast Optimized",
            "Nature/Tree Soft Occlusion Bark",
            "Nature/Tree Soft Occlusion Leaves"
        };

        public const string SHADER_UNITY_INTERNAL_ERROR = "Hidden/InternalErrorShader";
        public const string SHADER_UNITY_SPEEDTREE = "Nature/SpeedTree";
        public const string SHADER_UNITY_SPEEDTREE8 = "Nature/SpeedTree8";
        public const string SHADER_UNITY_SPEEDTREE9 = "Nature/SpeedTree9";

        public const string SHADER_GPUI_ERROR = "Hidden/GPUInstancerPro/InternalErrorShader";
        public const string SHADER_GPUI_TREE_PROXY = "Hidden/GPUInstancerPro/Nature/TreeProxy";
        public const string SHADER_GPUI_BILLBOARD_ALBEDO_BAKER = "Hidden/GPUInstancerPro/Billboard/AlbedoBake";
        public const string SHADER_GPUI_BILLBOARD_NORMAL_BAKER = "Hidden/GPUInstancerPro/Billboard/NormalBake";
        public const string SHADER_GPUI_BILLBOARD_URP = "GPUInstancerPro/Billboard/BillboardURP_GPUIPro";
        public const string SHADER_GPUI_BILLBOARD_HDRP = "GPUInstancerPro/Billboard/BillboardHDRP_GPUIPro";
        public const string SHADER_GPUI_BILLBOARD_Builtin = "GPUInstancerPro/Billboard/BillboardBuiltin_GPUIPro";
        public const string SHADER_GPUI_BILLBOARD_Builtin_SpeedTree = "GPUInstancerPro/Billboard/2DRendererSpeedTree";
        public const string SHADER_GPUI_BILLBOARD_Builtin_TreeCreator = "GPUInstancerPro/Billboard/2DRendererTreeCreator";
        public const string SHADER_GPUI_BILLBOARD_Builtin_SoftOcclusion = "GPUInstancerPro/Billboard/2DRendererSoftOcclusion";

        public static Shader _ShaderUnityInternalError;
        public static Shader ShaderUnityInternalError
        {
            get
            {
                if (_ShaderUnityInternalError == null)
                    _ShaderUnityInternalError = Shader.Find(SHADER_UNITY_INTERNAL_ERROR);
                return _ShaderUnityInternalError;
            }
        }

        public const string Kw_LOD_FADE_CROSSFADE = "LOD_FADE_CROSSFADE";

        public const string EXTENSION_CODE_CROWD = "CROWD";
        public const string SHADER_NAME_PREFIX = "GPUInstancerPro/";
        public const string SHADER_NAME_PREFIX_CROWD = "GPUInstancerPro/CrowdAnimations/";
        public const string PACKAGE_NAME_CROWD = "com.gurbu.gpui-pro.crowd-animations";

        public static string GetShaderNamePrefix(string extensionCode)
        {
            switch (extensionCode)
            {
                case EXTENSION_CODE_CROWD:
                    return SHADER_NAME_PREFIX_CROWD;
                default:
                    return SHADER_NAME_PREFIX;
            }
        }

        #endregion Shaders

        #region Compute Shaders 
#if UNITY_EDITOR
        public static void ReimportComputeShaders()
        {
            GPUIUtility.ReimportFilesInFolder(GetPackagesPath() + PATH_RUNTIME + PATH_COMPUTE, "*.hlsl");
            GPUIUtility.ReimportFilesInFolder(GetPackagesPath() + PATH_RUNTIME + PATH_COMPUTE, "*.compute");
            GPUIUtility.ReimportFilesInFolder(GetDefaultPath() + "Demos/Core/_SharedResources/Shaders/", "*.compute");
        }
#endif

        public static float CS_THREAD_COUNT;
        public static float CS_THREAD_COUNT_HEAVY;
        public static float CS_THREAD_COUNT_2D;
        public static float CS_THREAD_COUNT_3D;
        public static int TEXTURE_MAX_SIZE;
        public static long MAX_BUFFER_SIZE;

        private static ComputeShader _CS_GraphicsBufferUtility;
        public static ComputeShader CS_GraphicsBufferUtility
        {
            get
            {
                if (_CS_GraphicsBufferUtility == null)
                    _CS_GraphicsBufferUtility = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_GraphicsBufferUtility);
                return _CS_GraphicsBufferUtility;
            }
        }

        private static ComputeShader _CS_CommandBufferUtility;
        public static ComputeShader CS_CommandBufferUtility
        {
            get
            {
                if (_CS_CommandBufferUtility == null)
                    _CS_CommandBufferUtility = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_CommandBufferUtility);
                return _CS_CommandBufferUtility;
            }
        }

        private static ComputeShader _CS_CameraVisibility;
        public static ComputeShader CS_CameraVisibility
        {
            get
            {
                if (_CS_CameraVisibility == null)
                    _CS_CameraVisibility = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_CameraVisibility);
                return _CS_CameraVisibility;
            }
        }
#if GPUI_XR
        private static ComputeShader _CS_CameraVisibilityXR;
        public static ComputeShader CS_CameraVisibilityXR
        {
            get
            {
                if (_CS_CameraVisibilityXR == null)
                    _CS_CameraVisibilityXR = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_CameraVisibilityXR);
                return _CS_CameraVisibilityXR;
            }
        }
#endif
        public const string Kw_GPUI_LOD = "GPUI_LOD";
        public const string Kw_GPUI_LOD_CROSSFADE = "GPUI_LOD_CROSSFADE";
        public const string Kw_GPUI_LOD_CROSSFADE_ANIMATE = "GPUI_LOD_CROSSFADE_ANIMATE";
        public const string Kw_GPUI_OCCLUSION_CULLING = "GPUI_OCCLUSION_CULLING";
        public const string Kw_GPUI_SHADOWCASTING = "GPUI_SHADOWCASTING";
        public const string Kw_GPUI_SHADOWCULLED = "GPUI_SHADOWCULLED";
        public const string Kw_GPUI_NO_BUFFER = "GPUI_NO_BUFFER";
        public const string Kw_GPUI_TRANSFORM_OFFSET = "GPUI_TRANSFORM_OFFSET";

        public const string Kw_GPUI_THREAD_SIZE_512 = "GPUI_THREAD_SIZE_512";
        public const string Kw_GPUI_THREAD_SIZE_256 = "GPUI_THREAD_SIZE_256";
        public const string Kw_GPUI_THREAD_SIZE_HEAVY_256 = "GPUI_THREAD_SIZE_HEAVY_256";

        public const string Kw_GPUI_TRANSFORM_DATA = "GPUI_TRANSFORM_DATA";
        
        private static ComputeShader _CS_TextureUtility;
        public static ComputeShader CS_TextureUtility
        {
            get
            {
                if (_CS_TextureUtility == null)
                    _CS_TextureUtility = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TextureUtility);
                return _CS_TextureUtility;
            }
        }


        private static ComputeShader _CS_HiZTextureCopy;
        public static ComputeShader CS_HiZTextureCopy
        {
            get
            {
                if (_CS_HiZTextureCopy == null)
                    _CS_HiZTextureCopy = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_HiZTextureCopy);
                return _CS_HiZTextureCopy;
            }
        }

        private static ComputeShader _CS_TextureReduce;
        public static ComputeShader CS_TextureReduce
        {
            get
            {
                if (_CS_TextureReduce == null)
                    _CS_TextureReduce = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TextureReduce);
                return _CS_TextureReduce;
            }
        }

        private static ComputeShader _CS_Billboard;
        public static ComputeShader CS_Billboard
        {
            get
            {
                if (_CS_Billboard == null)
                    _CS_Billboard = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_Billboard);
                return _CS_Billboard;
            }
        }

        private static ComputeShader _CS_BufferToTexture;
        public static ComputeShader CS_BufferToTexture
        {
            get
            {
                if (_CS_BufferToTexture == null)
                    _CS_BufferToTexture = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_BufferToTexture);
                return _CS_BufferToTexture;
            }
        }
        public const string Kw_GPUI_FLOAT4_BUFFER = "GPUI_FLOAT4_BUFFER";
        
        private static ComputeShader _CS_TransformModifications;
        public static ComputeShader CS_TransformModifications
        {
            get
            {
                if (_CS_TransformModifications == null)
                    _CS_TransformModifications = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_TransformModifications);
                return _CS_TransformModifications;
            }
        }

        private static ComputeShader _CS_OptionalRenderer;
        public static ComputeShader CS_OptionalRenderer
        {
            get
            {
                if (_CS_OptionalRenderer == null)
                    _CS_OptionalRenderer = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_OptionalRenderer);
                return _CS_OptionalRenderer;
            }
        }

        private static ComputeShader _CS_LightProbeUtility;
        public static ComputeShader CS_LightProbeUtility
        {
            get
            {
                if (_CS_LightProbeUtility == null)
                    _CS_LightProbeUtility = GPUIUtility.LoadResource<ComputeShader>(FILE_CS_LightProbeUtility);
                return _CS_LightProbeUtility;
            }
        }

        #endregion Compute Shaders 

        #region Shader Props

        public static readonly int PROP_CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int PROP_unity_LODFade = Shader.PropertyToID("unity_LODFade");
        public static readonly int PROP_unity_LightmapST = Shader.PropertyToID("unity_LightmapST");
        public static readonly int PROP_unity_DynamicLightmapST = Shader.PropertyToID("unity_DynamicLightmapST");
        public static readonly int PROP_unity_SHAr = Shader.PropertyToID("unity_SHAr");
        public static readonly int PROP_unity_SHAg = Shader.PropertyToID("unity_SHAg");
        public static readonly int PROP_unity_SHAb = Shader.PropertyToID("unity_SHAb");
        public static readonly int PROP_unity_SHBr = Shader.PropertyToID("unity_SHBr");
        public static readonly int PROP_unity_SHBg = Shader.PropertyToID("unity_SHBg");
        public static readonly int PROP_unity_SHBb = Shader.PropertyToID("unity_SHBb");
        public static readonly int PROP_unity_SHC = Shader.PropertyToID("unity_SHC");
        public static readonly int PROP_unity_ProbesOcclusion = Shader.PropertyToID("unity_ProbesOcclusion");

        public static readonly int PROP_gpuiTransformBuffer = Shader.PropertyToID("gpuiTransformBuffer");
        public static readonly int PROP_gpuiPreviousFrameTransformBuffer = Shader.PropertyToID("gpuiPreviousFrameTransformBuffer");
        public static readonly int PROP_hasPreviousFrameTransformBuffer = Shader.PropertyToID("hasPreviousFrameTransformBuffer");
        public static readonly int PROP_gpuiTransformBufferTexture = Shader.PropertyToID("gpuiTransformBufferTexture");
        public static readonly int PROP_hasPerInstanceLightProbes = Shader.PropertyToID("hasPerInstanceLightProbes");
        public static readonly int PROP_gpuiPerInstanceLightProbesBuffer = Shader.PropertyToID("gpuiPerInstanceLightProbesBuffer");
        public static readonly int PROP_gpuiInstanceDataBuffer = Shader.PropertyToID("gpuiInstanceDataBuffer");
        public static readonly int PROP_gpuiInstanceDataBufferTexture = Shader.PropertyToID("gpuiInstanceDataBufferTexture");
        public static readonly int PROP_visibilityBuffer = Shader.PropertyToID("visibilityBuffer");
        public static readonly int PROP_gpuiTransformOffset = Shader.PropertyToID("gpuiTransformOffset");
        public static readonly int PROP_bufferSize = Shader.PropertyToID("bufferSize");
        public static readonly int PROP_multiplier = Shader.PropertyToID("multiplier");
        public static readonly int PROP_maxTextureSize = Shader.PropertyToID("maxTextureSize");
        public static readonly int PROP_mvpMatrix = Shader.PropertyToID("mvpMatrix");
        public static readonly int PROP_mvpMatrix2 = Shader.PropertyToID("mvpMatrix2");
        public static readonly int PROP_cameraPositionAndHalfAngle = Shader.PropertyToID("cameraPositionAndHalfAngle");
        public static readonly int PROP_commandBuffer = Shader.PropertyToID("commandBuffer");
        public static readonly int PROP_parameterBuffer = Shader.PropertyToID("parameterBuffer");
        public static readonly int PROP_instanceDataBufferShift = Shader.PropertyToID("instanceDataBufferShift");
        public static readonly int PROP_source = Shader.PropertyToID("source");
        public static readonly int PROP_textureArray = Shader.PropertyToID("textureArray");
        public static readonly int PROP_destination = Shader.PropertyToID("destination");
        public static readonly int PROP_offsetX = Shader.PropertyToID("offsetX");
        public static readonly int PROP_sourceSizeX = Shader.PropertyToID("sourceSizeX");
        public static readonly int PROP_sourceSizeY = Shader.PropertyToID("sourceSizeY");
        public static readonly int PROP_destinationSizeX = Shader.PropertyToID("destinationSizeX");
        public static readonly int PROP_destinationSizeY = Shader.PropertyToID("destinationSizeY");
        public static readonly int PROP_reverseZ = Shader.PropertyToID("reverseZ");
        public static readonly int PROP_textureArrayIndex = Shader.PropertyToID("textureArrayIndex");
        public static readonly int PROP_hiZMap = Shader.PropertyToID("hiZMap");
        public static readonly int PROP_hiZTxtrSize = Shader.PropertyToID("hiZTxtrSize");
        public static readonly int PROP_sizeAndIndexes = Shader.PropertyToID("sizeAndIndexes");
        public static readonly int PROP_sizeAndIndexes2 = Shader.PropertyToID("sizeAndIndexes2");
        public static readonly int PROP_instanceCount = Shader.PropertyToID("instanceCount");
        public static readonly int PROP_sourceBuffer = Shader.PropertyToID("sourceBuffer");
        public static readonly int PROP_targetBuffer = Shader.PropertyToID("targetBuffer");
        public static readonly int PROP_targetTexture = Shader.PropertyToID("targetTexture");
        public static readonly int PROP_sourceStartIndex = Shader.PropertyToID("sourceStartIndex");
        public static readonly int PROP_targetStartIndex = Shader.PropertyToID("targetStartIndex");
        public static readonly int PROP_count = Shader.PropertyToID("count");
        public static readonly int PROP_additionalValues = Shader.PropertyToID("additionalValues");
        public static readonly int PROP_position = Shader.PropertyToID("position");
        public static readonly int PROP_matrix44 = Shader.PropertyToID("matrix44");
        public static readonly int PROP_transformBufferSize = Shader.PropertyToID("transformBufferSize");
        public static readonly int PROP_instanceDataBufferSize = Shader.PropertyToID("instanceDataBufferSize");
        public static readonly int PROP_outputTexture = Shader.PropertyToID("outputTexture");
        public static readonly int PROP_startIndex = Shader.PropertyToID("startIndex");
        public static readonly int PROP_prototypeIndex = Shader.PropertyToID("prototypeIndex");
        public static readonly int PROP_counterBuffer = Shader.PropertyToID("counterBuffer");
        public static readonly int PROP_boundsCenter = Shader.PropertyToID("boundsCenter");
        public static readonly int PROP_boundsExtents = Shader.PropertyToID("boundsExtents");
        public static readonly int PROP_modifierTransform = Shader.PropertyToID("modifierTransform");
        public static readonly int PROP_modifierRadius = Shader.PropertyToID("modifierRadius");
        public static readonly int PROP_modifierHeight = Shader.PropertyToID("modifierHeight");
        public static readonly int PROP_valueToSet = Shader.PropertyToID("valueToSet");
        public static readonly int PROP_transformBufferStartIndex = Shader.PropertyToID("transformBufferStartIndex");
        public static readonly int PROP_textureDataSingleChannel = Shader.PropertyToID("textureDataSingleChannel");
        public static readonly int PROP_currentTime = Shader.PropertyToID("currentTime");
        public static readonly int PROP_optionalRendererStatusBuffer = Shader.PropertyToID("optionalRendererStatusBuffer");

        public static readonly int PROP_sphericalHarmonicsBuffer = Shader.PropertyToID("sphericalHarmonicsBuffer");
        public static readonly int PROP_occlusionProbesBuffer = Shader.PropertyToID("occlusionProbesBuffer");

        public static readonly int CONST_GPUILODConstants = Shader.PropertyToID("GPUILODConstants");

        public const string Kw_BILLBOARD_FACE_CAMERA_POS = "BILLBOARD_FACE_CAMERA_POS";

        public const string Kw_GPUI_OBJECT_MOTION_VECTOR_ON = "GPUI_OBJECT_MOTION_VECTOR_ON";
        public const string Kw_GPUI_PER_INSTANCE_LIGHTPROBES_ON = "GPUI_PER_INSTANCE_LIGHTPROBES_ON";

        #endregion Shader Props

        #region Other
        public const string TAG_MainCamera = "MainCamera";

        private static string _sanitizedUnityVersion = "";
        public static string UNITY_VERSION
        {
            get
            {
                if (string.IsNullOrEmpty(_sanitizedUnityVersion))
                {
                    Match match = Regex.Match(Application.unityVersion, @"^(\d+(\.\d+)*)");
                    _sanitizedUnityVersion = match.Success ? match.Value : "";
                }
                return _sanitizedUnityVersion;
            }
        }

        public static readonly GPUITransformData TRANSFORM_DATA_IDENTITY = new() { position = Vector3.one, rotation = Quaternion.identity, scale = Vector3.one };
        public static readonly GPUITransformData TRANSFORM_DATA_ZERO = new() { position = Vector3.zero, rotation = new Quaternion(0, 0, 0, 0), scale = Vector3.zero };

        public static readonly string LOG_PREFIX = "<b>[GPU Instancer Pro]</b> ";
        #endregion Other
    }
}