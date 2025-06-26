// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace GPUInstancerPro
{
    [ExecuteInEditMode]
    public class GPUINoGOUpdates : MonoBehaviour
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
        public bool enableColorVariations;
        public bool runUpdate;
        public Vector2 spinSpeed = Vector2.one;
        public Vector3 colorSpeeds = Vector3.one;
        public Text instanceCountText;

        private int _rendererKey;
        private NativeArray<Matrix4x4> _matrix4X4s;
        private NativeArray<Color> _colors;
        private GraphicsBuffer _colorBuffer;
        private int _instanceCount;
        private JobHandle _jobHandle;

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
                    _matrix4X4s = new NativeArray<Matrix4x4>(_instanceCount, Allocator.Persistent);
                    if (enableColorVariations)
                        _colors = new NativeArray<Color>(_instanceCount, Allocator.Persistent);
                    GenerateInstanceMatrices();
                    _jobHandle.Complete();
                    GPUICoreAPI.SetTransformBufferData(_rendererKey, _matrix4X4s); // Set matrices for the renderer

                    if (material != null)
                    {
                        if (enableColorVariations)
                        {
                            _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color)));
                            _colorBuffer.SetData(_colors);
                            material.EnableKeyword("GPUI_COLOR_VARIATION");
                            GPUICoreAPI.AddMaterialPropertyOverride(_rendererKey, "gpuiProFloat4Variation", _colorBuffer); // Set color buffer to the renderers' materials
                        }
                        else
                            material.DisableKeyword("GPUI_COLOR_VARIATION");
                    }
                }
            }

            if (instanceCountText != null)
                instanceCountText.text = _instanceCount.FormatNumberWithSuffix();

#if UNITY_EDITOR
            if (Application.isPlaying && profile != null)
                GPUIRenderingSystem.Editor_CacheProfile(profile); // Rolls back changes made during Play mode.
#endif
        }

        public void OnDisable()
        {
            Dispose();
        }

        private void Update()
        {
            if (_rendererKey != 0 && runUpdate)
            {
                if (_jobHandle.IsCompleted)
                {
                    _jobHandle.Complete();
                    GPUICoreAPI.SetTransformBufferData(_rendererKey, _matrix4X4s, isOverwritePreviousFrameBuffer: false); // Set matrices for the renderer
                    if (enableColorVariations)
                        _colorBuffer.SetData(_colors);
                    GenerateInstanceMatrices();
                }
            }
        }

        public void Dispose()
        {
            _jobHandle.Complete();
            if (_rendererKey != 0)
            {
                GPUICoreAPI.DisposeRenderer(_rendererKey); // Clear renderer data
                _rendererKey = 0;
            }
            if (_matrix4X4s.IsCreated)
                _matrix4X4s.Dispose();
            if (_colors.IsCreated)
                _colors.Dispose();
            if (_colorBuffer != null)
                _colorBuffer.Dispose();
            _colorBuffer = null;
        }

        private void OnValidate()
        {
            if (GPUIRenderingSystem.IsActive && _rendererKey != 0)
                OnEnable();
        }

        private void GenerateInstanceMatrices()
        {
            _jobHandle = new MatrixDataGeneratorJob()
            {
                matrices = _matrix4X4s,
                time = Time.time,
                radial = radial,
                vertical = vertical,
                circular = circular,
                spinSpeed = spinSpeed,
                posOffset = transform.position,
            }.Schedule(radial, 2);

            if (enableColorVariations)
            {
                _jobHandle = new ColorDataGeneratorJob()
                {
                    colors = _colors,
                    time = Time.time,
                    radial = radial,
                    vertical = vertical,
                    circular = circular,
                    colorSpeeds = colorSpeeds,
                }.Schedule(radial, 2, _jobHandle);
            }
        }

        [BurstCompile]
        struct MatrixDataGeneratorJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Matrix4x4> matrices;
            [ReadOnly]
            public float time;
            [ReadOnly]
            public int radial;
            [ReadOnly]
            public int vertical;
            [ReadOnly]
            public int circular;
            [ReadOnly]
            public Vector2 spinSpeed;
            [ReadOnly]
            public Vector3 posOffset;

            public void Execute(int r)
            {
                Matrix4x4 m = Matrix4x4.identity;
                Vector3 pos = Vector3.one;
                int index = r * vertical * circular;

                float rpow = r * 0.001f * math.sin(spinSpeed.y * time);
                for (int v = 0; v < vertical; v++)
                {
                    float radius = 5.0f + r * (Mathf.Pow(v * 0.02f, 1.6f) + 1);
                    for (int c = 0; c < circular; c++)
                    {
                        float angle = v * rpow + 2 * Mathf.PI * c / circular;
                        pos.x = radius * math.cos(angle - time * spinSpeed.x) + posOffset.x;
                        pos.y = v + posOffset.y;
                        pos.z = radius * math.sin(angle - time * spinSpeed.x) + posOffset.z;
                        m.SetPosition(pos);
                        matrices[index] = m;
                        index++;
                    }
                }
            }
        }
        [BurstCompile]
        struct ColorDataGeneratorJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Color> colors;
            [ReadOnly]
            public float time;
            [ReadOnly]
            public int radial;
            [ReadOnly]
            public int vertical;
            [ReadOnly]
            public int circular;
            [ReadOnly]
            public Vector3 colorSpeeds;

            public void Execute(int r)
            {
                Color color = Color.white;
                int index = r * vertical * circular;

                color.r = (r / radial + time * colorSpeeds.x) % 1f;
                for (float v = 0; v < vertical; v++)
                {
                    color.g = (v / vertical + time * colorSpeeds.y) % 1f;
                    for (float c = 0; c < circular; c++)
                    {
                        color.b = (c / circular - time * colorSpeeds.z) % 1f;
                        colors[index] = color;
                        index++;
                    }
                }
            }
        }
    }
}
