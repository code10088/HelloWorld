// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace GPUInstancerPro.TerrainModule
{
    public abstract class GPUITerrainManager<T> : GPUIManagerWithPrototypeData<T> where T : GPUIPrototypeData, new()
    {
        #region Serialized Properties
        [SerializeField]
        private List<GPUITerrain> _gpuiTerrains;
        [SerializeField]
        protected bool _isAutoAddPrototypesBasedOnTerrains = true;
        [SerializeField]
        protected bool _isAutoAddActiveTerrainsOnInitialization = false;
        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private Dictionary<int, GPUITerrain> _activeTerrains;
        [NonSerialized]
        protected bool _isTerrainsModified;
        [NonSerialized]
        private HashSet<int> _toRemoveActiveTerrains;
        [NonSerialized]
        private Dictionary<int, GPUITerrain> _gpuiTerrainsDict;

        private const int ERROR_CODE_ADDITION = 300;
        private Predicate<GPUITerrain> _isNullTerrainPredicate;
        #endregion Runtime Properties

        #region Editor Properties
#if UNITY_EDITOR
        [SerializeField]
        protected bool _isAutoRemovePrototypesBasedOnTerrains = false;
        [NonSerialized]
        public List<GPUITerrain> editor_EditModeAdditionalTerrains;
#endif
        #endregion Editor Properties

        #region MonoBehaviour Methods

        protected override void Awake()
        {
            base.Awake();
            
            _gpuiTerrains ??= new();
#if UNITY_EDITOR
            editor_EditModeAdditionalTerrains ??= new();
#endif
            _isNullTerrainPredicate = IsNullTerrain;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!IsInitialized)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DeterminePrototypeIndexes();
                _isTerrainsModified = true;
            }
#endif

            if (_isTerrainsModified)
                ApplyTerrainModifications();

            foreach (GPUITerrain gpuiTerrain in GetActiveTerrainValues())
            {
                if (gpuiTerrain != null && gpuiTerrain.IsInitialized)
                {
                    gpuiTerrain.NotifyTransformChanges();
                }
            }
        }

        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        public override bool IsValid(bool logError = true)
        {
            if (!base.IsValid(logError))
                return false;
            if (GetTerrainCount() == 0)
            {
                errorCode = -ERROR_CODE_ADDITION - 1; // No Terrain
                return false;
            }
            return true;
        }

        public override void Initialize()
        {
            base.Initialize();
            _isNullTerrainPredicate = IsNullTerrain;
            _toRemoveActiveTerrains = new();
            _gpuiTerrainsDict = new();
            _activeTerrains = new();
            _gpuiTerrains ??= new();
#if UNITY_EDITOR
            if (editor_EditModeAdditionalTerrains == null)
                editor_EditModeAdditionalTerrains = new();
            else if (!Application.isPlaying)
                editor_EditModeAdditionalTerrains.RemoveAll(_isNullTerrainPredicate);
            else
                editor_EditModeAdditionalTerrains.Clear();
#endif
            DeterminePrototypeIndexes();
            if (_isAutoAddActiveTerrainsOnInitialization)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorApplication.delayCall -= AddActiveTerrains;
                    EditorApplication.delayCall += AddActiveTerrains;
                }
                else
#endif
                    AddActiveTerrains();
                SceneManager.sceneLoaded += AddActiveTerrains;
            }
            UpdateActiveTerrains();
        }

        public override void Dispose()
        {
            SceneManager.sceneLoaded -= AddActiveTerrains;

            if (isEnableDefaultRenderingWhenDisabled && _activeTerrains != null)
            {
                foreach (GPUITerrain gpuiTerrain in _activeTerrains.Values)
                {
                    if (gpuiTerrain != null)
                        RemoveGPUITerrainManager(gpuiTerrain);
                }
            }

            base.Dispose();
            if (!IsInitialized) return;

            _activeTerrains = null;
            _toRemoveActiveTerrains = null;
            _gpuiTerrainsDict = null;
        }

        #endregion Initialize/Dispose

        #region Add/Remove Terrain

        /// <summary>
        /// Called when a terrain is added/removed or terrain prototypes are modified
        /// </summary>
        public void OnTerrainsModified()
        {
            _isTerrainsModified = true;
        }

        private void ApplyTerrainModifications()
        {
            Profiler.BeginSample("GPUITerrainManager.ApplyTerrainModifications");
            IsValid(Application.isPlaying);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                int terrainCount = GetTerrainCount();
                for (int t = 0; t < terrainCount; t++)
                {
                    GPUITerrain gpuiTerrain = GetTerrain(t);
                    if (gpuiTerrain == null) continue;
                    gpuiTerrain.LoadTerrainData();
                }
            }

            if (_isAutoRemovePrototypesBasedOnTerrains && !Application.isPlaying)
                RemoveUnusedPrototypes();
