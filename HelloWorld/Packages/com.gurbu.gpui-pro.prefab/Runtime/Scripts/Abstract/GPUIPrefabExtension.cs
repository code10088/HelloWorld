// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    public abstract class GPUIPrefabExtension : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public GPUIPrefab gpuiPrefab;

        public Transform CachedTransform => gpuiPrefab.CachedTransform;

        protected virtual void Start()
        {
            if (gpuiPrefab == null)
                gpuiPrefab = GetComponent<GPUIPrefab>();
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            gpuiPrefab = GetComponent<GPUIPrefab>();
        }
#endif
    }
}