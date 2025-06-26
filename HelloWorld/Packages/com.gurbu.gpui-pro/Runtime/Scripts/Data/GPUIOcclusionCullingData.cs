// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if GPUI_URP
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
#endif
#if GPUI_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GPUInstancerPro
{
    public class GPUIOcclusionCullingData : IDisposable
    {
        #region Properties
        /// <summary>
        /// Depth texture with mipmaps that is used for Occlusion Culling
        /// </summary>
        public RenderTexture HiZDepthTexture { get; private set; }
        public Vector2Int HiZTextureSize => _occlusionPassData.hiZTextureSize;
        public int HiZMipLevels => _occlusionPassData.hiZMipLevels;
        public Texture CameraDepthTexture { get; private set; }
        public GPUIOcclusionCullingMode ActiveCullingMode { get; private set; }
        public bool IsHiZDepthUpdated => HiZDepthTexture != null && _isHiZDepthUpdated;

        private bool IsDirectCameraDepthAccessRequired => ActiveCullingMode == GPUIOcclusionCullingMode.DirectTextureAccess || ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering;
        private bool IsCommandBufferRequired => ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera || ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering;

        private Camera _activeCamera;
        private bool _vrMultiPassMono;
        private bool _isHiZDepthUpdated;

        private GPUIOcclusionPassData _occlusionPassData;

        private CommandBuffer _occlusionCommandBuffer;
        private RenderTargetIdentifier _hiZDepthIdentifier;

        private const string GPUI_HiZ_DepthTexture_NAME = "GPUI_HiZDepthTexture";
        private const string GPUI_HiZ_CommandBuffer_NAME = "GPUI.HiZDepthPass";
        #endregion Properties

        public enum GPUIOcclusionCullingMode
        {
            Auto = 0,
            DirectTextureAccess = 1,
            CommandBufferAddedToCamera = 2,
            CommandBufferExecutedOnEndRendering = 3,
            URPScriptableRenderPass = 4,
            HDRPCustomPass = 5,
        }

        private class GPUIOcclusionPassData
        {
            public ComputeShader copyCS;
            public ComputeShader reduceCS;
            public bool isVRCulling;
            public bool isDepth2DArray;
            public int hiZMipLevels;
            public Vector2Int hiZTextureSize;

            public void CopyTo(GPUIOcclusionPassData other)
            {
                other.copyCS = this.copyCS;
                other.reduceCS = this.reduceCS;
                other.isVRCulling = this.isVRCulling;
                other.isDepth2DArray = this.isDepth2DArray;
                other.hiZMipLevels = this.hiZMipLevels;
                other.hiZTextureSize = this.hiZTextureSize;
            }
        }

        #region Initialize/Dispose

        public GPUIOcclusionCullingData(Camera camera, GPUIOcclusionCullingMode cullingMode, bool isVRCulling)
        {
            _activeCamera = camera;
            _occlusionPassData = new GPUIOcclusionPassData()
            {
                copyCS = GPUIConstants.CS_HiZTextureCopy,
                reduceCS = GPUIConstants.CS_TextureReduce,
                isVRCulling = isVRCulling,
                isDepth2DArray = GPUIRuntimeSettings.Instance.IsHDRP
            };
            Initialize(cullingMode);
        }

        public void Initialize(GPUIOcclusionCullingMode cullingMode)
        {
            Dispose();

            _activeCamera.depthTextureMode |= DepthTextureMode.Depth;

            DetermineOcclusionCullingMode(cullingMode);
        }

        public void Dispose()
        {
            DisposeOcclusionCommandBuffer();
            DisposeScriptableRenderPass();
            DisposeCustomPass();
            DisposeHiZDepthTexture();
            CameraDepthTexture = null;
            _isHiZDepthUpdated = false;
        }

        private void OnHiZTextureSizeChanged()
        {
            DisposeOcclusionCommandBuffer();
            DisposeScriptableRenderPass();
#if GPUI_HDRP
            _hiZGeneratorCustomPass = null;
            if (_hiZGeneratorCustomPassVolume != null)
                _hiZGeneratorCustomPassVolume.customPasses.Clear();
#endif
            DisposeHiZDepthTexture();
            _isHiZDepthUpdated = false;
        }

        private void OnScreenSizeChanged()
        {
            DisposeOcclusionCommandBuffer();
            _isHiZDepthUpdated = false;
        }

        private void DetermineOcclusionCullingMode(GPUIOcclusionCullingMode cullingMode)
        {
            if (cullingMode == GPUIOcclusionCullingMode.Auto)
            {
#if UNITY_6000_0_OR_NEWER && GPUI_URP
                if (GPUIRuntimeSettings.Instance.IsURP && !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.URPScriptableRenderPass;
                    return;
                }
#endif

                if (_occlusionPassData.isVRCulling)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                    return;
                }

                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                {
                    if (_activeCamera.actualRenderingPath == RenderingPath.DeferredShading)
                        ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                    else
                        ActiveCullingMode = GPUIOcclusionCullingMode.CommandBufferAddedToCamera;
                    return;
                }

#if GPUI_HDRP
                if (GPUIRuntimeSettings.Instance.IsHDRP)
                {
                    ActiveCullingMode = GPUIOcclusionCullingMode.HDRPCustomPass;
                    return;
                }
#endif

                ActiveCullingMode = GPUIOcclusionCullingMode.DirectTextureAccess;
                return;
            }

            if (cullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass)
            {
                if (!GPUIRuntimeSettings.Instance.IsURP)
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "OcclusionCullingMode.URPScriptableRenderPass is only supported in Universal Render Pipeline! Switching to OcclusionCullingMode.Auto.");
                    DetermineOcclusionCullingMode(GPUIOcclusionCullingMode.Auto);
                    return;
                }
#if !UNITY_6000_0_OR_NEWER
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "OcclusionCullingMode.URPScriptableRenderPass is only supported for Unity versions 6000 or higher! Switching to OcclusionCullingMode.Auto.");
                DetermineOcclusionCullingMode(GPUIOcclusionCullingMode.Auto);
                return;
