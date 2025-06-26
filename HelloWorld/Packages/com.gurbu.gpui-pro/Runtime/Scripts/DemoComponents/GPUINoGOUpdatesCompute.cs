// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
using UnityEngine.UI;

namespace GPUInstancerPro
{
    [ExecuteInEditMode]
    public class GPUINoGOUpdatesCompute : MonoBehaviour
    {
        public GameObject prefab;
        public GPUIProfile profile;
        [Range(1, 100)]
        public int radial = 32;
        [Range(1, 100)]
        public int vertical = 32;
        [Range(1, 100)]
        public int circular = 32;
        public Material material;
        public Vector2 spinSpeed = Vector2.one;
        public Vector3 colorSpeeds = Vector3.one;
        public Text instanceCountText;
        public ComputeShader computeShader;

        private int _rendererKey;
        private GraphicsBuffer _colorBuffer;
        private int _instanceCount;

        private static readonly int PROP_gpuiProFloat4Variation = Shader.PropertyToID("gpuiProFloat4Variation");
        private static readonly int PROP_startIndex = Shader.PropertyToID("startIndex");
        private static readonly int PROP_radial = Shader.PropertyToID("radial");
        private static readonly int PROP_vertical = Shader.PropertyToID("vertical");
        private static readonly int PROP_circular = Shader.PropertyToID("circular");
        private static readonly int PROP_time = Shader.PropertyToID("time");
        private static readonly int PROP_spinSpeed = Shader.PropertyToID("spinSpeed");
        private static readonly int PROP_colorSpeeds = Shader.PropertyToID("colorSpeeds");

        public void OnEnable()
        {
            Dispose();
            if (prefab == null) return;
            _instanceCount = radial * vertical * circular;
            if (_instanceCount > 0)
            {
                GPUICoreAPI.RegisterRenderer(this, prefab, profile, out _rendererKey); // Register the prefab as renderer
                if (_rendererKey != 0)
                {
                    GPUICoreAPI.SetBufferSize(_rendererKey, _instanceCount);
                    GPUICoreAPI.SetInstanceCount(_rendererKey, _instanceCount);

                    _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color)));
                    material.EnableKeyword("GPUI_COLOR_VARIATION");
                    GPUICoreAPI.AddMaterialPropertyOverride(_rendererKey, PROP_gpuiProFloat4Variation, _colorBuffer); // Set color buffer to the renderers' materials
                }
            }

            if (instanceCountText != null)
                instanceCountText.text = _instanceCount.FormatNumberWithSuffix();
        }

        public void OnDisable()
        {
            Dispose();
        }

        private void Update()
        {
            if (_rendererKey == 0 || _instanceCount == 0 || computeShader == null)
                return;
            if (GPUICoreAPI.TryGetTransformBufferData(_rendererKey, out GPUITransformBufferData transformBufferData, out int startIndex, out _))  // Retrieve the GPUITransformBufferData that contains the GraphicsBuffer.
            {
                computeShader.SetBuffer(0, GPUIConstants.PROP_gpuiTransformBuffer, transformBufferData.GetTransformBuffer().Buffer);  // Set the GraphicsBuffer that contains the Matrix4x4 transform data.
                computeShader.SetBuffer(0, PROP_gpuiProFloat4Variation, _colorBuffer);
                computeShader.SetInt(PROP_startIndex, startIndex);
                computeShader.SetInt(PROP_radial, radial);
                computeShader.SetInt(PROP_vertical, vertical);
                computeShader.SetInt(PROP_circular, circular);
                computeShader.SetFloat(PROP_time, Time.time);
                computeShader.SetVector(PROP_spinSpeed, spinSpeed);
                computeShader.SetVector(PROP_colorSpeeds, colorSpeeds);
                computeShader.DispatchXYZ(0, radial, vertical, circular); // Dispatch Compute Shader to set the transform and color buffers.

                transformBufferData.OnTransformDataModified(); // This call is necessary on platforms like Android, which have hardware limitations on the number of structured buffers that can be used in shaders. In such cases, the GraphicsBuffer data is copied into a texture.
            }
        }

        public void Dispose()
        {
            if (_rendererKey != 0)
            {
                GPUICoreAPI.DisposeRenderer(_rendererKey); // Clear renderer data
                _rendererKey = 0;
            }
            if (_colorBuffer != null)
                _colorBuffer.Dispose();
            _colorBuffer = null;
        }

        private void OnValidate()
        {
            if (GPUIRenderingSystem.IsActive && _rendererKey != 0)
                OnEnable();
        }
    }
}
