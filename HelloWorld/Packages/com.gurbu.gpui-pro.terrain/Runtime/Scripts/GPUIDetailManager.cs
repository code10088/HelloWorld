// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GPUInstancerPro.TerrainModule
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(200)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#The_Detail_Manager")]
    public class GPUIDetailManager : GPUITerrainManager<GPUIDetailPrototypeData>
    {
        #region Serialized Properties
        [SerializeField]
        public GPUIProfile defaultDetailTextureProfile;
        [SerializeField]
        public float detailObjectDistance = 250f;
        [SerializeField]
        public Vector4 windVector = new Vector2(0.4f, 0.8f);
        [SerializeField]
        public Texture2D healthyDryNoiseTexture;
        [SerializeField]
        [Range(0.0f, 100.0f)]
        public float detailUpdateDistance = 1.0f;
        [SerializeField]
        public bool disableAsyncDetailDataRequest;
        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private int _requireUpdateFrame;
        [NonSerialized]
        private Dictionary<int, GPUIDetailUpdateData> _detailUpdateDataDict;
        [NonSerialized]
        private Action<GPUIDataBuffer<GPUICounterData>> _processCounterDataCallback;
        [NonSerialized]
        private bool _forceImmediateUpdate;
        [NonSerialized]
        private int[] _sizeAndIndexes;
        [NonSerialized]
        private bool _reloadTerrainDetailTextures;

        private const int ERROR_CODE_ADDITION = 400;
        public const int DETAIL_SUB_SETTING_DIVIDER = 1000;
        #endregion Runtime Properties

        #region MonoBehaviour Methods

        protected override void Update()
        {
            base.Update();

            if (_detailUpdateDataDict == null) return;

            foreach (GPUIDetailUpdateData detailUpdateData in _detailUpdateDataDict.Values)
            {
                if (detailUpdateData.IsDataRequested())
                    continue;
                if (detailUpdateData.requireReadback)
                {
                    detailUpdateData.requireReadback = false;
                    detailUpdateData.AsyncDataRequest(_processCounterDataCallback, false);
                }
                else if (detailUpdateData.processReadback)
                {
                    detailUpdateData.processReadback = false;
                    detailUpdateData.ProcessGPUReadback(this);
                }
            }
        }

#if UNITY_EDITOR
        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!IsInitialized || Application.isPlaying)
                return;
            HandleEditorChanges();
        }

        private void HandleEditorChanges()
        {
            _forceImmediateUpdate = true; // In edit mode we need to wait for GPU readback on updates otherwise the detail instance counts might be always 0
            foreach (var activeTerrain in GetActiveTerrainValues())
            {
                if (activeTerrain != null && activeTerrain is GPUITerrainBuiltin gpuiTerrainBuiltin && gpuiTerrainBuiltin._latestDetailObjectDensity != gpuiTerrainBuiltin.GetTerrain().detailObjectDensity)
                {
                    gpuiTerrainBuiltin._latestDetailObjectDensity = gpuiTerrainBuiltin.GetTerrain().detailObjectDensity;
                    RequireUpdate();
                    break;
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();

            if (healthyDryNoiseTexture == null)
            {
                healthyDryNoiseTexture = GPUITerrainConstants.DefaultHealthyDryNoiseTexture;
                EditorUtility.SetDirty(this);
            }

            if (defaultDetailTextureProfile == null)
            {
                defaultDetailTextureProfile = GPUITerrainConstants.DefaultDetailTextureProfile;
                EditorUtility.SetDirty(this);
            }
        }
#endif

        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        public override bool IsValid(bool logError = true)
        {
            if (!base.IsValid(logError))
                return false;

            bool hasTerrainPrototype = false;
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain != null && gpuiTerrain.DetailPrototypes != null && gpuiTerrain.DetailPrototypes.Length > 0)
                {
                    hasTerrainPrototype = true;
                    break;
                }
            }
            if (!hasTerrainPrototype)
            {
                errorCode = -ERROR_CODE_ADDITION - 2; // No detail prototypes on the terrain
                return false;
            }

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            _sizeAndIndexes = new int[4];
            _detailUpdateDataDict = new();

            GPUIRenderingSystem.Instance.OnPreCull -= UpdateDetailMatrices;
            GPUIRenderingSystem.Instance.OnCommandBufferModified -= RequireUpdate;
            GPUIRenderingSystem.Instance.OnPreCull += UpdateDetailMatrices;
            GPUIRenderingSystem.Instance.OnCommandBufferModified += RequireUpdate;

            _processCounterDataCallback = ProcessCounterData; // Call inside OnEnable. Awake causes data loss in editor in some cases
            RequireUpdate();

            if (GPUITerrain._terrainsSearchingForDetailManager != null)
            {
                AddTerrains(GPUITerrain._terrainsSearchingForDetailManager);
                GPUITerrain._terrainsSearchingForDetailManager.Clear();
            }
        }

        protected override bool RegisterRenderer(int prototypeIndex)
        {
            if (base.RegisterRenderer(prototypeIndex))
            {
                var prototypeData = _prototypeDataArray[prototypeIndex];
                int initialBufferSize = Mathf.Max(prototypeData.initialBufferSize, 1);
                GPUIRenderingSystem.SetBufferSize(_runtimeRenderKeys[prototypeIndex], initialBufferSize, false);
                if (disableAsyncDetailDataRequest)
                    GPUIRenderingSystem.SetInstanceCount(_runtimeRenderKeys[prototypeIndex], initialBufferSize);
                prototypeData._bounds = GPUIRenderingSystem.Instance.LODGroupDataProvider.GetOrCreateLODGroupData(_prototypes[prototypeIndex]).bounds;

                if (_detailUpdateDataDict != null)
                {
                    foreach (var data in _detailUpdateDataDict.Values)
                    {
                        if (prototypeIndex >= data.Length)
                            data.Resize(prototypeIndex + 1); // Resize counter buffers
                    }
                }
                
                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            if (GPUIRenderingSystem.IsActive)
            {
                GPUIRenderingSystem.Instance.OnPreCull -= UpdateDetailMatrices;
                GPUIRenderingSystem.Instance.OnCommandBufferModified -= RequireUpdate;
            }
            base.Dispose();
            if (_detailUpdateDataDict != null)
            {
                foreach (var data in _detailUpdateDataDict)
                    data.Value.Dispose();
            }
            _detailUpdateDataDict = null;
        }

        #endregion Initialize/Dispose

        #region Detail Matrix Update

        private void UpdateDetailMatrices(GPUICameraData cameraData)
        {
            if (!IsInitialized)
                return;
            int prototypeCount = _prototypes.Length;
            if (prototypeCount == 0)
                return;
            var activeTerrains = GetActiveTerrainValues();

            if (GetActiveTerrainCount() == 0 && _runtimeRenderKeys != null) // When there are no terrains, release buffers
            {
                for (int i = 0; i < _runtimeRenderKeys.Length; i++)
                {
                    int renderKey = _runtimeRenderKeys[i];
                    if (renderKey != 0)
                    {
                        int initialBufferSize = Mathf.Max(GetPrototypeData(i).initialBufferSize, 1);

                        GPUIRenderingSystem.SetBufferSize(renderKey, initialBufferSize, false);
                        if (disableAsyncDetailDataRequest)
                            GPUIRenderingSystem.SetInstanceCount(renderKey, initialBufferSize);
                    }
                }
            }

            ComputeShader CS_VegetationGenerator = GPUITerrainConstants.CS_VegetationGenerator;
            _detailUpdateDataDict ??= new();

            Vector3 cameraPos = cameraData.GetCameraPosition();
            int cameraKey = cameraData.ActiveCamera.GetInstanceID();
            if (!_detailUpdateDataDict.TryGetValue(cameraKey, out var detailUpdateData))
            {
                detailUpdateData = new(cameraData, "GPUIDetailCounterBuffer", prototypeCount);
                _detailUpdateDataDict[cameraKey] = detailUpdateData;
            }
            if (detailUpdateData.Length < prototypeCount)
                detailUpdateData.Resize(prototypeCount);
            if (!_forceImmediateUpdate && detailUpdateData.IsDataRequested()) // Wait for readback before modifying counter buffer
                return;
            if (_requireUpdateFrame > detailUpdateData.lastUpdateFrame
                || detailUpdateDistance <= 0
                || Vector3.Distance(detailUpdateData.position, cameraPos) >= detailUpdateDistance)
            {
                if (_forceImmediateUpdate)
                {
                    detailUpdateData.WaitForReadbackCompletion();
                    _forceImmediateUpdate = false;
                    if (detailUpdateData.processReadback)
                    {
                        detailUpdateData.processReadback = false;
                        detailUpdateData.ProcessGPUReadback(this);
                    }
                }
                if (!detailUpdateData.UpdateBufferData())
                {
                    _sizeAndIndexes[0] = prototypeCount;
                    // CSResetCounterBuffer
                    CS_VegetationGenerator.SetBuffer(1, GPUITerrainConstants.PROP_detailCounterBuffer, detailUpdateData.Buffer);
                    CS_VegetationGenerator.SetInts(GPUIConstants.PROP_sizeAndIndexes, _sizeAndIndexes);
                    CS_VegetationGenerator.DispatchX(1, prototypeCount);
                }

                bool allUpdatesCompleted = true;

                Profiler.BeginSample("GPUIDetailManager.UpdateDetailMatrices");
                for (int i = 0; i < prototypeCount; i++)
                {
                    if (!_prototypes[i].isEnabled) continue;

                    if (GPUIRenderingSystem.TryGetRenderSourceGroup(_runtimeRenderKeys[i], out GPUIRenderSourceGroup rsg)
                        && cameraData.TryGetShaderBuffer(_runtimeRenderKeys[i], out GPUIShaderBuffer shaderBuffer))
                    {
                        Profiler.BeginSample(rsg.ToString());
                        GraphicsBuffer transformBuffer = shaderBuffer.Buffer;
                        if (transformBuffer == null)
                        {
                            allUpdatesCompleted = false;
                            continue;
                        }
                        _sizeAndIndexes[0] = transformBuffer.count;
                        _sizeAndIndexes[1] = i;

                        GPUIDetailPrototypeData prototypeData = _prototypeDataArray[i];
                        foreach (GPUITerrain gpuiTerrain in activeTerrains)
                        {
                            if (gpuiTerrain == null || !gpuiTerrain.isActiveAndEnabled
#if UNITY_EDITOR
                                || gpuiTerrain.editor_IsDisableDetailRendering
#endif
                                )
                                continue;
                            if (!gpuiTerrain.IsDetailDensityTexturesLoaded || _reloadTerrainDetailTextures)
                                gpuiTerrain.CreateDetailTextures();
                            gpuiTerrain.GenerateVegetation(prototypeData, transformBuffer, detailUpdateData, cameraPos, detailObjectDistance, healthyDryNoiseTexture, _sizeAndIndexes);
                        }

                        // CSClearTransformBuffer
                        CS_VegetationGenerator.SetBuffer(2, GPUIConstants.PROP_gpuiTransformBuffer, transformBuffer);
                        CS_VegetationGenerator.SetBuffer(2, GPUITerrainConstants.PROP_detailCounterBuffer, detailUpdateData.Buffer);
                        CS_VegetationGenerator.SetInts(GPUIConstants.PROP_sizeAndIndexes, _sizeAndIndexes);
                        CS_VegetationGenerator.DispatchX(2, transformBuffer.count);

                        rsg.TransformBufferData.OnTransformDataModified(shaderBuffer);
                        Profiler.EndSample();
                    }
                    else
                        allUpdatesCompleted = false;
                }
                _reloadTerrainDetailTextures = false;
                if (!disableAsyncDetailDataRequest)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        detailUpdateData.AsyncDataRequest(_processCounterDataCallback, false);
                    else
#endif
                        detailUpdateData.requireReadback = true;
                }
                if (allUpdatesCompleted)
                {
                    detailUpdateData.position = cameraPos;
                    detailUpdateData.lastUpdateFrame = Time.frameCount;
                }
                Profiler.EndSample();
            }
        }

        private void ProcessCounterData(GPUIDataBuffer<GPUICounterData> buffer)
        {
            GPUIDetailUpdateData detailUpdateData = buffer as GPUIDetailUpdateData;
            if (detailUpdateData != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    detailUpdateData.ProcessGPUReadback(this);
                else
#endif
                    detailUpdateData.processReadback = true;
            }
                
        }

        public void ExecuteProceduralDetails(GPUITerrain gpuiTerrain)
        {
            int prototypeCount = GetPrototypeCount();
            int[] terrainPrototypeIndexes = GetTerrainPrototypeIndexes(gpuiTerrain);
            if (terrainPrototypeIndexes == null)
                return;
            for (int p = 0; p < prototypeCount; p++)
            {
                GPUIDetailPrototypeData prototypeData = _prototypeDataArray[p];
                if (prototypeData.proceduralDensityData != null)
                {
                    for (int i = 0; i < terrainPrototypeIndexes.Length; i++)
                    {
                        int managerPrototypeIndex = terrainPrototypeIndexes[i] % DETAIL_SUB_SETTING_DIVIDER;
                        if (managerPrototypeIndex == p)
                            prototypeData.proceduralDensityData.Execute(gpuiTerrain, i);
                    }
                }
            }
        }

        #endregion Detail Matrix Update

        #region Prototype Changes

        protected override bool AddMissingPrototypesFromTerrain(GPUITerrain gpuiTerrain)
        {
            bool prototypeAdded = false;
            DetailPrototype[] detailPrototypes = gpuiTerrain.DetailPrototypes;
            int[] terrainPrototypeIndexes = GetTerrainPrototypeIndexes(gpuiTerrain);
            for (int i = 0; i < terrainPrototypeIndexes.Length; i++)
            {
                if (terrainPrototypeIndexes[i] < 0)
                {
                    AddDetailPrototype(detailPrototypes[i]);
                    prototypeAdded = true;
                }
            }

            return prototypeAdded;
        }

        /// <summary>
        /// Updates previously matched prototypes from terrain detail prototypes before determining detail prototype indexes
        /// </summary>
        protected override void BeginDeterminePrototypeIndexes()
        {
            int terrainCount = GetTerrainCount();
            int prototypeCount = GetPrototypeCount();
            for (int t = terrainCount - 1; t >= 0; t--)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain == null || gpuiTerrain.DetailPrototypeIndexes == null || gpuiTerrain.DetailPrototypes == null) continue;
                for (int p = 0; p < prototypeCount; p++)
                {
                    int terrainPrototypeIndex = gpuiTerrain.GetFirstTerrainDetailPrototypeIndex(p);
                    if (terrainPrototypeIndex >= 0 && gpuiTerrain.DetailPrototypes.Length > terrainPrototypeIndex)
                    {
                        DetailPrototype detailPrototype = gpuiTerrain.DetailPrototypes[terrainPrototypeIndex];
                        if (_prototypeDataArray[p].IsMatchingPrefabAndTexture(detailPrototype, _prototypes[p], false))
                            _prototypeDataArray[p].ReadFromDetailPrototypeData(gpuiTerrain.DetailPrototypes[terrainPrototypeIndex], gpuiTerrain.DetailPrototypeIndexes[terrainPrototypeIndex] / DETAIL_SUB_SETTING_DIVIDER, this, p);
                    }
                }
            }
        }

        internal int DetermineDetailPrototypeIndex(DetailPrototype detailPrototype)
        {
            if (_prototypes != null)
            {
                for (int p = 0; p < _prototypes.Length; p++)
                {
                    GPUIPrototype prototype = _prototypes[p];
                    GPUIDetailPrototypeData prototypeData = _prototypeDataArray[p];
                    if (prototypeData.IsMatchingPrefabAndTexture(detailPrototype, prototype))
                    {
                        int subSettingCount = prototypeData.GetSubSettingCount();
                        for (int s = 0; s < subSettingCount; s++)
                        {
                            if (prototypeData.HasSameSettingsWith(detailPrototype, s))
                                return p + s * DETAIL_SUB_SETTING_DIVIDER;
                        }
                        prototypeData.ReadFromDetailPrototypeData(detailPrototype, subSettingCount, this, p);
                        if (IsInitialized)
                            prototypeData.SetParameterBufferData();
                        return p + subSettingCount * DETAIL_SUB_SETTING_DIVIDER;
                    }
                }
            }
            if (_isAutoAddPrototypesBasedOnTerrains)
                _isTerrainsModified = true;
            return -1;
        }

        protected override void SetGPUITerrainManager(GPUITerrain gpuiTerrain)
        {
            gpuiTerrain.SetDetailManager(this);
        }

        protected override void RemoveGPUITerrainManager(GPUITerrain gpuiTerrain)
        {
            if (gpuiTerrain.DetailManager == this)
                gpuiTerrain.RemoveDetailManager();
        }

        protected override void OnNewPrototypeDataCreated(int prototypeIndex)
        {
            base.OnNewPrototypeDataCreated(prototypeIndex);

            if (_prototypeDataArray[prototypeIndex].detailTexture != null)
                _prototypes[prototypeIndex].name = _prototypeDataArray[prototypeIndex].detailTexture.name;
        }

        public override void CheckPrototypeChanges()
        {
            base.CheckPrototypeChanges();

            for (int i = 0; i < GetPrototypeCount(); i++)
            {
                GPUIPrototype prototype = _prototypes[i];
                if (prototype.prototypeType == GPUIPrototypeType.MeshAndMaterial)
                {
                    if (prototype.prototypeMesh == null)
                    {
                        prototype.prototypeMesh = GPUITerrainConstants.DefaultDetailMesh;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            EditorUtility.SetDirty(this);
#endif
                    }
                    if (prototype.prototypeMaterials == null || prototype.prototypeMaterials.Length == 0 || prototype.prototypeMaterials[0] == null)
                    {
                        prototype.prototypeMaterials = new Material[] { GPUITerrainConstants.DefaultDetailMaterial };
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            EditorUtility.SetDirty(this);
#endif
                    }

                    if (_prototypeDataArray[i].mpbDescription == null)
                    {
                        _prototypeDataArray[i].mpbDescription = GPUITerrainConstants.DefaultDetailMaterialDescription;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            EditorUtility.SetDirty(this);
#endif
                    }
                }
            }
        }

        protected override void DeterminePrototypeIndexes(GPUITerrain gpuiTerrain)
        {
            gpuiTerrain.DetermineDetailPrototypeIndexes(this);
        }

        protected override int[] GetTerrainPrototypeIndexes(GPUITerrain gpuiTerrain)
        {
            if (gpuiTerrain.DetailPrototypes == null)
                gpuiTerrain.LoadTerrain();
            if (gpuiTerrain.DetailPrototypeIndexes == null || (gpuiTerrain.DetailPrototypes != null && gpuiTerrain.DetailPrototypes.Length != gpuiTerrain.DetailPrototypeIndexes.Length))
                DeterminePrototypeIndexes(gpuiTerrain);
            return gpuiTerrain.DetailPrototypeIndexes;
        }

        public int AddDetailPrototype(DetailPrototype detailPrototype)
        {
            int prototypeIndex = -1;
            int subSettingIndex = 0;

            // Check if prefab or texture is already added with same color settings
            for (int p = 0; p < _prototypes.Length; p++)
            {
                GPUIPrototype prototype = _prototypes[p];
                GPUIDetailPrototypeData prototypeData = _prototypeDataArray[p];
                if (prototypeData.IsMatchingPrefabAndTexture(detailPrototype, prototype))
                {
                    prototypeIndex = p;
                    subSettingIndex = prototypeData.GetSubSettingCount();

                    for (int s = 0; s < subSettingIndex; s++)
                    {
                        if (prototypeData.HasSameSettingsWith(detailPrototype, s))
                        {
                            subSettingIndex = s;
                            break;
                        }
                    }
                    break;
                }
            }

            // Add new prototype if the prefab or texture is non-existent
            if (prototypeIndex < 0)
            {
                if (detailPrototype.prototype != null)
                    prototypeIndex = AddPrototype(new GPUIPrototype(detailPrototype.prototype.GetPrefabRoot(), GetDefaultProfile()));
                else
                    prototypeIndex = AddPrototype(new GPUIPrototype(
                        GPUITerrainConstants.DefaultDetailMesh,
                        new Material[] { GPUITerrainConstants.DefaultDetailMaterial },
                        GetTexturePrototypeProfile()));
            }

            if (prototypeIndex < 0)
            {
                if (detailPrototype.prototype != null)
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Failed adding a new Detail prototype: " + detailPrototype.prototype, detailPrototype.prototype);
                else
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Failed adding a new Detail prototype: " + detailPrototype.prototypeTexture, detailPrototype.prototypeTexture);
                return -1;
            }

            // Update settings from terrain
            _prototypeDataArray[prototypeIndex].ReadFromDetailPrototypeData(detailPrototype, subSettingIndex, this, prototypeIndex);
            OnNewPrototypeDataCreated(prototypeIndex);

            return prototypeIndex;
        }

        public override void OnPrototypePropertiesModified()
        {
            base.OnPrototypePropertiesModified();

            if (!IsInitialized)
                return;

            if (healthyDryNoiseTexture == null)
                healthyDryNoiseTexture = GPUITerrainConstants.DefaultHealthyDryNoiseTexture;

            for (int i = 0; i < _prototypes.Length; i++)
            {
                if (_runtimeRenderKeys[i] == 0)
                    continue;

                var prototypeData = _prototypeDataArray[i];
                if (prototypeData.detailTexture != null && GPUIRenderingSystem.TryGetRenderSourceGroup(_runtimeRenderKeys[i], out GPUIRenderSourceGroup rsg))
                    _prototypeDataArray[i].SetMPBValues(this, i, rsg);
            }

            for (int i = 0; i < _prototypeDataArray.Length; i++)
                _prototypeDataArray[i].SetParameterBufferData();

            RequireUpdate();
        }

        public void RemoveDetailPrototypeAtIndex(int index, bool removeFromTerrain)
        {
            if (removeFromTerrain)
            {
                int terrainCount = GetTerrainCount();
                for (int t = 0; t < terrainCount; t++)
                {
                    GPUITerrain gpuiTerrain = GetTerrain(t);
                    if (gpuiTerrain != null)
                        gpuiTerrain.RemoveDetailPrototypeAtIndex(index);
                }
            }
            RemovePrototypeAtIndex(index);
        }

        public override bool CanAddObjectAsPrototype(UnityEngine.Object obj)
        {
            if (base.CanAddObjectAsPrototype(obj))
                return true;
            if (obj is Texture2D)
                return true;
            return false;
        }

        public void AddPrototypeToTerrains(UnityEngine.Object pickerObject, int overwriteIndex)
        {
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain != null)
                    gpuiTerrain.AddDetailPrototypeToTerrain(pickerObject, overwriteIndex);
            };
        }

        protected override void OnFirstTerrainAdded(Terrain terrain)
        {
            base.OnFirstTerrainAdded(terrain);
            if (detailObjectDistance == 250f)
            {
                float firstTerrainDetailObjectDistance = terrain.detailObjectDistance;
                if (firstTerrainDetailObjectDistance > 0)
                    detailObjectDistance = firstTerrainDetailObjectDistance;
            }
        }

        #endregion Prototype Changes

        #region Getters/Setters

        public override void RequireUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                _requireUpdateFrame = Time.frameCount + 1;
            else
