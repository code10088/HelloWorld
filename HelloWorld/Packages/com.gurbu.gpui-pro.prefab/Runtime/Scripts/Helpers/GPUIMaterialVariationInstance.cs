// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(2000)]
    [RequireComponent(typeof(GPUIPrefab))]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:BestPractices#Prefab_Manager_Material_Variations")]
    public class GPUIMaterialVariationInstance : MonoBehaviour
    {
        [SerializeField]
        public GPUIMaterialVariationDefinition variationDefinition;
        [SerializeField]
        public Vector4[] values;

        [NonSerialized]
        private GPUIPrefab _gpuiPrefab;
        [NonSerialized]
        private List<Renderer> _variationRenderers;
        [NonSerialized]
        private MaterialPropertyBlock _variationMPB;

        private void OnEnable()
        {
            if (variationDefinition == null)
                return;

            if (_gpuiPrefab == null)
                _gpuiPrefab = GetComponent<GPUIPrefab>();

            ApplyVariation();

            _gpuiPrefab.OnInstancingStatusModified -= ApplyVariation;
            _gpuiPrefab.OnInstancingStatusModified += ApplyVariation;
        }

        private void OnDisable()
        {
            if (_gpuiPrefab != null)
                _gpuiPrefab.OnInstancingStatusModified -= ApplyVariation;
            RevertVariation();
        }

        private void LoadVariationRenderers()
        {
            if (_variationRenderers == null)
                _variationRenderers = new List<Renderer>();
            else
                _variationRenderers.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == variationDefinition.material)
                        _variationRenderers.Add(r);
                }
            }
        }

        private void CreateVariationMPB()
        {
            _variationMPB = new MaterialPropertyBlock();
            // Might not look exactly the same because we are overwriting the existing MaterialPropertyBlock. Exact look can be created by keeping the original MaterialPropertyBlock, however it will be much slower and memory intensive.
            //if (_variationRenderers == null)
            //    LoadVariationRenderers();
            //_variationRenderers[0].GetPropertyBlock(_variationMPB);
            SetMPBValues();
        }

        private void SetMPBValues()
        {
            for (int i = 0; i < variationDefinition.items.Length; i++)
            {
                switch (variationDefinition.items[i].variationType)
                {
                    case GPUIMaterialVariationType.Vector4:
                        _variationMPB.SetVector(variationDefinition.items[i].propertyName, GetVariation(i));
                        break;
                    case GPUIMaterialVariationType.Color:
                        _variationMPB.SetColor(variationDefinition.items[i].propertyName, GetVariation(i));
                        break;
                    case GPUIMaterialVariationType.Integer:
                        _variationMPB.SetInt(variationDefinition.items[i].propertyName, (int)GetVariation(i).x);
                        break;
                    case GPUIMaterialVariationType.Float:
                        _variationMPB.SetFloat(variationDefinition.items[i].propertyName, GetVariation(i).x);
                        break;
                }
            }
        }

        public Vector4 GetVariation(int index)
        {
            if (values != null && values.Length > index)
                return values[index];
            return variationDefinition.items[index].defaultValue;
        }

        public void ApplyVariation()
        {
            if (!enabled || variationDefinition == null || variationDefinition.items == null)
                return;

            if (_gpuiPrefab == null)
                _gpuiPrefab = GetComponent<GPUIPrefab>();

            if (_gpuiPrefab.IsInstanced)
                SetVariationBufferData();
            else if (!_gpuiPrefab.IsRenderersDisabled)
                ApplyVariationMPB();
        }

        public void SetVariation(int index, Vector4 variationValue)
        {
            if (values == null)
                values = new Vector4[index + 1];
            if (values.Length <= index)
                Array.Resize(ref values, index + 1);
            values[index] = variationValue;
            ApplyVariation();
        }

        private void RevertVariation()
        {
            if (_gpuiPrefab == null)
                return;

            if (!_gpuiPrefab.IsInstanced)
            {
                if (_variationRenderers == null)
                    LoadVariationRenderers();
                foreach (Renderer r in _variationRenderers)
                    r.SetPropertyBlock(GPUIRenderingSystem.EmptyMPB);
            }
        }

        private void SetVariationBufferData()
        {
            int variationCount = variationDefinition.items.Length;
            for (int i = 0; i < variationCount; i++)
            {
                if (variationDefinition.items[i].variationType == GPUIMaterialVariationType.Color && QualitySettings.activeColorSpace == ColorSpace.Linear)
                    variationDefinition.AddVariation(_gpuiPrefab.renderKey, _gpuiPrefab.bufferIndex * variationCount + i, ((Color)GetVariation(i)).linear);
                else
                    variationDefinition.AddVariation(_gpuiPrefab.renderKey, _gpuiPrefab.bufferIndex * variationCount + i, GetVariation(i));
            }
        }

        private void ApplyVariationMPB()
        {
            if (_variationMPB == null)
                CreateVariationMPB();
            else
                SetMPBValues();
            if (_variationRenderers == null)
                LoadVariationRenderers();
            foreach (Renderer r in _variationRenderers)
                r.SetPropertyBlock(_variationMPB);
        }
    }
}