#endif
            }

            if (cullingMode == GPUIOcclusionCullingMode.HDRPCustomPass)
            {
                if (!GPUIRuntimeSettings.Instance.IsHDRP)
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "OcclusionCullingMode.HDRPCustomPass is only supported in HDRP! Switching to OcclusionCullingMode.Auto.");
                    DetermineOcclusionCullingMode(GPUIOcclusionCullingMode.Auto);
                    return;
                }
            }

            ActiveCullingMode = cullingMode;
        }

        private bool CreateHiZDepthTexture(Vector2Int screenSize)
        {
            OnHiZTextureSizeChanged();
            _occlusionPassData.hiZTextureSize = screenSize;

            _occlusionPassData.hiZMipLevels = GetMipLevelCount();

            if (HiZTextureSize.x <= 0 || HiZTextureSize.y <= 0 || _occlusionPassData.hiZMipLevels == 0)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "HiZ Texture size is zero!", _activeCamera);
#endif
                return false;
            }

            HiZDepthTexture = new RenderTexture(HiZTextureSize.x, HiZTextureSize.y, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = GPUI_HiZ_DepthTexture_NAME,
                filterMode = FilterMode.Point,
                useMipMap = true,
                autoGenerateMips = false,
                enableRandomWrite = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            HiZDepthTexture.Create();
            HiZDepthTexture.GenerateMips();

            _hiZDepthIdentifier = new RenderTargetIdentifier(HiZDepthTexture);

            return true;
        }

        private void DisposeHiZDepthTexture()
        {
            if (HiZDepthTexture != null)
            {
                GPUITextureUtility.DestroyRenderTexture(HiZDepthTexture);
                HiZDepthTexture = null;
            }
        }

        private void DisposeScriptableRenderPass()
        {
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (_hiZGeneratorRenderPass != null)
            {
                _hiZGeneratorRenderPass.Dispose();
                _hiZGeneratorRenderPass = null;
            }
            if(_hiZGeneratorRenderPassRightEye != null)
            {
                _hiZGeneratorRenderPassRightEye.Dispose();
                _hiZGeneratorRenderPassRightEye = null;
            }
#endif
        }

        private void DisposeCustomPass()
        {
#if GPUI_HDRP
            if (_hiZGeneratorCustomPassVolume != null)
                _hiZGeneratorCustomPassVolume.DestroyGeneric();
            _hiZGeneratorCustomPass = null;
#endif
        }