#endif
                _requireUpdateFrame = Time.frameCount;
        }

        public void RequireUpdate(bool forceImmediateUpdate, bool reloadTerrainDetailTextures = false)
        {
            if (forceImmediateUpdate)
                _forceImmediateUpdate = true;
            if (reloadTerrainDetailTextures)
                _reloadTerrainDetailTextures = true;
            RequireUpdate();
        }

        public override GPUIProfile GetDefaultProfile()
        {
            if (defaultProfile != null)
                return defaultProfile;
            return GPUITerrainConstants.DefaultDetailPrefabProfile;
        }

        public GPUIProfile GetTexturePrototypeProfile()
        {
            if (defaultDetailTextureProfile != null)
                return defaultDetailTextureProfile;
            return GPUITerrainConstants.DefaultDetailTextureProfile;
        }

        public override int GetRendererGroupID(int prototypeIndex)
        {
            return GPUIUtility.GenerateHash(GetInstanceID(), prototypeIndex);
        }

        public override GPUITransformBufferType GetTransformBufferType(int prototypeIndex)
        {
            return GPUITransformBufferType.CameraBased;
        }

        public Bounds GetPrototypeBounds(int prototypeIndex)
        {
            return _prototypeDataArray[prototypeIndex]._bounds;
        }

        public void SetDistanceDensityReduction(bool enabled)
        {
            if (_prototypeDataArray != null)
            {
                for (int i = 0; i < _prototypeDataArray.Length; i++)
                {
                    _prototypeDataArray[i].isUseDensityReduction = enabled;
                }
                RequireUpdate();
            }
        }

        public void SetDetailObjectDistance(float distance)
        {
            detailObjectDistance = distance;
            RequireUpdate();
        }

        public bool IsReadTerrainDetails(int prototypeIndex)
        {
            GPUIDetailPrototypeData prototypeData = GetPrototypeData(prototypeIndex);
            if (prototypeData != null && prototypeData.proceduralDensityData != null && !prototypeData.proceduralDensityData.isReadTerrainDetails)
                return false;
            return true;
        }

        public bool HasProceduralDensity()
        {
            int prototypeCount = GetPrototypeCount();
            for (int p = 0; p < prototypeCount; p++)
            {
                if (_prototypeDataArray[p].proceduralDensityData != null)
                    return true;
            }
            return false;
        }

        #endregion Getters/Setters

        #region Editor Methods
