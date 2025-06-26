// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;

namespace GPUInstancerPro.PrefabModule
{
    [Unity.Burst.BurstCompile]
    internal unsafe struct GPUIAutoUpdateTransformsJob : IJobParallelForTransform
    {
        [ReadOnly] public int instanceCount;
        [ReadOnly] public Matrix4x4 zeroMatrix;
        [NativeDisableUnsafePtrRestriction] internal unsafe void* p_matrixArray;
        [NativeDisableUnsafePtrRestriction] internal unsafe void* p_isModifiedArray;

        public void Execute(int index, TransformAccess transform)
        {
            if (index >= instanceCount)
                return;

            Matrix4x4 transformMatrix = UnsafeUtility.ReadArrayElementWithStride<Matrix4x4>(p_matrixArray, index, 64);
            if (transform.isValid)
            {
                Matrix4x4 m = transform.localToWorldMatrix;
                if (!GPUIUtility.EqualsMatrix4x4(m, transformMatrix))
                {
                    UnsafeUtility.WriteArrayElementWithStride(p_isModifiedArray, index, 4, 1);
                    UnsafeUtility.WriteArrayElementWithStride(p_matrixArray, index, 64, m);
                }
                else
                    UnsafeUtility.WriteArrayElementWithStride(p_isModifiedArray, index, 4, 0);
            }
            else if (!GPUIUtility.EqualsMatrix4x4(zeroMatrix, transformMatrix))
            {
                UnsafeUtility.WriteArrayElementWithStride(p_isModifiedArray, index, 4, 1);
                UnsafeUtility.WriteArrayElementWithStride(p_matrixArray, index, 64, zeroMatrix);
            }
            else
                UnsafeUtility.WriteArrayElementWithStride(p_isModifiedArray, index, 4, 0);
        }
    }
}