#endregion Initialize/Dispose

        #region Update Methods

        private Vector2Int GetScreenSize()
        {
            Vector2Int screenSize = Vector2Int.zero;
#if GPUI_XR
            if (_occlusionPassData.isVRCulling)
            {
                screenSize.x = UnityEngine.XR.XRSettings.eyeTextureWidth;
                screenSize.y = UnityEngine.XR.XRSettings.eyeTextureHeight;
                screenSize.x *= 2;
            }
            else
            {
#endif
                screenSize.x = _activeCamera.pixelWidth;
                screenSize.y = _activeCamera.pixelHeight;
#if GPUI_XR
            }
#endif

#if GPUI_HDRP
            if (ActiveCullingMode != GPUIOcclusionCullingMode.DirectTextureAccess)
            {
                float scaleFactor = DynamicResolutionHandler.instance.GetCurrentScale();
                screenSize.x = Mathf.FloorToInt(screenSize.x * scaleFactor);
                screenSize.y = Mathf.FloorToInt(screenSize.y * scaleFactor);
            }
#endif

#if GPUI_URP
            if (!_occlusionPassData.isVRCulling && GPUIRuntimeSettings.Instance.IsURP && GPUIRuntimeSettings.TryGetURPAsset(out UniversalRenderPipelineAsset urpAsset) && urpAsset.renderScale != 1f)
            {
                screenSize.x = Mathf.FloorToInt(screenSize.x * urpAsset.renderScale);
                screenSize.y = Mathf.FloorToInt(screenSize.y * urpAsset.renderScale);
            }
#endif
            return screenSize;
        }

        private int GetMipLevelCount()
        {
            return 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(HiZTextureSize.x, HiZTextureSize.y), 2f));
        }

        internal void CheckScreenSize()
        {
            if (HiZDepthTexture == null)
                return;
            Vector2Int newScreenSize = GetScreenSize();
            if (newScreenSize.x != HiZTextureSize.x || newScreenSize.y != HiZTextureSize.y)
            {
                if (newScreenSize.x <= HiZDepthTexture.width && newScreenSize.y <= HiZDepthTexture.height)
                {
                    // Instead of recreating the buffers for every size change, we use part of the texture.
                    _occlusionPassData.hiZTextureSize = newScreenSize;
                    _occlusionPassData.hiZMipLevels = GetMipLevelCount();
                    OnScreenSizeChanged();
                    HiZDepthTexture.ClearRenderTexture(Color.white); // Fill the unused section of the texture to white to prevent incorrect culling.
                    HiZDepthTexture.GenerateMips(); // Clear mips
                }
                else
                    OnHiZTextureSizeChanged();
                CameraDepthTexture = null;
            }
        }

        internal void UpdateHiZTexture(ScriptableRenderContext context)
        {
            _isHiZDepthUpdated = false;

            if (IsDirectCameraDepthAccessRequired && CameraDepthTexture == null)
            {
                DisposeOcclusionCommandBuffer();
                CameraDepthTexture = Shader.GetGlobalTexture(GPUIConstants.PROP_CameraDepthTexture);
                if (CameraDepthTexture == null || CameraDepthTexture.name == "UnityBlack")
                {
                    CameraDepthTexture = null;
#if GPUIPRO_DEVMODE
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can not find Camera Depth Texture! Camera: " + _activeCamera.name, _activeCamera);
#endif
                    return;
                }

                _occlusionPassData.isDepth2DArray = CameraDepthTexture.dimension == TextureDimension.Tex2DArray;
            }

            if (HiZDepthTexture == null)
            {
                if (!CreateHiZDepthTexture(GetScreenSize()))
                    return;
            }

#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (ActiveCullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass)
            {
                _isHiZDepthUpdated = true;
                if (_hiZGeneratorRenderPass == null)
                {
                    _hiZGeneratorRenderPass = new GPUIHiZGeneratorRenderPass(this);
                    _isHiZDepthUpdated = false;
                }
                if (!_hiZGeneratorRenderPass.IsSetup)
                {
                    _hiZGeneratorRenderPass.Setup(HiZDepthTexture);
                    _isHiZDepthUpdated = false;
                }
#if GPUI_XR
                if (_occlusionPassData.isVRCulling && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && _hiZGeneratorRenderPassRightEye == null)
                {
                    _hiZGeneratorRenderPassRightEye = new GPUIHiZGeneratorRenderPass(this);
                    _isHiZDepthUpdated = false;
                }
                if (_hiZGeneratorRenderPassRightEye != null && !_hiZGeneratorRenderPassRightEye.IsSetup)
                {
                    _hiZGeneratorRenderPassRightEye.Setup(HiZDepthTexture, 1);
                    _isHiZDepthUpdated = false;
                }
#endif
                return;
            }
#endif

#if GPUI_HDRP
            if (ActiveCullingMode == GPUIOcclusionCullingMode.HDRPCustomPass)
            {
                _isHiZDepthUpdated = true;
                if (_hiZGeneratorCustomPassVolume == null)
                {
                    _hiZGeneratorCustomPassVolume = _activeCamera.gameObject.AddComponent<CustomPassVolume>();
                    _hiZGeneratorCustomPassVolume.isGlobal = false;
                    _hiZGeneratorCustomPassVolume.targetCamera = _activeCamera;
                    _hiZGeneratorCustomPassVolume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;
                    _hiZGeneratorCustomPassVolume.hideFlags = HideFlags.NotEditable;
                    _isHiZDepthUpdated = false;
                }
                if (_hiZGeneratorCustomPass == null)
                {
                    _hiZGeneratorCustomPass = new(this);
                    _hiZGeneratorCustomPassVolume.customPasses.Clear();
                    _hiZGeneratorCustomPassVolume.customPasses.Add(_hiZGeneratorCustomPass);
                    _hiZGeneratorCustomPass.name = GPUI_HiZ_CommandBuffer_NAME;
                    _isHiZDepthUpdated = false;
                }
                return;
            }
#endif

            if (IsCommandBufferRequired && _occlusionCommandBuffer == null)
            {
                CreateOcclusionCommandBuffer();

                if (ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera)
                {
                    _activeCamera.AddCommandBuffer(CameraEvent.AfterDepthTexture, _occlusionCommandBuffer);
                    return; // we do not set _isHiZDepthUpdated = true when the command buffer is first created, instead wait for a frame to command buffer to run.
                }
            }

            switch (ActiveCullingMode)
            {
                case GPUIOcclusionCullingMode.URPScriptableRenderPass:
                case GPUIOcclusionCullingMode.HDRPCustomPass:
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.CommandBufferAddedToCamera:
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.CommandBufferExecutedOnEndRendering:
                    if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                        Graphics.ExecuteCommandBuffer(_occlusionCommandBuffer);
                    else
                        context.ExecuteCommandBuffer(_occlusionCommandBuffer);
                    _isHiZDepthUpdated = true;
                    return;
                case GPUIOcclusionCullingMode.DirectTextureAccess:
                    DirectTextureAccessUpdate();
                    _isHiZDepthUpdated = true;
                    return;
            }
#if GPUIPRO_DEVMODE
            Debug.LogError(GPUIConstants.LOG_PREFIX + "Update method can not be found for the active OcclusionCullingMode: " + ActiveCullingMode, _activeCamera);
#endif
            return;
        }

        internal void UpdateHiZTextureOnBeginRendering(Camera camera, ScriptableRenderContext context)
        {
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (ActiveCullingMode == GPUIOcclusionCullingMode.URPScriptableRenderPass && _hiZGeneratorRenderPass != null)
            {
#if GPUI_XR
                if (_occlusionPassData.isVRCulling && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && _hiZGeneratorRenderPassRightEye != null)
                {
                    if (_renderPassQueuedFrameCount != Time.frameCount)
                        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPass);
                    else
                        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPassRightEye);
                    _renderPassQueuedFrameCount = Time.frameCount;
                }
                else
