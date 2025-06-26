// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancerPro
{
    [DefaultExecutionOrder(-310)]
    [DisallowMultipleComponent]
    public class GPUIOptionalRenderer : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public GPUIPrefabBase prefabBase;
        /// <summary>
        /// GPUIPrefabBase.GetOptionalRendererIndex(this) + 1
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public int optionalRendererNo;

        private void OnEnable()
        {
            prefabBase?.SetOptionalRendererEnabled(this, true);
        }
        private void OnDisable()
        {
            prefabBase?.SetOptionalRendererEnabled(this, false);
        }

#if UNITY_EDITOR
        public void Reset()
        {
            if (prefabBase == null)
            {
                var foundPrefabBase = gameObject.GetComponentInParent<GPUIPrefabBase>(true);
                if (foundPrefabBase != null)
                {
                    Undo.RecordObject(this, "GPUIOptionalRenderer set prefabBase");
                    prefabBase = foundPrefabBase;
                    optionalRendererNo = prefabBase.AddOptionalRenderer(this) + 1;
                }
            }
            else
            {
                int rendererNo = prefabBase.AddOptionalRenderer(this) + 1;
                if (optionalRendererNo != rendererNo)
                {
                    Undo.RecordObject(this, "GPUIOptionalRenderer set optionalRendererNo");
                    optionalRendererNo = rendererNo;
                }
            }
        }
#endif
    }
}
