// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    /// <summary>
    /// This component is automatically attached to Terrains that are used with GPUI Managers
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Terrain))]
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Terrain")]
    public class GPUITerrainBuiltin : GPUITerrain
    {
        #region Serialized Properties

        [SerializeField]
        internal float _terrainTreeDistance = 5000f;
        [SerializeField]
        internal bool _isBakedDetailTextures;
        [SerializeField]
        protected bool _isCustomBakedDetailTextures;
        [SerializeField]
        private Terrain _terrain;

        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private DetailScatterMode _detailScatterMode;
#if UNITY_EDITOR
        [NonSerialized]
        internal float _latestDetailObjectDensity;
#endif
        #endregion Runtime Properties

        #region Initialize/Dispose

        public override void LoadTerrain()
        {
            if (_terrain == null)
            {
                _terrain = GetComponent<Terrain>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(this);
#endif
            }
            base.LoadTerrain();
        }

        public override bool LoadTerrainData()
        {
            if (!base.LoadTerrainData())
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find TerrainData for terrain: " +  _terrain.name, gameObject);
                return false;
            }

            if (_terrain.terrainData != null)
            {
                _treePrototypes = _terrain.terrainData.treePrototypes;
                DetermineTreePrototypeIndexes(TreeManager);
                _detailPrototypes = _terrain.terrainData.detailPrototypes;
                DetermineDetailPrototypeIndexes(DetailManager);
                _detailScatterMode = _terrain.terrainData.detailScatterMode;

                if (_terrain.treeDistance > 0)
                    _terrainTreeDistance = _terrain.treeDistance;
                return true;
            }
            return false;
        }

        internal override void SetTerrainDetailObjectDistance(float value)
        {
            base.SetTerrainDetailObjectDistance(value);

            if (_terrain == null) return;
            _terrain.detailObjectDistance = value;
        }

        internal override void SetTerrainTreeDistance(float value)
        {
            base.SetTerrainTreeDistance(value);

            if (_terrain == null) return;
            _terrain.treeDistance = value;
        }

        #region Create Heightmap and Detail Textures

        protected override RenderTexture LoadHeightmapTexture()
        {
            if (_terrain == null || _terrain.terrainData == null) return null;
            return _terrain.terrainData.heightmapTexture;
        }

        protected override void LoadDetailDensityTextures()
        {
            if (_terrain == null || _terrain.terrainData == null)
                return;

            _detailPrototypes = _terrain.terrainData.detailPrototypes;
            DetermineDetailPrototypeIndexes(DetailManager);
            int detailCount = DetailPrototypes == null ? 0 : DetailPrototypes.Length;
            ResizeDetailDensityTextureArray(detailCount);

            if (detailCount == 0)
                return;

            Profiler.BeginSample("GPUITerrainBuiltin.LoadDetailDensityTextures");
            _detailScatterMode = _terrain.terrainData.detailScatterMode;

            string terrainName = _terrain.terrainData.name;
            for (int i = 0; i < detailCount; i++)
            {
                CreateDetailTexture(terrainName, i);
                if (!IsReadTerrainDetails(i))
                {
                    _detailDensityTextures[i].ClearRenderTexture();
                    continue;
                }
                if (_isBakedDetailTextures && (_isCustomBakedDetailTextures || Application.isPlaying))
                    BlitBakedDetailTexture(i);
                else
                    CaptureTerrainDetailsToRenderTexture(_detailDensityTextures[i], i);
            }
            ExecuteProceduralDetails();
            if (DetailManager != null)
                DetailManager.RequireUpdate(!Application.isPlaying);

            Profiler.EndSample();
        }

        private void CaptureTerrainDetailsToRenderTexture(RenderTexture rt, int detailLayer, bool captureWithComputeDetailInstanceTransforms = false)
        {
            Profiler.BeginSample("GPUITerrainBuiltin.CaptureTerrainDetailsToRenderTexture");
            if (captureWithComputeDetailInstanceTransforms)
                GPUITerrainUtility.CaptureTerrainDetailToRenderTextureWithComputeDetailInstanceTransforms(_terrain.terrainData, detailLayer, (DetailPrototypes[detailLayer].useDensityScaling ? _terrain.detailObjectDensity : 1f) * DetailPrototypes[detailLayer].density, rt, terrainHolesSampleMode == GPUITerrainHolesSampleMode.Initialization);
            else
                GPUITerrainUtility.CaptureTerrainDetailToRenderTexture(_terrain.terrainData, detailLayer, rt, terrainHolesSampleMode == GPUITerrainHolesSampleMode.Initialization);
            Profiler.EndSample();
        }

        public void SetDetailLayer(int layer, int[,] details)
        {
            int detailCount = _terrain.terrainData.detailPrototypes.Length;
            if (layer >= detailCount)
                return;
            ResizeDetailDensityTextureArray(detailCount);
            int detailResolution = _terrain.terrainData.detailResolution;
            CreateDetailTexture(_terrain.terrainData.name, layer);

            GPUITerrainUtility.CaptureTerrainDetailToRenderTexture(_terrain.terrainData, detailResolution, details, _detailDensityTextures[layer], terrainHolesSampleMode == GPUITerrainHolesSampleMode.Initialization);
            IsDetailDensityTexturesLoaded = true;
        }

        #endregion Create Heightmap and Detail Textures

        #endregion Initialize/Dispose

        #region Update Methods

        /// <summary>
        /// Saves the runtime detail density changes to TerrainData detail layers
        /// </summary>
        [ContextMenu("Save Detail Density Changes")]
        public void SaveDetailChangesToTerrainData()
        {
            for (int i = 0; i < GetDetailTextureCount(); i++)
            {
                GPUITerrainUtility.UpdateTerrainDetailWithRenderTexture(_terrain, i, GetDetailDensityTexture(i));
            }
        }

        /// <summary>
        /// Resets the runtime detail density changes
        /// </summary>
        [ContextMenu("Reset Detail Density Changes")]
        public void ResetDetailChanges()
        {
            CreateDetailTextures();
            if (DetailManager != null)
                DetailManager.RequireUpdate();
        }

        protected override void LoadTreeInstances()
        {
            Profiler.BeginSample("GPUITerrainBuiltin.LoadTreeInstances");
            if (_terrain != null && _terrain.terrainData != null)
            {
                _treeInstances = _terrain.terrainData.treeInstances;
                if (TreeManager != null)
                    ConvertToGPUITreeData(TreeManager);
            }
            Profiler.EndSample();
        }

        #endregion Update Methods

        #region Getters / Setters

        #region Prototype Management

        public override void AddTreePrototypeToTerrain(GameObject pickerGameObject, int overwriteIndex)
        {
            base.AddTreePrototypeToTerrain(pickerGameObject, overwriteIndex);

            _terrain.terrainData.treePrototypes = _treePrototypes;
            _terrain.terrainData.RefreshPrototypes();
        }

        public override void AddDetailPrototypeToTerrain(UnityEngine.Object pickerObject, int overwriteIndex)
        {
            base.AddDetailPrototypeToTerrain(pickerObject, overwriteIndex);

            _terrain.terrainData.detailPrototypes = _detailPrototypes;
            _terrain.terrainData.RefreshPrototypes();
        }

        protected override void OnRemoveTreePrototypesAtIndexes(List<int> terrainPrototypeIndexes)
        {
            _terrain.terrainData.treeInstances = _treeInstances;
            _terrain.terrainData.treePrototypes = _treePrototypes;
            _terrain.terrainData.RefreshPrototypes();
        }

        protected override void OnRemoveDetailPrototypesAtIndexes(List<int> terrainPrototypeIndexes)
        {
            if (terrainPrototypeIndexes.Count == 0)
                return;

            DetailPrototype[] terrainDetailPrototypes = _terrain.terrainData.detailPrototypes;
            List<int[,]> newDetailLayers = new List<int[,]>();
            for (int i = 0; i < terrainDetailPrototypes.Length; i++)
            {
                if (!terrainPrototypeIndexes.Contains(i))
                {
                    newDetailLayers.Add(_terrain.terrainData.GetDetailLayer(0, 0, _terrain.terrainData.detailResolution, _terrain.terrainData.detailResolution, i));
                }
            }
            for (int i = 0; i < newDetailLayers.Count; i++)
            {
                _terrain.terrainData.SetDetailLayer(0, 0, i, newDetailLayers[i]);
            }
            _terrain.terrainData.detailPrototypes = _detailPrototypes;
            _terrain.terrainData.RefreshPrototypes();
        }

        #endregion Prototype Management

        public override int GetHeightmapResolution()
        {
            return _terrain.terrainData.heightmapResolution;
        }

        public override bool SetTerrainBounds(bool forceNew = false)
        {
            if (_terrain == null || _terrain.terrainData == null)
                return false;
            Vector3 size = _terrain.terrainData.size;
            Bounds bounds = new Bounds(size / 2f, size);

            if (forceNew || _bounds != bounds)
            {
                _bounds = bounds;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(this);
#endif
                return true;
            }
            return false;
        }

        public override Vector3 GetSize()
        {
            return _terrain.terrainData.size;
        }

        public Terrain GetTerrain()
        {
            if (_terrain == null)
                LoadTerrain();
            return _terrain;
        }

        public override float GetTerrainTreeDistance()
        {
            return _terrainTreeDistance;
        }

        public override bool IsBakedDetailTextures()
        {
            return _isBakedDetailTextures;
        }

        public override float GetDetailDensity(int prototypeIndex)
        {
            DetailPrototype detailPrototype = DetailPrototypes[prototypeIndex];
            float prototypeDensity = detailPrototype.useDensityScaling ? _terrain.detailObjectDensity : 1f;
            if (_detailScatterMode == DetailScatterMode.CoverageMode)
            {
                prototypeDensity *= _terrain.terrainData.ComputeDetailCoverage(prototypeIndex);

                int detailResolution = _terrain.terrainData.detailResolution;
                Vector3 terrainSize = GetSize();
                float terrainSizeAdjustment = math.sqrt((terrainSize.x / detailResolution) * (terrainSize.z / detailResolution));
                prototypeDensity *= terrainSizeAdjustment;

                return prototypeDensity;
            }
            return prototypeDensity * 255f;
        }

        public override Color GetWavingGrassTint()
        {
            if (_terrain == null || _terrain.terrainData == null)
                return base.GetWavingGrassTint();

            return _terrain.terrainData.wavingGrassTint;
        }

        public override void SetBakedDetailTexture(int index, Texture2D texture)
        {
            base.SetBakedDetailTexture(index, texture);
            _isBakedDetailTextures = true;
            _isCustomBakedDetailTextures = true;
        }

        public override void SetTreeInstances(TreeInstance[] treeInstances, bool applyToTerrainData = false)
        {
            base.SetTreeInstances(treeInstances, applyToTerrainData);
            if (applyToTerrainData)
            {
                TerrainCollider terrainCollider = _terrain.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                    terrainCollider.enabled = false;
                _terrain.terrainData.treeInstances = treeInstances;
                if (terrainCollider != null)
                    terrainCollider.enabled = true;
            }
        }

        protected override int GetDetailResolution()
        {
            return _terrain.terrainData.detailResolution;
        }

        public override Texture GetHolesTexture()
        {
            return _terrain.terrainData.holesTexture;
        }

        public override int GetAlphamapTextureCount()
        {
            return _terrain.terrainData.alphamapTextureCount;
        }

        public override Texture2D[] GetAlphamapTextures()
        {
            return _terrain.terrainData.alphamapTextures;
        }

        public override TerrainLayer[] GetTerrainLayers()
        {
            return _terrain.terrainData.terrainLayers;
        }

        #endregion Getters / Setters

        #region Editor Methods