#endif
                    camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_hiZGeneratorRenderPass);
            }
#endif
        }

#endregion Update Methods

        #region Direct Texture Access Mode

        private void DirectTextureAccessUpdate()
        {
#if GPUI_XR
            if (_occlusionPassData.isVRCulling)
            {
                int sourceWidth = HiZTextureSize.x / 2;
                if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
                {
                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
                        UpdateTextureWithComputeShader(sourceWidth, 0);
                    else if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        UpdateTextureWithComputeShader(sourceWidth, sourceWidth);
                    else if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono) // When stereoActiveEye is not set, first get the left eye and then the right
                    {
                        if (!_vrMultiPassMono)
                        {
                            UpdateTextureWithComputeShader(sourceWidth, 0);
                            _vrMultiPassMono = true;
                        }
                        else
                        {
                            UpdateTextureWithComputeShader(sourceWidth, sourceWidth);
                            _vrMultiPassMono = false;
                        }
                    }
                }
                else if (_occlusionPassData.isDepth2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    UpdateTextureWithComputeShader(sourceWidth, 0);
                    UpdateTextureWithComputeShader(sourceWidth, sourceWidth, 1);
                }
                else
                    UpdateTextureWithComputeShader(HiZTextureSize.x, 0);
            }
            else if (_occlusionPassData.isDepth2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced && _activeCamera.stereoTargetEye == StereoTargetEyeMask.Right)
                UpdateTextureWithComputeShader(0, 0, 1);
            else