#endif
            if (_isAutoAddPrototypesBasedOnTerrains)
                AddMissingPrototypes();
                
            UpdateActiveTerrains();

            _isTerrainsModified = false;
            Profiler.EndSample();
        }

        private void UpdateActiveTerrains()
        {
            if (!IsInitialized)
                return;

            Profiler.BeginSample("GPUITerrainManager.UpdateActiveTerrains");

            _gpuiTerrainsDict.Clear();
            foreach (GPUITerrain gpuiTerrain in _gpuiTerrains)
            {
                if (gpuiTerrain != null)
                    _gpuiTerrainsDict[gpuiTerrain.GetInstanceID()] = gpuiTerrain;
            }
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
            {
                foreach (GPUITerrain gpuiTerrain in editor_EditModeAdditionalTerrains)
                {
                    if (gpuiTerrain != null)
                        _gpuiTerrainsDict[gpuiTerrain.GetInstanceID()] = gpuiTerrain;
                }
            }
#endif

            bool activeTerrainsModified = false;
            foreach (var activeTerrainKV in _activeTerrains)
            {
                if (_toRemoveActiveTerrains.Contains(activeTerrainKV.Key)) continue;
                if (activeTerrainKV.Value == null)
                    _toRemoveActiveTerrains.Add(activeTerrainKV.Key);
                else if (!_gpuiTerrainsDict.ContainsKey(activeTerrainKV.Key))
                {
                    _toRemoveActiveTerrains.Add(activeTerrainKV.Key);
                    RemoveGPUITerrainManager(activeTerrainKV.Value);
                }
            }
            foreach (int key in _toRemoveActiveTerrains)
            {
                if (_activeTerrains.ContainsKey(key))
                {
                    _activeTerrains.Remove(key);
                    activeTerrainsModified = true;
                }
            }
            _toRemoveActiveTerrains.Clear();

            foreach (var gpuiTerrainKV in _gpuiTerrainsDict)
            {
                if (!_activeTerrains.ContainsKey(gpuiTerrainKV.Key))
                {
                    _activeTerrains.Add(gpuiTerrainKV.Key, gpuiTerrainKV.Value);
                    SetGPUITerrainManager(gpuiTerrainKV.Value);
                    activeTerrainsModified = true;
                }
            }

            if (activeTerrainsModified)
                RequireUpdate();
            Profiler.EndSample();
        }

        public void RemoveNullTerrains()
        {
            if (_isNullTerrainPredicate == null)
                _isNullTerrainPredicate = IsNullTerrain;
            _gpuiTerrains.RemoveAll(_isNullTerrainPredicate);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(gameObject);
#endif
            OnTerrainsModified();
        }

        public void AddTerrains(IEnumerable<Terrain> terrains)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                    AddTerrain(terrain);
            }
        }

        public void AddTerrains(IEnumerable<GPUITerrain> terrains)
        {
            if (terrains != null)
            {
                foreach (var terrain in terrains)
                    AddTerrain(terrain);
            }
        }

        public bool AddTerrain(Terrain terrain)
        {
            if (terrain == null) return false;
            if (_gpuiTerrains.Count == 0
#if UNITY_EDITOR
                && !HasEditModeAdditionalTerrains()
#endif
                )
                OnFirstTerrainAdded(terrain);
            return AddTerrain(terrain.AddOrGetComponent<GPUITerrainBuiltin>());
        }

        public bool AddTerrain(GPUITerrain terrain)
        {
            if (terrain == null)
                return false;
            else if (_gpuiTerrains.Contains(terrain))
                return false;

#if UNITY_EDITOR
            if (!Application.isPlaying && (terrain.gameObject.scene != gameObject.scene || terrain.gameObject.HasComponent<GPUITerrain.GPUITerrainPaintingProxy>())) // Do not add terrains from other scenes to avoid Scene mismatch.
            {
                editor_EditModeAdditionalTerrains ??= new();
                if (editor_EditModeAdditionalTerrains.Contains(terrain))
                    return false;
                else
                    editor_EditModeAdditionalTerrains.Add(terrain);
            }
            else
            {
#endif
                _gpuiTerrains.Add(terrain);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(gameObject);
            }
#endif
            OnTerrainsModified();
            return true;
        }

        private void AddActiveTerrains(Scene arg0, LoadSceneMode arg1)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall -= AddActiveTerrains;
                EditorApplication.delayCall += AddActiveTerrains;
            }
            else
