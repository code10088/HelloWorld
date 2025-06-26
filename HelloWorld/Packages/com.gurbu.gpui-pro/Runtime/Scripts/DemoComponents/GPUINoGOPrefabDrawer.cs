// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace GPUInstancerPro
{
    [ExecuteInEditMode]
    public class GPUINoGOPrefabDrawer : MonoBehaviour
    {
        public GameObject prefabObject;
        public GPUIProfile profile;
        public int instanceCount = 1000;
        public float spacing = 1.5f;
        public bool enableColorVariations;
        public string variationKeyword = "GPUI_COLOR_VARIATION";
        public string variationBufferName = "gpuiProFloat4Variation";
        public Text instanceCountText;

        private int _rendererKey;
        private GraphicsBuffer _colorBuffer;
        private bool _isInitialized;

        public void OnEnable()
        {
            RegisterRenderers();
        }

        public void OnDisable()
        {
            DisposeRenderers();
        }

        private void OnValidate()
        {
            if (GPUIRenderingSystem.IsActive && _isInitialized)
                RegisterRenderers();
        }

        private void RegisterRenderers()
        {
            DisposeRenderers();
            _isInitialized = true;
            if (instanceCount <= 0 || prefabObject == null)
                return;

            GPUICoreAPI.RegisterRenderer(this, prefabObject, profile == null ? GPUIProfile.DefaultProfile : profile, out _rendererKey); // Register the prefab as renderer
            GPUICoreAPI.SetTransformBufferData(_rendererKey, GenerateMatrixArray()); // Set matrices for the renderer
            Renderer[] renderers = prefabObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (enableColorVariations)
                    {
                        material.EnableKeyword(variationKeyword);
                        GPUICoreAPI.AddMaterialPropertyOverride(_rendererKey, variationBufferName, GenerateColorBuffer()); // Set color buffer to the renderers' materials
                    }
                    else
                        material.DisableKeyword(variationKeyword);
                }
            }

            if (instanceCountText != null)
                instanceCountText.text = instanceCount.FormatNumberWithSuffix();
        }
        
        private void DisposeRenderers()
        {
            _isInitialized = false;
            if (_rendererKey != 0)
            {
                GPUICoreAPI.DisposeRenderer(_rendererKey); // Clear renderer data
                _rendererKey = 0;
            }
            if (_colorBuffer != null)
            {
                _colorBuffer.Dispose();
                _colorBuffer = null;
            }
        }

        private Matrix4x4[] GenerateMatrixArray()
        {
            Matrix4x4[] matrix4X4s = new Matrix4x4[instanceCount];
            Matrix4x4 matrix4X4 = Matrix4x4.identity;
            int cubeRoot = Mathf.FloorToInt(Mathf.Pow(instanceCount, 1f / 3f));
            int cubeRootSquare = cubeRoot * cubeRoot;
            Vector3 originPos = transform.position;
            for (int i = 0; i < instanceCount; i++)
            {
                matrix4X4.SetPosition(new Vector3(i % cubeRoot, i / cubeRootSquare, (i / cubeRoot) % cubeRoot) * spacing + originPos);
                matrix4X4s[i] = matrix4X4;
            }

            return matrix4X4s;
        }

        private GraphicsBuffer GenerateColorBuffer()
        {
            if (_colorBuffer != null)
                _colorBuffer.Dispose();
            Color[] colors = new Color[instanceCount];
            Random.InitState(42);
            for (int i = 0; i < instanceCount; i++)
                colors[i] = Random.ColorHSV();
            _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color)));
            _colorBuffer.SetData(colors);
            return _colorBuffer;
        }
    }
}