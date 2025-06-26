// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public class GPUIRuntimeSettings : ScriptableObject
    {
        [SerializeField]
        public GPUICameraLoadingType cameraLoadingType;
        [SerializeField]
        public GPUIOcclusionCullingCondition occlusionCullingCondition;
        [SerializeField]
        public GPUIOcclusionCullingData.GPUIOcclusionCullingMode occlusionCullingMode = GPUIOcclusionCullingData.GPUIOcclusionCullingMode.Auto;
        [SerializeField]
        public Vector3 instancingBoundsSize = new Vector3(1000f, 1000f, 1000f);
        [SerializeField]
        public float defaultHDRPShadowDistance = 250f;
        [SerializeField]
        public List<GPUIBillboard> billboardAssets;
#if GPUI_ADDRESSABLES
        [SerializeField]
        public bool loadShadersFromAddressables;
        [SerializeField]
        public bool loadResourcesFromAddressables;
#endif

        public GraphicsDeviceType GraphicsDeviceType { get; private set; }
        public GPUIRenderPipeline RenderPipeline { get; private set; }
        public GPUIMaxComputeWorkGroupSize ComputeWorkGroupSize { get; private set; }
        public float ComputeThreadCount { get; private set; }
        public float ComputeThreadCountHeavy { get; private set; }
        public float ComputeThreadCount2D { get; private set; }
        public float ComputeThreadCount3D { get; private set; }
        public int TextureMaxSize { get; private set; }
        public bool DisableShaderBuffers { get; private set; }
        public bool DisableOcclusionCulling { get; private set; }
        public long MaxBufferSize { get; private set; }
        public bool ReversedZBuffer { get; private set; }
        public bool API_HAS_GUARANTEED_R8_SUPPORT { get; private set; }

        public bool IsVREnabled { get; private set; }
        public bool Unsupported_Unity_Version { get; private set; }

        private static GPUIRuntimeSettings _instance;
        public static GPUIRuntimeSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetDefaultGPUIRuntimeSettings();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public bool IsHDRP
        {
            get
            {
                return RenderPipeline == GPUIRenderPipeline.HDRP;
            }
        }

        public bool IsURP
        {
            get
            {
                return RenderPipeline == GPUIRenderPipeline.URP;
            }
        }

        public bool IsBuiltInRP
        {
            get
            {
                return RenderPipeline == GPUIRenderPipeline.BuiltIn;
            }
        }

        private static GPUIRuntimeSettings GetDefaultGPUIRuntimeSettings()
        {
            GPUIRuntimeSettings runtimeSettings = null;
            GPUIRuntimeSettingsOverwrite overwrite = FindFirstObjectByType<GPUIRuntimeSettingsOverwrite>();
            if (overwrite != null && overwrite.runtimeSettingsOverwrite != null)
            {
                runtimeSettings = overwrite.runtimeSettingsOverwrite;
                runtimeSettings.DetermineOperationMode();
            }
            if (runtimeSettings == null)
            {
                runtimeSettings = ScriptableObject.CreateInstance<GPUIRuntimeSettings>();
                runtimeSettings.DetermineOperationMode();
                runtimeSettings.SetDefaultValues();
            }
            return runtimeSettings;
        }

        internal static void OverwriteSettings(GPUIRuntimeSettings overwriteSettings)
        {
            if (overwriteSettings == null || _instance == overwriteSettings) return;
            //_instance.DestroyGeneric();
            overwriteSettings.DetermineOperationMode();
            _instance = overwriteSettings;
        }

#if UNITY_EDITOR
        public void SaveAsAsset()
        {
            this.SaveAsAsset(GPUIConstants.GetDefaultUserDataPath() + GPUIConstants.PATH_SETTINGS, GPUIConstants.FILE_RUNTIME_SETTINGS + ".asset", true);
        }

        private ShaderVariantCollection _shaderVariantCollection;
        public ShaderVariantCollection VariantCollection
        {
            get
            {
                if (_shaderVariantCollection == null)
                    _shaderVariantCollection = GetDefaultShaderVariantCollection();
                return _shaderVariantCollection;
            }
            set
            {
                _shaderVariantCollection = value;
            }
        }

        private static ShaderVariantCollection GetDefaultShaderVariantCollection()
        {
            ShaderVariantCollection shaderVariantCollection = GPUIUtility.LoadResource<ShaderVariantCollection>(GPUIConstants.FILE_SHADER_VARIANT_COLLECTION);

            if (shaderVariantCollection == null)
            {
                shaderVariantCollection = new ShaderVariantCollection();
#if UNITY_EDITOR
                shaderVariantCollection.SaveAsAsset(GPUIConstants.GetDefaultUserDataPath() + GPUIConstants.PATH_RESOURCES, GPUIConstants.FILE_SHADER_VARIANT_COLLECTION + ".shadervariants");
#endif
            }

            return shaderVariantCollection;
        }
#endif

        public void DetermineRenderPipeline()
        {
            RenderPipeline = GPUIRenderPipeline.BuiltIn;
#if GPUI_URP
            if (TryGetURPAsset(out _))
                RenderPipeline = GPUIRenderPipeline.URP;
#endif
#if GPUI_HDRP
            if (TryGetHDRPAsset(out _))
                RenderPipeline = GPUIRenderPipeline.HDRP;
#endif
        }

        public void DetermineOperationMode()
        {
            DetermineRenderPipeline();
            GraphicsDeviceType = SystemInfo.graphicsDeviceType;
            DisableShaderBuffers = GraphicsDeviceType is not (
                GraphicsDeviceType.Direct3D11 
                or GraphicsDeviceType.PlayStation4 
                or GraphicsDeviceType.XboxOne 
                or GraphicsDeviceType.Metal 
                or GraphicsDeviceType.OpenGLCore 
                or GraphicsDeviceType.Direct3D12 
                or GraphicsDeviceType.XboxOneD3D12 
                or GraphicsDeviceType.GameCoreXboxOne 
                or GraphicsDeviceType.GameCoreXboxSeries 
                or GraphicsDeviceType.PlayStation5 
                or GraphicsDeviceType.PlayStation5NGGC
                /*or GraphicsDeviceType.Vulkan*/);

            if (!DisableShaderBuffers && SystemInfo.maxComputeBufferInputsFragment < 2)
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Current platform's maxComputeBufferInputsFragment count is under 2. Current maxComputeBufferInputsFragment: " + SystemInfo.maxComputeBufferInputsFragment + " graphicsDeviceType: " + GraphicsDeviceType);

            if (DisableShaderBuffers)
                Shader.EnableKeyword(GPUIConstants.Kw_GPUI_NO_BUFFER);
            else
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_NO_BUFFER);

            if (DisableShaderBuffers)
                DisableOcclusionCulling = GraphicsDeviceType is not (
                GraphicsDeviceType.OpenGLES3 
                or GraphicsDeviceType.Vulkan);
            else
                DisableOcclusionCulling = false;

            API_HAS_GUARANTEED_R8_SUPPORT = GraphicsDeviceType is not (
                GraphicsDeviceType.OpenGLES3 
                or GraphicsDeviceType.Vulkan);

            int maxComputeWorkGroupSize = SystemInfo.maxComputeWorkGroupSize;
            if (maxComputeWorkGroupSize >= 512)
            {
                ComputeWorkGroupSize = GPUIMaxComputeWorkGroupSize.x512;
                ComputeThreadCount = 512;
                ComputeThreadCount2D = 16;
                ComputeThreadCount3D = 8;
                ComputeThreadCountHeavy = 256;
                Shader.EnableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_512);
                Shader.EnableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_HEAVY_256);
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_256);
            }
            else if (maxComputeWorkGroupSize >= 256)
            {
                ComputeWorkGroupSize = GPUIMaxComputeWorkGroupSize.x256;
                ComputeThreadCount = 256;
                ComputeThreadCount2D = 16;
                ComputeThreadCount3D = 4;
                ComputeThreadCountHeavy = 256;
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_512);
                Shader.EnableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_HEAVY_256);
                Shader.EnableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_256);
            }
            else
            {
                if (maxComputeWorkGroupSize < 64)
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Max. Compute Work Group Size is: " + maxComputeWorkGroupSize + ". GPUI requires minimum work group size of 64.");
                ComputeWorkGroupSize = GPUIMaxComputeWorkGroupSize.x64;
                ComputeThreadCount = 64;
                ComputeThreadCount2D = 8;
                ComputeThreadCount3D = 4;
                ComputeThreadCountHeavy = 64;
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_512);
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_HEAVY_256);
                Shader.DisableKeyword(GPUIConstants.Kw_GPUI_THREAD_SIZE_256);
            }
            TextureMaxSize = SystemInfo.maxTextureSize;

            ClearEmptyBillboardAssets();

            MaxBufferSize = SystemInfo.maxGraphicsBufferSize / (4 * 4 * 4);

            ReversedZBuffer = SystemInfo.graphicsDeviceType is (GraphicsDeviceType.OpenGLCore or GraphicsDeviceType.OpenGLES3); // Checking platform instead of SystemInfo.usesReversedZBuffer because it is not correct for depth texture

            GPUIConstants.CS_THREAD_COUNT = ComputeThreadCount;
            GPUIConstants.CS_THREAD_COUNT_HEAVY = ComputeThreadCountHeavy;
            GPUIConstants.CS_THREAD_COUNT_2D = ComputeThreadCount2D;
            GPUIConstants.CS_THREAD_COUNT_3D = ComputeThreadCount3D;
            GPUIConstants.TEXTURE_MAX_SIZE = TextureMaxSize;
            GPUIConstants.MAX_BUFFER_SIZE = MaxBufferSize;