#endif
                AddActiveTerrains();
        }

        public void AddActiveTerrains()
        {
            AddTerrains(Terrain.activeTerrains);
        }

        public bool RemoveTerrain(Terrain terrain)
        {
            if (terrain.TryGetComponent(out GPUITerrainBuiltin gpuiTerrain))
                return RemoveTerrain(gpuiTerrain);
            return false;
        }

        public bool RemoveTerrain(GPUITerrain gpuiTerrain)
        {
            if (IsInitialized)
                _toRemoveActiveTerrains.Add(gpuiTerrain.GetInstanceID()); // we need to reset the data before waiting for update, the terrain can be changed and readded until update is executed
            int terrainIndex = _gpuiTerrains.IndexOf(gpuiTerrain);
            if (terrainIndex >= 0)
                return Internal_RemoveTerrainAtIndex(terrainIndex);
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
            {
                terrainIndex = editor_EditModeAdditionalTerrains.IndexOf(gpuiTerrain);
                if (terrainIndex >= 0)
                {
                    editor_EditModeAdditionalTerrains.RemoveAt(terrainIndex);
                    OnTerrainsModified();
                    return true;
                }
            }
#endif
            return false;
        }

        public bool RemoveTerrainAtIndex(int index)
        {
            if (index >= 0 && _gpuiTerrains.Count > index)
                return Internal_RemoveTerrainAtIndex(index);
            return false;
        }

        private bool Internal_RemoveTerrainAtIndex(int index)
        {
            _gpuiTerrains.RemoveAt(index);
            OnTerrainsModified();
            return true;
        }

        public bool ContainsTerrains(IEnumerable<Terrain> terrains)
        {
            if (terrains == null)
                return true;
            foreach (var terrain in terrains)
            {
                if (!ContainsTerrain(terrain))
                    return false;
            }
            return true;
        }

        public bool ContainsTerrains(IEnumerable<GPUITerrain> gpuiTerrains)
        {
            if (gpuiTerrains == null)
                return true;
            foreach (var terrain in gpuiTerrains)
            {
                if (!ContainsTerrain(terrain))
                    return false;
            }
            return true;
        }

        public bool ContainsTerrain(Terrain terrain)
        {
            if (terrain == null)
                return true;
            foreach (var gpuiTerrain in _gpuiTerrains)
                if (gpuiTerrain != null && gpuiTerrain is GPUITerrainBuiltin gpuiTerrainBuiltin && gpuiTerrainBuiltin.GetTerrain() == terrain)
                    return true;
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
            {
                foreach (var gpuiTerrain in editor_EditModeAdditionalTerrains)
                    if (gpuiTerrain != null && gpuiTerrain is GPUITerrainBuiltin gpuiTerrainBuiltin && gpuiTerrainBuiltin.GetTerrain() == terrain)
                        return true;
            }
#endif
            return false;
        }

        public bool ContainsTerrain(GPUITerrain gpuiTerrain)
        {
            if (gpuiTerrain == null || _gpuiTerrains.Contains(gpuiTerrain))
                return true;
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains() && editor_EditModeAdditionalTerrains.Contains(gpuiTerrain))
                return true;
