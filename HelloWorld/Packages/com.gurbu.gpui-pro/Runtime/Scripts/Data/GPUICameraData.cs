// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if GPUI_URP && UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
#endif

namespace GPUInstancerPro
{
    public class GPUICameraData : IGPUIDisposable
    {
        #region Properties
        private Camera _camera;
        public Camera ActiveCamera => _camera;
        private bool _isCullingInitialized;

        /// <summary>
        /// Contains data for each LOD, RenderSourceGroup and Camera combination
        /// </summary>
        internal GPUIVisibilityBuffer _visibilityBuffer;
        /// <summary>
        /// Draw call arguments
        /// </summary>
        internal GPUIDataBuffer<GraphicsBuffer.IndirectDrawIndexedArgs> _commandBuffer;
        /// <summary>
        /// Contains indexes of visibilityBuffer for each RenderSourceGroup
        /// <para>Key => Render Source Group Key</para> 
        /// <para>Value => Visibility Buffer Index</para> 
        /// </summary>
        internal Dictionary<int, int> _visibilityBufferIndexes;

        protected Transform _cachedTransform;
        protected ComputeShader _CS_CommandBufferUtility;
        protected ComputeShader _CS_CameraVisibility;
        protected ComputeShader _CS_OptionalRenderer;

        private Matrix4x4 _mvpMatrix;
        private Vector4 _cameraPositionAndHalfAngle;
        private Vector4 _additionalValues;
        private Quaternion _cameraRotation;
        private int[] _sizeAndIndexes;
        private int[] _sizeAndIndexes2;
        public string name;
        internal int _instanceCountMultiplier = 1;

        /// <summary>
        /// Making the initialization on pre-render gives the correct settings, that is why we use this field instead of directly calling the InitializeOcclusionCulling method
        /// </summary>
        public bool autoInitializeOcclusionCulling;
        public GPUIOcclusionCullingData OcclusionCullingData { get; private set; }
        private float _dynamicOcclusionOffsetIntensity;
        private int[] _hiZTextureSize;
        public RenderTexture HiZDepthTexture { get { return OcclusionCullingData == null ? null : OcclusionCullingData.HiZDepthTexture; } }
        public Vector2Int HiZTextureSize { get { return OcclusionCullingData == null ? Vector2Int.zero : OcclusionCullingData.HiZTextureSize; } }

#if GPUI_XR
        private Matrix4x4 _mvpMatrix2;
#endif
        private bool _isVRCulling;
        public bool IsVRCulling => _isVRCulling;

#if UNITY_EDITOR
        public bool renderToSceneView = true;
#endif
        #endregion Properties

        #region Initialize/Dispose
        public GPUICameraData(Camera camera)
        {
            _camera = camera;
            _cachedTransform = _camera.transform;
            name = camera.name;

            _CS_CommandBufferUtility = GPUIConstants.CS_CommandBufferUtility;
            _CS_CameraVisibility = GPUIConstants.CS_CameraVisibility;
            _CS_OptionalRenderer = GPUIConstants.CS_OptionalRenderer;

            _visibilityBuffer = new(this, "Visibility");
            _visibilityBufferIndexes = new();
            _commandBuffer = new("Command", 0, GraphicsBuffer.Target.IndirectArguments);
            _sizeAndIndexes = new int[4];
            _sizeAndIndexes2 = new int[4];
            _hiZTextureSize = new int[4];
        }

        public void ReleaseBuffers()
        {
            DisableOcclusionCulling();
            if (_visibilityBuffer != null)
                _visibilityBuffer.ReleaseBuffers();
            if (_visibilityBufferIndexes != null)
                _visibilityBufferIndexes.Clear();
            if (_commandBuffer != null)
                _commandBuffer.ReleaseBuffers();
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.RenderSourceGroupProvider.DisposeCameraData(this);
        }

        public void Dispose()
        {
            ReleaseBuffers();
            if (OcclusionCullingData != null)
            {
                OcclusionCullingData.Dispose();
                OcclusionCullingData = null;
            }
            _isCullingInitialized = false;
        }