#if UNITY_EDITOR
        public override bool Editor_IsAllowEditPrototype(int prototypeType)
        {
            if (prototypeType == 2) return true;
            return base.Editor_IsAllowEditPrototype(prototypeType);
        }
#endif
        #endregion Editor Methods

        #region GPUIDetailUpdateData

        internal class GPUIDetailUpdateData : GPUIDataBuffer<GPUICounterData>
        {
            public GPUICameraData cameraData;
            public Vector3 position;
            public int lastUpdateFrame;
            public bool requireReadback;
            public bool processReadback;

            public GPUIDetailUpdateData(GPUICameraData cameraData, string name, int length, GraphicsBuffer.Target target = GraphicsBuffer.Target.Structured) : base(name, length, target)
            {
                this.cameraData = cameraData;
                position = Vector3.negativeInfinity;
                lastUpdateFrame = 0;
            }

            public void ProcessGPUReadback(GPUIDetailManager detailManager)
            {
                if (detailManager._runtimeRenderKeys == null)
                    return;
                NativeArray<GPUICounterData> requestedData = GetRequestedData();
                if (!requestedData.IsCreated) return;
                Profiler.BeginSample("GPUIDetailManager.ProcessCounterData");
#if UNITY_EDITOR
                bool isMultiCamera = GPUIRenderingSystem.Instance.CameraDataProvider.CountWithEditModeCameras > 1 || !Application.isPlaying;
#else
                bool isMultiCamera = GPUIRenderingSystem.Instance.CameraDataProvider.Count > 1 || !Application.isPlaying;
#endif
                int requestedDataLength = requestedData.Length;
                int prototypeCount = detailManager.GetPrototypeCount();
                for (int i = 0; i < prototypeCount && i < requestedDataLength; i++)
                {
                    int renderKey = detailManager._runtimeRenderKeys[i];
                    if (renderKey == 0) continue;
                    if (GPUIRenderingSystem.TryGetRenderSourceGroup(renderKey, out var rsg))
                    {
                        int instanceCount = (int)requestedData[i].count;
                        GPUIDetailPrototypeData prototypeData = detailManager._prototypeDataArray[i];

                        if (instanceCount > 0 && instanceCount != rsg.InstanceCount)
                        {
                            int extraBufferSize = Mathf.Max(Mathf.CeilToInt(instanceCount * prototypeData.detailExtraBufferSizePercentage), 1024);
                            if (instanceCount > rsg.BufferSize)
                            {
                                GPUIRenderingSystem.SetBufferSize(renderKey, instanceCount + extraBufferSize, false);
                                if (!isMultiCamera)
                                    GPUIRenderingSystem.SetInstanceCount(renderKey, instanceCount);
                                detailManager.RequireUpdate();
                            }
                            else if (!isMultiCamera  // Do not reduce buffer size or limit instance count based on one camera visibility data when there are multiple cameras
                                    && prototypeData.detailBufferSizePercentageDifferenceForReduction > 0f)
                            {
                                int minDiffForReduction = Mathf.CeilToInt(instanceCount * prototypeData.detailBufferSizePercentageDifferenceForReduction) + extraBufferSize;
                                if (rsg.BufferSize - instanceCount > minDiffForReduction)
                                {
                                    GPUIRenderingSystem.SetBufferSize(renderKey, instanceCount + extraBufferSize, false);
                                    GPUIRenderingSystem.SetInstanceCount(renderKey, instanceCount);
                                    detailManager.RequireUpdate();
                                }
                                else
                                    GPUIRenderingSystem.SetInstanceCount(renderKey, instanceCount);
                            }
                            else if (instanceCount > rsg.InstanceCount)
                                GPUIRenderingSystem.SetInstanceCount(renderKey, instanceCount);
                        }
                    }
                    else
                        Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can not find renderer with key: " + detailManager._runtimeRenderKeys[i]);
                }
                Profiler.EndSample();
            }
        }

#endregion GPUIDetailUpdateData
    }
}