#endif
                UpdateTextureWithComputeShader(HiZTextureSize.x, 0);

            for (int i = 0; i < _occlusionPassData.hiZMipLevels - 1; i++)
                GPUIHiZDepthTextureUtility.ReduceTextureWithComputeShader(_occlusionPassData.reduceCS, HiZDepthTexture, HiZTextureSize.x, HiZTextureSize.y, i, i + 1);
        }

        private void UpdateTextureWithComputeShader(int sourceWidth, int offset, int textureArrayIndex = 0)
        {
            if (_occlusionPassData.isDepth2DArray)
                GPUIHiZDepthTextureUtility.CopyHiZTextureArrayWithComputeShader(_occlusionPassData.copyCS, CameraDepthTexture, HiZDepthTexture, offset, textureArrayIndex);
            else
                GPUIHiZDepthTextureUtility.CopyHiZTextureWithComputeShader(_occlusionPassData.copyCS, CameraDepthTexture, HiZDepthTexture, offset);
        }

        #endregion Direct Texture Access Mode

        #region Command Buffer Mode

        private void DisposeOcclusionCommandBuffer()
        {
            if (_occlusionCommandBuffer != null)
            {
                if (ActiveCullingMode == GPUIOcclusionCullingMode.CommandBufferAddedToCamera)
                    _activeCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, _occlusionCommandBuffer);
                _occlusionCommandBuffer.Dispose();
                _occlusionCommandBuffer = null;
            }
        }

        private void CreateOcclusionCommandBuffer()
        {
            DisposeOcclusionCommandBuffer();
            RenderTargetIdentifier unityDepthIdentifier;
            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                unityDepthIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);
            else
                unityDepthIdentifier = new RenderTargetIdentifier(CameraDepthTexture);

            _occlusionCommandBuffer = new CommandBuffer();
            _occlusionCommandBuffer.name = GPUI_HiZ_CommandBuffer_NAME;

            CreateOcclusionCommandBuffer(_occlusionCommandBuffer, unityDepthIdentifier);
        }

        private void CreateOcclusionCommandBuffer(CommandBuffer commandBuffer, RenderTargetIdentifier unityDepthIdentifier)
        {
            if (HiZDepthTexture == null)
                return;

#if GPUI_XR
            if (_occlusionPassData.isVRCulling)
            {
                int sourceWidth = HiZTextureSize.x / 2;
                if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
                {
                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left || _activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
                        UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, 0);
                    else
                        UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, sourceWidth);

                    if (_activeCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
                        UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, sourceWidth);
                }
                else if (_occlusionPassData.isDepth2DArray && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, 0);
                    UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, sourceWidth, 1);
                }
                else
                    UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, sourceWidth, 0);
            }
            else
