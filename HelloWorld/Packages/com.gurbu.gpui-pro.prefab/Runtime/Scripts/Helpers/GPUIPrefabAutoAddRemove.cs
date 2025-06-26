// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    /// <summary>
    /// This component is automatically attached to prefabs that are used as GPUI prototypes to identify them
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(GPUIPrefab))]
    public class GPUIPrefabAutoAddRemove : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public GPUIPrefab gpuiPrefab;

        private void Reset()
        {
            gpuiPrefab = GetComponent<GPUIPrefab>();
        }

        protected void OnEnable()
        {
            if (gpuiPrefab == null)
                gpuiPrefab = GetComponent<GPUIPrefab>();
            GPUIPrefabManager.AddPrefabInstance(gpuiPrefab);
        }

        protected void OnDisable()
        {
            if (gpuiPrefab != null)
                gpuiPrefab.RemovePrefabInstance();
        }
    }
}