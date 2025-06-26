// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUILODGroupDataProvider : GPUIDataProvider<int, GPUILODGroupData>
    {
        /// <summary>
        /// List of runtime generated GPUILODGroupData
        /// </summary>
        private List<GPUILODGroupData> _generatedLODGroups;

        public override void Initialize()
        {
            base.Initialize();
            if (_generatedLODGroups == null)
                _generatedLODGroups = new();
        }

        public override void Dispose()
        {
            base.Dispose();
            DestroyGeneratedLODGroups();
        }

        public void DestroyGeneratedLODGroups()
        {
            if (_generatedLODGroups != null)
            {
                foreach (GPUILODGroupData lgd in _generatedLODGroups)
                {
                    lgd.Dispose();
                    lgd.DestroyGeneric();
                }
                _generatedLODGroups.Clear();
            }
        }

        public void RegenerateLODGroups()
        {
            if (IsInitialized)
            {
                foreach (var keyValue in _dataDict)
                {
                    if (keyValue.Value != null && keyValue.Value.prototype != null)
                        keyValue.Value.CreateRenderersFromPrototype(keyValue.Value.prototype);
                }
                GPUIRenderingSystem.Instance.UpdateCommandBuffers(true);
            }
        }

        public void RecalculateLODGroupBounds()
        {
            if (IsInitialized)
            {
                foreach (var keyValue in _dataDict)
                {
                    if (keyValue.Value != null && keyValue.Value.prototype != null)
                        keyValue.Value.CalculateBounds();
                }
            }
        }

        public void RegenerateLODGroupData(GPUIPrototype prototype)
        {
            if (IsInitialized)
            {
                GPUILODGroupData lodGroupData = GetOrCreateLODGroupData(prototype);
                if (lodGroupData != null)
                {
                    lodGroupData.CreateRenderersFromPrototype(prototype);
                    lodGroupData.SetParameterBufferData();
                    GPUIRenderingSystem.Instance.UpdateCommandBuffers();
                }
            }
        }

        public GPUILODGroupData GetOrCreateLODGroupData(GPUIPrototype prototype)
        {
            if (!IsInitialized)
                Initialize();

            int key = prototype.GetKey();
            if (!TryGetData(key, out GPUILODGroupData lodGroupData) || lodGroupData == null)
            {
                if (prototype.prototypeType == GPUIPrototypeType.LODGroupData)
                    lodGroupData = prototype.gpuiLODGroupData;
                else
                {
                    lodGroupData = GPUILODGroupData.CreateLODGroupData(prototype);
                    _generatedLODGroups.Add(lodGroupData);
                }
                _dataDict[key] = lodGroupData;
            }
            return lodGroupData;
        }

        public GameObject FindPrefabObjectFromPrefabID(int prefabID)
        {
            if (!IsInitialized)
                return null;
            foreach (var lgd in _dataDict.Values)
            {
                if (lgd.prototype == null || lgd.prototype.prefabObject == null || !lgd.prototype.prefabObject.TryGetComponent(out GPUIPrefabBase prefabBase) || prefabBase.GetPrefabID() != prefabID)
                    continue;
                return lgd.prototype.prefabObject;
            }
            return null;
        }

        public void ClearNullValues()
        {
            if (!IsInitialized)
                return;

            for (int i = 0; i < Count; i++)
            {
                var kvPair = GetKVPairAtIndex(i);
                if (kvPair.Value == null)
                {
                    Remove(kvPair.Key);
                    i--;
                }
            }
        }
    }
}