#endif
            return false;
        }

        protected virtual void OnFirstTerrainAdded(Terrain terrain) { }

        #endregion Add/Remove Terrain

        #region Prototype Changes

        /// <summary>
        /// Removes unused prototypes and adds missing prototypes based on the terrain prototypes.
        /// </summary>
        public void ResetPrototypesFromTerrains()
        {
            int terrainCount = GetTerrainCount();
            if (terrainCount == 0) return;
            bool isInitialized = IsInitialized;
            Dispose();
            DeterminePrototypeIndexes();
            RemoveUnusedPrototypes();
            AddMissingPrototypes();
            if (isInitialized)
                Initialize();
        }

        /// <summary>
        /// Removes prototypes from the Manager that does not have a corresponding prototype on the Manager.
        /// </summary>
        /// <returns>True if at least one prototype is removed.</returns>
        private bool RemoveUnusedPrototypes()
        {
            int prototypeCount = GetPrototypeCount();
            bool prototypesModified = false;
            if (prototypeCount > 0)
            {
                for (int p = 0; p < prototypeCount; p++)
                {
                    if (!PrototypeHasMatchOnTerrains(p))
                    {
                        RemovePrototypeAtIndex(p);
                        p--;
                        prototypeCount--;
                        prototypesModified = true;
                    }
                }
            }
            if (prototypesModified)
                DeterminePrototypeIndexes();
            return prototypesModified;
        }

        /// <summary>
        /// Adds prototypes that are on the terrain but does not have a corresponding prototype on the Manager.
        /// </summary>
        /// <returns>True if at least one prototype is added.</returns>
        private bool AddMissingPrototypes()
        {
            bool prototypesModified = false;
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain == null) continue;
                prototypesModified |= AddMissingPrototypesFromTerrain(gpuiTerrain);
            }
            if (prototypesModified)
                DeterminePrototypeIndexes();
            return prototypesModified;
        }

        /// <summary>
        /// Adds prototypes that are on the terrain but does not have a corresponding prototype on the Manager.
        /// Returns true when at least one prototype is added.
        /// </summary>
        protected abstract bool AddMissingPrototypesFromTerrain(GPUITerrain gpuiTerrain);

        /// <summary>
        /// Returns true when the prototype on the given index has a match on one of the terrains
        /// </summary>
        private bool PrototypeHasMatchOnTerrains(int prototypeIndex)
        {
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain == null) continue;
                if (GetTerrainPrototypeIndex(gpuiTerrain, prototypeIndex) >= 0)
                    return true;
            }
            return false;
        }

        private void DeterminePrototypeIndexes()
        {
            BeginDeterminePrototypeIndexes();
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain == null) continue;
                DeterminePrototypeIndexes(gpuiTerrain);
            }
        }

        protected virtual void BeginDeterminePrototypeIndexes() { }

        /// <summary>
        /// Calls either the <see cref="GPUITerrain.DetermineTreePrototypeIndexes"/> or the <see cref="GPUITerrain.DetermineDetailPrototypeIndexes"/> method based on the manager type.
        /// </summary>
        protected abstract void DeterminePrototypeIndexes(GPUITerrain gpuiTerrain);

        /// <summary>
        /// Calls either the <see cref="GPUITerrain.SetTreeManager"/> or the <see cref="GPUITerrain.SetDetailManager"/> method based on the manager type.
        /// </summary>
        protected abstract void SetGPUITerrainManager(GPUITerrain gpuiTerrain);

        /// <summary>
        /// Calls either the <see cref="GPUITerrain.RemoveTreeManager"/> or the <see cref="GPUITerrain.RemoveDetailManager"/> method based on the manager type.
        /// </summary>
        protected abstract void RemoveGPUITerrainManager(GPUITerrain gpuiTerrain);

        /// <summary>
        /// Returns either the <see cref="GPUITerrain.TreePrototypeIndexes"/> or the <see cref="GPUITerrain.DetailPrototypeIndexes"/> array based on the manager type.
        /// </summary>
        protected abstract int[] GetTerrainPrototypeIndexes(GPUITerrain gpuiTerrain);

        protected int GetTerrainPrototypeIndex(GPUITerrain gpuiTerrain, int managerPrototypeIndex)
        {
            int[] terrainPrototypeIndexes = GetTerrainPrototypeIndexes(gpuiTerrain);
            if (terrainPrototypeIndexes == null) return -1;

            for (int i = 0; i < terrainPrototypeIndexes.Length; i++)
            {
                if (terrainPrototypeIndexes[i] == managerPrototypeIndex)
                    return i;
            }
            return -1;
        }

        public override void OnPrototypeEnabledStatusChanged(int prototypeIndex, bool isEnabled)
        {
            base.OnPrototypeEnabledStatusChanged(prototypeIndex, isEnabled);
            RequireUpdate();
        }

        public override void RemovePrototypeAtIndex(int index)
        {
            base.RemovePrototypeAtIndex(index);
            DeterminePrototypeIndexes();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }

        #endregion Prototype Changes

        #region Getters/Setters

        public abstract void RequireUpdate();

        public int GetTerrainCount()
        {
            int count = _gpuiTerrains.Count;
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
                count += editor_EditModeAdditionalTerrains.Count;
#endif
            return count;
        }

        public void ReloadTerrains()
        {
            foreach (GPUITerrain gpuiTerrain in _gpuiTerrains)
            {
                if (gpuiTerrain == null) continue;
                gpuiTerrain.LoadTerrainData();
            }
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
            {
                foreach (GPUITerrain gpuiTerrain in editor_EditModeAdditionalTerrains)
                {
                    if (gpuiTerrain == null) continue;
                    gpuiTerrain.LoadTerrainData();
                }
            }
#endif
        }

        public int GetActiveTerrainCount()
        {
            return _activeTerrains.Count;
        }

        public GPUITerrain GetTerrain(int terrainIndex)
        {
            if (terrainIndex < _gpuiTerrains.Count)
                return _gpuiTerrains[terrainIndex];
#if UNITY_EDITOR
            if (HasEditModeAdditionalTerrains())
            {
                int additionalTerrainIndex = terrainIndex - _gpuiTerrains.Count;
                if (additionalTerrainIndex < editor_EditModeAdditionalTerrains.Count)
                    return editor_EditModeAdditionalTerrains[additionalTerrainIndex];
            }
#endif
            return null;
        }

        public Dictionary<int, GPUITerrain>.ValueCollection GetActiveTerrainValues()
        {
            return _activeTerrains.Values;
        }
        