#endif
                UpdateTextureWithComputeShaderCB(commandBuffer, unityDepthIdentifier, _hiZDepthIdentifier, HiZTextureSize.x, 0);

            for (int i = 0; i < _occlusionPassData.hiZMipLevels - 1; i++)
                GPUIHiZDepthTextureUtility.ReduceTextureWithComputeShader(_occlusionPassData.reduceCS, commandBuffer, _hiZDepthIdentifier, HiZTextureSize.x, HiZTextureSize.y, i, i + 1);
        }

        private void UpdateTextureWithComputeShaderCB(CommandBuffer commandBuffer, RenderTargetIdentifier unityDepthIdentifier, RenderTargetIdentifier hiZIdentifier, int sourceWidth, int offset, int textureArrayIndex = 0)
        {
            if (_occlusionPassData.isDepth2DArray)
                GPUIHiZDepthTextureUtility.CopyHiZTextureArrayWithComputeShader(_occlusionPassData.copyCS, commandBuffer, unityDepthIdentifier, HiZTextureSize.x, HiZTextureSize.y, hiZIdentifier, offset, textureArrayIndex);
            else
                GPUIHiZDepthTextureUtility.CopyHiZTextureWithComputeShader(_occlusionPassData.copyCS, commandBuffer, unityDepthIdentifier, HiZTextureSize.x, HiZTextureSize.y, hiZIdentifier, offset);
        }
        #endregion Command Buffer Mode

        #region URP Scriptable Render Pass Mode

