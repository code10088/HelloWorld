// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace GPUInstancerPro.PrefabModule
{
    /// <summary>
    /// This component is automatically attached to prefabs that are used as GPUI prototypes to identify them
    /// </summary>
    [DefaultExecutionOrder(200)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Prefab")]
    public class GPUIPrefab : GPUIPrefabBase
    {
        [SerializeField]
        private bool _isRenderersDisabled;
        /// <summary>
        /// True when Mesh Renderer components are disabled
        /// </summary>
        public bool IsRenderersDisabled => _isRenderersDisabled;

        /// <summary>
        /// Prefab Manager that is currently rendering this instance
        /// </summary>
        public GPUIPrefabManager registeredManager { get; internal set; }

        internal bool _isBeingAddedToThePrefabManager;

        internal void SetInstancingData(GPUIPrefabManager registeredManager, int prefabID, int renderKey, int bufferIndex)
        {
            //Debug.Assert(_prefabID == 0 || _prefabID == prefabID, "Prefab ID mismatch. Current ID: " + _prefabID + " Given ID: " + prefabID, gameObject);
            this.registeredManager = registeredManager;
            this.renderKey = renderKey;
            this.bufferIndex = bufferIndex;
            if (_prefabID == 0)
                _prefabID = prefabID;
            registeredManager.SetPrefabInstanceRenderersEnabled(this, false);
            _isBeingAddedToThePrefabManager = false;
            OnInstancingStatusModified?.Invoke();
        }

        internal void ClearInstancingData(bool enableRenderers)
        {
            if (enableRenderers && registeredManager != null)
                registeredManager.SetPrefabInstanceRenderersEnabled(this, true);
            registeredManager = null;
            renderKey = 0;
            bufferIndex = -1;
            _isBeingAddedToThePrefabManager = false;
            OnInstancingStatusModified?.Invoke();
        }

        public void RemovePrefabInstance()
        {
            if (!IsInstanced) return;
            if (!registeredManager.RemovePrefabInstance(this))
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not remove prefab instance with prefab ID: " + GetPrefabID(), this);
        }

        internal void UpdateTransformData()
        {
            if (!IsInstanced) return;
            if (CachedTransform.hasChanged)
                registeredManager.UpdateTransformData(this);
        }

        public void SetRenderersEnabled(bool enabled)
        {
            if (_isRenderersDisabled != enabled)
                return;
            Profiler.BeginSample("GPUIPrefabManager.SetRenderersEnabled");

            GPUIRenderingSystem.prefabRendererList.Clear();
            transform.GetPrefabRenderers(GPUIRenderingSystem.prefabRendererList);
            foreach (Renderer renderer in GPUIRenderingSystem.prefabRendererList)
                renderer.enabled = enabled;

            if (TryGetComponent(out LODGroup lodGroup))
                lodGroup.enabled = enabled;
            _isRenderersDisabled = !enabled;
            Profiler.EndSample();
        }

        internal void SetBufferIndex(int bufferIndex)
        {
            int previousBufferIndex = this.bufferIndex;
            if (previousBufferIndex != bufferIndex)
            {
                this.bufferIndex = bufferIndex;
                OnBufferIndexModified?.Invoke(previousBufferIndex);
            }
        }

        protected override void OnOptionalRendererStatusChanged()
        {
            if (!IsInstanced) return;
            var prototypeData = registeredManager.GetPrototypeDataWithRenderKey(renderKey);
            if (prototypeData == null) return;
            prototypeData.optionalRendererStatusData[bufferIndex] = optionalRendererStatus;
            prototypeData.isOptionalRendererStatusModified = true;
        }
    }
}