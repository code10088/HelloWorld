// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.PrefabModule
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(100)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#The_Prefab_Manager")]
    public class GPUIPrefabManager : GPUIManagerWithPrototypeData<GPUIPrefabPrototypeData>, IGPUIInstanceTransformProvider
    {
        #region Serialized Properties
        [SerializeField]
        public bool isFindInstancesAtInitialization = true;
        #endregion Serialized Properties

        #region Runtime Properties
        private const int BUFFER_SIZE_INCREMENT = 128;
        private const int TRANSFORM_UPDATE_JOB_BATCH_SIZE = 32;
        private static List<GPUIPrefab> _instancesToAdd;
        private Predicate<GPUIPrefab> _isNullOrInstancedPredicate;
        private NativeArray<JobHandle> _jobHandles;
        private List<int> _prefabIDCheckList;
        private static readonly List<string> MATERIAL_VARIATION_SHADER_KEYWORDS = new List<string>() { GPUIPrefabConstants.Kw_GPUI_MATERIAL_VARIATION };
        private static readonly Matrix4x4 ZERO_MATRIX = Matrix4x4.zero;

#if UNITY_EDITOR
        private GPUIPrefab[] editor_PrefabInstances;
        private List<Matrix4x4> editor_Matrices;
        [SerializeField]
        private bool editor_isReadPrefabTransformsEveryUpdate = true;
        [SerializeField]
        public bool editor_enableEditModeRendering = true;
#endif
        #endregion Runtime Properties

        #region MonoBehaviour Methods

        protected override void OnEnable()
        {
            _isNullOrInstancedPredicate = IsPrefabNullOrInstanced;
            base.OnEnable();

#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= Editor_OnHierarchyChanged;
            if (!Application.isPlaying)
                EditorApplication.hierarchyChanged += Editor_OnHierarchyChanged;
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();

#if UNITY_EDITOR
            Editor_OnDisable();
            EditorApplication.hierarchyChanged -= Editor_OnHierarchyChanged;
#endif
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Editor_LateUpdate(editor_isReadPrefabTransformsEveryUpdate);
#endif

            if (!GPUIRenderingSystem.IsActive || !IsInitialized)
                return;

            AddRemoveInstances();

            StartAutoUpdateTransformJobs();
        }

        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        public override void Initialize()
        {
            base.Initialize();

            if (!GPUIRenderingSystem.IsActive || !IsInitialized)
                return;

            Dictionary<int, List<GPUIPrefab>> registeredPrefabs = new();
            for (int i = 0; i < _prototypes.Length; i++)
            {
                var instances = new List<GPUIPrefab>();
                int prefabID = GetPrefabID(i);
                if (registeredPrefabs.ContainsKey(prefabID))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "There are multiple prototypes with the same prefab ID: " + prefabID, this);
                    continue;
                }
                registeredPrefabs.Add(prefabID, instances);
                if (!isFindInstancesAtInitialization && _prototypeDataArray[i].GetRegisteredInstanceCount() > 0)
                {
                    foreach (var go in _prototypeDataArray[i].registeredInstances.prefabInstances)
                    {
                        if (go != null && go.TryGetComponent(out GPUIPrefab p) && !p.IsInstanced)
                        {
                            instances.Add(p);
                            p._isBeingAddedToThePrefabManager = true;
                        }
                    }
                }
            }

            if (isFindInstancesAtInitialization)
            {
                GPUIPrefab[] prefabInstances = FindObjectsByType<GPUIPrefab>(FindObjectsSortMode.None);
                foreach (GPUIPrefab prefabInstance in prefabInstances)
                {
                    if (prefabInstance.IsInstanced)
                        continue;
                    int prefabID = prefabInstance.GetPrefabID();
                    if (prefabID != 0 && registeredPrefabs.TryGetValue(prefabID, out List<GPUIPrefab> list) && !prefabInstance._isBeingAddedToThePrefabManager)
                    {
                        list.Add(prefabInstance);
                        prefabInstance._isBeingAddedToThePrefabManager = true;
                    }
                }
            }

            for (int i = 0; i < _prototypes.Length; i++)
            {
                GPUIPrototype prototype = _prototypes[i];
                var prototypeData = _prototypeDataArray[i];
                if (!prototype.isEnabled || !prototypeData.IsInitialized)
                    continue;
                GameObject prefabObject = prototype.prefabObject;
                if (prefabObject == null)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Prefab object reference is not set on the prototype: " + prototype, this);
                    continue;
                }

                List<GPUIPrefab> instances = registeredPrefabs[GetPrefabID(i)];
                if (instances.Count > 0)
                    AddPrefabInstances(instances, i);
            }

            GPUIRenderingSystem.Instance.OnPreCull -= ApplyTransformBufferChanges;
            GPUIRenderingSystem.Instance.OnPreCull -= GPUIMaterialVariationDataProvider.Instance.UpdateVariationBuffers;
            GPUIRenderingSystem.Instance.OnPreCull += ApplyTransformBufferChanges;
            GPUIRenderingSystem.Instance.OnPreCull += GPUIMaterialVariationDataProvider.Instance.UpdateVariationBuffers;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (GPUIRenderingSystem.IsActive)
            {
                GPUIRenderingSystem.Instance.OnPreCull -= ApplyTransformBufferChanges;
                GPUIRenderingSystem.Instance.OnPreCull -= GPUIMaterialVariationDataProvider.Instance.UpdateVariationBuffers;
            }

            if (_jobHandles.IsCreated)
                _jobHandles.Dispose();
        }

        private bool IsPrefabNullOrInstanced(GPUIPrefab p)
        {
            return p == null || p.IsInstanced;
        }

        protected override void OnPrototypeEnabled(int prototypeIndex)
        {
            base.OnPrototypeEnabled(prototypeIndex);

            List<GPUIPrefab> list = new();
            if (!isFindInstancesAtInitialization && _prototypeDataArray[prototypeIndex].GetRegisteredInstanceCount() > 0)
            {
                foreach (var go in _prototypeDataArray[prototypeIndex].registeredInstances.prefabInstances)
                {
                    if (go != null && go.TryGetComponent(out GPUIPrefab p) && !p.IsInstanced)
                    {
                        list.Add(p);
                        p._isBeingAddedToThePrefabManager = true;
                    }
                }
            }
            else
            {
                GPUIPrefab[] prefabInstances = FindObjectsByType<GPUIPrefab>(FindObjectsSortMode.None);
                int prefabID = GetPrefabID(prototypeIndex);
                foreach (GPUIPrefab prefabInstance in prefabInstances)
                {
                    if (prefabInstance.IsInstanced)
                        continue;
                    if (prefabID == prefabInstance.GetPrefabID())
                    {
                        list.Add(prefabInstance);
                        prefabInstance._isBeingAddedToThePrefabManager = true;
                    }
                }
            }
            if (list.Count > 0)
                AddPrefabInstances(list, prototypeIndex);

        }

        protected override void DisposeRenderer(int prototypeIndex)
        {
            if (_prototypeDataArray == null) return;

            var prototypeData = _prototypeDataArray[prototypeIndex];
            if (prototypeData == null || !prototypeData.IsInitialized)
                return;

#if UNITY_EDITOR
            if (GPUIRenderingSystem.playModeStateChange != PlayModeStateChange.ExitingPlayMode)
            {
#endif
                GameObject prefab = _prototypes[prototypeIndex].prefabObject;
                if (prefab != null && prototypeData.instanceTransforms != null)
                {
                    if (!isFindInstancesAtInitialization)
                    {
                        prototypeData.registeredInstances = new GPUIPrefabPrototypeData.GPUIPrefabInstances()
                        {
                            prefabInstances = new GameObject[prototypeData.instanceTransforms.Length]
                        };
                    }
                    for (int j = 0; j < prototypeData.instanceTransforms.Length; j++)
                    {
                        Transform t = prototypeData.instanceTransforms[j];
                        if (t != null)
                        {
                            if (t.TryGetComponent(out GPUIPrefab prefabInstance))
                            {
                                ClearInstancingData(prefabInstance, isEnableDefaultRenderingWhenDisabled);
                                if (!isFindInstancesAtInitialization)
                                    prototypeData.registeredInstances.prefabInstances[j] = prefabInstance.gameObject;
                            }
                        }
                    }
                }
#if UNITY_EDITOR
            }
#endif
            base.DisposeRenderer(prototypeIndex);
        }

