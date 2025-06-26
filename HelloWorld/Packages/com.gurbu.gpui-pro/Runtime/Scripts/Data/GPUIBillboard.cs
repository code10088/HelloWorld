// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIBillboard : ScriptableObject
    {
        #region Serialized Properties
        // Inputs
        [SerializeField]
        public GameObject prefabObject;
        [SerializeField]
        public GPUIBillboardResolution atlasResolution = GPUIBillboardResolution.x2048;
        [Range(1, 32)]
        [SerializeField]
        public int frameCount = 8;
        [Range(0f, 1f)]
        [SerializeField]
        public float brightness = 0.5f;
        [Range(0f, 1f)]
        [SerializeField]
        public float cutoffOverride = 0.5f;
        [Range(0f, 1f)]
        [SerializeField]
        public float normalStrength = 0.5f;
        [SerializeField]
        public GPUIBillboardShaderType billboardShaderType = GPUIBillboardShaderType.Default;

        // Outputs
        [SerializeField]
        public Vector2 quadSize;
        [SerializeField]
        public float yPivotOffset;

        [SerializeField]
        public Texture2D albedoAtlasTexture;
        [SerializeField]
        public Texture2D normalAtlasTexture;
        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        public RenderTexture albedoAtlasRT;
        [NonSerialized]
        public RenderTexture normalAtlasRT;
        [NonSerialized]
        internal Mesh _quadMesh; // Keep a reference to the generated mesh to avoid duplication and memory leak.
        [NonSerialized]
        internal Material _billboardMaterial; // Keep a reference to the generated material to avoid avoid duplication and memory leak.
        #endregion Runtime Properties

        #region Getters/Setters

        public override string ToString()
        {
            return prefabObject.name;
        }

        public Texture GetAlbedoTexture()
        {
            if (albedoAtlasTexture != null)
                return albedoAtlasTexture;
            return albedoAtlasRT;
        }

        public Texture GetNormalTexture()
        {
            if (normalAtlasTexture != null)
                return normalAtlasTexture;
            return normalAtlasRT;
        }

        #endregion Getters/Setters

        public enum GPUIBillboardResolution
        {
            x256 = 256,
            x512 = 512,
            x1024 = 1024,
            x2048 = 2048,
            x4096 = 4096,
            x8192 = 8192
        }

        public enum GPUIBillboardShaderType
        {
            Default = 0,
            SpeedTree = 1,
            TreeCreator = 2,
            SoftOcclusion = 3
        }
    }
}