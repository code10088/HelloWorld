// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    public class GPUIDetailDensityModifier : MonoBehaviour
    {
        public GPUIDetailManager detailManager;
        public List<Collider> selectedColliders;
        public bool useBounds;
        public Vector3 boundsSize;
        public bool applyEveryUpdate = false;
        public float offset = 0;
        public List<int> selectedPrototypeIndexes;
        [Range(0f, 255f)]
        public float densityValue;

        private Bounds _bounds;
        private bool _isExecuted;

        private void OnEnable()
        {
            SetDetailManager();
            ModifyDetailDensity();
            _bounds = new Bounds(transform.position, boundsSize);
        }

        private void Update()
        {
            if (applyEveryUpdate)
                ModifyDetailDensity();
            else
            {
                if (_isExecuted)
                    enabled = false;
                else
                    ModifyDetailDensity();
            }
            
        }

#if UNITY_EDITOR
        private void Reset()
        {
            SetDetailManager();
        }
#endif

        private void SetDetailManager()
        {
            if (detailManager == null)
                detailManager = FindAnyObjectByType<GPUIDetailManager>();
        }

        private void ModifyDetailDensity()
        {
            if (detailManager != null && detailManager.IsInitialized)
            {
                if (useBounds)
                {
                    _bounds.center = transform.position;
                    GPUITerrainUtility.SetDetailDensityInsideBounds(detailManager, densityValue, _bounds, offset, selectedPrototypeIndexes);
                }
                else if (selectedColliders != null)
                {
                    foreach (var collider in selectedColliders)
                    {
                        if (collider != null)
                            GPUITerrainUtility.SetDetailDensityInsideCollider(detailManager, collider, densityValue, offset, selectedPrototypeIndexes);
                    }
                }
                _isExecuted = true;
            }
        }
    }
}