#if UNITY_EDITOR
        public override bool CanRenderInEditMode()
        {
            return false;
        }
#endif
        #endregion Initialize/Dispose

        #region Prototype Changes

        protected override void ClearNullPrototypes()
        {
            base.ClearNullPrototypes();

#if UNITY_EDITOR
            _prefabIDCheckList ??= new();
            _prefabIDCheckList.Clear();

            for (int i = 0; i < _prototypes.Length; i++)
            {
                GPUIPrototype p = _prototypes[i];
                if (p.prefabObject.TryGetComponent(out GPUIPrefab gpuiPrefab))
                {
                    int prefabID = gpuiPrefab.GetPrefabID();
                    int previousIndex = _prefabIDCheckList.IndexOf(prefabID);
                    if (previousIndex >= 0)
                    {
                        _prototypes[previousIndex].prefabObject.GetComponent<GPUIPrefab>().Reset();
                        gpuiPrefab.Reset();
                    }
                    _prefabIDCheckList.Add(gpuiPrefab.GetPrefabID());
                }
                else
                    _prefabIDCheckList.Add(0);
            }
#endif
        }

        public override void RemovePrototypeAtIndex(int index)
        {
#if UNITY_EDITOR 
            // Revert prefabID of the variant to the original
            if (_prototypes.Length <= index)
                return;
            try // There might be errors if the GPUIPrefab components were modified manually.
            {
                GPUIPrototype prototypeToRemove = _prototypes[index];
                if (prototypeToRemove != null && prototypeToRemove.prefabObject != null)
                {
                    PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prototypeToRemove.prefabObject);
                    if (prefabAssetType == PrefabAssetType.Variant)
                    {
                        if (PrefabUtility.GetPrefabAssetType(GPUIPrefabUtility.GetCorrespondingPrefabOfVariant(prototypeToRemove.prefabObject)) != PrefabAssetType.Model 
                            && prototypeToRemove.prefabObject.TryGetComponent(out GPUIPrefab gpuiPrefab))
                        {
                            SerializedObject serializedObject = new SerializedObject(gpuiPrefab);
                            PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("_prefabID"), InteractionMode.AutomatedAction);
                            GPUIPrefabUtility.MergeAllPrefabInstances(prototypeToRemove.prefabObject);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
#endif

            base.RemovePrototypeAtIndex(index);
        }

        public override bool CanAddObjectAsPrototype(UnityEngine.Object obj)
        {
            if (base.CanAddObjectAsPrototype(obj))
            {
#if UNITY_EDITOR
                if (obj is not GameObject)
                    return false;
                if (!Application.isPlaying)
                {
                    var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                    if (prefabType != PrefabAssetType.Regular && prefabType != PrefabAssetType.Variant)
                        return false;
                }
#endif
                return true;
            }
            return false;
        }

        #endregion Prototype Changes

        #region Runtime Methods
        private bool _isAutoUpdateTransformJobsStarted;
        private unsafe void StartAutoUpdateTransformJobs()
        {
            Profiler.BeginSample("GPUIPrefabManager.StartAutoUpdateTransformJobs");
            if (!_jobHandles.IsCreated)
                _jobHandles = new NativeArray<JobHandle>(_prototypes.Length, Allocator.Persistent);
            else if (_jobHandles.Length != _prototypes.Length)
            {
                _jobHandles.Dispose();
                _jobHandles = new NativeArray<JobHandle>(_prototypes.Length, Allocator.Persistent);
            }
            for (int i = 0; i < _prototypeDataArray.Length; i++)
            {
                var prototypeData = _prototypeDataArray[i];
                if (!prototypeData.IsInitialized || !prototypeData.isAutoUpdateTransformData || !prototypeData.HasMatrixArray() || prototypeData.instanceTransforms == null || prototypeData.instanceCount <= 0)
                    continue;

                prototypeData.UpdateTransformAccessArray();

                prototypeData.autoUpdateTransformsJob.instanceCount = prototypeData.instanceCount;
                prototypeData.autoUpdateTransformsJob.zeroMatrix = ZERO_MATRIX;
                prototypeData.autoUpdateTransformsJob.p_matrixArray = prototypeData.GetMatrixArrayUnsafePtr();
                prototypeData.autoUpdateTransformsJob.p_isModifiedArray = NativeArrayUnsafeUtility.GetUnsafePtr(prototypeData.isModifiedArray);
                _jobHandles[i] = prototypeData.autoUpdateTransformsJob.ScheduleReadOnly(prototypeData.transformAccessArray, TRANSFORM_UPDATE_JOB_BATCH_SIZE);
                prototypeData.isMatrixArrayModified = true;
                if (_isAutoUpdateTransformJobsStarted) // Already started before, this is second call. We cant rely on modified indexes.
                    prototypeData.SetAllMatricesModified();
                else
                    _isAutoUpdateTransformJobsStarted = true;
            }
            _dependentJob = JobHandle.CombineDependencies(_jobHandles);
            Profiler.EndSample();
        }

        private unsafe void ApplyTransformBufferChanges()
        {
            _dependentJob.Complete();
            _isAutoUpdateTransformJobsStarted = false;

            Profiler.BeginSample("GPUIPrefabManager.ApplyTransformBufferChanges");
            for (int i = 0; i < _prototypeDataArray.Length; i++)
            {
                var prototypeData = _prototypeDataArray[i];
                if (!prototypeData.IsInitialized)
                    continue;

                if (prototypeData.isMatrixArrayModified && prototypeData.HasMatrixArray())
                {
                    Profiler.BeginSample(_prototypes[i].ToString());

                    int minModifiedIndex = prototypeData.minModifiedIndex;
                    int maxModifiedIndex = prototypeData.maxModifiedIndex;
                    bool isFullWrite = minModifiedIndex == 0 && maxModifiedIndex == prototypeData.instanceCount - 1;
                    if (prototypeData.isAutoUpdateTransformData && prototypeData.instanceCount > 0 && !isFullWrite)
                    {
                        void* p_isModifiedArray = NativeArrayUnsafeUtility.GetUnsafePtr(prototypeData.isModifiedArray);
                        for (int m = 0; m < prototypeData.instanceCount; m++)
                        {
                            if (UnsafeUtility.ReadArrayElementWithStride<int>(p_isModifiedArray, m, 4) != 0)
                            {
                                if (minModifiedIndex > m)
                                    minModifiedIndex = m;
                                if (maxModifiedIndex < m)
                                    maxModifiedIndex = m;
                            }
                        }
                    }

                    minModifiedIndex = Mathf.Max(0, minModifiedIndex);
                    maxModifiedIndex = Mathf.Min(prototypeData.instanceCount - 1, maxModifiedIndex);
                    isFullWrite = minModifiedIndex == 0 && maxModifiedIndex == prototypeData.instanceCount - 1;

                    int renderKey = _runtimeRenderKeys[i];
                    GPUIRenderingSystem.SetBufferSize(renderKey, prototypeData.GetMatrixLength(), !isFullWrite || minModifiedIndex > maxModifiedIndex);
                    if (minModifiedIndex <= maxModifiedIndex)
                    {
                        GPUIRenderingSystem.SetTransformBufferData(renderKey, prototypeData.GetTransformationMatrixArray(), minModifiedIndex, minModifiedIndex, maxModifiedIndex - minModifiedIndex + 1, false);
//#if GPUIPRO_DEVMODE
//                        Debug.Log(GPUIConstants.LOG_PREFIX + "Min: " + minModifiedIndex + " Max: " + maxModifiedIndex);
//#endif
                    }
                    GPUIRenderingSystem.SetInstanceCount(renderKey, prototypeData.instanceCount);
                    
                    Profiler.EndSample();
                    prototypeData.isMatrixArrayModified = false;
                    prototypeData.minModifiedIndex = int.MaxValue;
                    prototypeData.maxModifiedIndex = -1;
                }
                if (prototypeData.hasOptionalRenderers && prototypeData.isOptionalRendererStatusModified && prototypeData.optionalRendererStatusData.IsCreated)
                {
                    GPUIRenderingSystem.Instance.SetOptionalRendererStatusData(_runtimeRenderKeys[i], prototypeData.optionalRendererStatusData);
                    prototypeData.isOptionalRendererStatusModified = false;
                }
            }
            Profiler.EndSample();
        }

        private void ApplyTransformBufferChanges(GPUICameraData cameraData)
        {
            ApplyTransformBufferChanges();
        }

        private void AddRemoveInstances()
        {
            _dependentJob.Complete();

            Profiler.BeginSample("GPUIPrefabManager.AddRemoveInstances");

            int toAddCount = 0;
            if (_instancesToAdd != null && _instancesToAdd.Count > 0)
            {
                _instancesToAdd.RemoveAll(_isNullOrInstancedPredicate);
                toAddCount = _instancesToAdd.Count;
            }

            for (int i = 0; i < _prototypeDataArray.Length; i++)
            {
                var prototypeData = _prototypeDataArray[i];
                if (!prototypeData.IsInitialized)
                    continue;

                prototypeData.instancesToAdd.RemoveAll(_isNullOrInstancedPredicate);

                if (toAddCount > 0)
                {
                    for (int t = toAddCount - 1; t >= 0; t--)
                    {
                        GPUIPrefab instance = _instancesToAdd[t];
                        if (instance.GetPrefabID() == prototypeData.prefabID)
                        {
                            //if (!prototypeData.instancesToAdd.Contains(instance)) // too slow, instead check IsInstanced while adding
                            prototypeData.instancesToAdd.Add(instance);
                            _instancesToAdd.RemoveAtSwapBack(t);
                            toAddCount--;
                        }
                    }
                }

                int countToRemove = prototypeData.indexesToRemove.Count;
                int countToAdd = prototypeData.instancesToAdd.Count;
                int lastIndex = prototypeData.instanceCount - 1;

                // Remove Instances
                if (countToRemove > 0)
                {
                    foreach (int toRemoveIndex in prototypeData.indexesToRemove)
                    {
                        Transform toRemoveTransform = prototypeData.instanceTransforms[toRemoveIndex];
                        if (toRemoveTransform != null && toRemoveTransform.TryGetComponent(out GPUIPrefab gpuiPrefab))
                            ClearInstancingData(gpuiPrefab, false);

                        if (lastIndex != toRemoveIndex)
                        {
                            prototypeData.SetMatrix(toRemoveIndex, prototypeData.GetMatrix(lastIndex));

                            Transform lastIndexTransform = prototypeData.instanceTransforms[lastIndex];
                            if (lastIndexTransform)
                            {
                                GPUIPrefab lastIndexInstance = lastIndexTransform.GetComponent<GPUIPrefab>();
                                prototypeData.instanceTransforms[toRemoveIndex] = lastIndexTransform;
                                lastIndexInstance.SetBufferIndex(toRemoveIndex);
                                if (prototypeData.hasOptionalRenderers)
                                    prototypeData.optionalRendererStatusData[toRemoveIndex] = lastIndexInstance.optionalRendererStatus;
                            }
                        }
                        prototypeData.SetMatrix(lastIndex, ZERO_MATRIX);

                        lastIndex--;
                    }
                    prototypeData.indexesToRemove.Clear();

                    prototypeData.instanceCount = lastIndex + 1;
                    prototypeData.isMatrixArrayModified = true;
                    prototypeData.isTransformReferencesModified = true;
                    prototypeData.isOptionalRendererStatusModified = true;

                    if (countToAdd == 0 && prototypeData.instanceCount < prototypeData.GetMatrixLength() - BUFFER_SIZE_INCREMENT * 2)
                        ResizeArrays(i, (prototypeData.instanceCount / BUFFER_SIZE_INCREMENT + 1) * BUFFER_SIZE_INCREMENT);
                }

                // Add Instances
                if (countToAdd > 0)
                {
                    // Resize arrays if necessary
                    int newSize = prototypeData.instanceCount + countToAdd;
                    if (newSize > prototypeData.GetMatrixLength())
                        ResizeArrays(i, (newSize / BUFFER_SIZE_INCREMENT + 1) * BUFFER_SIZE_INCREMENT);

                    for (int j = 0; j < countToAdd; j++)
                    {
                        GPUIPrefab toAdd = prototypeData.instancesToAdd[j];
                        if (toAdd.IsInstanced || !toAdd.enabled) continue; // to avoid adding the same instance twice
                        lastIndex++;

                        SetupPrefabInstanceForInstancing(toAdd, i, lastIndex);
                    }
                    prototypeData.instancesToAdd.Clear();
                    prototypeData.instanceCount = lastIndex + 1;
                    prototypeData.isMatrixArrayModified = true;
                    prototypeData.isTransformReferencesModified = true;
                    prototypeData.isOptionalRendererStatusModified = true;
                }
            }
            Profiler.EndSample();
        }

        /// <summary>
        /// Notifies the Prefab Manager to update the transform data buffers.
        /// </summary>
        public void UpdateTransformData()
        {
            for (int i = 0; i < _prototypeDataArray.Length; i++)
            {
                var prototypeData = _prototypeDataArray[i];
                if (!prototypeData.IsInitialized)
                    continue;
                prototypeData.SetAllMatricesModified();
            }
        }

        public void UpdateTransformData(int prototypeIndex)
        {
            if (prototypeIndex < 0 || prototypeIndex >= _prototypeDataArray.Length)
            {
                Debug.Log(GPUIConstants.LOG_PREFIX + "Invalid prototype index: " + prototypeIndex + ". Current number of prototypes: " + _prototypeDataArray.Length, this);
                return;
            }
            var prototypeData = _prototypeDataArray[prototypeIndex];
            if (!prototypeData.IsInitialized)
                return;
            prototypeData.SetAllMatricesModified();
        }

        protected virtual void ClearInstancingData(GPUIPrefab gpuiPrefab, bool enableRenderers)
        {
            gpuiPrefab.ClearInstancingData(enableRenderers);
        }

        public int GetPrototypeIndex(GPUIPrefab gpuiPrefab)
        {
            return GetPrototypeIndexWithPrefabID(gpuiPrefab.GetPrefabID());
        }

        public int GetPrototypeIndexWithPrefabID(int prefabID)
        {
            if (prefabID != 0)
            {
                if (IsInitialized)
                {
                    for (int i = 0; i < _prototypes.Length; i++)
                    {
                        if (_prototypeDataArray[i].prefabID == prefabID)
                            return i;
                    }
                }
                else
                {
                    for (int i = 0; i < _prototypes.Length; i++)
                    {
                        if (_prototypes[i].prefabObject.GetComponent<GPUIPrefab>().GetPrefabID() == prefabID)
                            return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Adds the prefab instance to an existing Prefab Manager. The corresponding prefab should be defined as a prototype on the Prefab Manager.
        /// The prefab instance will be registered on the Prefab Manager with the manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        public static void AddPrefabInstance(GPUIPrefab gpuiPrefab)
        {
            if (gpuiPrefab == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not add prefab instance! Given prefab is null.");
                return;
            }

            if (gpuiPrefab.IsInstanced || gpuiPrefab._isBeingAddedToThePrefabManager)
                return;

            if (gpuiPrefab.GetPrefabID() == 0)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not add prefab instance. Unknown prefab ID! Make sure the prefab ID is not overridden or use the AddPrefabInstance(GPUIPrefab gpuiPrefab, int prototypeIndex) method with the prototypeIndex parameter.", gpuiPrefab.gameObject);
                return;
            }

            if (_instancesToAdd == null)
                _instancesToAdd = new List<GPUIPrefab>();

            //if (!_instancesToAdd.Contains(gpuiPrefab))  // too slow, instead check IsInstanced while adding
            _instancesToAdd.Add(gpuiPrefab);
            gpuiPrefab._isBeingAddedToThePrefabManager = true;
        }

        /// <summary>
        /// Adds the collection of prefab instances to an existing Prefab Manager. The corresponding prefabs should be defined as a prototype on the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instances immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="gpuiPrefabs"></param>
        public static void AddPrefabInstances(IEnumerable<GPUIPrefab> gpuiPrefabs)
        {
            if (gpuiPrefabs == null)
                return;

            if (_instancesToAdd == null)
                _instancesToAdd = new List<GPUIPrefab>();

            _instancesToAdd.AddRange(gpuiPrefabs);
        }

        /// <summary>
        /// Adds the collection of instances of a specific prefab to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instances immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="instances">Collection of instances of a specific prefab</param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns>True, if added successfully</returns>
        public bool AddPrefabInstances(IEnumerable<GPUIPrefab> instances, int prototypeIndex)
        {
            if (prototypeIndex < 0 || prototypeIndex > _prototypeDataArray.Length)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Invalid prototype index: " + prototypeIndex, gameObject);
                return false;
            }
            var prototypeData = _prototypeDataArray[prototypeIndex];
            if (prototypeData == null || !prototypeData.IsInitialized)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find runtime data at index: " + prototypeIndex, gameObject);
                return false;
            }
            prototypeData.instancesToAdd.AddRange(instances);

            return true;
        }

        /// <summary>
        /// Adds the prefab instance to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate"/> method.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns></returns>
        public bool AddPrefabInstance(GameObject go, int prototypeIndex)
        {
            if (prototypeIndex < 0 || prototypeIndex > _prototypeDataArray.Length)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype at index " + prototypeIndex + " for: " + go, go);
                return false;
            }

            return AddPrefabInstance(go.AddOrGetComponent<GPUIPrefab>(), prototypeIndex);
        }

        /// <summary>
        /// Adds the prefab instance to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate"/> method.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        public void AddPrefabInstances(IEnumerable<GameObject> instances, int prototypeIndex)
        {
            if (prototypeIndex < 0 || prototypeIndex > _prototypeDataArray.Length)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype at index " + prototypeIndex + ".");
                return;
            }

            foreach (var go in instances)
                AddPrefabInstance(go.AddOrGetComponent<GPUIPrefab>(), prototypeIndex);
        }

        /// <summary>
        /// Adds the prefab instance to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns></returns>
        public bool AddPrefabInstance(GPUIPrefab gpuiPrefab, int prototypeIndex = -1)
        {
            if (gpuiPrefab == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not add prefab instance! Given prefab is null.");
                return false;
            }

            if (gpuiPrefab.IsInstanced || gpuiPrefab._isBeingAddedToThePrefabManager)
                return true;

            if (prototypeIndex < 0 || prototypeIndex > _prototypeDataArray.Length)
            {
                prototypeIndex = GetPrototypeIndex(gpuiPrefab);
                if (prototypeIndex < 0)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype for: " + gpuiPrefab, gpuiPrefab);
                    return false;
                }
            }
            var prototypeData = _prototypeDataArray[prototypeIndex];
            if (prototypeData == null || !prototypeData.IsInitialized)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find runtime data for: " + gpuiPrefab, gpuiPrefab);
                return false;
            }

            prototypeData.instancesToAdd.Add(gpuiPrefab);
            gpuiPrefab._isBeingAddedToThePrefabManager = true;
            return true;
        }

        /// <summary>
        /// Immediately adds the prefab instance to the Prefab Manager without waiting for LateUpdate.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        /// <param name="prototypeIndex"></param>
        /// <returns></returns>
        public int AddPrefabInstanceImmediate(GPUIPrefab gpuiPrefab, int prototypeIndex = -1)
        {
            if (gpuiPrefab == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not add prefab instance! Given prefab is null.");
                return -1;
            }

            if (gpuiPrefab.IsInstanced)
                return gpuiPrefab.bufferIndex;

            if (prototypeIndex < 0)
            {
                prototypeIndex = GetPrototypeIndex(gpuiPrefab);
                if (prototypeIndex < 0)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype for: " + gpuiPrefab, gpuiPrefab);
                    return -1;
                }
            }

            _dependentJob.Complete();

            GPUIPrefabPrototypeData prefabPrototypeData = _prototypeDataArray[prototypeIndex];

            int bufferIndex = prefabPrototypeData.GetMatrixLength();

            ResizeArrays(prototypeIndex, bufferIndex + 1);
            SetupPrefabInstanceForInstancing(gpuiPrefab, prototypeIndex, bufferIndex);
            prefabPrototypeData.isTransformReferencesModified = true;
            prefabPrototypeData.isOptionalRendererStatusModified = true;
            prefabPrototypeData.isMatrixArrayModified = true;
            prefabPrototypeData.instanceCount++;

            return bufferIndex;
        }

        /// <summary>
        /// Removes the prefab instance from the Prefab Manager.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        /// <param name="prototypeIndex"></param>
        /// <returns></returns>
        public bool RemovePrefabInstance(GPUIPrefab gpuiPrefab, int prototypeIndex = -1)
        {
#if UNITY_EDITOR
            if (GPUIRenderingSystem.playModeStateChange == PlayModeStateChange.ExitingPlayMode)
                return true;
#endif
            GPUIPrefabPrototypeData prototypeData;
            if (!gpuiPrefab.IsInstanced)
            {
                if (gpuiPrefab._isBeingAddedToThePrefabManager)
                {
                    if (prototypeIndex < 0)
                        prototypeIndex = GetPrototypeIndex(gpuiPrefab);
                    prototypeData = _prototypeDataArray[prototypeIndex];
                    int index = prototypeData.instancesToAdd.IndexOf(gpuiPrefab);
                    if (index >= 0)
                    {
                        prototypeData.instancesToAdd.RemoveAt(index);
                        gpuiPrefab._isBeingAddedToThePrefabManager = false;
                        return true;
                    }
                    if (_instancesToAdd != null)
                    {
                        index = _instancesToAdd.IndexOf(gpuiPrefab);
                        if (index >= 0)
                        {
                            _instancesToAdd.RemoveAt(index);
                            gpuiPrefab._isBeingAddedToThePrefabManager = false;
                            return true;
                        }
                    }
                }
                return true;
            }

            if (prototypeIndex < 0)
            {
                prototypeIndex = GetPrototypeIndex(gpuiPrefab);
                if (prototypeIndex < 0)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype for: " + gpuiPrefab, gpuiPrefab);
                    return false;
                }
            }
            if (_prototypeDataArray == null || _prototypeDataArray.Length <= prototypeIndex)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find runtime data at index: " + prototypeIndex, gpuiPrefab);
                return false;
            }

            prototypeData = _prototypeDataArray[prototypeIndex];
            Debug.Assert(prototypeData.IsInitialized, "Runtime data is not initialized!");

            Debug.Assert(gpuiPrefab.bufferIndex >= 0, "Buffer index is negative");
            prototypeData.indexesToRemove.Add(gpuiPrefab.bufferIndex);
            return true;
        }

        /// <summary>
        /// Updates the Matrix4x4 data for the given prefab instance.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        public void UpdateTransformData(GPUIPrefab gpuiPrefab)
        {
            int prototypeIndex;
            if (gpuiPrefab.IsInstanced)
                prototypeIndex = GetPrototypeIndex(gpuiPrefab.renderKey);
            else
                prototypeIndex = GetPrototypeIndex(gpuiPrefab);
            if (prototypeIndex < 0)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find prototype for: " + gpuiPrefab, gpuiPrefab);
                return;
            }
            if (_prototypeDataArray == null || _prototypeDataArray.Length <= prototypeIndex)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find runtime data at index: " + prototypeIndex, gpuiPrefab);
                return;
            }

            _dependentJob.Complete();

            var prototypeData = _prototypeDataArray[prototypeIndex];
            Debug.Assert(prototypeData.IsInitialized, "Runtime data is not initialized!");

            if (!prototypeData.isAutoUpdateTransformData && prototypeData.HasMatrixArray() && prototypeData.GetMatrixLength() > gpuiPrefab.bufferIndex)
            {
                prototypeData.isMatrixArrayModified = true;
                Transform instanceTransform = gpuiPrefab.CachedTransform;
                prototypeData.SetMatrix(gpuiPrefab.bufferIndex, instanceTransform.localToWorldMatrix);
                instanceTransform.hasChanged = false;
            }
        }

        public virtual void SetupPrefabInstanceForInstancing(GPUIPrefab gpuiPrefab, int prototypeIndex, int bufferIndex)
        {
            Profiler.BeginSample("GPUIPrefabManager.SetupPrefabInstanceForInstancing");
            var prototypeData = _prototypeDataArray[prototypeIndex];

            Transform instanceTransform = gpuiPrefab.CachedTransform;
            if (!prototypeData.isAutoUpdateTransformData)
                prototypeData.SetMatrix(bufferIndex, instanceTransform.localToWorldMatrix);
            prototypeData.instanceTransforms[bufferIndex] = instanceTransform;
            if (prototypeData.hasOptionalRenderers)
                prototypeData.optionalRendererStatusData[bufferIndex] = gpuiPrefab.optionalRendererStatus;
            instanceTransform.hasChanged = false;

            gpuiPrefab.SetInstancingData(this, prototypeData.prefabID, _runtimeRenderKeys[prototypeIndex], bufferIndex);
            Profiler.EndSample();
        }

        protected virtual void ResizeArrays(int prototypeIndex, int newSize)
        {
            var prototypeData = _prototypeDataArray[prototypeIndex];
            prototypeData.ResizeMatrixArray(newSize);
            prototypeData.isMatrixArrayModified = true;
            if (prototypeData.instanceTransforms == null)
                prototypeData.instanceTransforms = new Transform[newSize];
            else
                Array.Resize(ref prototypeData.instanceTransforms, newSize);
            prototypeData.isTransformReferencesModified = true;
            if (prototypeData.hasOptionalRenderers)
                prototypeData.optionalRendererStatusData.ResizeNativeArray(newSize, Allocator.Persistent);
        }

        public int GetPrefabID(int prototypeIndex)
        {
            return GetPrefabID(_prototypes[prototypeIndex]);
        }

        internal static int GetPrefabID(GPUIPrototype prototype)
        {
            if (prototype.prefabObject.TryGetComponent(out GPUIPrefab gpuiPrefab))
                return gpuiPrefab.GetPrefabID();
            return prototype.GetKey();
        }

        public override int GetRegisteredInstanceCount(int prototypeIndex)
        {
            if (!Application.isPlaying && !isFindInstancesAtInitialization && _prototypeDataArray != null && _prototypeDataArray.Length > prototypeIndex && _prototypeDataArray[prototypeIndex] != null && _prototypeDataArray[prototypeIndex].registeredInstances != null && _prototypeDataArray[prototypeIndex].registeredInstances.prefabInstances != null)
                return _prototypeDataArray[prototypeIndex].registeredInstances.prefabInstances.Length;
            return base.GetRegisteredInstanceCount(prototypeIndex);
        }

        public virtual void SetPrefabInstanceRenderersEnabled(GPUIPrefab prefabInstance, bool enabled)
        {
            prefabInstance.SetRenderersEnabled(enabled);
        }

        protected override void OnUpdatePerInstanceLightProbes(int prototypeIndex)
        {
            _prototypeDataArray[prototypeIndex].SetAllMatricesModified();
        }
        #endregion Runtime Methods

        #region Getters/Setters

        public override List<string> GetShaderKeywords(int prototypeIndex)
        {
            if (_prototypes[prototypeIndex].prefabObject.HasComponent<GPUIMaterialVariationInstance>())
                return MATERIAL_VARIATION_SHADER_KEYWORDS;
            return base.GetShaderKeywords(prototypeIndex);
        }

        public int GetPrefabID(GameObject prefabObject)
        {
            return prefabObject.GetComponent<GPUIPrefab>().GetPrefabID();
        }

        public NativeArray<Matrix4x4> GetTransformMatrix(int prefabID)
        {
            _dependentJob.Complete();
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return default;
            return GetPrototypeData(prototypeIndex).GetTransformationMatrixArray();
        }

        public void SetTransformMatrixModified(int prefabID)
        {
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return;
            GetPrototypeData(prototypeIndex).SetAllMatricesModified();
        }

        public TransformAccessArray GetTransformAccessArray(int prefabID)
        {
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return default;
            var prototypeData = GetPrototypeData(prototypeIndex);
            prototypeData.UpdateTransformAccessArray();
            return prototypeData.transformAccessArray;
        }

        public Transform[] GetInstanceTransforms(int prefabID)
        {
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return null;
            var prototypeData = GetPrototypeData(prototypeIndex);
            return prototypeData.instanceTransforms;
        }

        public Transform GetInstanceTransform(int prefabID, int bufferIndex)
        {
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return null;
            var prototypeData = GetPrototypeData(prototypeIndex);
            if (prototypeData.instanceTransforms.Length <= bufferIndex)
                return null;
            return prototypeData.instanceTransforms[bufferIndex];
        }

        public Transform GetInstanceTransformWithRenderKey(int renderKey, int bufferIndex)
        {
            int prototypeIndex = GetPrototypeIndex(renderKey);
            if (prototypeIndex < 0)
                return null;
            var prototypeData = GetPrototypeData(prototypeIndex);
            if (prototypeData.instanceTransforms.Length <= bufferIndex)
                return null;
            return prototypeData.instanceTransforms[bufferIndex];
        }

        public int GetInstanceCount(int prefabID)
        {
            int prototypeIndex = GetPrototypeIndexWithPrefabID(prefabID);
            if (prototypeIndex < 0)
                return 0;
            var prototypeData = GetPrototypeData(prototypeIndex);
            return prototypeData.instanceCount;
        }

        #endregion Getters/Setters

        #region Editor Methods