#if UNITY_EDITOR

        protected virtual void Reset()
        {
            if (!Application.isPlaying && !GetComponent<Terrain>())
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Terrain Modifier components must be added to Terrains!");
                DestroyImmediate(this);
            }
            LoadTerrainData();
        }

        [NonSerialized]
        private long _lastDetailChangeTicks;
        [NonSerialized]
        private long _lastTreeChangeTicks;
        [NonSerialized]
        private static readonly long _waitForTicks = 1000;
        [NonSerialized]
        private bool _isTreesBeingModified;
        [NonSerialized]
        private bool _isDetailsBeingModified;

        private void OnTerrainChanged(TerrainChangedFlags flags)
        {
            if (Application.isPlaying)
                return;
            //Debug.Log(flags);

            bool isFlushEverything = (flags & TerrainChangedFlags.FlushEverythingImmediately) != 0;
            if (isFlushEverything)
                LoadTerrainData();

            bool isHeightmapChanged = (flags & TerrainChangedFlags.Heightmap) != 0 || (flags & TerrainChangedFlags.HeightmapResolution) != 0;
            bool isHoles = (flags & TerrainChangedFlags.Holes) != 0;

            if (IsDetailDensityTexturesLoaded && DetailManager != null && (isFlushEverything || isHoles || (flags & TerrainChangedFlags.RemoveDirtyDetailsImmediately) != 0))
            {
                _lastDetailChangeTicks = DateTime.Now.Ticks;
                if (!_isDetailsBeingModified)
                {
                    _isDetailsBeingModified = true;
                    EditorApplication.update -= DelayedCaptureTerrainDetails;
                    EditorApplication.update += DelayedCaptureTerrainDetails;
                }
            }

            if (TreeManager != null && (isFlushEverything || (flags & TerrainChangedFlags.TreeInstances) != 0))
            {
                _lastTreeChangeTicks = DateTime.Now.Ticks;
                if (!_isTreesBeingModified)
                {
                    _isTreesBeingModified = true;
                    EditorApplication.update -= DelayedCaptureTerrainTrees;
                    EditorApplication.update += DelayedCaptureTerrainTrees;
                }
            }

            if (isFlushEverything || isHeightmapChanged)
            {
                CreateHeightmapTexture();
                if (TreeManager != null && TreeManager.IsInitialized)
                    TreeManager.RequireUpdate();
                if (DetailManager != null && DetailManager.IsInitialized)
                    DetailManager.RequireUpdate();
            }
        }

        private void DelayedCaptureTerrainDetails()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= DelayedCaptureTerrainDetails;
                _isDetailsBeingModified = false;
                return;
            }
            if (DateTime.Now.Ticks - _lastDetailChangeTicks < _waitForTicks)
                return;

            try
            {
                CreateDetailTextures();

                if (DetailManager != null)
                    DetailManager.RequireUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            _isDetailsBeingModified = false;
            EditorApplication.update -= DelayedCaptureTerrainDetails;
        }

        private void DelayedCaptureTerrainTrees()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= DelayedCaptureTerrainTrees;
                _isTreesBeingModified = false;
                return;
            }
            if (DateTime.Now.Ticks - _lastTreeChangeTicks < _waitForTicks)
                return;


            if (TreeManager != null)
            {
                SetTreeManager(TreeManager); // required to match the prototype indexes
                TreeManager.RequireUpdate();
            }

            _isTreesBeingModified = false;
            EditorApplication.update -= DelayedCaptureTerrainTrees;
        }

        public void Editor_EnableBakedDetailTextures()
        {
            _isBakedDetailTextures = true;
            _isCustomBakedDetailTextures = false;
            CreateDetailTextures();
        }

        public void Editor_DeleteBakedDetailTextures()
        {
            if (!EditorUtility.DisplayDialog("Delete Baked Density Textures", "Do you wish to delete the generated detail density textures?", "Yes", "No"))
                return;
            _isBakedDetailTextures = false;
            if (_bakedDetailTextures != null)
            {
                for (int i = 0; i < _bakedDetailTextures.Length; i++)
                {
                    if (_bakedDetailTextures[i] != null && AssetDatabase.Contains(_bakedDetailTextures[i]) && _bakedDetailTextures[i].name.Contains(GPUITerrainConstants.NAME_SUFFIX_DETAILTEXTURE))
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_bakedDetailTextures[i]));
                }
                _bakedDetailTextures = null;
            }
            CreateDetailTextures();
        }

        public void Editor_SaveDetailRenderTexturesToBakedTextures()
        {
            int detailCount = _terrain.terrainData.detailPrototypes.Length;
            for (int detailLayer = 0; detailLayer < detailCount; detailLayer++)
            {
                if (_isBakedDetailTextures && !Application.isPlaying)
                {
                    string folderPath = _terrain.terrainData.GetAssetFolderPath();
                    if (string.IsNullOrEmpty(folderPath))
                        folderPath = "Assets/";
                    if (_bakedDetailTextures[detailLayer] != null && AssetDatabase.Contains(_bakedDetailTextures[detailLayer]) && _bakedDetailTextures[detailLayer].name.Contains(GPUITerrainConstants.NAME_SUFFIX_DETAILTEXTURE))
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_bakedDetailTextures[detailLayer]));
                    _bakedDetailTextures[detailLayer] = GPUITerrainUtility.SaveDetailDensityTexture(GetDetailDensityTexture(detailLayer), folderPath);
                }
            }
        }

#endif //UNITY_EDITOR

        #endregion Editor Methods
    }
}