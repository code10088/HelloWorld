// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [RequireComponent (typeof (GPUIPrefabManager))]
    [DefaultExecutionOrder(-1000)]
    public class GPUIRuntimePrototypeDefiner : MonoBehaviour
    {
        public List<GameObject> prefabs;
        public bool enableTransformUpdates;

        private GPUIPrefabManager _prefabManager;

        private void OnEnable()
        {
            _prefabManager = GetComponent<GPUIPrefabManager>();
            if (prefabs == null)
                return;

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) 
                    continue;
                int prototypeIndex = _prefabManager.GetPrototypeIndex(prefab); // Check if the prefab is already added to the Prefab Manager.
                if (prototypeIndex < 0) // Prefab is not added before.
                {
                    prototypeIndex = _prefabManager.AddPrototype(prefab); // Add the prefab to the Prefab Manager.
                    if (prototypeIndex < 0) // Adding prefab was not successful.
                    {
                        Debug.LogError(GPUIConstants.LOG_PREFIX + "Add Prototype operation failed for prefab: " + prefab, prefab);
                        continue;
                    }
                }
                _prefabManager.GetPrototypeData(prototypeIndex).isAutoUpdateTransformData = enableTransformUpdates;
            }
        }
    }
}
