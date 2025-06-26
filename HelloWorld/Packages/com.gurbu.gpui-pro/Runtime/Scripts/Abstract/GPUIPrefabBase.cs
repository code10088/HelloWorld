// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    [DisallowMultipleComponent]
    public abstract class GPUIPrefabDefinition : MonoBehaviour { }

    public abstract class GPUIPrefabBase : GPUIPrefabDefinition
    {
        /// <summary>
        /// Unique identifier to find instances of a prefab
        /// </summary>
        [SerializeField]
        protected int _prefabID;

        [SerializeField]
        public List<GPUIOptionalRenderer> childOptionalRenderers;

        /// <summary>
        /// Render key on the registered manager
        /// </summary>
        public int renderKey { get; protected set; }

        /// <summary>
        /// Buffer index of the instance
        /// </summary>
        public int bufferIndex { get; protected set; }

        public bool IsInstanced => renderKey != 0;

        protected Transform _cachedTransform;
        public Transform CachedTransform
        {
            get
            {
                if (_cachedTransform == null)
                    _cachedTransform = transform;
                return _cachedTransform;
            }
        }

        public UnityAction OnInstancingStatusModified;
        public Action<int> OnBufferIndexModified;

        public uint optionalRendererStatus { get; protected set; }

        internal int AddOptionalRenderer(GPUIOptionalRenderer optionalRenderer)
        {
            childOptionalRenderers ??= new();
            int result = childOptionalRenderers.IndexOf(optionalRenderer);
            if (result == -1)
            {
                result = childOptionalRenderers.Count;
                childOptionalRenderers.Add(optionalRenderer);
            }
            return result;
        }

        internal void SetOptionalRendererEnabled(GPUIOptionalRenderer optionalRenderer, bool enabled)
        {
            if (optionalRenderer.optionalRendererNo <= 0 || optionalRenderer.optionalRendererNo > 32)
                return;
            int index = optionalRenderer.optionalRendererNo - 1;
            if (enabled)
                optionalRendererStatus |= 1U << index; // Set bit at index to 1
            else
                optionalRendererStatus &= ~(1U << index); // Set bit at index to 0
            OnOptionalRendererStatusChanged();
        }

        protected virtual void OnOptionalRendererStatusChanged() { }

        public int GetPrefabID()
        {
#if UNITY_EDITOR
            if (_prefabID == 0 && !Application.isPlaying)
            {
                if (GPUIPrefabUtility.IsPrefabAsset(gameObject, out GameObject prefabObject, false))
                {
                    if (gameObject == prefabObject)
                    {
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefabObject.GetInstanceID(), out string guid, out long localId))
                        {
                            Undo.RecordObject(this, "Set Prefab ID");
                            _prefabID = guid.GetHashCode();
#if GPUIPRO_DEVMODE
                            Debug.Log(GPUIConstants.LOG_PREFIX + name + " prefab ID set to: " + _prefabID, gameObject);
#endif
                            EditorUtility.SetDirty(gameObject);
                            GPUIPrefabUtility.SavePrefabAsset(prefabObject);
                            GPUIPrefabUtility.MergeAllPrefabInstances(gameObject);
                        }
                    }
                    else
                        PrefabUtility.RevertPrefabInstance(gameObject, InteractionMode.AutomatedAction);
                }
            }
#endif
            return _prefabID;
        }

#if UNITY_EDITOR
        public virtual void Reset()
        {
            Undo.RecordObject(this, "Reset GPUIPrefab");
            childOptionalRenderers ??= new();
            for (int i = 0; i < childOptionalRenderers.Count; i++)
            {
                if (childOptionalRenderers[i] == null)
                {
                    childOptionalRenderers.RemoveAt(i);
                    i--;
                }
            }
            GPUIOptionalRenderer[] optionalRenderers = GetComponentsInChildren<GPUIOptionalRenderer>();
            foreach (var optionalRenderer in optionalRenderers)
                optionalRenderer.Reset();
#if GPUIPRO_DEVMODE
            Debug.Log(GPUIConstants.LOG_PREFIX + name + " prefab ID reset.");
#endif
            _prefabID = 0;
            GetPrefabID();
            EditorUtility.SetDirty(gameObject);
        }
#endif
    }

    [DisallowMultipleComponent]
    public abstract class GPUISkinningBase : MonoBehaviour { }
}
