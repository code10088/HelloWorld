// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Area_Culler")]
    [DefaultExecutionOrder(1200)]
    [ExecuteInEditMode]
    public class GPUIAreaCuller : MonoBehaviour
    {
        [SerializeField]
        public bool useColliders = true;
        [SerializeField] 
        public Bounds bounds;
        [SerializeField]
        public List<GPUIManager> gpuiManagerFilter;
        [SerializeField]
        public List<int> prototypeIndexFilter;
        [SerializeField]
        public float offset;

        [NonSerialized]
        private Collider[] _colliders;

        private void OnEnable()
        {
            if (useColliders)
            {
                _colliders = GetComponents<Collider>();
                if (Application.isPlaying && (_colliders == null || _colliders.Length == 0))
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "GPUI Area Culler can not find any colliders on its GameObject.", gameObject);
                    enabled = false;
                    return;
                }
            }
            GPUIRenderingSystem.OnBufferDataModified -= CullInstances;
            GPUIRenderingSystem.OnBufferDataModified += CullInstances;
            OnValuesChanged();
        }

        private void OnDisable()
        {
            GPUIRenderingSystem.OnBufferDataModified -= CullInstances;
            OnValuesChanged();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                OnValuesChanged();
            else
#endif
            if (transform.hasChanged)
                OnValuesChanged();
        }

        public void OnValuesChanged()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && useColliders)
                _colliders = GetComponents<Collider>();
#endif
            transform.hasChanged = false;
            GPUITransformBufferUtility.ResetAllCulledInstances();
        }

        private void CullInstances(GPUITransformBufferData transformBufferData)
        {
            if (useColliders && (_colliders == null || _colliders.Length == 0))
                return;

            int bufferStartIndex = 0;
            int bufferSize = transformBufferData.RenderSourceGroup.BufferSize;

            bool hasPrototypeIndexFilter = prototypeIndexFilter != null && prototypeIndexFilter.Count > 0;

            if (gpuiManagerFilter != null && gpuiManagerFilter.Count > 0)
            {
                for (int m = 0; m < gpuiManagerFilter.Count; m++)
                {
                    GPUIManager manager = gpuiManagerFilter[m];
                    if (manager == null)
                        continue;
                    int prototypeCount = manager.GetPrototypeCount();
                    for (int p = 0; p < prototypeCount; p++)
                    {
                        if (hasPrototypeIndexFilter && !prototypeIndexFilter.Contains(p))
                            continue;
                        if (!GPUIRenderingSystem.TryGetTransformBufferData(manager.GetRenderKey(p), out GPUITransformBufferData managerBufferData, out bufferStartIndex, out bufferSize) || managerBufferData != transformBufferData)
                            continue;

                        if (useColliders)
                        {
                            foreach (var collider in _colliders)
                                GPUITransformBufferUtility.CullInstancesInsideCollider(transformBufferData, bufferStartIndex, bufferSize, collider, offset);
                        }
                        else
                        {
                            Bounds positionedBounds = bounds;
                            positionedBounds.center += transform.position;
                            GPUITransformBufferUtility.CullInstancesInsideBounds(transformBufferData, bufferStartIndex, bufferSize, positionedBounds, offset);
                        }
                    }

                }
                return;
            }

            if (useColliders)
            {
                foreach (var collider in _colliders)
                    GPUITransformBufferUtility.CullInstancesInsideCollider(transformBufferData, bufferStartIndex, bufferSize, collider, offset);
            }
            else
            {
                Bounds positionedBounds = bounds;
                positionedBounds.center += transform.position;
                GPUITransformBufferUtility.CullInstancesInsideBounds(transformBufferData, bufferStartIndex, bufferSize, positionedBounds, offset);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnValuesChanged();
        }

        private void OnDrawGizmosSelected()
        {
            if (useColliders)
                return;
            Color gizmoDefaultColor = Gizmos.color;

            Gizmos.color = Color.blue;
            Bounds positionedBounds = bounds;
            positionedBounds.center += transform.position;
            Gizmos.DrawWireCube(positionedBounds.center, positionedBounds.size);

            Gizmos.color = gizmoDefaultColor;
        }
#endif
    }
}