        internal bool InitializeCulling()
        {
            GPUIRuntimeSettings.Instance.SetRuntimeSettings();
            _instanceCountMultiplier = 1;
            _isVRCulling = false;
#if GPUI_XR
            if (GPUIRuntimeSettings.Instance.IsVREnabled)
            {
                if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
                    _instanceCountMultiplier = 2;
                _isVRCulling = _camera.stereoTargetEye == StereoTargetEyeMask.Both
                    && !GPUIRuntimeSettings.Instance.IsHDRP; // Currently VR culling is not supported with HDRP
                if (_isVRCulling)
                {
                    _CS_CameraVisibility = GPUIConstants.CS_CameraVisibilityXR;
                    if (_CS_CameraVisibility == null)
                    {
                        Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find XR visibility compute shader for camera. Make sure to import the XR support package by selecting Tools -> GPU Instancer Pro -> Reimport Packages or by manually importing the unity package under Packages/GPU Instancer Pro - Core/Editor/Extras/XR_Support_GPUIPro.", _camera);
                        return false;
                    }
                }
            }
#endif
            _isCullingInitialized = true;
            return true;
        }

        #endregion Initialize/Dispose

        #region Occlusion Culling

        private void InitializeOcclusionCulling()
        {
            if (!_isCullingInitialized)
                if (!InitializeCulling())
                    return;

            autoInitializeOcclusionCulling = false;

            if (GPUIRuntimeSettings.Instance.DisableOcclusionCulling || GPUIRuntimeSettings.Instance.occlusionCullingCondition == GPUIOcclusionCullingCondition.Never)
                return;

            if (GPUIRuntimeSettings.Instance.occlusionCullingCondition == GPUIOcclusionCullingCondition.IfDepthAvailable && !ActiveCamera.IsDepthTextureAvailable())
                return;

#if GPUI_XR
            if (GPUIRuntimeSettings.Instance.IsVREnabled && GPUIRuntimeSettings.Instance.IsHDRP)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "GPU Instancer Pro currently does not support Occlusion Culling for VR devices on HDRP. Occlusion culling will be disabled.");
                return;
            }
#endif