#if UNITY_6000_0_OR_NEWER && GPUI_URP
        private GPUIHiZGeneratorRenderPass _hiZGeneratorRenderPass;
        private GPUIHiZGeneratorRenderPass _hiZGeneratorRenderPassRightEye;
        private int _renderPassQueuedFrameCount;

        private class GPUIHiZGeneratorRenderPass : ScriptableRenderPass, IDisposable
        {
            public bool IsSetup { get; private set; }
            private GPUIOcclusionCullingData _occlusionCullingData;
            private BaseRenderFunc<PassData, ComputeGraphContext> _renderFunc;
            private RTHandle _hiZTextureHandle;
            private int _eyeIndex;

            private CommandBuffer _compatibilityCB;

            private class PassData : GPUIOcclusionPassData
            {
                public TextureHandle cameraDepthHandle;
                public TextureHandle hiZTextureHandle;
                public int eyeIndex;
            }

            public GPUIHiZGeneratorRenderPass(GPUIOcclusionCullingData occlusionCullingData)
            {
                _occlusionCullingData = occlusionCullingData;
            }

            public void Setup(RenderTexture renderTexture, int eyeIndex = 0)
            {
                _hiZTextureHandle = RTHandles.Alloc(renderTexture);
                _eyeIndex = eyeIndex;
                IsSetup = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;

                TextureHandle inputTexture = renderGraph.ImportTexture(_hiZTextureHandle);
                int cameraDepthSlices = cameraDepthTexture.GetDescriptor(renderGraph).slices;

                if (_renderFunc == null)
                    _renderFunc = CopyAndReducePass;

                using (var builder = renderGraph.AddComputePass(GPUI_HiZ_CommandBuffer_NAME, out PassData passData))
                {
                    _occlusionCullingData._occlusionPassData.CopyTo(passData);
                    passData.cameraDepthHandle = cameraDepthTexture;
                    passData.hiZTextureHandle = inputTexture;
                    passData.isDepth2DArray = cameraDepthSlices > 1;
                    passData.eyeIndex = _eyeIndex;

                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(inputTexture, AccessFlags.Write);
                    builder.SetRenderFunc(_renderFunc);
                }
            }

            private static void CopyAndReducePass(PassData data, ComputeGraphContext cgContext)
            {
                int sourceWidth = data.isVRCulling ? data.hiZTextureSize.x / 2 : data.hiZTextureSize.x;

                #region Copy
                if (data.isDepth2DArray)
                {
                    GPUIHiZDepthTextureUtility.CopyHiZTextureArrayWithComputeShader(data.copyCS, cgContext.cmd, data.cameraDepthHandle, sourceWidth, data.hiZTextureSize.y, data.hiZTextureHandle, 0, 0);
                    if (data.isVRCulling)
                        GPUIHiZDepthTextureUtility.CopyHiZTextureArrayWithComputeShader(data.copyCS, cgContext.cmd, data.cameraDepthHandle, sourceWidth, data.hiZTextureSize.y, data.hiZTextureHandle, sourceWidth, 1);
                }
                else
                {
                    if (data.eyeIndex == 0)
                        GPUIHiZDepthTextureUtility.CopyHiZTextureWithComputeShader(data.copyCS, cgContext.cmd, data.cameraDepthHandle, sourceWidth, data.hiZTextureSize.y, data.hiZTextureHandle, 0);
                    else if (data.isVRCulling)
                        GPUIHiZDepthTextureUtility.CopyHiZTextureWithComputeShader(data.copyCS, cgContext.cmd, data.cameraDepthHandle, sourceWidth, data.hiZTextureSize.y, data.hiZTextureHandle, sourceWidth);
                }
                #endregion Copy

                #region Reduce
                for (int i = 0; i < data.hiZMipLevels - 1; i++)
                    GPUIHiZDepthTextureUtility.ReduceTextureWithComputeShader(data.reduceCS, cgContext.cmd, data.hiZTextureHandle, data.hiZTextureSize.x, data.hiZTextureSize.y, i, i + 1);
                #endregion Reduce

            }

            public void Dispose()
            {
                _hiZTextureHandle.Release();
                IsSetup = false;
                if (_compatibilityCB != null)
                {
                    _compatibilityCB.Dispose();
                    _compatibilityCB = null;
                }
            }
        }
#endif

        #endregion URP Scriptable Render Pass Mode

        #region HDRP Custom Pass Mode

#if GPUI_HDRP
        internal GPUIHiZGeneratorCustomPass _hiZGeneratorCustomPass;
        internal CustomPassVolume _hiZGeneratorCustomPassVolume;

        internal class GPUIHiZGeneratorCustomPass : CustomPass
        {
            private GPUIOcclusionCullingData _occlusionCullingData;

            public GPUIHiZGeneratorCustomPass(GPUIOcclusionCullingData occlusionCullingData)
            {
                _occlusionCullingData = occlusionCullingData;
                _occlusionCullingData._occlusionPassData.isDepth2DArray = true;
            }

            protected override void Execute(CustomPassContext cgContext)
            {
                _occlusionCullingData.CreateOcclusionCommandBuffer(cgContext.cmd, cgContext.cameraDepthBuffer);
            }
        }
#endif

        #endregion HDRP Custom Pass Mode
    }
}