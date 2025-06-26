// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    /// <summary>
    /// Key => Variation Definition Instance ID & RenderKey Hash
    /// Value => Variation Data
    /// </summary>
    public class GPUIMaterialVariationDataProvider : GPUIDataProvider<int, GPUIMaterialVariationData>
    {
        public static GPUIMaterialVariationDataProvider _instance;
        public static GPUIMaterialVariationDataProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GPUIMaterialVariationDataProvider();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        public override void Initialize()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Duplicate GPUIMaterialVariationDataProvider initialization.");
                return;
            }
            base.Initialize();
            GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.AddDependentDisposable(this);
        }

        public void UpdateVariationBuffers()
        {
            if (!IsInitialized)
                return;
            foreach (GPUIMaterialVariationData variationData in Values)
                variationData.UpdateVariationBuffer();
        }

        internal void UpdateVariationBuffers(GPUICameraData cameraData)
        {
            UpdateVariationBuffers();
        }

        public static GPUIMaterialVariationData GetMaterialVariationData(GPUIMaterialVariationDefinition materialVariationDefinition, int renderKey)
        {
            return GetMaterialVariationDataWithHash(materialVariationDefinition, GPUIUtility.GenerateHash(materialVariationDefinition.GetInstanceID(), renderKey));
        }

        public static GPUIMaterialVariationData GetMaterialVariationDataWithHash(GPUIMaterialVariationDefinition materialVariationDefinition, int hash)
        {
            if (!Instance.TryGetData(hash, out GPUIMaterialVariationData result))
            {
                result = new GPUIMaterialVariationData(materialVariationDefinition);
                result.Initialize();
                Instance.AddOrSet(hash, result);
            }

            return result;
        }
    }
}