#if UNITY_EDITOR
        public bool HasEditModeAdditionalTerrains()
        {
            if (Application.isPlaying || editor_EditModeAdditionalTerrains == null)
                return false;
            if (_isNullTerrainPredicate == null)
                _isNullTerrainPredicate = IsNullTerrain;
            editor_EditModeAdditionalTerrains.RemoveAll(_isNullTerrainPredicate);
            return editor_EditModeAdditionalTerrains != null && editor_EditModeAdditionalTerrains.Count > 0;
        }
#endif

        private bool IsNullTerrain(GPUITerrain t)
        {
            return t == null;
        }

        public IEnumerable<TerrainLayer> GetAllTerrainLayers()
        {
            if (_gpuiTerrains == null || _gpuiTerrains.Count == 0)
                return null;
            List<TerrainLayer> result = new List<TerrainLayer>();
            foreach (var gpuiTerrain in _gpuiTerrains)
            {
                if (gpuiTerrain == null)
                    continue;
                TerrainLayer[] terrainLayers = gpuiTerrain.GetTerrainLayers();
                if (terrainLayers == null || terrainLayers.Length == 0)
                    continue;
                foreach (var terrainLayer in terrainLayers)
                {
                    if (!result.Contains(terrainLayer))
                        result.Add(terrainLayer);
                }
            }

            return result;
        }

        #endregion Getters/Setters
    }
}
