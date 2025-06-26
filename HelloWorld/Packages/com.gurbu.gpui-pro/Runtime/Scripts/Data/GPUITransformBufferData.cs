// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUITransformBufferData : IGPUIDisposable
    {
        private GPUIRenderSourceGroup _renderSourceGroup;
        public GPUIRenderSourceGroup RenderSourceGroup {  get { return _renderSourceGroup; } }

        /// <summary>
        /// Contains transform buffers (per camera when CameraBased)
        /// </summary>
        private Dictionary<int, GPUIShaderBuffer> _transformBufferDict;
        /// <summary>
        /// Contains transform buffers (per camera when CameraBased)
        /// </summary>
        public Dictionary<int, GPUIShaderBuffer>.ValueCollection TransformBufferValues => _transformBufferDict == null ? null : _transformBufferDict.Values;

        public GraphicsBuffer PreviousFrameTransformBuffer { get; private set; }
        private bool _hasPreviousFrameTransformBuffer;
        public bool HasPreviousFrameTransformBuffer
        {
            get => _hasPreviousFrameTransformBuffer;
            private set
            {
                if (_hasPreviousFrameTransformBuffer != value)
                {
                    _hasPreviousFrameTransformBuffer = value;
                    if (!GPUIShaderBindings.Instance.stripObjectMotionVectorVariants)
                    {
                        if (value)
                        {
                            if (_renderSourceGroup.AddShaderKeyword(GPUIConstants.Kw_GPUI_OBJECT_MOTION_VECTOR_ON))
                                _renderSourceGroup.RemoveReplacementMaterials();
                        }
                        else
                        {
                            if (_renderSourceGroup.RemoveShaderKeyword(GPUIConstants.Kw_GPUI_OBJECT_MOTION_VECTOR_ON))
                                _renderSourceGroup.RemoveReplacementMaterials();
                        }
                    }
                    else if (value)
                    {
                        Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not generate Per Object Motion Vector data. Disabled by the Editor Settings.");
                        _hasPreviousFrameTransformBuffer = false;
                    }
                }
            }
        }
        private int _previousFrameBufferFrameNo;

        internal GraphicsBuffer _perInstanceLightProbesBuffer;
        private bool _hasPerInstanceLightProbes;
        public bool HasPerInstanceLightProbes
        {
            get => _hasPerInstanceLightProbes;
            private set
            {
                if (_hasPerInstanceLightProbes != value)
                {
                    _hasPerInstanceLightProbes = value;
                    if (!GPUIShaderBindings.Instance.stripPerInstanceLightProbeVariants)
                    {
                        if (value)
                        {
                            if (_renderSourceGroup.AddShaderKeyword(GPUIConstants.Kw_GPUI_PER_INSTANCE_LIGHTPROBES_ON))
                                _renderSourceGroup.RemoveReplacementMaterials();
                        }
                        else
                        {
                            if (_renderSourceGroup.RemoveShaderKeyword(GPUIConstants.Kw_GPUI_PER_INSTANCE_LIGHTPROBES_ON))
                                _renderSourceGroup.RemoveReplacementMaterials();
                        }
                    }
                    else if (value)
                    {
                        Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not generate Per Instance Light Probe data. Disabled by the Editor Settings.");
                        _hasPerInstanceLightProbes = false;
                    }
                }
            }
        }
        private bool _isAllowPerInstanceLightProbes;

        /// <summary>
        /// Contains results of the visibility calculations for LODs and shadows (such as instanceID and crossFadeValue) for each camera
        /// </summary>
        private Dictionary<int, GPUIShaderBuffer> _instanceDataBufferDict;

        public int resetCrossFadeDataFrame;

        public bool IsCameraBasedBuffer => _renderSourceGroup.TransformBufferType == GPUITransformBufferType.CameraBased;

        /// <summary>
        /// Keeps bit-wise enabled/disabled status for each instance's optional renderers. Each instance is represented with a 32 bit uint - so they can have maximum 32 optional renderers. 0 means enabled, 1 means disabled, so all renderers would be enabled by default.
        /// </summary>
        private GraphicsBuffer _optionalRendererStatusBuffer;

        public bool IsGeneratePerInstanceLightProbes => _isAllowPerInstanceLightProbes && _renderSourceGroup.Profile.lightProbeSetting == GPUILightProbeSetting.PerInstance;

        public GPUITransformBufferData(GPUIRenderSourceGroup renderSourceGroup)
        {
            _renderSourceGroup = renderSourceGroup;

            if (Application.isPlaying && !IsCameraBasedBuffer && !GPUIRuntimeSettings.Instance.DisableShaderBuffers && !GPUIRuntimeSettings.Instance.IsBuiltInRP && renderSourceGroup.LODGroupData != null && renderSourceGroup.Profile != null && renderSourceGroup.Profile.enablePerObjectMotionVectors && !GPUIShaderBindings.Instance.stripObjectMotionVectorVariants)
            {
                if (renderSourceGroup.LODGroupData.HasObjectMotion())
                {
                    HasPreviousFrameTransformBuffer = true;
                    _previousFrameBufferFrameNo = -1;
                }
            }

            _isAllowPerInstanceLightProbes = Application.isPlaying && !IsCameraBasedBuffer && !GPUIRuntimeSettings.Instance.DisableShaderBuffers && !GPUIRuntimeSettings.IsAdaptiveProbeVolumesEnabled() && !GPUIShaderBindings.Instance.stripPerInstanceLightProbeVariants;
        }

        public void ReleaseBuffers()
        {
            ReleaseTransformBuffers();
            ReleaseInstanceDataBuffers();
            ReleaseOptionalRendererBuffers();
            ReleaseLightProbeBuffers();
        }

        internal void ReleaseTransformBuffers()
        {
            if (_transformBufferDict != null)
            {
                foreach (var tb in _transformBufferDict.Values)
                {
                    if (tb != null)
                        tb.Dispose();
                }
                _transformBufferDict = null;
            }
            if (PreviousFrameTransformBuffer != null)
                PreviousFrameTransformBuffer.Dispose();
        }

        internal void ReleaseInstanceDataBuffers()
        {
            if (_instanceDataBufferDict != null)
            {
                foreach (GPUIShaderBuffer instanceDataBuffer in _instanceDataBufferDict.Values)
                {
                    if (instanceDataBuffer != null)
                        instanceDataBuffer.Dispose();
                }
                _instanceDataBufferDict = null;
            }
        }

        internal void ReleaseOptionalRendererBuffers()
        {
            if (_optionalRendererStatusBuffer != null)
            {
                _optionalRendererStatusBuffer.Dispose();
                _optionalRendererStatusBuffer = null;
            }
        }

        internal void ReleaseLightProbeBuffers()
        {
            HasPerInstanceLightProbes = false;
            if (_perInstanceLightProbesBuffer != null)
            {
                _perInstanceLightProbesBuffer.Dispose();
                _perInstanceLightProbesBuffer = null;
            }
        }

        internal void ReleaseInstanceDataBuffers(GPUICameraData cameraData)
        {
            int key = cameraData.ActiveCamera.GetInstanceID();
            if (_instanceDataBufferDict != null && _instanceDataBufferDict.TryGetValue(key, out GPUIShaderBuffer instanceDataBuffer))
            {
                if (instanceDataBuffer != null)
                    instanceDataBuffer.Dispose();
                _instanceDataBufferDict.Remove(key);
            }
        }

        public void Dispose()
        {
            ReleaseBuffers();
            _transformBufferDict = null;
            _instanceDataBufferDict = null;
        }

        internal void Dispose(GPUICameraData cameraData)
        {
            if (IsCameraBasedBuffer && cameraData != null && cameraData.ActiveCamera != null)
            {
                int key = cameraData.ActiveCamera.GetInstanceID();
                if (_transformBufferDict != null && _transformBufferDict.TryGetValue(key, out var tb))
                {
                    tb.Dispose();
                    _transformBufferDict.Remove(key);
                }
                if (_instanceDataBufferDict != null && _instanceDataBufferDict.TryGetValue(key, out var idb))
                {
                    idb.Dispose();
                    _instanceDataBufferDict.Remove(key);
                }
            }
        }

        internal void ResizeTransformBuffer(bool isCopyPreviousData)
        {
            if (IsCameraBasedBuffer)
            {
                Dispose();
                return;
            }

            if (_transformBufferDict == null)
                _transformBufferDict = new();

            _transformBufferDict.TryGetValue(0, out GPUIShaderBuffer previousTransformBuffer);
            bool hasPreviousBuffer = previousTransformBuffer != null;
            int previousBufferSize = hasPreviousBuffer ? previousTransformBuffer.BufferSize : 0;

            if (!hasPreviousBuffer || previousBufferSize != _renderSourceGroup.BufferSize)
            {
                GPUIShaderBuffer transformBuffer = CreateTransformBuffer();
                _transformBufferDict[0] = transformBuffer;

                if (previousTransformBuffer != null)
                {
                    if (isCopyPreviousData)
                        transformBuffer.Buffer.SetData(previousTransformBuffer.Buffer, 0, 0, Math.Min(_renderSourceGroup.BufferSize, previousBufferSize));
                    previousTransformBuffer.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousTransformBuffer"></param>
        /// <param name="transformBuffer"></param>
        /// <returns>True if an existing buffer is replaced with a new one</returns>
        internal bool ResizeTransformBuffer(out GPUIShaderBuffer previousTransformBuffer, out GPUIShaderBuffer transformBuffer)
        {
            previousTransformBuffer = null;
            transformBuffer = null;
            if (IsCameraBasedBuffer)
            {
                Dispose();
                return false;
            }

            if (_transformBufferDict == null)
                _transformBufferDict = new();

            _transformBufferDict.TryGetValue(0, out previousTransformBuffer);
            bool hasPreviousBuffer = previousTransformBuffer != null;
            int previousBufferSize = hasPreviousBuffer ? previousTransformBuffer.BufferSize : 0;

            if (!hasPreviousBuffer || previousBufferSize != _renderSourceGroup.BufferSize)
            {
                transformBuffer = CreateTransformBuffer();
                _transformBufferDict[0] = transformBuffer;
                return true;
            }
            return false;
        }

        public unsafe void CalculateInterpolatedLightAndOcclusionProbes<T>(NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : unmanaged
        {
            if (IsGeneratePerInstanceLightProbes)
            {
                HasPerInstanceLightProbes = true;
                Vector3 offset = RenderSourceGroup.Profile.lightProbePositionOffset;
                offset += RenderSourceGroup.LODGroupData.bounds.center;
                GPUIRenderingSystem.Instance.CalculateInterpolatedLightAndOcclusionProbes(this, NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(matrices), managedBufferStartIndex, graphicsBufferStartIndex, count, RenderSourceGroup.BufferSize, offset);
            }
            else if (HasPerInstanceLightProbes)
                ReleaseLightProbeBuffers();
        }

        public unsafe void CalculateInterpolatedLightAndOcclusionProbes<T>(T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : unmanaged
        {
            if (IsGeneratePerInstanceLightProbes)
            {
                HasPerInstanceLightProbes = true;
                Vector3 offset = RenderSourceGroup.Profile.lightProbePositionOffset;
                offset += RenderSourceGroup.LODGroupData.bounds.center;
                fixed (void* ptr = matrices)
                    GPUIRenderingSystem.Instance.CalculateInterpolatedLightAndOcclusionProbes(this, ptr, managedBufferStartIndex, graphicsBufferStartIndex, count, RenderSourceGroup.BufferSize, offset);
            }
            else if (HasPerInstanceLightProbes)
                ReleaseLightProbeBuffers();
        }

        public unsafe void CalculateInterpolatedLightAndOcclusionProbes<T>(List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : unmanaged
        {
            if (IsGeneratePerInstanceLightProbes)
            {
                HasPerInstanceLightProbes = true;
                Vector3 offset = RenderSourceGroup.Profile.lightProbePositionOffset;
                offset += RenderSourceGroup.LODGroupData.bounds.center;
                T[] matricesArray = GPUIUtility.GetListInternalArray(matrices);
                fixed (void* ptr = matricesArray)
                    GPUIRenderingSystem.Instance.CalculateInterpolatedLightAndOcclusionProbes(this, ptr, managedBufferStartIndex, graphicsBufferStartIndex, count, RenderSourceGroup.BufferSize, offset);
            }
            else if (HasPerInstanceLightProbes)
                ReleaseLightProbeBuffers();
        }

        internal void SetTransformBufferData<T>(NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : unmanaged
        {
            GPUIShaderBuffer transformBuffer = GetTransformBuffer();
            transformBuffer.Buffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);
            if (isOverwritePreviousFrameBuffer && HasPreviousFrameTransformBuffer && PreviousFrameTransformBuffer != null && graphicsBufferStartIndex < PreviousFrameTransformBuffer.count)
                PreviousFrameTransformBuffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, Math.Min(count, PreviousFrameTransformBuffer.count - graphicsBufferStartIndex));

            CalculateInterpolatedLightAndOcclusionProbes(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);

            OnTransformDataModified();
        }

        internal void SetTransformBufferData<T>(T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : unmanaged
        {
            GPUIShaderBuffer transformBuffer = GetTransformBuffer();
            transformBuffer.Buffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);
            if (isOverwritePreviousFrameBuffer && HasPreviousFrameTransformBuffer && PreviousFrameTransformBuffer != null && graphicsBufferStartIndex < PreviousFrameTransformBuffer.count)
                PreviousFrameTransformBuffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, Math.Min(count, PreviousFrameTransformBuffer.count - graphicsBufferStartIndex));

            CalculateInterpolatedLightAndOcclusionProbes(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);

            OnTransformDataModified();
        }

        internal void SetTransformBufferData<T>(List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer) where T : unmanaged
        {
            GPUIShaderBuffer transformBuffer = GetTransformBuffer();
            transformBuffer.Buffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);
            if (isOverwritePreviousFrameBuffer && HasPreviousFrameTransformBuffer && PreviousFrameTransformBuffer != null && graphicsBufferStartIndex < PreviousFrameTransformBuffer.count)
                PreviousFrameTransformBuffer.SetData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, Math.Min(count, PreviousFrameTransformBuffer.count - graphicsBufferStartIndex));

            CalculateInterpolatedLightAndOcclusionProbes(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count);

            OnTransformDataModified();
        }

        internal void RemoveIndexes(int startIndex, int count)
        {
            if (IsCameraBasedBuffer)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "RemoveIndexes method can not be used with Camera Based transform buffers.");
                return;
            }

            GPUIShaderBuffer transformBuffer = CreateTransformBuffer();

            if (_transformBufferDict.TryGetValue(0, out GPUIShaderBuffer previousTransformBuffer))
            {
                transformBuffer.Buffer.SetData(previousTransformBuffer.Buffer, 0, 0, startIndex);
                transformBuffer.Buffer.SetData(previousTransformBuffer.Buffer, startIndex + count, startIndex, _renderSourceGroup.BufferSize - startIndex);
                previousTransformBuffer.Dispose();
            }

            _transformBufferDict[0] = transformBuffer;
            OnTransformDataModified();
        }

        private GPUIShaderBuffer CreateTransformBuffer()
        {
            //Debug.Log(GPUIConstants.LOG_PREFIX + "Creating new buffer with size: " + _renderSourceGroup.bufferSize);
            ReleaseInstanceDataBuffers();
            return new GPUIShaderBuffer(_renderSourceGroup.BufferSize, 4 * 4 * 4);
        }

        private GPUIShaderBuffer CreateInstanceDataBuffer(int instanceDataBufferSize)
        {
            resetCrossFadeDataFrame = Time.frameCount;
            return new GPUIShaderBuffer(instanceDataBufferSize, 4 * 4);
        }

        public GPUIShaderBuffer GetTransformBuffer()
        {
            if (IsCameraBasedBuffer)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "GetTransformBuffer method can not be used with Camera Based transform buffers.");
                return null;
            }
            if (_transformBufferDict == null)
                _transformBufferDict = new();
            else if (_transformBufferDict.TryGetValue(0, out GPUIShaderBuffer transformBuffer) && transformBuffer != null)
                return transformBuffer;
            GPUIShaderBuffer result = CreateTransformBuffer();
            _transformBufferDict[0] = result;
            return result;
        }

        public GPUIShaderBuffer GetTransformBuffer(GPUICameraData cameraData)
        {
            if (!IsCameraBasedBuffer || cameraData == null)
                return GetTransformBuffer();
            int key = cameraData.ActiveCamera.GetInstanceID();
            if (_transformBufferDict == null)
                _transformBufferDict = new();
            if (!_transformBufferDict.TryGetValue(key, out var result) || result == null)
            {
                result = CreateTransformBuffer();
                _transformBufferDict[key] = result;
            }
            return result;
        }

        public GPUIShaderBuffer GetInstanceDataBuffer(GPUICameraData cameraData)
        {
            int key = cameraData.ActiveCamera.GetInstanceID();
            if (_instanceDataBufferDict == null)
                _instanceDataBufferDict = new();

            GPUILODGroupData lodGroupData = _renderSourceGroup.LODGroupData;
            Debug.Assert(lodGroupData != null, "Can not find GPUILODGroupData");

            int instanceDataBufferSize = _renderSourceGroup.BufferSize * ((lodGroupData.Length + lodGroupData.optionalRendererCount)
                * (_renderSourceGroup.Profile.isShadowCasting ? 2 : 1)  // multiply with 2 for shadows
                + (_renderSourceGroup.Profile.isLODCrossFade && _renderSourceGroup.Profile.isAnimateCrossFade && !IsCameraBasedBuffer ? 1 : 0) // +1 for keeping crossfading state
                );

            if (!_instanceDataBufferDict.TryGetValue(key, out GPUIShaderBuffer instanceDataBuffer) || instanceDataBuffer == null || instanceDataBuffer.BufferSize != instanceDataBufferSize)
            {
                if (instanceDataBuffer != null)
                    instanceDataBuffer.Dispose();
                instanceDataBuffer = CreateInstanceDataBuffer(instanceDataBufferSize);
                _instanceDataBufferDict[key] = instanceDataBuffer;
            }
            return instanceDataBuffer;
        }

        public void SetMPBBuffers(MaterialPropertyBlock mpb, GPUICameraData cameraData)
        {
            GPUIShaderBuffer transformBuffer = GetTransformBuffer(cameraData);
            GPUIShaderBuffer instanceDataBuffer = GetInstanceDataBuffer(cameraData);
            if (GPUIRuntimeSettings.Instance.DisableShaderBuffers)
            {
                mpb.SetTexture(GPUIConstants.PROP_gpuiTransformBufferTexture, transformBuffer.Texture);
                mpb.SetTexture(GPUIConstants.PROP_gpuiInstanceDataBufferTexture, instanceDataBuffer.Texture);
            }
            else
            {
                mpb.SetBuffer(GPUIConstants.PROP_gpuiTransformBuffer, transformBuffer.Buffer);
                mpb.SetBuffer(GPUIConstants.PROP_gpuiInstanceDataBuffer, instanceDataBuffer.Buffer);

                #region Motion Vectors
                if (HasPreviousFrameTransformBuffer)
                {
                    bool hasMotionVectorBuffer = PreviousFrameTransformBuffer != null;
                    mpb.SetInt(GPUIConstants.PROP_hasPreviousFrameTransformBuffer, hasMotionVectorBuffer ? 1 : 0);
                    if (hasMotionVectorBuffer)
                    {
                        int bufferSize = transformBuffer.Buffer.count;
                        if (PreviousFrameTransformBuffer.count < bufferSize)
                        {
                            int previousBufferSize = PreviousFrameTransformBuffer.count;
                            GraphicsBuffer newBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4 * 4 * 4);
                            newBuffer.SetData(PreviousFrameTransformBuffer, 0, 0, previousBufferSize);
                            newBuffer.SetData(transformBuffer.Buffer, previousBufferSize, previousBufferSize, bufferSize - previousBufferSize);
                            PreviousFrameTransformBuffer.Release();
                            PreviousFrameTransformBuffer = newBuffer;
                        }
                        mpb.SetBuffer(GPUIConstants.PROP_gpuiPreviousFrameTransformBuffer, PreviousFrameTransformBuffer);
                    }
                    else
                        mpb.SetBuffer(GPUIConstants.PROP_gpuiPreviousFrameTransformBuffer, GPUIRenderingSystem.Instance.DummyGraphicsBuffer);
                }
                #endregion Motion Vectors

                #region Light Probes
                if (HasPerInstanceLightProbes)
                {
                    if (RenderSourceGroup.Profile.lightProbeSetting != GPUILightProbeSetting.PerInstance)
                    {
                        ReleaseLightProbeBuffers();
                    }
                    else
                    {
                        bool hasPerInstanceLightProbeBuffer = _perInstanceLightProbesBuffer != null;
                        mpb.SetInt(GPUIConstants.PROP_hasPerInstanceLightProbes, hasPerInstanceLightProbeBuffer ? 1 : 0);
                        if (hasPerInstanceLightProbeBuffer)
                            mpb.SetBuffer(GPUIConstants.PROP_gpuiPerInstanceLightProbesBuffer, _perInstanceLightProbesBuffer);
                        else
                            mpb.SetBuffer(GPUIConstants.PROP_gpuiPerInstanceLightProbesBuffer, GPUIRenderingSystem.Instance.DummyGraphicsBuffer);
                    }
                }
                #endregion Light Probes
            }
            mpb.SetFloat(GPUIConstants.PROP_transformBufferSize, _renderSourceGroup.BufferSize);
            mpb.SetFloat(GPUIConstants.PROP_instanceDataBufferSize, instanceDataBuffer.BufferSize);
            mpb.SetFloat(GPUIConstants.PROP_maxTextureSize, GPUIConstants.TEXTURE_MAX_SIZE);
        }

        internal void UpdateData(int frameNo)
        {
            if (HasPreviousFrameTransformBuffer)
            {
                if (_renderSourceGroup.BufferSize > 0 && frameNo > _previousFrameBufferFrameNo && _transformBufferDict.TryGetValue(0, out GPUIShaderBuffer transformBuffer) && transformBuffer.Buffer != null)
                {
                    _previousFrameBufferFrameNo = frameNo;
                    int bufferSize = transformBuffer.Buffer.count;
                    if (PreviousFrameTransformBuffer == null)
                        PreviousFrameTransformBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4 * 4 * 4);
                    else if (PreviousFrameTransformBuffer.count != bufferSize)
                    {
                        PreviousFrameTransformBuffer.Release();
                        PreviousFrameTransformBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4 * 4 * 4);
                    }

                    PreviousFrameTransformBuffer.SetData(transformBuffer.Buffer, 0, 0, bufferSize);
                }
            }
        }

        public void OnTransformDataModified()
        {
            if (_transformBufferDict == null)
                return;
            foreach (var tb in _transformBufferDict.Values)
            {
                if (tb != null)
                    tb.OnDataModified();
            }
            GPUIRenderingSystem.OnBufferDataModified?.Invoke(this);
        }

        public void OnTransformDataModified(GPUIShaderBuffer tb)
        {
            tb.OnDataModified();
            GPUIRenderingSystem.OnBufferDataModified?.Invoke(this);
        }

        public void ResetPreviousFrameBuffer()
        {
            UpdateData(_previousFrameBufferFrameNo + 1);
            _previousFrameBufferFrameNo = -1;
        }

        public void SetOptionalRendererStatusBufferData(NativeArray<uint> optionalRendererStatusData, int bufferStartIndex)
        {
            int bufferSize = RenderSourceGroup.BufferSize;
            if (_optionalRendererStatusBuffer != null && _optionalRendererStatusBuffer.count != bufferSize)
            {
                _optionalRendererStatusBuffer.Dispose();
                _optionalRendererStatusBuffer = null;
            }
            if (_optionalRendererStatusBuffer == null)
            {
                _optionalRendererStatusBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4);
                _optionalRendererStatusBuffer.ClearBufferData();
            }
            _optionalRendererStatusBuffer.SetData(optionalRendererStatusData, 0, bufferStartIndex, Mathf.Min(optionalRendererStatusData.Length, bufferSize - bufferStartIndex));
        }

        public GraphicsBuffer GetOptionalRendererStatusBuffer()
        {
            if (_optionalRendererStatusBuffer == null)
                return null;
            int bufferSize = RenderSourceGroup.BufferSize;
            if (_optionalRendererStatusBuffer.count != bufferSize)
            {
                var previousBuffer = _optionalRendererStatusBuffer;
                _optionalRendererStatusBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4);
                _optionalRendererStatusBuffer.ClearBufferData();
                _optionalRendererStatusBuffer.SetData(previousBuffer, 0, 0, Mathf.Min(previousBuffer.count, bufferSize));
                previousBuffer.Dispose();
            }
            return _optionalRendererStatusBuffer;
        }
    }

    public enum GPUITransformBufferType
    {
        /// <summary>
        /// One buffer for each prototype
        /// </summary>
        Default = 0,
        /// <summary>
        /// One buffer for each prototype and camera combination
        /// </summary>
        CameraBased = 1,
    }

    [Serializable]
    public struct GPUITransformData : IEquatable<GPUITransformData>
    {
        public Vector3 position;    // Translation (12 bytes)
        public Quaternion rotation; // Quaternion for rotation (16 bytes)
        public Vector3 scale;       // Non-uniform scale (12 bytes)

        public const int STRIDE = 40;

        public void SetFromMatrix(Matrix4x4 matrix)
        {
            position = matrix.GetPosition();
            rotation = matrix.rotation;
            scale = matrix.lossyScale;
        }

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(position, rotation, scale);
        }

        public void SetToTransform(Transform transform)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }

        public void SetTransformRelativeToParent(Matrix4x4 parentLTW, Matrix4x4 childLTW)
        {
            // The calculations can be performed in a single line of code, as shown below. However, manually calculating without allocations yields a more performant result.
            // SetFromMatrix(parentLTW.inverse * childLTW);

            // Extract parent matrix columns
            float pr00 = parentLTW.m00, pr01 = parentLTW.m01, pr02 = parentLTW.m02;
            float pr10 = parentLTW.m10, pr11 = parentLTW.m11, pr12 = parentLTW.m12;
            float pr20 = parentLTW.m20, pr21 = parentLTW.m21, pr22 = parentLTW.m22;

            // Parent scale (length of columns)
            float psx = (float)Math.Sqrt(pr00 * pr00 + pr10 * pr10 + pr20 * pr20);
            float psy = (float)Math.Sqrt(pr01 * pr01 + pr11 * pr11 + pr21 * pr21);
            float psz = (float)Math.Sqrt(pr02 * pr02 + pr12 * pr12 + pr22 * pr22);

            float invPsx = 1f / psx, invPsy = 1f / psy, invPsz = 1f / psz;

            // Normalize parent rotation matrix
            float r00 = pr00 * invPsx, r01 = pr01 * invPsy, r02 = pr02 * invPsz;
            float r10 = pr10 * invPsx, r11 = pr11 * invPsy, r12 = pr12 * invPsz;
            float r20 = pr20 * invPsx, r21 = pr21 * invPsy, r22 = pr22 * invPsz;

            // Extract child matrix columns
            float cr00 = childLTW.m00, cr01 = childLTW.m01, cr02 = childLTW.m02;
            float cr10 = childLTW.m10, cr11 = childLTW.m11, cr12 = childLTW.m12;
            float cr20 = childLTW.m20, cr21 = childLTW.m21, cr22 = childLTW.m22;

            // Child scale
            float csx = (float)Math.Sqrt(cr00 * cr00 + cr10 * cr10 + cr20 * cr20);
            float csy = (float)Math.Sqrt(cr01 * cr01 + cr11 * cr11 + cr21 * cr21);
            float csz = (float)Math.Sqrt(cr02 * cr02 + cr12 * cr12 + cr22 * cr22);

            float invCsx = 1f / csx, invCsy = 1f / csy, invCsz = 1f / csz;

            // Relative scale
            scale.x = csx * invPsx;
            scale.y = csy * invPsy;
            scale.z = csz * invPsz;

            // Normalize child rotation matrix
            float crn00 = cr00 * invCsx, crn01 = cr01 * invCsy, crn02 = cr02 * invCsz;
            float crn10 = cr10 * invCsx, crn11 = cr11 * invCsy, crn12 = cr12 * invCsz;
            float crn20 = cr20 * invCsx, crn21 = cr21 * invCsy, crn22 = cr22 * invCsz;

            // Transpose of parent rotation matrix
            float ir00 = r00, ir01 = r10, ir02 = r20;
            float ir10 = r01, ir11 = r11, ir12 = r21;
            float ir20 = r02, ir21 = r12, ir22 = r22;

            // localRotationMatrix = invParentRot * childRotation
            float lr00 = ir00 * crn00 + ir01 * crn10 + ir02 * crn20;
            float lr01 = ir00 * crn01 + ir01 * crn11 + ir02 * crn21;
            float lr02 = ir00 * crn02 + ir01 * crn12 + ir02 * crn22;

            float lr10 = ir10 * crn00 + ir11 * crn10 + ir12 * crn20;
            float lr11 = ir10 * crn01 + ir11 * crn11 + ir12 * crn21;
            float lr12 = ir10 * crn02 + ir11 * crn12 + ir12 * crn22;

            float lr20 = ir20 * crn00 + ir21 * crn10 + ir22 * crn20;
            float lr21 = ir20 * crn01 + ir21 * crn11 + ir22 * crn21;
            float lr22 = ir20 * crn02 + ir21 * crn12 + ir22 * crn22;

            // Convert rotation matrix to quaternion
            float trace = lr00 + lr11 + lr22;
            float qw, qx, qy, qz;

            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 0.5f;
                float invS = 0.25f / s;
                qw = s;
                qx = (lr21 - lr12) * invS;
                qy = (lr02 - lr20) * invS;
                qz = (lr10 - lr01) * invS;
            }
            else if (lr00 > lr11 && lr00 > lr22)
            {
                float s = (float)Math.Sqrt(1f + lr00 - lr11 - lr22) * 0.5f;
                float invS = 0.25f / s;
                qw = (lr21 - lr12) * invS;
                qx = s;
                qy = (lr01 + lr10) * invS;
                qz = (lr02 + lr20) * invS;
            }
            else if (lr11 > lr22)
            {
                float s = (float)Math.Sqrt(1f + lr11 - lr00 - lr22) * 0.5f;
                float invS = 0.25f / s;
                qw = (lr02 - lr20) * invS;
                qx = (lr01 + lr10) * invS;
                qy = s;
                qz = (lr12 + lr21) * invS;
            }
            else
            {
                float s = (float)Math.Sqrt(1f + lr22 - lr00 - lr11) * 0.5f;
                float invS = 0.25f / s;
                qw = (lr10 - lr01) * invS;
                qx = (lr02 + lr20) * invS;
                qy = (lr12 + lr21) * invS;
                qz = s;
            }

            rotation.x = qx;
            rotation.y = qy;
            rotation.z = qz;
            rotation.w = qw;

            // local position = inverseParentRotation * (childPos - parentPos) / parentScale
            float dx = childLTW.m03 - parentLTW.m03;
            float dy = childLTW.m13 - parentLTW.m13;
            float dz = childLTW.m23 - parentLTW.m23;

            position.x = (ir00 * dx + ir01 * dy + ir02 * dz) * invPsx;
            position.y = (ir10 * dx + ir11 * dy + ir12 * dz) * invPsy;
            position.z = (ir20 * dx + ir21 * dy + ir22 * dz) * invPsz;
        }

        public bool Equals(GPUITransformData other)
        {
            return position == other.position && rotation == other.rotation && scale == other.scale;
        }

        public override bool Equals(object obj)
        {
            if (obj is GPUITransformData crowdTransformData)
                return Equals(crowdTransformData);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return GPUIUtility.GenerateHash(position.GetHashCode(), rotation.GetHashCode(), scale.GetHashCode());
        }

        public static bool operator ==(GPUITransformData left, GPUITransformData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GPUITransformData left, GPUITransformData right)
        {
            return !(left == right);
        }

        private static readonly string TO_STRING_TEXT = "Position: {0}, Rotation: {1}, Scale: {2}";
        public override string ToString()
        {
            return string.Format(TO_STRING_TEXT, position.ToString("F4"), rotation.ToString("F4"), scale.ToString("F4"));
        }
    };
}