#if UNITY_EDITOR
        private void Editor_LateUpdate(bool forceUpdateTransformData = false)
        {
            int prototypeCount = GetPrototypeCount();
            for (int p = 0; p < prototypeCount; p++)
            {
                var prototype = GetPrototype(p);
                var prototypeData = GetPrototypeData(p);
                if (prototype == null || prototypeData == null) continue;
                if (!editor_enableEditModeRendering || prototype.prefabObject == null || !prototype.prefabObject.TryGetComponent(out GPUIPrefab gpuiPrefab) || !gpuiPrefab.IsRenderersDisabled)
                {
                    if (prototypeData.editor_editModeRenderKey != 0)
                    {
                        GPUICoreAPI.DisposeRenderer(prototypeData.editor_editModeRenderKey);
                        prototypeData.editor_editModeRenderKey = 0;
                    }
                    continue;
                }
                bool updateTransformData = forceUpdateTransformData;
                if (prototypeData.editor_editModeRenderKey == 0)
                {
                    if (!GPUICoreAPI.RegisterRenderer(this, prototype, out prototypeData.editor_editModeRenderKey))
                        continue;
                    updateTransformData = true;
                }
                if (updateTransformData)
                {
                    if (editor_Matrices == null)
                        editor_Matrices = new();
                    else
                        editor_Matrices.Clear();

                    if (editor_PrefabInstances == null)
                        editor_PrefabInstances = FindObjectsByType<GPUIPrefab>(FindObjectsSortMode.None);
                    int prefabID = gpuiPrefab.GetPrefabID();
                    foreach (var prefabInstance in editor_PrefabInstances)
                    {
                        if (prefabInstance == null || prefabInstance.GetPrefabID() != prefabID)
                            continue;
                        editor_Matrices.Add(prefabInstance.CachedTransform.localToWorldMatrix);
                    }

                    GPUICoreAPI.SetTransformBufferData(prototypeData.editor_editModeRenderKey, editor_Matrices);
                }
            }
        }

        private void Editor_OnDisable()
        {
            editor_PrefabInstances = null;
            int prototypeCount = GetPrototypeCount();
            for (int p = 0; p < prototypeCount; p++)
            {
                var prototypeData = GetPrototypeData(p);
                if (prototypeData != null && prototypeData.editor_editModeRenderKey != 0)
                {
                    GPUICoreAPI.DisposeRenderer(prototypeData.editor_editModeRenderKey);
                    prototypeData.editor_editModeRenderKey = 0;
                }
            }
        }

        private void Editor_OnHierarchyChanged()
        {
            editor_PrefabInstances = null;
            Editor_LateUpdate(true);
        }
#endif
        #endregion Editor Methods
    }
}
