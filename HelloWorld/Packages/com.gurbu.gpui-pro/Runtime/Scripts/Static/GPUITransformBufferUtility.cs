// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public static class GPUITransformBufferUtility
    {
        #region CullInstances

        #region CullInstancesInsideBounds
        public static void CullInstancesInsideBounds(Bounds bounds, float offset = 0)
        {
            foreach (var renderSourceGroup in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                if (renderSourceGroup.TransformBufferData == null)
                    continue;
                CullInstancesInsideBounds(renderSourceGroup.TransformBufferData, 0, renderSourceGroup.BufferSize, bounds, offset);
            }
        }

        public static void CullInstancesInsideBounds(GPUIManager gpuiManager, Bounds bounds, List<int> prototypeIndexFilter = null, float offset = 0)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                CullInstancesInsideBounds(gpuiManager.GetRenderKey(i), bounds, offset);
            }
        }

        public static void CullInstancesInsideBounds(int renderKey, Bounds bounds, float offset = 0)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                CullInstancesInsideBounds(transformBufferData, bufferStartIndex, bufferSize, bounds, offset);
        }

        public static void CullInstancesInsideBounds(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, Bounds bounds, float offset = 0)
        {
            if (bufferSize == 0)
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents + Vector3.one * offset;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 2;
            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                cs.SetVector(GPUIConstants.PROP_boundsExtents, extents);
                cs.DispatchX(kernelIndex, bufferSize);
            }
        }
        #endregion CullInstancesInsideBounds

        #region CullInstancesInsideCollider
        public static void CullInstancesInsideCollider(Collider collider, float offset = 0)
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            foreach (var renderSourceGroup in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                if (renderSourceGroup.TransformBufferData == null)
                    continue;
                CullInstancesInsideCollider(renderSourceGroup.TransformBufferData, 0, renderSourceGroup.BufferSize, collider, offset);
            }
        }

        public static void CullInstancesInsideCollider(GPUIManager gpuiManager, Collider collider, List<int> prototypeIndexFilter = null, float offset = 0)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                CullInstancesInsideCollider(gpuiManager.GetRenderKey(i), collider, offset);
            }
        }

        public static void CullInstancesInsideCollider(int renderKey, Collider collider, float offset = 0)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                CullInstancesInsideCollider(transformBufferData, bufferStartIndex, bufferSize, collider, offset);
        }

        public static void CullInstancesInsideCollider(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, Collider collider, float offset = 0)
        {
            if (collider == null || bufferSize == 0)
                return;
            if (collider is BoxCollider boxCollider)
                CullInstancesInsideBoxCollider(transformBufferData, bufferStartIndex, bufferSize, boxCollider, offset);
            else if (collider is SphereCollider sphereCollider)
                CullInstancesInsideSphereCollider(transformBufferData, bufferStartIndex, bufferSize, sphereCollider, offset);
            else if (collider is CapsuleCollider capsuleCollider)
                CullInstancesInsideCapsuleCollider(transformBufferData, bufferStartIndex, bufferSize, capsuleCollider, offset);
            else
                CullInstancesInsideBounds(transformBufferData, bufferStartIndex, bufferSize, collider.bounds, offset);
        }
        #endregion CullInstancesInsideCollider

        #region CullInstancesInsideBoxCollider
        public static void CullInstancesInsideBoxCollider(GPUIManager gpuiManager, BoxCollider boxCollider, List<int> prototypeIndexFilter = null, float offset = 0)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                CullInstancesInsideBoxCollider(gpuiManager.GetRenderKey(i), boxCollider, offset);
            }
        }

        public static void CullInstancesInsideBoxCollider(int renderKey, BoxCollider boxCollider, float offset = 0)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                CullInstancesInsideBoxCollider(transformBufferData, bufferStartIndex, bufferSize, boxCollider, offset);
        }

        public static void CullInstancesInsideBoxCollider(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, BoxCollider boxCollider, float offset = 0)
        {
            if (bufferSize == 0)
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;
            Vector3 center = boxCollider.center;
            Vector3 extents = boxCollider.size / 2 + Vector3.one * offset;
            Matrix4x4 modifierTransform = boxCollider.transform.localToWorldMatrix;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 3;
            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                cs.SetVector(GPUIConstants.PROP_boundsExtents, extents);
                cs.SetMatrix(GPUIConstants.PROP_modifierTransform, modifierTransform);
                cs.DispatchX(kernelIndex, bufferSize);
            }
        }
        #endregion CullInstancesInsideBoxCollider

        #region CullInstancesInsideSphereCollider
        public static void CullInstancesInsideSphereCollider(GPUIManager gpuiManager, SphereCollider sphereCollider, List<int> prototypeIndexFilter = null, float offset = 0)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                CullInstancesInsideSphereCollider(gpuiManager.GetRenderKey(i), sphereCollider, offset);
            }
        }

        public static void CullInstancesInsideSphereCollider(int renderKey, SphereCollider sphereCollider, float offset = 0)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                CullInstancesInsideSphereCollider(transformBufferData, bufferStartIndex, bufferSize, sphereCollider, offset);
        }

        public static void CullInstancesInsideSphereCollider(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, SphereCollider sphereCollider, float offset = 0)
        {
            if (bufferSize == 0)
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;

            Vector3 center = sphereCollider.center + sphereCollider.transform.position;
            Vector3 scale = sphereCollider.transform.localScale;
            float radius = sphereCollider.radius * Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z) + offset;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 4;

            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
                cs.DispatchX(kernelIndex, bufferSize);
            }
        }
        #endregion CullInstancesInsideSphereCollider

        #region CullInstancesInsideCapsuleCollider
        public static void CullInstancesInsideCapsuleCollider(GPUIManager gpuiManager, CapsuleCollider capsuleCollider, List<int> prototypeIndexFilter = null, float offset = 0)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                CullInstancesInsideCapsuleCollider(gpuiManager.GetRenderKey(i), capsuleCollider, offset);
            }
        }

        public static void CullInstancesInsideCapsuleCollider(int renderKey, CapsuleCollider capsuleCollider, float offset = 0)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                CullInstancesInsideCapsuleCollider(transformBufferData, bufferStartIndex, bufferSize, capsuleCollider, offset);
        }

        public static void CullInstancesInsideCapsuleCollider(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, CapsuleCollider capsuleCollider, float offset = 0)
        {
            if (bufferSize == 0)
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;
            Vector3 center = capsuleCollider.center;
            Vector3 scale = capsuleCollider.transform.localScale;
            float radius = capsuleCollider.radius * Mathf.Max(Mathf.Max(
                capsuleCollider.direction == 0 ? 0 : scale.x,
                capsuleCollider.direction == 1 ? 0 : scale.y),
                capsuleCollider.direction == 2 ? 0 : scale.z) + offset;
            float height = capsuleCollider.height * (
                    capsuleCollider.direction == 0 ? scale.x : 0 +
                    capsuleCollider.direction == 1 ? scale.y : 0 +
                    capsuleCollider.direction == 2 ? scale.z : 0);

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 5;

            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
                cs.SetFloat(GPUIConstants.PROP_modifierHeight, height);
                cs.DispatchX(kernelIndex, bufferSize);
            }
        }
        #endregion CullInstancesInsideCapsuleCollider

        #region ResetCulledInstances
        public static void ResetAllCulledInstances()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            foreach (var renderSourceGroup in GPUIRenderingSystem.Instance.RenderSourceGroupProvider.Values)
            {
                if (renderSourceGroup.TransformBufferData == null)
                    continue;
                ResetCulledInstances(renderSourceGroup.TransformBufferData, 0, renderSourceGroup.BufferSize);
            }
        }

        public static void ResetCulledInstances(GPUIManager gpuiManager, List<int> prototypeIndexFilter = null)
        {
            int count = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < count; i++)
            {
                if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                    continue;

                ResetCulledInstances(gpuiManager.GetRenderKey(i));
            }
        }

        public static void ResetCulledInstances(int renderKey)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                ResetCulledInstances(transformBufferData, bufferStartIndex, bufferSize);
        }

        public static void ResetCulledInstances(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize)
        {
            if (bufferSize == 0)
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            int kernelIndex = 9;

            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.DispatchX(kernelIndex, bufferSize);
            }

            transformBufferData.OnTransformDataModified();
        }
        #endregion ResetCulledInstances

        #endregion CullInstances

        #region ApplyMatrixOffset
        public static void ApplyMatrixOffsetToTransforms(GPUIManager gpuiManager, Matrix4x4 matrixOffset)
        {
            int prototypeCount = gpuiManager.GetPrototypeCount();
            for (int i = 0; i < prototypeCount; i++)
                ApplyMatrixOffsetToTransforms(gpuiManager.GetRenderKey(i), matrixOffset);
        }

        public static void ApplyMatrixOffsetToTransforms(int renderKey, Matrix4x4 matrixOffset)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(renderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, false))
                ApplyMatrixOffsetToTransforms(transformBufferData, bufferStartIndex, bufferSize, matrixOffset);
        }

        public static void ApplyMatrixOffsetToTransforms(GPUITransformBufferData transformBufferData, int bufferStartIndex, int bufferSize, Matrix4x4 matrixOffset)
        {
            if (bufferSize == 0) 
                return;
            var transformBufferValues = transformBufferData.TransformBufferValues;
            if (transformBufferValues == null)
                return;

            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            foreach (var shaderBuffer in transformBufferValues)
            {
                if (shaderBuffer == null || shaderBuffer.Buffer == null) continue;

                cs.SetBuffer(1, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_startIndex, bufferStartIndex);
                cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                cs.SetMatrix(GPUIConstants.PROP_matrix44, matrixOffset);
                cs.DispatchX(1, bufferSize);
            }
            transformBufferData.OnTransformDataModified();
        }
        #endregion ApplyMatrixOffset
    }
}