            if (OcclusionCullingData == null)
                OcclusionCullingData = new GPUIOcclusionCullingData(ActiveCamera, GPUIRuntimeSettings.Instance.occlusionCullingMode, _isVRCulling);
        }

        public void DisableOcclusionCulling()
        {
            autoInitializeOcclusionCulling = false;
            if (OcclusionCullingData != null)
            {
                OcclusionCullingData.Dispose();
                OcclusionCullingData = null;
            }
        }

        internal void UpdateHiZTexture(ScriptableRenderContext context)
        {
            if (OcclusionCullingData == null)
                return;
            OcclusionCullingData.UpdateHiZTexture(context);
            _hiZTextureSize[0] = OcclusionCullingData.HiZTextureSize.x;
            _hiZTextureSize[1] = OcclusionCullingData.HiZTextureSize.y;
            if (OcclusionCullingData.HiZDepthTexture != null)
            {
                _hiZTextureSize[2] = OcclusionCullingData.HiZDepthTexture.width;
                _hiZTextureSize[3] = OcclusionCullingData.HiZDepthTexture.height;
            }
        }

        internal void UpdateHiZTextureOnBeginRendering(Camera camera, ScriptableRenderContext context)
        {
            if (OcclusionCullingData == null)
                return;
            OcclusionCullingData.UpdateHiZTextureOnBeginRendering(camera, context);
        }

        #endregion Occlusion Culling

        #region Visibility Calculations
        public virtual void UpdateCameraData()
        {
            if (!_isCullingInitialized)
            {
                if (!InitializeCulling())
                    return;
            }
            if (autoInitializeOcclusionCulling)
                InitializeOcclusionCulling();
            Profiler.BeginSample("GPUICamera.UpdateCamera");

            _commandBuffer.UpdateBufferData();
            Matrix4x4 worldToCameraMatrix = _camera.worldToCameraMatrix;
#if GPUI_XR
            if (GPUIRuntimeSettings.Instance.IsVREnabled)
            {
                if (_camera.stereoEnabled)
                {
                    if (_isVRCulling)
                    {
                        _mvpMatrix = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * worldToCameraMatrix;
                        _mvpMatrix2 = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * worldToCameraMatrix;
                    }
                    else if (_camera.stereoTargetEye == StereoTargetEyeMask.Left)
                        _mvpMatrix = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * worldToCameraMatrix;
                    else if (_camera.stereoTargetEye == StereoTargetEyeMask.Right)
                        _mvpMatrix = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * worldToCameraMatrix;
                    else
                        _mvpMatrix = _camera.projectionMatrix * worldToCameraMatrix;

                }
                else
                    _mvpMatrix = _camera.projectionMatrix * worldToCameraMatrix;
            }
            else
#endif
                _mvpMatrix = _camera.projectionMatrix * worldToCameraMatrix;

            Vector3 cameraPos = _cachedTransform.position;
            if (OcclusionCullingData != null)
            {
                if (_dynamicOcclusionOffsetIntensity > 0) // Dynamic occlusion offset
                {
                    Quaternion cameraRot = _cachedTransform.rotation;
                    _additionalValues.y = MathF.Max(Vector3.Distance(cameraPos, _cameraPositionAndHalfAngle) * 0.01f, (1f - Mathf.Abs(Quaternion.Dot(cameraRot, _cameraRotation))) * 100f) * _dynamicOcclusionOffsetIntensity;
                    _cameraRotation = cameraRot;
                }
                else
                    _additionalValues.y = 0f;

                OcclusionCullingData.CheckScreenSize();  // Screen size check
            }

            _cameraPositionAndHalfAngle.x = cameraPos.x;
            _cameraPositionAndHalfAngle.y = cameraPos.y;
            _cameraPositionAndHalfAngle.z = cameraPos.z;
            _cameraPositionAndHalfAngle.w = Mathf.Tan(Mathf.Deg2Rad * _camera.fieldOfView * 0.25f);

            MakeVisibilityCalculations();

            Profiler.EndSample();
        }

        protected void MakeVisibilityCalculations()
        {
            bool isVisibilityBufferUpdated = _visibilityBuffer.UpdateBufferData();

            if (_visibilityBuffer.Buffer == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Visibility buffer is null! Camera: " + ActiveCamera, ActiveCamera);
#endif
                return;
            }

            if (GPUIRenderingSystem.Instance.ParameterBuffer.Buffer == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "ParameterBuffer is null!");
#endif
                return;
            }

            Profiler.BeginSample("GPUICamera.Visibility");
            int frameCount = Time.frameCount;

            Profiler.BeginSample("GPUICamera.BeforeVisibility");
            if (!isVisibilityBufferUpdated)
            {
                if (_visibilityBuffer.IsDataRequested())
                    _visibilityBuffer.WaitForReadbackCompletion();
                _CS_CommandBufferUtility.SetBuffer(0, GPUIConstants.PROP_visibilityBuffer, _visibilityBuffer);
                _CS_CommandBufferUtility.SetInt(GPUIConstants.PROP_bufferSize, _visibilityBuffer.Length);
                _CS_CommandBufferUtility.DispatchX(0, _visibilityBuffer.Length);
            }
            _CS_CameraVisibility.SetMatrix(GPUIConstants.PROP_mvpMatrix, _mvpMatrix);
            _CS_CameraVisibility.SetVector(GPUIConstants.PROP_cameraPositionAndHalfAngle, _cameraPositionAndHalfAngle);
            _CS_CameraVisibility.SetBuffer(0, GPUIConstants.PROP_parameterBuffer, GPUIRenderingSystem.Instance.ParameterBuffer);
            _CS_CameraVisibility.SetBuffer(0, GPUIConstants.PROP_visibilityBuffer, _visibilityBuffer);
            Profiler.EndSample();

            bool hasHiZDepth = OcclusionCullingData != null && OcclusionCullingData.IsHiZDepthUpdated && _hiZTextureSize[2] > 0 && _hiZTextureSize[3] > 0;
            int qualityMaximumLODLevel = QualitySettings.maximumLODLevel;

            foreach (GPUIRenderSourceGroup rsg in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                GPUILODGroupData lodGroupData = rsg.LODGroupData;
                if (rsg.BufferSize <= 0 || rsg.InstanceCount <= 0
                    || lodGroupData == null
                    || !_visibilityBufferIndexes.TryGetValue(rsg.Key, out _sizeAndIndexes[1])
                    || !rsg.Profile.TryGetParameterBufferIndex(out _sizeAndIndexes[2])
                    || !lodGroupData.TryGetParameterBufferIndex(out _sizeAndIndexes[3]))
                    continue;

                Profiler.BeginSample(rsg.ToString());
                bool hasLOD = lodGroupData.Length > 1 || lodGroupData.transitionValues[0] > 0f;
                _sizeAndIndexes[0] = rsg.BufferSize;

                GPUIShaderBuffer transformBuffer = rsg.TransformBufferData.GetTransformBuffer(this);
                if (transformBuffer.Buffer == null)
                {
                    Profiler.EndSample();
                    continue;
                }
                _additionalValues.z = Mathf.Max(qualityMaximumLODLevel, rsg.Profile.maximumLODLevel);

                GPUIShaderBuffer instanceDataBuffer = rsg.TransformBufferData.GetInstanceDataBuffer(this);
                _CS_CameraVisibility.SetBuffer(0, GPUIConstants.PROP_gpuiTransformBuffer, transformBuffer.Buffer);
                instanceDataBuffer.SetBuffer(_CS_CameraVisibility, 0, GPUIConstants.PROP_gpuiInstanceDataBuffer);

                if (hasLOD)
                {
                    if (rsg.Profile.isLODCrossFade)
                    {
                        if (rsg.Profile.isAnimateCrossFade && !rsg.TransformBufferData.IsCameraBasedBuffer)
                        {
                            _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD);
                            _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE);
                            _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE_ANIMATE);

                            _additionalValues.x = rsg.TransformBufferData.resetCrossFadeDataFrame == frameCount ? 0f : GPUIRenderingSystem.Instance.TimeSinceLastDrawCall;
                        }
                        else
                        {
                            _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD);
                            _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE);
                            _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE_ANIMATE);
                        }
                    }
                    else
                    {
                        _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_LOD);
                        _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE);
                        _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE_ANIMATE);
                    }
                }
                else
                {
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD);
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE);
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_LOD_CROSSFADE_ANIMATE);
                }

                if (rsg.Profile.isShadowCasting)
                {
                    if (rsg.Profile.isShadowFrustumCulling || rsg.Profile.isShadowOcclusionCulling)
                    {
                        _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_SHADOWCASTING);
                        _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_SHADOWCULLED);
                    }
                    else
                    {
                        _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_SHADOWCASTING);
                        _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_SHADOWCULLED);
                    }
                }
                else
                {
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_SHADOWCASTING);
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_SHADOWCULLED);
                }

                if (rsg.Profile.isOcclusionCulling && hasHiZDepth)
                {
                    _CS_CameraVisibility.EnableKeyword(GPUIConstants.Kw_GPUI_OCCLUSION_CULLING);
                    _CS_CameraVisibility.SetTexture(0, GPUIConstants.PROP_hiZMap, OcclusionCullingData.HiZDepthTexture);
                    _CS_CameraVisibility.SetInts(GPUIConstants.PROP_hiZTxtrSize, _hiZTextureSize);
                }
                else
                    _CS_CameraVisibility.DisableKeyword(GPUIConstants.Kw_GPUI_OCCLUSION_CULLING);