#if UNITY_EDITOR
            Unsupported_Unity_Version = false;
            try
            {
                string unityVersion = GPUIConstants.UNITY_VERSION;
                if (!string.IsNullOrEmpty(unityVersion) && Version.TryParse(unityVersion, out Version unityVersionParsed) && unityVersionParsed.Major > 0)
                    Unsupported_Unity_Version = !IsUnityVersionValid(unityVersionParsed);
            }
            catch (Exception) { }
#endif
        }

#if UNITY_EDITOR
        private bool IsUnityVersionValid(Version unityVersion)
        {
            if (unityVersion.Major < 2022 || unityVersion.Major == 2023)
                return false;
            if (unityVersion.Major == 2022 && (unityVersion.Minor != 3 || unityVersion.Build < 32))
                return false;
            if (unityVersion.Major == 6000 && unityVersion.Minor == 0 && unityVersion.Build < 23)
                return false;
            return true;
        }
#endif

        public void SetDefaultValues()
        {
            RuntimePlatform platform = Application.platform;
            if (platform is (RuntimePlatform.Android or RuntimePlatform.IPhonePlayer or RuntimePlatform.Switch))
                occlusionCullingCondition = GPUIOcclusionCullingCondition.IfDepthAvailable; // If depth texture is not already available, disable occlusion culling by default for mobile devices. Very often rendering depth on these devices are more expensive than occlusion culling benefits.
            else
                occlusionCullingCondition = GPUIOcclusionCullingCondition.Always;
        }

        public void ClearEmptyBillboardAssets()
        {
#if UNITY_EDITOR
            bool isBillboardAssetsModified = false;
#endif
            if (billboardAssets == null)
            {
                billboardAssets = new List<GPUIBillboard>();
#if UNITY_EDITOR
                isBillboardAssetsModified = true;
#endif
            }
            for (int i = 0; i < billboardAssets.Count; i++)
            {
                if (billboardAssets[i] == null)
                {
                    billboardAssets.RemoveAt(i);
                    i--;
#if UNITY_EDITOR
                    isBillboardAssetsModified = true;
#endif
                }
            }
#if UNITY_EDITOR
            if (isBillboardAssetsModified)
                EditorUtility.SetDirty(this);
#endif
        }

        public void SetRuntimeSettings()
        {
#if GPUI_XR
            IsVREnabled = UnityEngine.XR.XRSettings.enabled;
#else
            IsVREnabled = false;
#endif
            DetermineRenderPipeline();
        }

        public bool IsSupportedPlatform()
        {
            if (!SystemInfo.supportsInstancing)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Current platform does not support GPU instancing.");
                return false;
            }
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Current platform does not support Compute Shaders.");
                return false;
            }
            if (SystemInfo.graphicsShaderLevel < 35)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Current platform's Graphics Shader Level is under 35. Current shader level: " + SystemInfo.graphicsShaderLevel);
                return false;
            }
            if (SystemInfo.maxComputeWorkGroupSize < 64)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Current platform's Max. Compute Work Group Size is under 64. Current Max. Compute Work Group Size: " + SystemInfo.maxComputeWorkGroupSize);
                return false;
            }

            return true;
        }

        public float GetDefaultShadowDistance()
        {
            switch (RenderPipeline)
            {
#if GPUI_HDRP
                case GPUIRenderPipeline.HDRP:
                    return defaultHDRPShadowDistance;
#elif GPUI_URP
                case GPUIRenderPipeline.URP:
                    if (TryGetURPAsset(out var urpAsset))
                        return urpAsset.shadowDistance;
                    return QualitySettings.shadowDistance;
#endif
                default:
                    return QualitySettings.shadowDistance;
            }
        }

        public static bool TryGetRenderPipelineAsset(out RenderPipelineAsset renderPipelineAsset)
        {
            if (QualitySettings.renderPipeline != null)
            {
                renderPipelineAsset = QualitySettings.renderPipeline;
                return true;
            }
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
                return true;
            }
            renderPipelineAsset = null;
            return false;
        }

