// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(200)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#The_Tree_Manager")]
    public class GPUITreeManager : GPUITerrainManager<GPUITreePrototypeData>
    {
        #region Serialized Properties
        [SerializeField]
        internal bool _enableTreeInstanceColors;
        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private bool _requireUpdate;
        [NonSerialized]
        private int[] _treeInstanceCounts;
        [NonSerialized]
        private GPUITransformBufferData[] _treeTransformBuffers;
        [NonSerialized]
        private int[] _treeTransformBufferStartIndexes;
        [NonSerialized]
        private GPUIDataBuffer<GPUICounterData> _counterDataBuffer;
        [NonSerialized]
        private bool _reloadTreeInstances;

        private const int ERROR_CODE_ADDITION = 500;
        private static readonly List<string> TREE_INSTANCE_COLORS_SHADER_KEYWORDS = new List<string>() { GPUITerrainConstants.Kw_GPUI_TREE_INSTANCE_COLOR };
        #endregion Runtime Properties

        #region MonoBehaviour Methods

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
                if (gpuiTerrain != null && gpuiTerrain.TreePrototypes != null && gpuiTerrain.TreePrototypes.Length > 0)
                {
                    hasTerrainPrototype = true;
                    break;
                }
            }
            if (!hasTerrainPrototype)
            {
                errorCode = -ERROR_CODE_ADDITION - 2; // No tree prototypes on the terrain
                return false;
            }

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            int prototypeCount = _prototypes.Length;
            _treeInstanceCounts = new int[prototypeCount];
            _treeTransformBuffers = new GPUITransformBufferData[prototypeCount];
            _treeTransformBufferStartIndexes = new int[prototypeCount];

            _counterDataBuffer = new GPUIDataBuffer<GPUICounterData>("Tree Counter Buffer", prototypeCount);

            GPUIRenderingSystem.Instance.OnPreCull -= UpdateTreeMatrices;
            GPUIRenderingSystem.Instance.OnPreCull += UpdateTreeMatrices;

            if (GPUITerrain._terrainsSearchingForTreeManager != null)
            {
                AddTerrains(GPUITerrain._terrainsSearchingForTreeManager);
                GPUITerrain._terrainsSearchingForTreeManager.Clear();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _treeInstanceCounts = null;
            _treeTransformBuffers = null;
            _treeTransformBufferStartIndexes = null;

            if (_counterDataBuffer != null)
            {
                _counterDataBuffer.Dispose();
                _counterDataBuffer = null;
            }

            if (GPUIRenderingSystem.IsActive) 
                GPUIRenderingSystem.Instance.OnPreCull -= UpdateTreeMatrices;
        }

        #endregion Initialize/Dispose

        #region UpdateTreeMatrices

        private void UpdateTreeMatrices(GPUICameraData cameraData)
        {
            UpdateTreeMatrices();
        }

        private void UpdateTreeMatrices()
        {
            if (!_requireUpdate || !GPUIRenderingSystem.IsActive || !IsInitialized)
                return;
            _requireUpdate = false;

            int prototypeCount = _prototypes.Length;
            if (prototypeCount == 0)
                return;

            Profiler.BeginSample("GPUITreeManager.UpdateTreeMatrices");

            if (_treeInstanceCounts.Length != prototypeCount)
                _treeInstanceCounts = new int[prototypeCount];
            if(_counterDataBuffer.Length != prototypeCount)
                _counterDataBuffer.Resize(prototypeCount);
            _counterDataBuffer.UpdateBufferData(true); // to make sure counter is set to 0
            int maxTreeDataSize = 0;
            foreach (GPUITerrain gpuiTerrain in GetActiveTerrainValues())
            {
                if (!IsRenderTerrainTrees(gpuiTerrain)) continue;
                int[] prototypeIndexes = GetTerrainPrototypeIndexes(gpuiTerrain);
                if (prototypeIndexes == null) continue;
                TreeInstance[] treeData = gpuiTerrain.GetTreeInstances(_reloadTreeInstances);
                maxTreeDataSize = Mathf.Max(maxTreeDataSize, treeData.Length);

                for (int i = 0; i < treeData.Length; i++)
                {
                    int treeDataPrototypeIndex = treeData[i].prototypeIndex;
                    if (treeDataPrototypeIndex >= prototypeIndexes.Length)
                        continue;
                    int prototypeIndex = prototypeIndexes[treeDataPrototypeIndex];
                    if (prototypeIndex >= 0 && prototypeIndex < prototypeCount)
                        _treeInstanceCounts[prototypeIndex]++;
                }
            }
            _reloadTreeInstances = false;

            if (_treeTransformBuffers.Length != prototypeCount)
                _treeTransformBuffers = new GPUITransformBufferData[prototypeCount];
            if (_treeTransformBufferStartIndexes.Length != prototypeCount)
                _treeTransformBufferStartIndexes = new int[prototypeCount];
            for (int i = 0; i < prototypeCount; i++)
            {
                if (!_prototypes[i].isEnabled || _runtimeRenderKeys[i] == 0)
                {
                    _treeTransformBuffers[i] = null;
                    continue;
                }
                int instanceCount = _treeInstanceCounts[i];
                GPUIRenderingSystem.SetBufferSize(_runtimeRenderKeys[i], instanceCount, false);
                GPUIRenderingSystem.SetInstanceCount(_runtimeRenderKeys[i], instanceCount);
                _prototypeDataArray[i]._treeInstanceDataBuffer?.Release();
                if (instanceCount > 0)
                {
                    if (_enableTreeInstanceColors)
                        _prototypeDataArray[i]._treeInstanceDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, 4 * 4);
                    if (GPUIRenderingSystem.TryGetTransformBufferData(_runtimeRenderKeys[i], out _treeTransformBuffers[i], out _treeTransformBufferStartIndexes[i], out _))
                        _treeTransformBuffers[i].GetTransformBuffer()?.CompleteAsyncRequests();
                    else
                        Debug.LogError(GPUIConstants.LOG_PREFIX + "Tree Manager can not find transform buffer for prototype: " + _prototypes[i]);
                }
                else
                    _treeTransformBuffers[i] = null;
            }

            ComputeShader cs = GPUITerrainConstants.CS_TerrainTreeGenerator;
            if (_enableTreeInstanceColors)
            {
                cs.EnableKeyword(GPUITerrainConstants.Kw_GPUI_TREE_INSTANCE_COLOR);

                for (int i = 0; i < prototypeCount; i++)
                {
                    int instanceCount = _treeInstanceCounts[i];
                    if (instanceCount > 0)
                    {
                        if (GPUIRenderingSystem.TryGetRenderSourceGroup(_runtimeRenderKeys[i], out GPUIRenderSourceGroup rsg))
                            rsg.AddMaterialPropertyOverride(GPUITerrainConstants.PROP_gpuiTreeInstanceDataBuffer, _prototypeDataArray[i]._treeInstanceDataBuffer, -1, -1, true);
                    }
                }
            }
            else
                cs.DisableKeyword(GPUITerrainConstants.Kw_GPUI_TREE_INSTANCE_COLOR);

            if (maxTreeDataSize > 0)
            {
                GraphicsBuffer treeDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxTreeDataSize, System.Runtime.InteropServices.Marshal.SizeOf(typeof(TreeInstance)));
                List<int> terrainPrototypeIndexes = new List<int>();

                foreach (GPUITerrain gpuiTerrain in GetActiveTerrainValues())
                {
                    if (!IsRenderTerrainTrees(gpuiTerrain) || gpuiTerrain.TreePrototypeIndexes == null) continue;

                    TreeInstance[] treeInstances = gpuiTerrain.GetTreeInstances();
                    int bufferSize = treeInstances.Length;
                    if (bufferSize == 0)
                        continue;
                    treeDataBuffer.SetData(treeInstances);

                    Vector3 terrainSize = gpuiTerrain.GetSize();
                    Vector3 terrainPosition = gpuiTerrain.GetPosition();
                    bool isSampleTerrainHoles = gpuiTerrain.terrainHolesSampleMode == GPUITerrain.GPUITerrainHolesSampleMode.Runtime;
                    Texture holesTexture = gpuiTerrain.GetHolesTexture();
                    bool hasMatrixOffset = gpuiTerrain.HasMatrixOffset();
                    Matrix4x4 matrixOffset = gpuiTerrain.GetMatrixOffset();

                    for (int p = 0; p < prototypeCount; p++)
                    {
                        GPUITransformBufferData transformBufferData = _treeTransformBuffers[p];
                        if (transformBufferData == null)
                            continue;
                        GPUIShaderBuffer transformShaderBuffer = transformBufferData.GetTransformBuffer();
                        if (transformShaderBuffer == null || transformShaderBuffer.Buffer == null)
                            continue;

                        gpuiTerrain.GetTerrainTreePrototypeIndexes(p, ref terrainPrototypeIndexes);
                        foreach (int terrainPrototypeIndex in terrainPrototypeIndexes)
                        {
                            int transformBufferStartIndex = _treeTransformBufferStartIndexes[p];

                            cs.SetBuffer(0, GPUIConstants.PROP_gpuiTransformBuffer, transformShaderBuffer.Buffer);
                            cs.SetBuffer(0, GPUITerrainConstants.PROP_treeData, treeDataBuffer);
                            cs.SetBuffer(0, GPUIConstants.PROP_counterBuffer, _counterDataBuffer);
                            if (_enableTreeInstanceColors)
                                cs.SetBuffer(0, GPUITerrainConstants.PROP_gpuiTreeInstanceDataBuffer, _prototypeDataArray[p]._treeInstanceDataBuffer);
                            cs.SetInt(GPUIConstants.PROP_bufferSize, bufferSize);
                            cs.SetInt(GPUIConstants.PROP_transformBufferStartIndex, transformBufferStartIndex);
                            cs.SetInt(GPUIConstants.PROP_prototypeIndex, p);
                            cs.SetInt(GPUITerrainConstants.PROP_terrainPrototypeIndex, terrainPrototypeIndex);
                            cs.SetVector(GPUITerrainConstants.PROP_terrainSize, terrainSize);
                            cs.SetVector(GPUITerrainConstants.PROP_terrainPosition, terrainPosition);
                            cs.SetVector(GPUITerrainConstants.PROP_prefabScale, _prototypes[p].prefabObject.transform.localScale);
                            cs.SetBool(GPUITerrainConstants.PROP_applyPrefabScale, _prototypeDataArray[p].isApplyPrefabScale);
                            cs.SetBool(GPUITerrainConstants.PROP_applyRotation, _prototypeDataArray[p].isApplyRotation);
                            cs.SetBool(GPUITerrainConstants.PROP_applyHeight, _prototypeDataArray[p].isApplyHeight);

                            if (isSampleTerrainHoles && holesTexture != null)
                            {
                                cs.EnableKeyword(GPUITerrainConstants.Kw_GPUI_TERRAIN_HOLES);
                                cs.SetTexture(0, GPUITerrainConstants.PROP_terrainHoleTexture, holesTexture);
                            }
                            else
                                cs.DisableKeyword(GPUITerrainConstants.Kw_GPUI_TERRAIN_HOLES);

                            if (hasMatrixOffset)
                            {
                                cs.EnableKeyword(GPUIConstants.Kw_GPUI_TRANSFORM_OFFSET);
                                cs.SetMatrix(GPUIConstants.PROP_gpuiTransformOffset, matrixOffset);
                            }
                            else
                                cs.DisableKeyword(GPUIConstants.Kw_GPUI_TRANSFORM_OFFSET);

                            cs.DispatchX(0, bufferSize);
                        }

                        transformBufferData.OnTransformDataModified(transformShaderBuffer);
                        transformBufferData.ResetPreviousFrameBuffer();
                    }
                }

                treeDataBuffer.Dispose();
            }

            for (int i = 0; i < _treeInstanceCounts.Length; i++)
                _treeInstanceCounts[i] = 0;

            OnLightProbesUpdated();

            Profiler.EndSample();
        }

        private bool IsRenderTerrainTrees(GPUITerrain gpuiTerrain)
        {
            return gpuiTerrain != null && gpuiTerrain.isActiveAndEnabled
#if UNITY_EDITOR
                && !gpuiTerrain.editor_IsDisableTreeRendering
#endif
                ;
        }

        protected override void OnUpdatePerInstanceLightProbes(int prototypeIndex)
        {
            if (GPUIRenderingSystem.TryGetTransformBufferData(_runtimeRenderKeys[prototypeIndex], out var transformBufferData, out int bufferStartIndex, out int bufferSize, false))
            {
                var shaderBuffer = transformBufferData.GetTransformBuffer();
                if (shaderBuffer == null)
                    return;
//#if GPUIPRO_DEVMODE
//                Debug.Log(GPUIConstants.LOG_PREFIX + "TreeManager.OnUpdatePerInstanceLightProbes " + prototypeIndex);
//#endif
                shaderBuffer.CompleteAsyncRequests();
                shaderBuffer.AsyncRequestIntoNativeArray((matrices) =>
                {
//#if GPUIPRO_DEVMODE
//                    Debug.Log(GPUIConstants.LOG_PREFIX + "TreeManager.CalculateInterpolatedLightAndOcclusionProbes " + prototypeIndex);
//#endif
                    transformBufferData.CalculateInterpolatedLightAndOcclusionProbes(matrices, 0, bufferStartIndex, bufferSize);
                    matrices.Dispose();
                });
            }
        }

        #endregion UpdateTreeMatrices

        #region Prototype Changes

        protected override bool AddMissingPrototypesFromTerrain(GPUITerrain gpuiTerrain)
        {
            bool prototypeAdded = false;
            TreePrototype[] treePrototypes = gpuiTerrain.TreePrototypes;
            int[] terrainPrototypeIndexes = GetTerrainPrototypeIndexes(gpuiTerrain);
            for (int i = 0; i < terrainPrototypeIndexes.Length; i++)
            {
                prototypeAdded |= terrainPrototypeIndexes[i] < 0 && AddTreePrototype(treePrototypes[i]) >= 0;
            }

            return prototypeAdded;
        }

        protected override void SetGPUITerrainManager(GPUITerrain gpuiTerrain)
        {
            gpuiTerrain.SetTreeManager(this);
        }


        protected override void RemoveGPUITerrainManager(GPUITerrain gpuiTerrain)
        {
            if (gpuiTerrain.TreeManager == this)
                gpuiTerrain.RemoveTreeManager();
        }

        internal int DetermineTreePrototypeIndex(TreePrototype treePrototype)
        {
            if (_prototypes != null)
            {
                for (int p = 0; p < _prototypes.Length; p++)
                {
                    GPUIPrototype prototype = _prototypes[p];
                    if (treePrototype.prefab == prototype.prefabObject)
                        return p;
                }
            }
            if (_isAutoAddPrototypesBasedOnTerrains)
                _isTerrainsModified = true;
            return -1;
        }

        protected override void DeterminePrototypeIndexes(GPUITerrain gpuiTerrain)
        {
            gpuiTerrain.DetermineTreePrototypeIndexes(this);
        }

        protected override int[] GetTerrainPrototypeIndexes(GPUITerrain gpuiTerrain)
        {
            if (gpuiTerrain.TreePrototypes == null)
                gpuiTerrain.LoadTerrainData();
            if (gpuiTerrain.TreePrototypes != null && (gpuiTerrain.TreePrototypeIndexes == null || gpuiTerrain.TreePrototypes.Length != gpuiTerrain.TreePrototypeIndexes.Length))
                DeterminePrototypeIndexes(gpuiTerrain);
            return gpuiTerrain.TreePrototypeIndexes;
        }

        public int AddTreePrototype(TreePrototype treePrototype)
        {
            if (treePrototype == null || treePrototype.prefab == null)
                return -1;
            if (_prototypes != null)
            {
                for (int i = 0; i < _prototypes.Length; i++)
                {
                    if (_prototypes[i] != null && _prototypes[i].prefabObject == treePrototype.prefab)
                        return i;
                }
            }
            GPUITreePrototypeData treePrototypeData = new(treePrototype);

            int length = _prototypeDataArray.Length;
            Array.Resize(ref _prototypeDataArray, length + 1);
            _prototypeDataArray[length] = treePrototypeData;

            GPUIPrototype prototype = new GPUIPrototype(treePrototype.prefab, GetDefaultProfile());
            if (!treePrototype.prefab.HasComponent<LODGroup>() || treePrototype.prefab.HasComponentInChildren<BillboardRenderer>())
                prototype.isGenerateBillboard = true;
            int index = AddPrototype(prototype);
            OnNewPrototypeDataCreated(length);
            return index;
        }

        public void RemoveTreePrototypeAtIndex(int index, bool removeFromTerrain)
        {
            if (removeFromTerrain)
            {
                int terrainCount = GetTerrainCount();
                for (int t = 0; t < terrainCount; t++)
                {
                    GPUITerrain gpuiTerrain = GetTerrain(t);
                    if (gpuiTerrain != null)
                        gpuiTerrain.RemoveTreePrototypeAtIndex(index);
                }
            }
            RemovePrototypeAtIndex(index);
        }

        public void AddPrototypeToTerrains(GameObject pickerGameObject, int overwriteIndex)
        {
            int terrainCount = GetTerrainCount();
            for (int t = 0; t < terrainCount; t++)
            {
                GPUITerrain gpuiTerrain = GetTerrain(t);
                if (gpuiTerrain != null)
                    gpuiTerrain.AddTreePrototypeToTerrain(pickerGameObject, overwriteIndex);
            }
        }

        #endregion Prototype Changes

        #region Getters/Setters

        public override void RequireUpdate()
        {
            _requireUpdate = true;
        }

        public void RequireUpdate(bool reloadTreeInstances)
        {
            _reloadTreeInstances = reloadTreeInstances;
            RequireUpdate();
        }

        public override GPUIProfile GetDefaultProfile()
        {
            if (defaultProfile != null)
                return defaultProfile;
            return GPUITerrainConstants.DefaultTreeProfile;
        }

        public override List<string> GetShaderKeywords(int prototypeIndex)
        {
            if (_enableTreeInstanceColors)
                return TREE_INSTANCE_COLORS_SHADER_KEYWORDS;
            return base.GetShaderKeywords(prototypeIndex);
        }

        #endregion Getters/Setters
    }
}