// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    [ExecuteInEditMode]
    public class GPUISphericalVegetationDrawer : MonoBehaviour
    {
        [SerializeField]
        public Transform sphereTransform;
        [SerializeField]
        public float scaleMultiplier = 1.0f;
        [SerializeField]
        public int seed;
        [SerializeField]
        public List<VegetationSetting> vegetationSettings;

        [NonSerialized]
        private int[] _renderKeys;
        [NonSerialized]
        private Matrix4x4 _previousMatrix;
        [NonSerialized]
        private bool _isInitialized;

        [Serializable]
        public struct VegetationSetting
        {
            public GameObject prefab;
            public GPUIProfile profile;
            [Range(0, 10000)]
            public int spawnAmount;
            public Vector2 minMaxScale;
            [NonSerialized]
            public Matrix4x4[] matrices;
        }

        private void OnEnable()
        {
            if (vegetationSettings == null  || vegetationSettings.Count == 0 || sphereTransform == null)
                return;
            RegisterRenderers();
        }

        private void OnDisable()
        {
            DisposeRenderers();
        }

        private void OnValidate()
        {
            if (GPUIRenderingSystem.IsActive && _isInitialized)
                RegisterRenderers();
        }

        public void RegisterRenderers()
        {
            DisposeRenderers();
            CreateMatrices();
            _renderKeys = new int[vegetationSettings.Count];

            for (int i = 0; i < vegetationSettings.Count; i++)
            {
                if (vegetationSettings[i].matrices == null) continue;
                if (GPUICoreAPI.RegisterRenderer(this, vegetationSettings[i].prefab, vegetationSettings[i].profile, out _renderKeys[i]))
                    GPUICoreAPI.SetTransformBufferData(_renderKeys[i], vegetationSettings[i].matrices);
            }
            _previousMatrix.SetTRS(sphereTransform.position, Quaternion.identity, sphereTransform.lossyScale);
            GPUICoreAPI.AddCameraEventOnPreCull(HandleFloatingOrigin);
            _isInitialized = true;
        }

        public void DisposeRenderers()
        {
            _isInitialized = false;
            GPUICoreAPI.RemoveCameraEventOnPreCull(HandleFloatingOrigin);

            if (_renderKeys == null) return;
            for (int i = 0; i < _renderKeys.Length; i++)
            {
                if (_renderKeys[i] != 0)
                    GPUICoreAPI.DisposeRenderer(_renderKeys[i]);
            }
            _renderKeys = null;
        }

        public void CreateMatrices()
        {
            for (int i = 0; i < vegetationSettings.Count; i++)
            {
                if (seed != 0)
                    Random.InitState(seed + i);
                VegetationSetting vegetationSetting = vegetationSettings[i];

                Matrix4x4[] matrices = new Matrix4x4[vegetationSettings[i].spawnAmount];
                Vector3 spherePos = sphereTransform.position;
                Vector3 sphereScale = sphereTransform.localScale * scaleMultiplier;
                for (int m = 0; m < matrices.Length; m++)
                {
                    Vector3 pos = Vector3.Normalize(Random.insideUnitSphere) * sphereScale.x / 2f + spherePos;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, pos - spherePos) * Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    matrices[m] = Matrix4x4.TRS(pos, rotation, Vector3.one * Random.Range(vegetationSettings[i].minMaxScale.x, vegetationSettings[i].minMaxScale.y));
                }
                vegetationSetting.matrices = matrices;
                vegetationSettings[i] = vegetationSetting;
            }
        }

        private void HandleFloatingOrigin(GPUICameraData cameraData)
        {
            Matrix4x4 newMatrix = sphereTransform.localToWorldMatrix;
            if (newMatrix == _previousMatrix) return;

            Matrix4x4 matrixOffset = newMatrix * _previousMatrix.inverse;
            for (int i = 0; i < _renderKeys.Length; i++)
                GPUITransformBufferUtility.ApplyMatrixOffsetToTransforms(_renderKeys[i], matrixOffset);
            _previousMatrix = newMatrix;
        }
    }
}
