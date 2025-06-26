// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro
{
    [Serializable]
    public class GPUIPrototype : IEquatable<GPUIPrototype>
    {
        #region Serialized Properties

        [SerializeField]
        public GPUIPrototypeType prototypeType;
        [SerializeField]
        public GPUIProfile profile;
        [SerializeField]
        public GameObject prefabObject;
        [SerializeField]
        public GPUILODGroupData gpuiLODGroupData;
        [SerializeField]
        public Mesh prototypeMesh;
        [SerializeField]
        public Material[] prototypeMaterials;
        [SerializeField]
        public int layer;

        [SerializeField]
        public bool isGenerateBillboard;
        [SerializeField]
        public bool isBillboardReplaceLODCulled = true;
        [SerializeField]
        [Range(0, 1)]
        public float billboardDistance = 0.9f;
        [SerializeField]
        public GPUIBillboard billboardAsset;

        [SerializeField]
        public bool isEnabled = true;

        [SerializeField]
        public string name;
        #endregion Serialized Properties

        #region Runtime Properties

        [NonSerialized]
        public int errorCode;
        [NonSerialized]
        public UnityEngine.Events.UnityAction errorFixAction;

        private const int ERROR_CODE_ADDITION = 1000;

        #endregion Runtime Properties

        #region Constructors

        public GPUIPrototype(GameObject prefabObject, GPUIProfile profile)
        {
            prototypeType = GPUIPrototypeType.Prefab;
            this.prefabObject = prefabObject;
            this.profile = profile;
        }

        public GPUIPrototype(GPUILODGroupData gpuiLODGroupData, GPUIProfile profile)
        {
            prototypeType = GPUIPrototypeType.LODGroupData;
            this.gpuiLODGroupData = gpuiLODGroupData;
            this.profile = profile;
        }

        public GPUIPrototype(Mesh mesh, Material[] materials, GPUIProfile profile)
        {
            prototypeType = GPUIPrototypeType.MeshAndMaterial;
            this.prototypeMesh = mesh;
            this.prototypeMaterials = materials;
            this.profile = profile;
        }

        #endregion Constructors

        #region Runtime Methods

        public bool IsValid(bool logError)
        {
            errorCode = 0;
            errorFixAction = null;
            if (profile == null)
                profile = GPUIProfile.DefaultProfile;
            switch (prototypeType)
            {
                case GPUIPrototypeType.Prefab:
                    if (prefabObject == null)
                    {
                        if (logError)
                            Debug.LogError(GPUIConstants.LOG_PREFIX + this + " prefabObject is null.");
                        errorCode = ERROR_CODE_ADDITION + 1;
                        return false;
                    }
                    break;
                case GPUIPrototypeType.LODGroupData:
                    if (gpuiLODGroupData == null)
                    {
                        if (logError)
                            Debug.LogError(GPUIConstants.LOG_PREFIX + this + " gpuiLODGroupData is null.");
                        errorCode = ERROR_CODE_ADDITION + 2;
                        return false;
                    }
                    break;
                case GPUIPrototypeType.MeshAndMaterial:
                    if (prototypeMesh == null)
                    {
                        if (logError)
                            Debug.LogError(GPUIConstants.LOG_PREFIX + this + " mesh is null.");
                        errorCode = ERROR_CODE_ADDITION + 3;
                        return false;
                    }
                    if (prototypeMaterials == null)
                    {
                        if (logError)
                            Debug.LogError(GPUIConstants.LOG_PREFIX + this + " materials is null.");
                        errorCode = ERROR_CODE_ADDITION + 4;
                        return false;
                    }
                    break;
            }
            return true;
        }

        public bool Equals(GPUIPrototype other)
        {
            if (base.Equals(other))
                return true;
            if (prototypeType == GPUIPrototypeType.Prefab && other.prototypeType == GPUIPrototypeType.Prefab && prefabObject != null)
                return prefabObject.Equals(other.prefabObject);
            if (prototypeType == GPUIPrototypeType.LODGroupData && other.prototypeType == GPUIPrototypeType.LODGroupData && gpuiLODGroupData != null)
                return gpuiLODGroupData.Equals(other.gpuiLODGroupData);
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is GPUIPrototype prototype)
                return Equals(prototype);
            return base.Equals(obj);
        }

        public void GenerateBillboard(bool forceNew = true)
        {
            if (isGenerateBillboard && prefabObject != null)
            {
                if (billboardAsset == null)
                {
                    billboardAsset = GPUIBillboardUtility.FindBillboardAsset(prefabObject);
                    if (Application.isPlaying)
                        return;
                    if (billboardAsset == null)
                        billboardAsset = GPUIBillboardUtility.GenerateBillboardData(prefabObject);
                }

                if (forceNew || billboardAsset.albedoAtlasTexture == null)
                    GPUIBillboardUtility.GenerateBillboardDelayed(billboardAsset, true);
            }
        }

        #endregion Runtime Methods

        #region Getters/Setters

        public int GetLODCount()
        {
            int result = 0;
            if (prototypeType == GPUIPrototypeType.Prefab && prefabObject != null)
            {
                result = prefabObject.GetLODCount();
                if (isGenerateBillboard)
                {
                    if (billboardAsset == null)
                        billboardAsset = GPUIBillboardUtility.FindBillboardAsset(prefabObject);
                    if (billboardAsset != null)
                        result++;
                }
            }
            else if (prototypeType == GPUIPrototypeType.LODGroupData && gpuiLODGroupData != null)
                result = gpuiLODGroupData.Length;
            else if (prototypeType == GPUIPrototypeType.MeshAndMaterial && prototypeMesh != null && prototypeMaterials != null && prototypeMaterials[0] != null)
                result = 1;
            return result;
        }

        public override int GetHashCode()
        {
            if (prototypeType == GPUIPrototypeType.Prefab && prefabObject != null)
                return prefabObject.GetHashCode();
            if (prototypeType == GPUIPrototypeType.LODGroupData && gpuiLODGroupData != null)
                return gpuiLODGroupData.GetHashCode();
            return base.GetHashCode();
        }

        public int GetKey()
        {
            if (prototypeType == GPUIPrototypeType.Prefab && prefabObject != null)
                return prefabObject.GetInstanceID();
            if (prototypeType == GPUIPrototypeType.LODGroupData && gpuiLODGroupData != null)
                return gpuiLODGroupData.GetInstanceID();
            return GetHashCode();
        }

        public Bounds GetBounds()
        {
            if (prototypeType == GPUIPrototypeType.Prefab && prefabObject != null)
                return prefabObject.GetBounds();
            if (prototypeType == GPUIPrototypeType.LODGroupData && gpuiLODGroupData != null)
                return gpuiLODGroupData.bounds;
            if (prototypeType == GPUIPrototypeType.MeshAndMaterial && prototypeMesh != null && prototypeMaterials != null && prototypeMaterials[0] != null)
                return prototypeMesh.bounds;
            return new Bounds(Vector3.zero, Vector3.one);
        }

        private const string DEFAULT_PROTOTYPE_NAME = "[GPUIPrototype]";
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(name))
                return name;
            switch (prototypeType)
            {
                case GPUIPrototypeType.Prefab:
                    if (prefabObject != null)
                    {
                        name = prefabObject.name;
                        return name;
                    }
                    break;
                case GPUIPrototypeType.LODGroupData:
                    if (gpuiLODGroupData != null)
                    {
                        name = gpuiLODGroupData.ToString();
                        return name;
                    }
                    break;
                case GPUIPrototypeType.MeshAndMaterial:
                    if (prototypeMesh != null)
                    {
                        name = prototypeMesh.name;
                        return name;
                    }
                    break;
            }
            return DEFAULT_PROTOTYPE_NAME;
        }

        #endregion Getters/Setters
    }

    public enum GPUIPrototypeType
    {
        Prefab = 0,
        LODGroupData = 1,
        MeshAndMaterial = 2,
    }
}