#if GPUI_XR
                if (_isVRCulling)
                    _CS_CameraVisibility.SetMatrix(GPUIConstants.PROP_mvpMatrix2, _mvpMatrix2);
#endif

                _CS_CameraVisibility.SetInts(GPUIConstants.PROP_sizeAndIndexes, _sizeAndIndexes);
                _CS_CameraVisibility.SetVector(GPUIConstants.PROP_additionalValues, _additionalValues);
                for (int s = 0; s < rsg.RenderSources.Count; s++)
                {
                    GPUIRenderSource rs = rsg.RenderSources[s];

                    int instanceCount = rsg.TransformBufferData.IsCameraBasedBuffer ? rsg.BufferSize : rs.instanceCount;
                    if (instanceCount <= 0 || rs.bufferStartIndex < 0)
                        continue;
                    _sizeAndIndexes2[0] = instanceCount;
                    _sizeAndIndexes2[1] = rs.bufferStartIndex;
                    _CS_CameraVisibility.SetInts(GPUIConstants.PROP_sizeAndIndexes2, _sizeAndIndexes2);

                    _CS_CameraVisibility.DispatchXHeavy(0, instanceCount);
                }

                SetOptionalRendererInstanceData(rsg, lodGroupData, instanceDataBuffer);

                instanceDataBuffer.OnDataModified();
                Profiler.EndSample();
            }
            SetCommandBufferInstanceCounts(_instanceCountMultiplier);
            Profiler.EndSample();
        }

        private void SetOptionalRendererInstanceData(GPUIRenderSourceGroup rsg, GPUILODGroupData lodGroupData, GPUIShaderBuffer instanceDataBuffer)
        {
            if (lodGroupData.optionalRendererCount > 0)
            {
                GraphicsBuffer optionalRendererStatusBuffer = rsg.TransformBufferData.GetOptionalRendererStatusBuffer();
                if (optionalRendererStatusBuffer != null)
                {
                    int kernelIndex = 0;

                    _sizeAndIndexes[2] = lodGroupData.optionalRendererCount;
                    _CS_OptionalRenderer.SetInts(GPUIConstants.PROP_sizeAndIndexes, _sizeAndIndexes);

                    if (rsg.Profile.isShadowCasting)
                        _CS_OptionalRenderer.EnableKeyword(GPUIConstants.Kw_GPUI_SHADOWCASTING);
                    else
                        _CS_OptionalRenderer.DisableKeyword(GPUIConstants.Kw_GPUI_SHADOWCASTING);

                    _CS_OptionalRenderer.SetBuffer(kernelIndex, GPUIConstants.PROP_visibilityBuffer, _visibilityBuffer);
                    instanceDataBuffer.SetBuffer(_CS_OptionalRenderer, kernelIndex, GPUIConstants.PROP_gpuiInstanceDataBuffer);
                    _CS_OptionalRenderer.SetBuffer(kernelIndex, GPUIConstants.PROP_optionalRendererStatusBuffer, optionalRendererStatusBuffer);

                    _CS_OptionalRenderer.DispatchXY(kernelIndex, _sizeAndIndexes[0], _sizeAndIndexes[2]);
                }
            }
        }

        internal void SetCommandBufferInstanceCounts(int instanceCountMultiplier)
        {
            if (_commandBuffer.Length == 0)
                return;

            Profiler.BeginSample("GPUICamera.AfterVisibility");
            _CS_CommandBufferUtility.SetBuffer(1, GPUIConstants.PROP_visibilityBuffer, _visibilityBuffer);
            _CS_CommandBufferUtility.SetBuffer(1, GPUIConstants.PROP_commandBuffer, _commandBuffer);
            _CS_CommandBufferUtility.SetInt(GPUIConstants.PROP_bufferSize, _visibilityBuffer.Length);
            _CS_CommandBufferUtility.SetInt(GPUIConstants.PROP_multiplier, instanceCountMultiplier);
            _CS_CommandBufferUtility.DispatchX(1, _visibilityBuffer.Length);
            Profiler.EndSample();
        }

        internal void ClearVisibilityData()
        {
            _visibilityBuffer.ReleaseBuffers();
            _visibilityBufferIndexes.Clear();
            _commandBuffer.ReleaseBuffers();
        }
        #endregion Visibility Calculations

        #region Getters/Setters

        public GPUIVisibilityData[] GetVisibilityDataArray()
        {
            return _visibilityBuffer.GetBufferData();
        }

        public bool TryGetVisibilityBufferIndex(GPUIRenderSourceGroup renderSourceGroup, out int visibilityBufferIndex)
        {
            return TryGetVisibilityBufferIndex(renderSourceGroup.Key, out visibilityBufferIndex);
        }

        public bool TryGetVisibilityBufferIndex(int renderSourceGroupKey, out int visibilityBufferIndex)
        {
            visibilityBufferIndex = -1;
            if (_visibilityBuffer.Length == 0)
                return false;
            if (_visibilityBufferIndexes.TryGetValue(renderSourceGroupKey, out visibilityBufferIndex))
                return true;
            return false;
        }

        public bool TryGetShaderBuffer(GPUIManager manager, int prototypeIndex, out GPUIShaderBuffer shaderBuffer)
        {
            if (manager == null)
            {
                shaderBuffer = null;
                return false;
            }
            return TryGetShaderBuffer(manager.GetRenderKey(prototypeIndex), out shaderBuffer);
        }

        public bool TryGetShaderBuffer(int renderKey, out GPUIShaderBuffer shaderBuffer)
        {
            shaderBuffer = null;
            if (renderKey == 0)
                return false;

            if (!GPUIRenderingSystem.Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource rs))
                return false;
            GPUIRenderSourceGroup renderSourceGroup = rs.renderSourceGroup;
            if (renderSourceGroup != null && renderSourceGroup.TransformBufferData != null)
            {
                shaderBuffer = renderSourceGroup.TransformBufferData.GetTransformBuffer(this);
                if (shaderBuffer != null && shaderBuffer.BufferSize > 0)
                    return true;
            }
            return false;
        }

        public GPUIDataBuffer<GPUIVisibilityData> GetVisibilityBuffer()
        {
            _visibilityBuffer.UpdateBufferData(); // In case there is a change that is not yet submitted
            return _visibilityBuffer;
        }

        public Vector3 GetCameraPosition()
        {
            return (Vector3)_cameraPositionAndHalfAngle;
        }

        public GraphicsBuffer.IndirectDrawIndexedArgs GetCommandDataAtIndex(int index)
        {
            return _commandBuffer[index];
        }

        public int GetCommandDataLength()
        {
            return _commandBuffer.Length;
        }

        public void SetDynamicOcclusionOffsetIntensity(float intensity)
        {
            _dynamicOcclusionOffsetIntensity = intensity;
        }

        #endregion Getters/Setters
    }

    #region Visibility Data Buffer
    public struct GPUIVisibilityData
    {
        /// <summary>
        /// Number of visible instances
        /// </summary>
        public uint visibleCount;
        /// <summary>
        /// Command buffer start index
        /// </summary>
        public uint commandStartIndex;
        /// <summary>
        /// Command buffer element count (e.g. number of sub-meshes and child renderers)
        /// </summary>
        public uint commandCount;
        /// <summary>
        ///  0 for instances,  1 for shadows
        /// </summary>
        public uint additional;
    }

    public class GPUIVisibilityBuffer : GPUIDataBuffer<GPUIVisibilityData>
    {
        public GPUICameraData cameraData;

        public GPUIVisibilityBuffer(GPUICameraData cameraData, string name, GraphicsBuffer.Target target = GraphicsBuffer.Target.Structured) : base(name, 0, target)
        {
            this.cameraData = cameraData;
        }
    }
    #endregion Visibility Data Buffer
}