#if GPUI_URP
        public static bool TryGetURPAsset(out UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urpAsset)
        {
            if (TryGetRenderPipelineAsset(out RenderPipelineAsset renderPipelineAsset) && renderPipelineAsset is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset rpAsset)
            {
                urpAsset = rpAsset;
                return true;
            }
            urpAsset = null;
            return false;
        }
#endif

#if GPUI_HDRP
        public static bool TryGetHDRPAsset(out UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset hdrpAsset)
        {
            if (TryGetRenderPipelineAsset(out RenderPipelineAsset renderPipelineAsset) && renderPipelineAsset is UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset rpAsset)
            {
                hdrpAsset = rpAsset;
                return true;
            }
            hdrpAsset = null;
            return false;
        }
#endif

        public static bool IsAdaptiveProbeVolumesEnabled()
        {
#if GPUI_HDRP
            if (TryGetHDRPAsset(out var hdrpAsset))
#if UNITY_6000_0_OR_NEWER
                return hdrpAsset.supportProbeVolume;
#else
                return hdrpAsset.currentPlatformRenderPipelineSettings.lightProbeSystem == UnityEngine.Rendering.HighDefinition.RenderPipelineSettings.LightProbeSystem.ProbeVolumes;
#endif
#endif
#if GPUI_URP && UNITY_6000_0_OR_NEWER
            if (TryGetURPAsset(out var urpAsset))
                return urpAsset.supportProbeVolume;
#endif
            return false;
        }
    }

    public enum GPUIRenderPipeline
    {
        BuiltIn = 0,
        URP = 1000,
        HDRP = 2000
    }

    [Serializable]
    public class GPUIManagerDefaults
    {
        public string managerTypeName;
        public GPUIProfile defaultProfileOverride;
    }

    public enum GPUICameraLoadingType
    {
        /// <summary>
        /// Possible camera selection by priority:
        /// GPUICamera component, MainCamera tag
        /// </summary>
        Default = 0,
        /// <summary>
        /// Possible camera selection by priority:
        /// GPUICamera component, MainCamera tag, Camera.allCameras[0]
        /// </summary>
        Any = 1,
        /// <summary>
        /// Possible camera selection by priority:
        /// GPUICamera component
        /// </summary>
        GPUICameraComponent = 2
    }

    public enum GPUIOcclusionCullingCondition
    {
        /// <summary>
        /// Occlusion culling will always be enabled. This is the default behaviour for all devices except mobile such as iPhone, Android and Switch.
        /// </summary>
        Always = 0,
        /// <summary>
        /// Occlusion culling will be enabled only if the camera is already rendering the depth texture. This is the default behaviour for mobile such as iPhone, Android and Switch.
        /// </summary>
        IfDepthAvailable = 1,
        /// <summary>
        /// Occlusion culling will never be enabled.
        /// </summary>
        Never = 2
    }

    #region Graphics Device Setting Struct

    public enum GPUIMaxComputeWorkGroupSize
    {
        x64 = 10, // 2D 8
        x256 = 20, // 2D 16
        x512 = 30 // 2D 16
    }

    #endregion Graphics Device Setting Struct
}