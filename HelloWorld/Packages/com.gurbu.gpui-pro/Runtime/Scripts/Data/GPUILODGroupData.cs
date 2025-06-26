// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace GPUInstancerPro
{
    [CreateAssetMenu(menuName = "Rendering/GPU Instancer Pro/LOD Group Data", order = 611)]
    [Serializable]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_LOD_Group_Data")]
    public class GPUILODGroupData : ScriptableObject, IGPUIParameterBufferData, IGPUIDisposable
    {
        public GPUILODData[] lodDataArray;
        public float[] transitionValues = new float[8];
        public float[] fadeTransitionWidth = new float[8];
        public Bounds bounds;
        public float lodGroupSize = 1f;
        public int optionalRendererCount;

        /// <summary>
        /// Prototype reference for runtime created GPUILODGroupData
        /// </summary>
        [NonSerialized]
        public GPUIPrototype prototype;
        [NonSerialized]
        public bool requiresTreeProxy;
        [NonSerialized]
        private bool _hasSkinnedMeshes;
        public bool HasSkinning { 
            get => _hasSkinnedMeshes; 
            private set 
            {
                _hasSkinnedMeshes = value;
                if (_hasSkinnedMeshes && requiresTreeProxy)
                    requiresTreeProxy = false;
            }
        }
        [NonSerialized]
        private bool _hasSkinningComponent;
        public UnityAction<GPUILODGroupData> OnRegeneratedRenderers;

        public GPUILODGroupData()
        {
            InitializeTransitionValues();
        }

        #region Array Methods
        /// <summary>
        /// LOD count
        /// </summary>
        public int Length => lodDataArray == null ? 0 : lodDataArray.Length;

        public GPUILODData this[int index]
        {
            get => lodDataArray[index];
            set => lodDataArray[index] = value;
        }

        #endregion Array Methods

        #region Create Renderers

        public static GPUILODGroupData CreateLODGroupData(GPUIPrototype prototype)
        {
            if (prototype == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not create LODGroupData. Prototype is null.");
                return null;
            }

            if (prototype.prototypeType == GPUIPrototypeType.LODGroupData)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not create LODGroupData. Prototype type is already LODGroupData.");
                return null;
            }

            GPUILODGroupData result = CreateInstance<GPUILODGroupData>();
            result.name = prototype.ToString();
            result.CreateRenderersFromPrototype(prototype);

            return result;
        }

        public static GPUILODGroupData CreateLODGroupData(GameObject prefabObject)
        {
            if (prefabObject == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not create LODGroupData. Prefab object is null.");
                return null;
            }

            GPUILODGroupData result = CreateInstance<GPUILODGroupData>();
            result.name = prefabObject.name;
            result.CreateRenderersFromGameObject(prefabObject);

            return result;
        }

        public static GPUILODGroupData CreateLODGroupData(Mesh mesh, Material[] materials, ShadowCastingMode shadowCastingMode = ShadowCastingMode.On, int layer = 0)
        {
            if (mesh == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not create LODGroupData. Mesh is null.");
                return null;
            }
            if (materials == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not create LODGroupData. Materials is null.");
                return null;
            }

            GPUILODGroupData result = CreateInstance<GPUILODGroupData>();
            result.name = mesh.name;
            result.CreateRenderersFromMeshAndMaterial(mesh, materials, shadowCastingMode, layer);

            return result;
        }

        public bool CreateRenderersFromPrototype(GPUIPrototype prototype)
        {
            if (prototype == null)
                return false;
            this.prototype = prototype;

            lodDataArray = new GPUILODData[0];

            if (prototype.prototypeType == GPUIPrototypeType.Prefab)
            {
                if (prototype.prefabObject == null)
                    return false;
                if (!requiresTreeProxy) // Add tree proxy for all prefabs that have the Tree component. We do not check for specific shaders because creating custom shaders is also possible.
                {
                    Tree[] treeComponents = prototype.prefabObject.GetComponentsInChildren<Tree>();
                    foreach (var treeComponent in treeComponents)
                    {
                        if (treeComponent != null && treeComponent.gameObject.HasComponent<MeshRenderer>() && treeComponent.gameObject.HasComponent<MeshFilter>())
                        {
                            requiresTreeProxy = true;
                            break;
                        }
                    }
                    if (!requiresTreeProxy)
                    {
                        Renderer[] rendererComponents = prototype.prefabObject.GetComponentsInChildren<Renderer>();
                        foreach (var renderer in rendererComponents)
                        {
                            foreach (var material in renderer.sharedMaterials)
                            {
                                if (material != null && material.shader != null && material.shader.name.Contains(GPUIConstants.SHADER_UNITY_SPEEDTREE))
                                {
                                    requiresTreeProxy = true;
                                    break;
                                }
                            }
                            if (requiresTreeProxy)
                                break;
                        }
                    }
                }

                if (CreateRenderersFromGameObject(prototype.prefabObject))
                {
                    if (prototype.isGenerateBillboard)
                    {
                        if (prototype.billboardAsset == null)
                            prototype.billboardAsset = GPUIBillboardUtility.FindBillboardAsset(prototype.prefabObject);
                        if (prototype.billboardAsset != null && prototype.billboardAsset.albedoAtlasTexture != null)
                        {
                            int lodCount = Length;
                            AddLOD(0);
                            if (!prototype.prefabObject.HasComponent<LODGroup>() || !prototype.isBillboardReplaceLODCulled)
                                transitionValues[lodCount - 1] = 1 - prototype.billboardDistance;

                            AddRenderer(lodCount, GPUIBillboardUtility.GenerateQuadMesh(prototype.billboardAsset), new Material[] { GPUIBillboardUtility.CreateBillboardMaterial(prototype.billboardAsset) }, Matrix4x4.identity, prototype.prefabObject.layer, ShadowCastingMode.Off, true, MotionVectorGenerationMode.Camera, false, true, 1, LightProbeUsage.BlendProbes);
                        }
                    }
                    OnRegeneratedRenderers?.Invoke(this);
                    return true;
                }
            }

            if (prototype.prototypeType == GPUIPrototypeType.MeshAndMaterial)
            {
                if (prototype.prototypeMesh == null)
                    return false;
                return CreateRenderersFromMeshAndMaterial(prototype.prototypeMesh, prototype.prototypeMaterials, ShadowCastingMode.On, prototype.layer);
            }

            return false;
        }

        /// <summary>
        /// Generates instancing renderer data for a given GameObject.
        /// </summary>
        public bool CreateRenderersFromGameObject(GameObject prefabObject)
        {
            if (prefabObject == null)
                return false;

            _hasSkinningComponent = prefabObject.HasComponent<GPUISkinningBase>();

            lodDataArray = new GPUILODData[0];

            if (prefabObject.TryGetComponent(out LODGroup lodGroup))
                return GenerateRenderersFromLODGroup(lodGroup);
            else
                return GenerateRenderersFromMeshRenderers(prefabObject);
        }

        public bool CreateRenderersFromMeshAndMaterial(Mesh mesh, Material[] materials, ShadowCastingMode shadowCastingMode, int layer)
        {
            lodDataArray = new GPUILODData[0];

            AddLOD();

            Material[] clonedMaterials = new Material[materials.Length];
            Array.Copy(materials, clonedMaterials, materials.Length);
            AddRenderer(0, mesh, clonedMaterials, Matrix4x4.identity, layer, shadowCastingMode, true, MotionVectorGenerationMode.Camera, false, false, 1, LightProbeUsage.Off);

            return true;
        }

        /// <summary>
        /// Generates all LOD and renderer data from the supplied Unity LOD Group.
        /// </summary>
        private bool GenerateRenderersFromLODGroup(LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            lodGroupSize = lodGroup.size;
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                bool hasBillboardRenderer = false;
                List<Renderer> lodRenderers = new List<Renderer>();
                LOD lod = lods[lodIndex];
                Renderer[] renderers = lod.renderers;
                if (renderers != null)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            if (renderer is MeshRenderer)
                                lodRenderers.Add(renderer);
                            else if (renderer is BillboardRenderer)
                                hasBillboardRenderer = true;
                            else if (renderer is SkinnedMeshRenderer)
                                lodRenderers.Add(renderer);
                        }
                    }
                }

                if (lodRenderers.Count == 0)
                {
                    if (!hasBillboardRenderer)
                        Debug.LogWarning(GPUIConstants.LOG_PREFIX + "LOD Group has no mesh renderers. Prefab: " + lodGroup.gameObject.name + " LODIndex: " + lodIndex, lodGroup.gameObject);
                    continue;
                }

                AddLOD(lod.screenRelativeTransitionHeight, lod.fadeTransitionWidth);

                for (int r = 0; r < lodRenderers.Count; r++)
                    AddRenderer(lodRenderers[r], lodGroup.transform, lodIndex);
            }

            return true;
        }

        /// <summary>
        /// Generates renderer data for a given game object from its Mesh renderers.
        /// </summary>
        private bool GenerateRenderersFromMeshRenderers(GameObject prefabObject)
        {
            AddLOD();

            if (!prefabObject)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can't create renderer(s): GameObject is null");
                return false;
            }

            List<Renderer> meshRenderers = new List<Renderer>();
            prefabObject.transform.GetMeshRenderers(meshRenderers, true);

            if (meshRenderers == null || meshRenderers.Count == 0)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can't create renderer(s): no MeshRenderers found in the reference GameObject <" + prefabObject.name + "> or any of its children", prefabObject);
                return false;
            }

            foreach (Renderer meshRenderer in meshRenderers)
            {
                var renderer = AddRenderer(meshRenderer, prefabObject.transform, 0);
                #region Optional Renderers
                if (renderer != null && meshRenderer.gameObject != prefabObject && meshRenderer.gameObject.TryGetComponent(out GPUIOptionalRenderer optionalRenderer))
                {
                    renderer.optionalRendererNo = optionalRenderer.optionalRendererNo;
                    if (renderer.optionalRendererNo != 0)
                    {
                        bool exists = false;
                        for (int r = 0; r < lodDataArray[0].Length; r++)
                        {
                            var rendererOther = lodDataArray[0][r];
                            if (rendererOther != null && rendererOther != renderer && rendererOther.optionalRendererNo == renderer.optionalRendererNo)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                            optionalRendererCount++;
                    }
                }
                #endregion Optional Renderers
            }

            return true;
        }

        #endregion Create Renderers

        #region Add LOD and Renderer

        public GPUILODData AddLODAtIndex(int index, float transitionValue = -1, float fadeTransitionWidth = 0)
        {
            Array.Resize(ref lodDataArray, Length + 1);
            if (Length > 1)
            {
                for (int i = Length - 2; i >= index; i--)
                {
                    lodDataArray[i + 1] = lodDataArray[i];
                    transitionValues[i + 1] = transitionValues[i];
                }
            }
            lodDataArray[index] = new GPUILODData();

            if (transitionValue >= 0f)
                transitionValues[index] = transitionValue;
            else
            {
                if (index == Length - 1)
                    transitionValues[index] = 0;
                else
                {
                    float leftValue = index == 0 ? 1 : transitionValues[index - 1];
                    float rightValue = transitionValues[index + 1];
                    transitionValues[index] = (leftValue - rightValue) / 2f + rightValue;
                }
            }
            this.fadeTransitionWidth[index] = fadeTransitionWidth;

            return lodDataArray[index];
        }

        public GPUILODData AddLOD(float transitionValue = -1, float fadeTransitionWidth = 0)
        {
            return AddLODAtIndex(Length, transitionValue, fadeTransitionWidth);
        }

        public void RemoveLODAtIndex(int index)
        {
            for (int i = index; i < Length - 1; i++)
            {
                lodDataArray[i] = lodDataArray[i + 1];
                transitionValues[i] = transitionValues[i + 1];
            }
            for (int i = Length - 1; i < 8; i++)
            {
                transitionValues[i] = 0f;
            }
            Array.Resize(ref lodDataArray, Length - 1);
        }

        public GPUIRendererData AddRenderer(Renderer renderer, Transform parentTransform, int lodIndex)
        {
            if (renderer is SkinnedMeshRenderer smr)
            {
                if (smr.sharedMesh == null)
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can't add renderer: mesh is null. Make sure that all the SkinnedMeshRenderers on the prototype has a mesh assigned.", parentTransform.gameObject);
                    return null;
                }
                if (smr.sharedMaterials == null || smr.sharedMaterials.Length == 0)
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can't add renderer: no materials. Make sure that all the SkinnedMeshRenderers have their materials assigned.", parentTransform.gameObject);
                    return null;
                }

                if (_hasSkinningComponent)
                    HasSkinning = true;
                AddRenderer(lodIndex, smr.sharedMesh, (Material[])renderer.sharedMaterials.Clone(), parentTransform.GetTransformOffset(renderer.gameObject.transform), renderer.gameObject.layer, renderer.shadowCastingMode, renderer.receiveShadows, renderer.motionVectorGenerationMode, HasSkinning, false, renderer.renderingLayerMask, renderer.lightProbeUsage);
                return null;
            }
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "MeshRenderer with no MeshFilter found on GameObject <" + parentTransform.name + ">. Are you missing a component?", parentTransform.gameObject);
                return null;
            }

            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can't add renderer: mesh is null. Make sure that all the MeshFilters on the prototype has a mesh assigned.", parentTransform.gameObject);
                return null;
            }

            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can't add renderer: no materials. Make sure that all the MeshRenderers have their materials assigned.", parentTransform.gameObject);
                return null;
            }

            return AddRenderer(lodIndex, meshFilter.sharedMesh, (Material[])renderer.sharedMaterials.Clone(), parentTransform.GetTransformOffset(renderer.gameObject.transform), renderer.gameObject.layer, renderer.shadowCastingMode, renderer.receiveShadows, renderer.motionVectorGenerationMode, false, false, renderer.renderingLayerMask, renderer.lightProbeUsage);
        }

        public GPUIRendererData AddRenderer(int lodIndex, Mesh mesh, Material[] materials, Matrix4x4 transformOffset, int layer, ShadowCastingMode shadowCastingMode, bool receiveShadows = true, MotionVectorGenerationMode motionVectorGenerationMode = MotionVectorGenerationMode.Camera, bool isSkinnedMesh = false, bool doesNotContributeToBounds = false, uint renderingLayerMask = 1, LightProbeUsage lightProbeUsage = LightProbeUsage.Off)
        {
            if (Length <= lodIndex || this[lodIndex] == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can't add renderer: Invalid LOD");
                return null;
            }
            GPUIRendererData renderer = new GPUIRendererData(mesh, materials, transformOffset, layer, shadowCastingMode, receiveShadows, motionVectorGenerationMode, isSkinnedMesh, doesNotContributeToBounds, renderingLayerMask, lightProbeUsage);
            this[lodIndex].Add(renderer);
            CalculateBounds();
            return renderer;
        }

        public void CalculateBounds()
        {
            if (lodDataArray == null || lodDataArray.Length == 0 || lodDataArray[0].rendererDataArray == null || lodDataArray[0].rendererDataArray.Length == 0)
                return;

            Bounds rendererBounds;
            for (int lod = 0; lod < lodDataArray.Length; lod++)
            {
                GPUILODData lodData = lodDataArray[lod];
                for (int r = 0; r < lodData.rendererDataArray.Length; r++)
                {
                    GPUIRendererData renderer = lodData.rendererDataArray[r];
                    if (renderer.doesNotContributeToBounds)
                        continue;
                    rendererBounds = renderer.rendererMesh.bounds;
                    rendererBounds = rendererBounds.GetMatrixAppliedBounds(renderer.transformOffset);
                    if (lod == 0 && r == 0)
                        bounds = rendererBounds;
                    else
                        bounds.Encapsulate(rendererBounds);
                }
            }
            if (prototype != null && prototype.profile != null)
                bounds.Expand(prototype.profile.boundsOffset);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            SetParameterBufferData();
        }

        public void InitializeTransitionValues()
        {
            if (transitionValues == null)
                transitionValues = new float[8];
            else if (transitionValues.Length != 8)
                Array.Resize(ref transitionValues, 8);
            for (int i = 0; i < 8; i++)
            {
                if (i >= Length)
                    transitionValues[i] = 0;
                else
                {
                    if (i == 0)
                        transitionValues[i] = Mathf.Clamp01(transitionValues[i]);
                    else
                        transitionValues[i] = Mathf.Clamp(transitionValues[i], 0, transitionValues[i - 1]);
                }
            }

            if (fadeTransitionWidth == null)
                fadeTransitionWidth = new float[8];
            else if (fadeTransitionWidth.Length != 8)
                Array.Resize(ref fadeTransitionWidth, 8);
        }

        #endregion Add LOD and Renderer

        #region Parameter Buffer

        public void SetParameterBufferData()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIDataBuffer<float> parameterBuffer = GPUIRenderingSystem.Instance.ParameterBuffer;
            InitializeTransitionValues();

            if (TryGetParameterBufferIndex(out int startIndex))
            {
                parameterBuffer[startIndex + 0] = Length;
                parameterBuffer[startIndex + 1] = bounds.center.x;
                parameterBuffer[startIndex + 2] = bounds.center.y;
                parameterBuffer[startIndex + 3] = bounds.center.z;
                parameterBuffer[startIndex + 4] = bounds.extents.x;
                parameterBuffer[startIndex + 5] = bounds.extents.y;
                parameterBuffer[startIndex + 6] = bounds.extents.z;
                parameterBuffer[startIndex + 23] = lodGroupSize;
            }
            else
            {
                startIndex = parameterBuffer.Length;
                GPUIRenderingSystem.Instance.ParameterBufferIndexes.Add(this, startIndex);

                parameterBuffer.Add(Length, bounds.center.x, bounds.center.y, bounds.center.z, bounds.extents.x, bounds.extents.y, bounds.extents.z);
                parameterBuffer.Add(transitionValues);
                parameterBuffer.Add(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
                parameterBuffer.Add(lodGroupSize);
                parameterBuffer.Add(fadeTransitionWidth);
            }
            float lodBias = QualitySettings.lodBias;
            for (int i = 0; i < 8; i++)
                parameterBuffer[startIndex + 7 + i] = transitionValues[i] / lodBias;
            for (int i = 0; i < 8 && i < Length; i++)
                parameterBuffer[startIndex + 15 + i] = lodDataArray[i].IsShadowCasting() ? 1f : 0f;
            for (int i = 0; i < 8; i++)
                parameterBuffer[startIndex + 24 + i] = fadeTransitionWidth[i];
        }

        public bool TryGetParameterBufferIndex(out int index)
        {
            return GPUIRenderingSystem.Instance.ParameterBufferIndexes.TryGetValue(this, out index);
        }

        #endregion Parameter Buffer

        public int GetMeshMaterialCombinationCount()
        {
            int result = 0;
            for (int l = 0; l < Length; l++)
            {
                GPUILODData rdg = this[l];
                for (int r = 0; r < rdg.Length; r++)
                {
                    GPUIRendererData rd = rdg[r];
                    result += rd.rendererMaterials.Length;
                }
            }
            return result;
        }

        public override string ToString()
        {
            if (prototype != null)
                return prototype.ToString();
            return GPUIUtility.CamelToTitleCase(this.name.Replace("_" , ""));
        }

        internal bool HasObjectMotion()
        {
            for (int i = 0; i < Length; i++)
            {
                for (int j = 0; j < lodDataArray[i].Length; j++)
                {
                    if (lodDataArray[i].rendererDataArray[j].motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                        return true;
                }
            }
            return false;
        }

        public void ReleaseBuffers() { }

        public void Dispose()
        {
            for (int l = 0; l < Length; l++)
            {
                this[l].Dispose();
            }
        }

        public void RemoveReplacementMaterials()
        {
            if (lodDataArray == null)
                return;
            foreach (var item in lodDataArray)
            {
                item.RemoveReplacementMaterials();
            }
        }
    }

    [Serializable]
    public class GPUILODData : IGPUIDisposable
    {
        public GPUIRendererData[] rendererDataArray;

        [NonSerialized]
        private List<GraphicsBuffer.IndirectDrawIndexedArgs> _commandBufferArgs;
        [NonSerialized]
        private List<GraphicsBuffer.IndirectDrawIndexedArgs> _optionalRendererCommandBufferArgs;

        public GPUILODData()
        {
            rendererDataArray = new GPUIRendererData[0];
        }

        #region Array Methods
        /// <summary>
        /// Number of renderers
        /// </summary>
        public int Length => rendererDataArray == null ? 0 : rendererDataArray.Length;

        public GPUIRendererData this[int index]
        {
            get => rendererDataArray[index];
            set => rendererDataArray[index] = value;
        }

        public void Add(GPUIRendererData renderer)
        {
            if (rendererDataArray == null)
            {
                rendererDataArray = new GPUIRendererData[1];
                rendererDataArray[0] = renderer;
                return;
            }
            Array.Resize(ref rendererDataArray, Length + 1);
            rendererDataArray[Length - 1] = renderer;
        }

        public void ReleaseBuffers() { }

        public void Dispose()
        {
            for (int r = 0; r < Length; r++)
            {
                this[r].Dispose();
            }
        }

        #endregion Array Methods

        public bool IsShadowCasting()
        {
            for (int i = 0; i < Length; i++)
                if (rendererDataArray[i].IsShadowCasting) return true;
            return false;
        }

        internal void CreateCommandBufferArgs()
        {
            if (_commandBufferArgs == null)
                _commandBufferArgs = new();
            else
                _commandBufferArgs.Clear();

            for (int r = 0; r < Length; r++)
            {
                GPUIRendererData renderer = this[r];
                if (renderer.rendererMesh != null && renderer.optionalRendererNo == 0)
                {
                    int subMeshCount = renderer.rendererMesh.subMeshCount;
                    for (int m = 0; m < renderer.rendererMaterials.Length; m++)
                    {
                        int submeshIndex = m;
                        if (subMeshCount <= submeshIndex)
                            submeshIndex = subMeshCount - 1;

                        _commandBufferArgs.Add(new GraphicsBuffer.IndirectDrawIndexedArgs()
                        {
                            baseVertexIndex = renderer.rendererMesh.GetBaseVertex(submeshIndex),
                            indexCountPerInstance = renderer.rendererMesh.GetIndexCount(submeshIndex),
                            startIndex = renderer.rendererMesh.GetIndexStart(submeshIndex),
                            instanceCount = 0,
                            startInstance = 0
                        });
                    }
                }
            }
        }

        internal List<GraphicsBuffer.IndirectDrawIndexedArgs> GetOptionalRendererCommandBufferArgs(int optionalRendererNo)
        {
            _optionalRendererCommandBufferArgs ??= new();
            _optionalRendererCommandBufferArgs.Clear();

            for (int r = 0; r < Length; r++)
            {
                GPUIRendererData renderer = this[r];
                if (renderer.rendererMesh != null && renderer.optionalRendererNo == optionalRendererNo)
                {
                    int subMeshCount = renderer.rendererMesh.subMeshCount;
                    for (int m = 0; m < renderer.rendererMaterials.Length; m++)
                    {
                        int submeshIndex = m;
                        if (subMeshCount <= submeshIndex)
                            submeshIndex = subMeshCount - 1;

                        _optionalRendererCommandBufferArgs.Add(new GraphicsBuffer.IndirectDrawIndexedArgs()
                        {
                            baseVertexIndex = renderer.rendererMesh.GetBaseVertex(submeshIndex),
                            indexCountPerInstance = renderer.rendererMesh.GetIndexCount(submeshIndex),
                            startIndex = renderer.rendererMesh.GetIndexStart(submeshIndex),
                            instanceCount = 0,
                            startInstance = 0
                        });
                    }
                }
            }

            return _optionalRendererCommandBufferArgs;
        }

        internal List<GraphicsBuffer.IndirectDrawIndexedArgs> GetCommandBufferArgs()
        {
            if (_commandBufferArgs == null)
                CreateCommandBufferArgs();
            return _commandBufferArgs;
        }

        public void RemoveReplacementMaterials()
        {
            if (rendererDataArray == null)
                return;
            foreach (var item in rendererDataArray)
            {
                item.RemoveReplacementMaterials();
            }
        }
    }

    [Serializable]
    public class GPUIRendererData : IGPUIDisposable
    {
        public Mesh rendererMesh;
        public Material[] rendererMaterials;
        public Matrix4x4 transformOffset;
        public int layer;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        public bool receiveShadows;
        public MotionVectorGenerationMode motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
        public bool isSkinnedMesh;
        public bool doesNotContributeToBounds;
        public uint renderingLayerMask;
        /// <summary>
        /// 0 when not an optional renderer.
        /// </summary>
        public int optionalRendererNo;
        public LightProbeUsage lightProbeUsage;

        [NonSerialized]
        public Material[] replacementMaterials;
        [NonSerialized]
        public Mesh replacementMesh;

        public bool IsShadowCasting => shadowCastingMode != ShadowCastingMode.Off;
        public bool IsShadowsOnly => shadowCastingMode == ShadowCastingMode.ShadowsOnly;

        public GPUIRendererData()
        {
            transformOffset = Matrix4x4.identity;
            rendererMaterials = new Material[0];
        }

        public GPUIRendererData(Mesh mesh, Material[] materials, Matrix4x4 transformOffset, int layer, ShadowCastingMode shadowCastingMode, bool receiveShadows, MotionVectorGenerationMode motionVectorGenerationMode, bool isSkinnedMesh, bool doesNotContributeToBounds, uint renderingLayerMask, LightProbeUsage lightProbeUsage)
        {
            if (transformOffset == Matrix4x4.zero || isSkinnedMesh)
                transformOffset = Matrix4x4.identity;

            this.rendererMesh = mesh;
            this.rendererMaterials = materials;
            this.transformOffset = transformOffset;
            this.layer = layer;
            this.shadowCastingMode = shadowCastingMode;
            this.receiveShadows = receiveShadows;
            this.motionVectorGenerationMode = motionVectorGenerationMode;
            this.isSkinnedMesh = isSkinnedMesh;
            this.doesNotContributeToBounds = doesNotContributeToBounds;
            this.renderingLayerMask = renderingLayerMask;
            this.lightProbeUsage = lightProbeUsage != LightProbeUsage.Off ? LightProbeUsage.BlendProbes : LightProbeUsage.Off;
        }

        public void InitializeReplacementMaterials(GPUIMaterialProvider materialProvider)
        {
            replacementMaterials = new Material[rendererMaterials.Length];
        }

        public void RemoveReplacementMaterials()
        {
            if (replacementMaterials == null) return;
            for (int i = 0; i < replacementMaterials.Length; i++)
                replacementMaterials[i] = null;
        }

        public Mesh GetMesh()
        {
            if (replacementMesh != null)
                return replacementMesh;
            return rendererMesh;
        }

        public void ReleaseBuffers() { }

        public void Dispose()
        {
            if (replacementMesh != null)
                replacementMesh.DestroyGeneric();
        }
    }
}
