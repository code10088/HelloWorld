// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEngine.Profiling;
#if GPUI_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    /// <summary>
    /// This component is automatically attached to a hidden GameObject when GPUI rendering starts. 
    /// It is a singleton that persists between scenes. 
    /// It should not be added or removed manually.
    /// </summary>
    [ExecuteInEditMode]
    [DefaultExecutionOrder(1000)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Debugger_Window")]
    public sealed class GPUIRenderingSystem : MonoBehaviour, IGPUIDisposable
    {
        #region Runtime Properties

        public static GPUIRenderingSystem Instance { get; private set; }

        public static bool IsActive => Instance != null && Instance.IsInitialized;

        /// <summary>
        /// Set to true if the buffers are initialized
        /// </summary>
        public bool IsInitialized { get; private set; }
        /// <summary>
        /// <para>key => Original Material Instance ID (+ Extension Code Hash)</para>
        /// <para>value => Replacement material</para>
        /// </summary>
        public GPUIMaterialProvider MaterialProvider { get; private set; }
        /// <summary>
        /// Contains the list of runtime generated LODGroupData for each prefab GameObject
        /// <para>Key => Prototype key</para> 
        /// <para>Value => Generated LODGroupData</para> 
        /// </summary>
        public GPUILODGroupDataProvider LODGroupDataProvider { get; private set; }
        /// <summary>
        /// <para>Key => Source Group Key</para> 
        /// <para>Value => RenderSourceGroup</para> 
        /// </summary>
        public GPUIRenderSourceGroupProvider RenderSourceGroupProvider { get; private set; }
        /// <summary>
        /// <para>Key => Source Key</para> 
        /// <para>Value => GPUIRenderSource</para> 
        /// </summary>
        public GPUIRenderSourceProvider RenderSourceProvider { get; private set; }
        /// <summary>
        /// Contains data for each camera in use by GPUI
        /// </summary>
        public GPUICameraDataProvider CameraDataProvider { get; private set; }
        /// <summary>
        /// Contains references to Tree Proxy GameObjects for each camera in use by GPUI
        /// </summary>
        public GPUITreeProxyProvider TreeProxyProvider { get; private set; }

        /// <summary>
        /// Contains compute shader parameters for each GPUIProfile and GPUILODGroupData
        /// </summary>
        public GPUIDataBuffer<float> ParameterBuffer { get; private set; }
        /// <summary>
        /// Parameter buffer indexes for each object (e.g. GPUIProfile)
        /// </summary>
        public Dictionary<IGPUIParameterBufferData, int> ParameterBufferIndexes { get; private set; }

        public List<GPUIManager> ActiveGPUIManagers { get; private set; }

        /// <summary>
        /// Internal rendering parameters used by GPUI rendering functions.
        /// </summary>
        [NonSerialized]
        private RenderParams _renderParams;
        /// <summary>
        /// Defines world space bounds for the geometry. Used to cull and sort the rendered geometry.
        /// </summary>
        [NonSerialized]
        private Bounds _worldBounds;

        [NonSerialized]
        private List<IGPUIDisposable> _dependentDisposables;

        // Time management
        [NonSerialized]
        private int _lastDrawCallFrame;
        [NonSerialized]
        private float _lastDrawCallTime;
        public float TimeSinceLastDrawCall { get; private set; }
        public bool IsPaused { get; private set; }

        // Events
        public Action OnCommandBufferModified;
        public Action<GPUICameraData> OnPreCull;
        public Action<GPUICameraData> OnPreRender;
        public Action<GPUICameraData> OnPostRender;

        // System extensions
        private static UnityAction _onRenderingSystemInitialized;
        private List<GPUISystemExtension> _activeSystemExtensions;

        private static MaterialPropertyBlock _emptyMPB;
        public static MaterialPropertyBlock EmptyMPB
        {
            get
            {
                if (_emptyMPB == null)
                    _emptyMPB = new MaterialPropertyBlock();
                return _emptyMPB;
            }
        }

        public delegate void OnBufferDataModifiedCallback(GPUITransformBufferData transformBufferData);
        public static OnBufferDataModifiedCallback OnBufferDataModified;

        #region Light Probes
        private GraphicsBuffer _lightProbesSphericalHarmonicsBuffer;
        private GraphicsBuffer _lightProbesOcclusionProbesBuffer;
        private List<Vector3> _lightProbesPositions;
        private List<SphericalHarmonicsL2> _lightProbesSphericalHarmonics;
        private List<Vector4> _lightProbesOcclusionProbes;
        private int _pendingLightProbeUpdateFrame;
        private int _lastLightProbeBufferUsedFrame;
        #endregion Light Probes

        public GraphicsBuffer DummyGraphicsBuffer { get; private set; }
        private HashSet<int> _ignoreCameraIIDCollection;

        public static List<Renderer> prefabRendererList = new List<Renderer>();

        #endregion Runtime Properties

        #region MonoBehaviour Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyInstance();
                return;
            }
            else if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            if (CheckIsSingleton())
            {
                Initialize();
                UpdateCommandBuffers();
#if UNITY_EDITOR
                Editor_HandlePlayModeStates();
#endif
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        #endregion MonoBehaviour Methods

        #region Draw Calls

        // Camera events for Built-in Render Pipeline
        private static void CameraOnPreCull(Camera camera)
        {
#if UNITY_EDITOR
            if (playModeStateChange == PlayModeStateChange.ExitingEditMode || playModeStateChange == PlayModeStateChange.ExitingPlayMode) return;
#endif
            if (Instance.RenderSourceGroupProvider.Count == 0)
                return;
            ProcessCamera(camera);
        }
        private static void CameraOnPostRender(Camera camera)
        {
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTexture(default);
        }
        // Camera events for Scriptable Render Pipeline
        private static void CameraOnBeginRendering(ScriptableRenderContext context, Camera camera)
        {
            CameraOnPreCull(camera);
#if UNITY_6000_0_OR_NEWER && GPUI_URP
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTextureOnBeginRendering(camera, context);
#endif
        }
        private static void CameraOnEndRendering(ScriptableRenderContext context, Camera camera)
        {
            if (Instance.CameraDataProvider.TryGetData(camera.GetInstanceID(), out GPUICameraData cameraData))
                cameraData.UpdateHiZTexture(context);
        }
        private static void OnEndContextRendering(ScriptableRenderContext context, List<Camera> list)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            int frameNo = Time.frameCount;
            foreach (GPUIRenderSourceGroup renderSourceGroup in Instance.RenderSourceGroupProvider.Values)
            {
                renderSourceGroup.UpdateTransformBufferData(frameNo);
            }
        }

        private static void ProcessCamera(Camera camera)
        {
            var renderingSystem = Instance;
            renderingSystem.ParameterBuffer.UpdateBufferData();
            renderingSystem.ExecuteLightProbeUpdates();
            CameraType cameraType = camera.cameraType;
            int cameraIID = camera.GetInstanceID();
            GPUICameraDataProvider CameraDataProvider = renderingSystem.CameraDataProvider;
#if UNITY_EDITOR // Camera rendering in Edit Mode
            if (!Application.isPlaying)
            {
                if (cameraType == CameraType.Preview && camera.name != PREVIEW_CAMERA_NAME)
                    return;

                CameraDataProvider.ClearNullEditModeCameras();
                if (!CameraDataProvider.TryGetEditModeCameraData(cameraIID, out GPUICameraData editModeCameraData))
                {
                    editModeCameraData = new GPUICameraData(camera);
                    editModeCameraData.renderToSceneView = false;
                    CameraDataProvider.AddEditModeCameraData(editModeCameraData);
                }

                renderingSystem.ExecuteOnPreCull(editModeCameraData);
                editModeCameraData.UpdateCameraData();
                ProcessCameraData(camera, editModeCameraData, true);
                return;
            }
#endif
            if (CameraDataProvider.Count == 0)
                CameraDataProvider.RegisterDefaultCamera();

            bool hasCameraData = CameraDataProvider.TryGetData(cameraIID, out GPUICameraData cameraData);
            if (!hasCameraData && !renderingSystem._ignoreCameraIIDCollection.Contains(cameraIID))
            {
                if (cameraType == CameraType.Reflection)
                {
                    cameraData = new GPUICameraData(camera);
                    CameraDataProvider.AddCameraData(cameraData);
                    hasCameraData = true;
#if UNITY_EDITOR
                    cameraData.renderToSceneView = false;
#endif
                }
                else if (GPUIRuntimeSettings.Instance.cameraLoadingType != GPUICameraLoadingType.GPUICameraComponent && camera.CompareTag(GPUIConstants.TAG_MainCamera))
                {
                    cameraData = CameraDataProvider.AddCamera(camera);
                    hasCameraData = cameraData != null;
                }

                if (hasCameraData)
                    renderingSystem.UpdateCommandBuffers(cameraData);
                else
                    renderingSystem._ignoreCameraIIDCollection.Add(cameraIID);
            }

            if (hasCameraData)
            {
#if GPUI_XR
                if (cameraData.IsVRCulling && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass && cameraData.ActiveCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right) // Avoid making draw calls multiple times for the same frame when using XR MultiPass.
                    return;
#endif
                renderingSystem.ExecuteOnPreCull(cameraData);
                cameraData.UpdateCameraData();
                ProcessCameraData(camera, cameraData, true);
                return;
            }

#if UNITY_EDITOR
            // Scene view camera rendering in Play Mode
            if (cameraType == CameraType.SceneView)
            {
                CameraDataProvider.ClearNullEditModeCameras();
                if (CameraDataProvider.TryGetEditModeCameraData(cameraIID, out GPUICameraData editModeCameraData))
                {
                    renderingSystem.ExecuteOnPreCull(editModeCameraData);
                    editModeCameraData.UpdateCameraData();
                    ProcessCameraData(camera, editModeCameraData, true);
                }
                else
                {
                    foreach (var cd in CameraDataProvider.Values)
                    {
                        if (cd.renderToSceneView)
                        {
                            if (!renderingSystem.IsPaused)
                                if (cd._instanceCountMultiplier > 1) // Instance counts might be multiplied for stereo rendering, we set it back it original value for Scene View rendering
                                    cd.SetCommandBufferInstanceCounts(1);
                            ProcessCameraData(camera, cd, false);
                        }
                    }
                }
            }
#endif
        }

        private static void ProcessCameraData(Camera camera, GPUICameraData cameraData, bool invokeEvents)
        {
            if (cameraData._commandBuffer.Buffer == null) return;

            Profiler.BeginSample(cameraData.name);
            GPUIRenderingSystem renderingSystem = Instance;
            Vector3 cameraPos = cameraData.GetCameraPosition();
            if (invokeEvents)
            {
                renderingSystem.ExecuteOnPreRender(cameraData);
                if (Application.isPlaying)
                    renderingSystem.TreeProxyProvider.SetTreeProxyPosition(cameraData.GetCameraPosition());
            }
            else
                cameraPos = camera.transform.position;
            renderingSystem._worldBounds.center = cameraPos;
            renderingSystem._worldBounds.size = GPUIRuntimeSettings.Instance.instancingBoundsSize;
            renderingSystem._renderParams.worldBounds = renderingSystem._worldBounds;
            renderingSystem._renderParams.camera = camera;
            renderingSystem.MakeDrawCalls(cameraData);
            if (invokeEvents)
                renderingSystem.ExecuteOnPostRender(cameraData);
            Profiler.EndSample();
        }

        private void MakeDrawCalls(GPUICameraData cameraData)
        {
            if (cameraData.ActiveCamera == null)
                return;
            Profiler.BeginSample("GPUIRenderingSystem.MakeDrawCalls");
            if (_lastDrawCallFrame != Time.frameCount)
            {
                _lastDrawCallFrame = Time.frameCount;
                TimeSinceLastDrawCall = Time.realtimeSinceStartup - _lastDrawCallTime;
                _lastDrawCallTime = Time.realtimeSinceStartup;
            }

            int cullingMask = cameraData.ActiveCamera.cullingMask;
#if UNITY_EDITOR
            uint shadowCascades = (uint)QualitySettings.shadowCascades;
#endif
            int qualityMaximumLODLevel = QualitySettings.maximumLODLevel;
            foreach (GPUIRenderSourceGroup renderSourceGroup in RenderSourceGroupProvider.Values)
            {
                GPUILODGroupData lodGroupData = renderSourceGroup.LODGroupData;
                if (renderSourceGroup.BufferSize > 0 && renderSourceGroup.InstanceCount > 0
                    && lodGroupData != null
                    && cameraData.TryGetVisibilityBufferIndex(renderSourceGroup, out int visibilityBufferIndex))
                {
                    Profiler.BeginSample(renderSourceGroup.Name);
                    MaterialPropertyBlock mpb = renderSourceGroup.GetMaterialPropertyBlock(lodGroupData);
                    renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, -1, -1);
                    renderSourceGroup.TransformBufferData.SetMPBBuffers(mpb, cameraData);
                    var profile = renderSourceGroup.Profile;
                    bool isProfileShadowCasting = profile.isShadowCasting;
                    bool isProfileAllowLightProbes = profile.lightProbeSetting != GPUILightProbeSetting.Off;

                    _renderParams.matProps = mpb;

                    int lodCount = lodGroupData.Length;

                    int maximumLODLevel = GetMaximumLODLevel(lodCount, profile.maximumLODLevel, qualityMaximumLODLevel);
#if UNITY_EDITOR
                    GPUIRenderStatistics[] lodRenderStatistics = renderSourceGroup.GetRenderStatisticsArray(lodCount);
#endif
                    for (int l = 0; l < lodCount; l++)
                    {
                        RenderLOD(l, cameraData, cullingMask, renderSourceGroup, lodGroupData, visibilityBufferIndex, mpb, isProfileShadowCasting, lodCount, maximumLODLevel, 0, isProfileAllowLightProbes
#if UNITY_EDITOR
                            , lodRenderStatistics, shadowCascades
#endif
                            );
                    }

                    #region Optional Renderers
                    if (lodCount == 1 && lodGroupData.optionalRendererCount > 0)
                    {
                        GPUILODData gpuiLOD = lodGroupData[0];
                        for (int ori = 0; ori < lodGroupData.optionalRendererCount; ori++)
                        {
                            RenderLOD(0, cameraData, cullingMask, renderSourceGroup, lodGroupData, visibilityBufferIndex, mpb, isProfileShadowCasting, lodCount, maximumLODLevel, ori + 1, isProfileAllowLightProbes
#if UNITY_EDITOR
                            , lodRenderStatistics, shadowCascades
#endif
                            );
                        }
                    }
                    #endregion Optional Renderers
                    Profiler.EndSample();
                }
            }

            Profiler.EndSample();
        }

        private void RenderLOD(int lodNo, GPUICameraData cameraData, int cullingMask, GPUIRenderSourceGroup renderSourceGroup, GPUILODGroupData lodGroupData, int visibilityBufferIndex, MaterialPropertyBlock mpb, bool isProfileShadowCasting, int lodCount, int maximumLODLevel, int optionalRendererNo, bool isProfileAllowLightProbes
#if UNITY_EDITOR
            , GPUIRenderStatistics[] lodRenderStatistics, uint shadowCascades
#endif
            )
        {
            if (isProfileShadowCasting && !renderSourceGroup.Profile.HasLODLevelShadows(lodNo))
                isProfileShadowCasting = false;
            int visibilityIndex = visibilityBufferIndex + lodNo + optionalRendererNo * 2;
            int commandIndex = (int)cameraData._visibilityBuffer[visibilityIndex].commandStartIndex;
            int shadowCommandIndex = (int)cameraData._visibilityBuffer[visibilityIndex + lodCount].commandStartIndex;
            int instanceDataShiftMultiplier = isProfileShadowCasting ? 2 : 1;

            renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, lodNo, -1);
            GPUILODData gpuiLOD = lodGroupData[lodNo];
            for (int r = 0; r < gpuiLOD.Length; r++)
            {
                GPUIRendererData renderer = gpuiLOD[r];
                if (renderer.optionalRendererNo != optionalRendererNo)
                    continue;
                Mesh mesh = renderer.GetMesh();
                if (mesh != null && GPUIUtility.IsInLayer(cullingMask, renderer.layer) && lodNo >= maximumLODLevel)
                {
                    _renderParams.receiveShadows = renderer.receiveShadows;
                    _renderParams.lightProbeUsage = isProfileAllowLightProbes ? renderer.lightProbeUsage : LightProbeUsage.Off;

                    renderSourceGroup.ApplyMaterialPropertyOverrides(mpb, lodNo, r);
                    mpb.SetMatrix(GPUIConstants.PROP_gpuiTransformOffset, renderer.transformOffset);
                    _renderParams.layer = renderer.layer;
                    if (renderer.motionVectorGenerationMode == MotionVectorGenerationMode.Object && !renderSourceGroup.TransformBufferData.HasPreviousFrameTransformBuffer)
                        _renderParams.motionVectorMode = MotionVectorGenerationMode.Camera;
                    else
                        _renderParams.motionVectorMode = renderer.motionVectorGenerationMode;
                    _renderParams.renderingLayerMask = renderer.renderingLayerMask;

                    if (Application.isPlaying && renderer.replacementMaterials == null)
                        renderer.InitializeReplacementMaterials(MaterialProvider);
                    for (int m = 0; m < renderer.rendererMaterials.Length; m++)
                    {
                        _renderParams.material = GetReplacementMaterial(renderer, m, renderSourceGroup.ShaderKeywords);

                        int instanceDataBufferShift = renderSourceGroup.BufferSize * (lodNo + optionalRendererNo * instanceDataShiftMultiplier);
                        if (!renderer.IsShadowsOnly)
                        {
                            mpb.SetInt(GPUIConstants.PROP_instanceDataBufferShift, instanceDataBufferShift);
                            _renderParams.shadowCastingMode = ShadowCastingMode.Off;
                            GPUIUtility.RenderMeshIndirect(_renderParams, mesh, cameraData._commandBuffer, 1, commandIndex);
#if UNITY_EDITOR
                            lodRenderStatistics[lodNo].drawCount++;
#endif
                        }
                        if (isProfileShadowCasting && renderer.IsShadowCasting)
                        {
                            mpb.SetInt(GPUIConstants.PROP_instanceDataBufferShift, instanceDataBufferShift + renderSourceGroup.BufferSize * lodCount);
                            _renderParams.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                            GPUIUtility.RenderMeshIndirect(_renderParams, mesh, cameraData._commandBuffer, 1, shadowCommandIndex);
#if UNITY_EDITOR
                            lodRenderStatistics[lodNo].shadowDrawCount += shadowCascades;
#endif
                        }

                        commandIndex++;
                        shadowCommandIndex++;
                    }
#if UNITY_EDITOR
                    if (!renderer.IsShadowsOnly)
                        lodRenderStatistics[lodNo].vertexCount += (uint)mesh.vertexCount;
                    if (isProfileShadowCasting && renderer.IsShadowCasting)
                        lodRenderStatistics[lodNo].shadowVertexCount += (uint)mesh.vertexCount * shadowCascades;
#endif
                }
                else
                {
                    commandIndex += renderer.rendererMaterials.Length;
                    shadowCommandIndex += renderer.rendererMaterials.Length;
                }
            }
        }

        private int GetMaximumLODLevel(int lodCount, int profileMaximumLODLevel, int qualityMaximumLODLevel)
        {
            if (lodCount <= 1) return 0;
            return Mathf.Max(profileMaximumLODLevel, qualityMaximumLODLevel);
        }

        private Material GetReplacementMaterial(GPUIRendererData renderer, int materialIndex, List<string> keywords)
        {
            Material replacementMat = null;
            if (Application.isPlaying)
                replacementMat = renderer.replacementMaterials[materialIndex];
            if (replacementMat == null)
            {
                string extensionCode = null;
#if GPUI_CROWD
                if (renderer.isSkinnedMesh)
                    extensionCode = GPUIConstants.EXTENSION_CODE_CROWD;
#endif
                if (MaterialProvider.TryGetReplacementMaterial(renderer.rendererMaterials[materialIndex], keywords, extensionCode, out replacementMat))
                {
                    if (Application.isPlaying)
                    {
                        renderer.replacementMaterials[materialIndex] = replacementMat;
#if UNITY_EDITOR
                        MaterialProvider.AddMaterialVariant(replacementMat);
#endif
                    }
                }
            }
            return replacementMat;
        }

        private void ExecuteOnPreCull(GPUICameraData cameraData)
        {
            OnPreCull?.Invoke(cameraData);
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.ExecuteOnPreCull(cameraData);
            }
        }
        private void ExecuteOnPreRender(GPUICameraData cameraData)
        {
            OnPreRender?.Invoke(cameraData);
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.ExecuteOnPreRender(cameraData);
            }
        }
        private void ExecuteOnPostRender(GPUICameraData cameraData)
        {
            OnPostRender?.Invoke(cameraData);
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.ExecuteOnPostRender(cameraData);
            }
        }

        public static void OnLightProbesUpdated()
        {
            if (!IsActive || !Application.isPlaying) return;
            Instance._pendingLightProbeUpdateFrame = Time.frameCount;
//#if GPUIPRO_DEVMODE
//            Debug.Log(GPUIConstants.LOG_PREFIX + "OnLightProbesUpdated " + Time.frameCount);
//#endif
        }

        public static bool WillUpdateLightProbes()
        {
            if (!IsActive) return false;
            return Instance._pendingLightProbeUpdateFrame >= 0;
        }

        private void ExecuteLightProbeUpdates()
        {
            int frameCount = Time.frameCount;
            if (_pendingLightProbeUpdateFrame < 0)
            {
                if (_lightProbesPositions != null && _lastLightProbeBufferUsedFrame < frameCount - 30) // if buffers are not used for 30 frames, release to free up memory
                    ReleaseLightProbeBuffers();
                return;
            }
            if (_pendingLightProbeUpdateFrame + 2 >= frameCount) // wait for 3 frames before updating
                return;
            _lastLightProbeBufferUsedFrame = frameCount;
            _pendingLightProbeUpdateFrame = -1;
//#if GPUIPRO_DEVMODE
//            Debug.Log(GPUIConstants.LOG_PREFIX + "ExecuteLightProbeUpdates " + frameCount);
//#endif
            foreach (var rs in Instance.RenderSourceProvider.Values)
            {
                if (rs.source is IGPUILightProbeDataProvider lightProbeDataProvider)
                    lightProbeDataProvider.OnLightProbesUpdated();
            }
        }

        #endregion Draw Calls

        #region Initialize / Dispose

        private bool CheckIsSingleton()
        {
            if (Instance == null)
            {
                DestroyInstance();
                return false;
            }
            else if (Instance != this)
            {
                DestroyInstance();
                return false;
            }
            return true;
        }

        private void Initialize()
        {
            if (!GPUIRuntimeSettings.Instance.IsSupportedPlatform())
            {
                DestroyInstance();
                return;
            }
            if (!IsInitialized)
            {
                IsInitialized = true;
                GPUIRuntimeSettings.Instance.DetermineOperationMode();

                MaterialProvider = new();
                MaterialProvider.Initialize();

                LODGroupDataProvider = new();
                LODGroupDataProvider.Initialize();

                RenderSourceGroupProvider = new();
                RenderSourceGroupProvider.Initialize();

                RenderSourceProvider = new();
                RenderSourceProvider.Initialize();

                CameraDataProvider = new();
                CameraDataProvider.Initialize();

                TreeProxyProvider = new();
                TreeProxyProvider.Initialize();

                ParameterBuffer = new("Parameter");
                ParameterBufferIndexes = new();

                ActiveGPUIManagers = new();

                _renderParams = new(GPUIShaderBindings.Instance.ErrorMaterial);
                _worldBounds = new Bounds(Vector3.zero, GPUIRuntimeSettings.Instance.instancingBoundsSize);

                _dependentDisposables = new List<IGPUIDisposable>();

                //SetGlobalWindVector(); // We are using Tree Proxy, so there is no need to set the _Wind parameter.

                _activeSystemExtensions = new();
                _onRenderingSystemInitialized?.Invoke();

                if (DummyGraphicsBuffer != null)
                    DummyGraphicsBuffer.Dispose();
                DummyGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4);

                _ignoreCameraIIDCollection = new();

                prefabRendererList ??= new();

                Camera.onPreCull -= CameraOnPreCull;
                RenderPipelineManager.beginCameraRendering -= CameraOnBeginRendering;
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    Camera.onPreCull += CameraOnPreCull;
                else
                    RenderPipelineManager.beginCameraRendering += CameraOnBeginRendering;

                Camera.onPostRender -= CameraOnPostRender;
                RenderPipelineManager.endCameraRendering -= CameraOnEndRendering;
                if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    Camera.onPostRender += CameraOnPostRender;
                else
                {
                    RenderPipelineManager.endCameraRendering += CameraOnEndRendering;
                    if (!GPUIRuntimeSettings.Instance.IsBuiltInRP)
                    {
                        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
                        RenderPipelineManager.endContextRendering += OnEndContextRendering;
                    }
                }

                LightProbes.lightProbesUpdated -= OnLightProbesUpdated;
                LightProbes.lightProbesUpdated += OnLightProbesUpdated;
            }
        }

        public void Dispose()
        {
            IsInitialized = false;

            foreach (var systemExtension in _activeSystemExtensions)
                systemExtension.Dispose();
            _activeSystemExtensions = null;

            if (MaterialProvider != null)
            {
                MaterialProvider.Dispose();
                MaterialProvider = null;
            }

            if (LODGroupDataProvider != null)
            {
                LODGroupDataProvider.Dispose();
                LODGroupDataProvider = null;
            }

            if (RenderSourceGroupProvider != null)
            {
                RenderSourceGroupProvider.Dispose();
                RenderSourceGroupProvider = null;
            }

            if (RenderSourceProvider != null)
            {
                RenderSourceProvider.Dispose();
                RenderSourceProvider = null;
            }

            if (CameraDataProvider != null)
            {
                CameraDataProvider.Dispose();
                CameraDataProvider = null;
            }

            if (TreeProxyProvider != null)
            {
                TreeProxyProvider.Dispose();
                TreeProxyProvider = null;
            }

            if (ParameterBuffer != null)
            {
                ParameterBuffer.Dispose();
                ParameterBuffer = null;
            }

            if (DummyGraphicsBuffer != null)
            {
                DummyGraphicsBuffer.Dispose();
                DummyGraphicsBuffer = null;
            }

            _ignoreCameraIIDCollection = null;

            ReleaseLightProbeBuffers();

            if (_dependentDisposables != null)
            {
                foreach (IGPUIDisposable disposable in _dependentDisposables)
                    disposable.Dispose();
                _dependentDisposables = null;
            }

            ParameterBufferIndexes = null;

            ActiveGPUIManagers = null;

            OnCommandBufferModified = null;
            OnPreCull = null;
            OnPreRender = null;
            OnPostRender = null;

            if (!IsActive) // Check if disposing a duplicate or the original
            {
                Camera.onPreCull -= CameraOnPreCull;
                RenderPipelineManager.beginCameraRendering -= CameraOnBeginRendering;
                Camera.onPostRender -= CameraOnPostRender;
                RenderPipelineManager.endCameraRendering -= CameraOnEndRendering;
                RenderPipelineManager.endContextRendering -= OnEndContextRendering;

                LightProbes.lightProbesUpdated -= OnLightProbesUpdated;
            }
        }

        public void ReleaseLightProbeBuffers()
        {
//#if GPUIPRO_DEVMODE
//            Debug.Log(GPUIConstants.LOG_PREFIX + "ReleaseLightProbeBuffers");
//#endif
            if (_lightProbesSphericalHarmonicsBuffer != null)
            {
                _lightProbesSphericalHarmonicsBuffer.Dispose();
                _lightProbesSphericalHarmonicsBuffer = null;
            }
            if (_lightProbesOcclusionProbesBuffer != null)
            {
                _lightProbesOcclusionProbesBuffer.Dispose();
                _lightProbesOcclusionProbesBuffer = null;
            }
            _lightProbesPositions = null;
            _lightProbesSphericalHarmonics = null;
            _lightProbesOcclusionProbes = null;
        }

        public void ReleaseBuffers()
        {
            if (CameraDataProvider != null)
                CameraDataProvider.ReleaseBuffers();
            if (ParameterBuffer != null)
                ParameterBuffer.ReleaseBuffers();
        }

        private void DestroyInstance()
        {
            gameObject.DestroyGeneric();
        }

#if GPUIPRO_DEVMODE && UNITY_EDITOR
        [MenuItem("Tools/GPU Instancer Pro/Development/Reset Rendering System", validate = false, priority = 9999)]
#endif
        public static void ResetRenderingSystem()
        {
            if (Instance != null)
                Instance.DestroyInstance();
            InitializeRenderingSystem();
        }


#if GPUIPRO_DEVMODE && UNITY_EDITOR
        [MenuItem("Tools/GPU Instancer Pro/Development/Regenerate Renderers", validate = false, priority = 9999)]
#endif
        public static void RegenerateRenderers()
        {
            if (Instance != null)
            {
                Instance.LODGroupDataProvider.RegenerateLODGroups();
                //Instance.UpdateCommandBuffers(true); Called with RegenerateLODGroups
                Instance.UpdateParameterBufferData();
                Instance.MaterialProvider.Reset();
            }
        }

        public static void InitializeRenderingSystem()
        {
            if (IsActive) return;
            if (Instance == null)
            {
                GameObject go = new GameObject();
                Instance = go.AddComponent<GPUIRenderingSystem>();
                if (Instance == null)
                    return;
                go.name = "===GPUI Rendering System [" + Instance.GetInstanceID() + "]===";
#if GPUIPRO_DEVMODE
                go.hideFlags = HideFlags.DontSave;
#else
                go.hideFlags = HideFlags.HideAndDontSave;
#endif
            }
            Instance.Initialize();
        }

        public static void AddActiveManager(GPUIManager manager)
        {
            InitializeRenderingSystem();
            if (!Instance.ActiveGPUIManagers.Contains(manager))
                Instance.ActiveGPUIManagers.Add(manager);
        }

        public static void RemoveActiveManager(GPUIManager manager)
        {
            if (Instance != null && Instance.IsInitialized)
                Instance.ActiveGPUIManagers.Remove(manager);
        }

        public static void AddOnRenderingSystemInitializedListener(UnityAction action)
        {
            _onRenderingSystemInitialized -= action;
            _onRenderingSystemInitialized += action;
            if (IsActive)
                action.Invoke();
        }

        public void AddRenderingSystemExtension(GPUISystemExtension systemExtension)
        {
            if (!_activeSystemExtensions.Contains(systemExtension))
                _activeSystemExtensions.Add(systemExtension);
        }

        public void RemoveRenderingSystemExtension(GPUISystemExtension systemExtension)
        {
            _activeSystemExtensions.Remove(systemExtension);
        }

        #endregion Initialize / Dispose

        #region RenderSource

        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            return RegisterRenderer(source, prefab, GPUIProfile.DefaultProfile, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, GameObject prefab, GPUIProfile profile, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            if (prefab == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Given prefab is null! Can not register renderer.");
                rendererKey = 0;
                return false;
            }
            GPUIPrototype prototype = new GPUIPrototype(prefab, profile);
            return RegisterRenderer(source, prototype, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, GPUIPrototype prototype, out int rendererKey, int groupID = 0, GPUITransformBufferType transformBufferType = GPUITransformBufferType.Default, List<string> shaderKeywords = null)
        {
            InitializeRenderingSystem();

            return RegisterRenderer(source, prototype.GetKey(), Instance.LODGroupDataProvider.GetOrCreateLODGroupData(prototype), prototype.profile, out rendererKey, groupID, transformBufferType, shaderKeywords);
        }

        public static bool RegisterRenderer(UnityEngine.Object source, int prototypeKey, GPUILODGroupData lodGroupData, GPUIProfile profile, out int rendererKey, int groupID, GPUITransformBufferType transformBufferType, List<string> shaderKeywords)
        {
            InitializeRenderingSystem();

            rendererKey = 0;
            if (source == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Source is null!");
                return false;
            }
            if (lodGroupData == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "LODGroupData is null!", source);
                return false;
            }
            if (profile == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Profile is null!", source);
                return false;
            }
            if (profile.isLODCrossFade && (shaderKeywords == null || !shaderKeywords.Contains(GPUIConstants.Kw_LOD_FADE_CROSSFADE)))
            {
                shaderKeywords ??= new List<string>();
                shaderKeywords.Add(GPUIConstants.Kw_LOD_FADE_CROSSFADE);
            }

            GPUIRenderSourceGroup renderSourceGroup = Instance.RenderSourceGroupProvider.GetOrCreateRenderSourceGroup(prototypeKey, lodGroupData, profile, groupID, transformBufferType, shaderKeywords);
            if (Instance.RenderSourceProvider.TryCreateRenderSource(source, renderSourceGroup, out GPUIRenderSource renderSource))
            {
                rendererKey = renderSource.Key;
                return true;
            }
            return false;
        }

        public static int GetBufferSize(int renderKey)
        {
            if (IsActive && Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
                return renderSource.bufferSize;
            return 0;
        }

        public static bool SetBufferSize(int renderKey, int bufferSize, bool isCopyPreviousData = true)
        {
            if (bufferSize < 0)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Buffer size is not set for renderer with key: " + renderKey);
                return false;
            }
            if (bufferSize > GPUIConstants.MAX_BUFFER_SIZE)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + bufferSize.ToString("#,0") + " exceeds maximum allowed buffer size (" + GPUIConstants.MAX_BUFFER_SIZE.ToString("#,0") + ").");
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetBufferSize(bufferSize, isCopyPreviousData);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static int GetInstanceCount(int renderKey)
        {
            if (IsActive && Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
                return renderSource.instanceCount;
            return 0;
        }

        public static bool SetInstanceCount(int renderKey, int instanceCount)
        {
            if (instanceCount < 0)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Instance Count is not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetInstanceCount(instanceCount);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, NativeArray<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : unmanaged
        {
            if (matrices == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, T[] matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : unmanaged
        {
            if (matrices == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static bool SetTransformBufferData<T>(int renderKey, List<T> matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, bool isOverwritePreviousFrameBuffer = true) where T : unmanaged
        {
            if (matrices == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Matrices are not set for renderer with key: " + renderKey);
                return false;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.SetTransformBufferData(matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, isOverwritePreviousFrameBuffer);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return false;
        }

        public static void AddMaterialPropertyOverride(int renderKey, string propertyName, object propertyValue, int lodIndex = -1, int rendererIndex = -1, bool isPersistent = false)
        {
            AddMaterialPropertyOverride(renderKey, Shader.PropertyToID(propertyName), propertyValue, lodIndex, rendererIndex, isPersistent);
        }

        public static void AddMaterialPropertyOverride(int renderKey, int nameID, object propertyValue, int lodIndex = -1, int rendererIndex = -1, bool isPersistent = false)
        {
            if (Instance == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Rendering system is not initialized. Can not override MaterialPropertyBlock.");
                return;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.AddMaterialPropertyOverride(nameID, propertyValue, lodIndex, rendererIndex, isPersistent);
                return;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
        }

        public static void AddMaterialPropertyOverrideToRenderSourceGroup(int renderSourceGroupKey, int nameID, object propertyValue, int lodIndex = -1, int rendererIndex = -1, bool isPersistent = false)
        {
            if (Instance == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Rendering system is not initialized. Can not override MaterialPropertyBlock.");
                return;
            }
            if (Instance.RenderSourceGroupProvider.TryGetData(renderSourceGroupKey, out GPUIRenderSourceGroup renderSourceGroup))
            {
                renderSourceGroup.AddMaterialPropertyOverride(nameID, propertyValue, lodIndex, rendererIndex, isPersistent);
                return;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "RenderSourceGroup is not registered with key: " + renderSourceGroup);
        }

        public static void RemoveMaterialPropertyOverrides(int renderKey, string propertyName)
        {
            RemoveMaterialPropertyOverrides(renderKey, Shader.PropertyToID(propertyName));
        }

        public static void RemoveMaterialPropertyOverrides(int renderKey, int nameID)
        {
            if (Instance == null)
                return;
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.RemoveMaterialPropertyOverrides(nameID);
                return;
            }
        }

        public static void ClearMaterialPropertyOverrides(int renderKey)
        {
            if (Instance == null)
                return;
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.ClearMaterialPropertyOverrides();
                return;
            }
        }

        public static void AddDependentDisposable(IGPUIDisposable gpuiDisposable)
        {
            if (Instance == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Rendering system is not initialized. Can not add Disposable.");
                return;
            }
            if (!Instance._dependentDisposables.Contains(gpuiDisposable))
                Instance._dependentDisposables.Add(gpuiDisposable);
        }

        public static void AddDependentDisposable(int renderKey, IGPUIDisposable gpuiDisposable)
        {
            if (Instance == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Rendering system is not initialized. Can not add Disposable.");
                return;
            }
            if (Instance.RenderSourceProvider.TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                renderSource.renderSourceGroup.AddDependentDisposable(gpuiDisposable);
                return;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Renderer is not registered with key: " + renderKey);
            return;
        }

        public static bool AddDependentDisposableToRenderSourceGroup(int renderSourceGroupKey, IGPUIDisposable gpuiDisposable)
        {
            if (Instance == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Rendering system is not initialized. Can not add Disposable.");
                return false;
            }
            if (Instance.RenderSourceGroupProvider.TryGetData(renderSourceGroupKey, out GPUIRenderSourceGroup renderSourceGroup))
            {
                renderSourceGroup.AddDependentDisposable(gpuiDisposable);
                return true;
            }

            Debug.LogError(GPUIConstants.LOG_PREFIX + "Render Source Group is not registered with key: " + renderSourceGroupKey);
            return false;
        }

        public static void DisposeRenderer(int renderKey)
        {
            if (Instance == null || !Instance.IsInitialized)
                return;
            Instance.RenderSourceProvider.DisposeRenderer(renderKey);
        }

        internal void UpdateCommandBuffers(bool forceNew = false)
        {
            if (CameraDataProvider == null) return;
            Profiler.BeginSample("GPUIRenderingSystem.UpdateCommandBuffer");
            foreach (GPUICameraData cameraData in CameraDataProvider.Values)
                UpdateCommandBuffers(cameraData, forceNew);
#if UNITY_EDITOR
            CameraDataProvider.UpdateEditModeCameraDataCommandBuffers(forceNew);
#endif
            OnCommandBufferModified?.Invoke();
            Profiler.EndSample();
        }

        internal void UpdateCommandBuffers(GPUICameraData cameraData, bool forceNew = false)
        {
            if (forceNew)
                cameraData.ClearVisibilityData();
            foreach (GPUIRenderSourceGroup renderSourceGroup in RenderSourceGroupProvider.Values)
            {
                renderSourceGroup.TransformBufferData?.ReleaseInstanceDataBuffers(cameraData);
                renderSourceGroup.UpdateCommandBuffer(cameraData);
            }
        }

        internal void UpdateCommandBuffers(GPUIRenderSourceGroup rsg)
        {
            foreach (GPUICameraData cameraData in CameraDataProvider.Values)
                rsg.UpdateCommandBuffer(cameraData);
#if UNITY_EDITOR
            CameraDataProvider.UpdateEditModeCameraDataCommandBuffers(rsg);
#endif
        }

        public static bool TryGetLODGroupData(GPUIPrototype prototype, out GPUILODGroupData lodGroupData)
        {
            if (prototype == null)
            {
                lodGroupData = null;
                return false;
            }
            return TryGetLODGroupData(prototype.GetKey(), out lodGroupData);
        }

        public static bool TryGetLODGroupData(int prototypeKey, out GPUILODGroupData lodGroupData)
        {
            if (!IsActive)
            {
                lodGroupData = null;
                return false;
            }
            return Instance.LODGroupDataProvider.TryGetData(prototypeKey, out lodGroupData);
        }

        public static bool TryGetRenderSourceGroup(int runtimeRenderKey, out GPUIRenderSourceGroup renderSourceGroup)
        {
            renderSourceGroup = null;
            if (!IsActive || runtimeRenderKey == 0)
                return false;
            foreach (var rsg in Instance.RenderSourceGroupProvider.Values)
            {
                foreach (var rs in rsg.RenderSources)
                {
                    if (rs.Key == runtimeRenderKey)
                    {
                        renderSourceGroup = rsg;
                        return renderSourceGroup != null;
                    }
                }
            }

            return false;
        }

        public static bool TryGetRenderSource(int runtimeRenderKey, out GPUIRenderSource renderSource)
        {
            renderSource = null;
            if (!IsActive || runtimeRenderKey == 0)
                return false;
            return Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out renderSource);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            return TryGetTransformBuffer(runtimeRenderKey, out transformBuffer, out bufferStartIndex, out _, cameraData, resetCrossFade);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GraphicsBuffer transformBuffer, out int bufferStartIndex, out int bufferSize, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            transformBuffer = null;
            if (TryGetTransformBuffer(runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out bufferStartIndex, out bufferSize, cameraData, resetCrossFade))
            {
                transformBuffer = shaderBuffer.Buffer;
                return transformBuffer != null;
            }

            return false;
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out int bufferStartIndex, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            return TryGetTransformBuffer(runtimeRenderKey, out shaderBuffer, out bufferStartIndex, out _, cameraData, resetCrossFade);
        }

        public static bool TryGetTransformBuffer(int runtimeRenderKey, out GPUIShaderBuffer shaderBuffer, out int bufferStartIndex, out int bufferSize, GPUICameraData cameraData = null, bool resetCrossFade = true)
        {
            shaderBuffer = null;
            if (TryGetTransformBufferData(runtimeRenderKey, out GPUITransformBufferData transformBufferData, out bufferStartIndex, out bufferSize, resetCrossFade))
            {
                shaderBuffer = transformBufferData.GetTransformBuffer(cameraData);
                return shaderBuffer != null;
            }
            return false;
        }

        public static bool TryGetTransformBufferData(int runtimeRenderKey, out GPUITransformBufferData transformBufferData, out int bufferStartIndex, out int bufferSize, bool resetCrossFade = true)
        {
            transformBufferData = null;
            bufferStartIndex = 0;
            bufferSize = 0;
            if (runtimeRenderKey == 0 || !IsActive)
                return false;
            if (Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out GPUIRenderSource rs) && rs.renderSourceGroup != null)
            {
                bufferStartIndex = rs.bufferStartIndex;
                bufferSize = rs.bufferSize;
                transformBufferData = rs.renderSourceGroup.TransformBufferData;
                if (transformBufferData != null)
                {
                    if (resetCrossFade)
                        transformBufferData.resetCrossFadeDataFrame = Time.frameCount;
                    return true;
                }
            }
            return false;
        }

        public static void SetLODColorDebuggingEnabled(int runtimeRenderKey, bool enabled, string colorPropertyName = null)
        {
            if (runtimeRenderKey == 0 || !IsActive)
                return;
            if (Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out GPUIRenderSource rs) && rs.renderSourceGroup != null)
                rs.renderSourceGroup.SetLODColorDebuggingEnabled(enabled, colorPropertyName);
        }

        internal void OnCreatedRenderSourceGroup(GPUIRenderSourceGroup rsg)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnCreatedRenderSourceGroup(rsg);
            }
        }

        internal void OnRemovedRenderSourceGroup(int key)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnRemovedRenderSourceGroup(key);
            }
        }

        internal void OnRenderSourceGroupBufferSizeChanged(GPUIRenderSourceGroup rsg)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnRenderSourceGroupBufferSizeChanged(rsg);
            }
        }

        internal void OnCreatedRenderSource(GPUIRenderSource rs)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnCreatedRenderSource(rs);
            }
        }

        internal void OnRemovedRenderSource(int key)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnRemovedRenderSource(key);
            }
        }

        internal void OnRenderSourceBufferSizeChanged(GPUIRenderSource rs, int previousBufferSize)
        {
            foreach (var systemExtension in _activeSystemExtensions)
            {
                if (systemExtension != null)
                    systemExtension.OnRenderSourceBufferSizeChanged(rs, previousBufferSize);
            }
        }

        public void SetOptionalRendererStatusData(int runtimeRenderKey, NativeArray<uint> optionalRendererStatusData)
        {
            if (RenderSourceProvider.TryGetData(runtimeRenderKey, out GPUIRenderSource rs) && rs.renderSourceGroup != null)
                rs.renderSourceGroup.TransformBufferData.SetOptionalRendererStatusBufferData(optionalRendererStatusData, rs.bufferStartIndex);
        }

        internal unsafe void CalculateInterpolatedLightAndOcclusionProbes(GPUITransformBufferData transformBufferData, void* p_matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, int rsgBufferSize, Vector3 positionOffset)
        {
            _lastLightProbeBufferUsedFrame = Time.frameCount;
            _lightProbesPositions ??= new(count);
            _lightProbesSphericalHarmonics ??= new(count);
            _lightProbesOcclusionProbes ??= new(count);
            if (_lightProbesSphericalHarmonicsBuffer == null || _lightProbesSphericalHarmonicsBuffer.count < count)
            {
                if (_lightProbesSphericalHarmonicsBuffer != null)
                    _lightProbesSphericalHarmonicsBuffer.Dispose();
                _lightProbesSphericalHarmonicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, sizeof(SphericalHarmonicsL2));
            }
            if (_lightProbesOcclusionProbesBuffer == null || _lightProbesOcclusionProbesBuffer.count < count)
            {
                if (_lightProbesOcclusionProbesBuffer != null)
                    _lightProbesOcclusionProbesBuffer.Dispose();
                _lightProbesOcclusionProbesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, 4 * 4);
            }
            GPUIUtility.CalculateInterpolatedLightAndOcclusionProbes(ref transformBufferData._perInstanceLightProbesBuffer, p_matrices, managedBufferStartIndex, graphicsBufferStartIndex, count, rsgBufferSize, _lightProbesPositions, _lightProbesSphericalHarmonics, _lightProbesOcclusionProbes, _lightProbesSphericalHarmonicsBuffer, _lightProbesOcclusionProbesBuffer, positionOffset);
        }

        public Transform GetInstanceTransform(int runtimeRenderKey, int bufferIndex)
        {
            if (runtimeRenderKey == 0 || !Instance.RenderSourceProvider.TryGetData(runtimeRenderKey, out GPUIRenderSource rs) || rs.source is not IGPUIInstanceTransformProvider transformProvider)
                return null;
            return transformProvider.GetInstanceTransformWithRenderKey(runtimeRenderKey, bufferIndex);
        }

        public Transform GetInstanceTransformFromRSG(int rsgKey, int rsgBufferIndex)
        {
            if (rsgKey == 0 || !Instance.RenderSourceGroupProvider.TryGetData(rsgKey, out GPUIRenderSourceGroup rsg))
                return null;
            foreach (var rs in rsg.RenderSources)
            {
                if (rsgBufferIndex < rs.bufferStartIndex || rsgBufferIndex >= rs.bufferStartIndex + rs.bufferSize)
                    continue;
                if (rs.source is not IGPUIInstanceTransformProvider transformProvider)
                    return null;
                return transformProvider.GetInstanceTransformWithRenderKey(rs.Key, rsgBufferIndex - rs.bufferStartIndex);
            }
            return null;
        }

        #endregion RenderSource

        #region Camera Data

        internal static void AddCameraData(GPUICameraData cameraData)
        {
            InitializeRenderingSystem();
            Instance.CameraDataProvider.AddCameraData(cameraData);
        }

        #endregion Camera Data

        #region Parameters

        internal void UpdateParameterBufferData()
        {
            foreach (var parameterBufferData in ParameterBufferIndexes.Keys)
            {
                parameterBufferData.SetParameterBufferData();
            }
        }

        public static bool SetGlobalWindVector()
        {
            WindZone[] sceneWindZones = FindObjectsByType<WindZone>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneWindZones.Length; i++)
            {
                if (sceneWindZones[i].mode == WindZoneMode.Directional)
                {
                    Shader.SetGlobalVector("_Wind", new Vector4(sceneWindZones[i].windTurbulence, sceneWindZones[i].windPulseMagnitude, sceneWindZones[i].windPulseFrequency, sceneWindZones[i].windMain));
                    return true;
                }
            }
            return false;
        }

        #endregion Parameters

        #region Editor Methods
#if UNITY_EDITOR
        #region Editor Fields
        private const string PREVIEW_CAMERA_NAME = "Preview Camera";
        public static PlayModeStateChange playModeStateChange = PlayModeStateChange.EnteredEditMode;
        internal static Dictionary<GPUIManager, List<int>> editor_managerUISelectedPrototypeIndexes;
        private static Dictionary<Component, Dictionary<string, int>> editor_UIStoredValues;
        public static Dictionary<GPUIProfile, GPUIProfile> editor_profileRollbackCache;
        public static UnityAction editor_UpdateMethod;
        public static List<Camera> editor_PlayModeFullRenderSceneViewCameras;
        #endregion Editor Fields

        public static int Editor_GetUIStoredValue(Component component, string key)
        {
            if (editor_UIStoredValues == null || string.IsNullOrEmpty(key))
                return 0;
            if (editor_UIStoredValues.TryGetValue(component, out var componentDict) && componentDict != null && componentDict.TryGetValue(key, out int result))
                return result;
            return 0;
        }

        public static void Editor_SetUIStoredValue(Component component, string key, int value)
        {
            editor_UIStoredValues ??= new();
            if (!editor_UIStoredValues.TryGetValue(component, out var componentDict) || componentDict == null)
            {
                componentDict = new();
                editor_UIStoredValues[component] = componentDict;
            }
            componentDict[key] = value;
        }

        private void Editor_HandlePlayModeStates()
        {
            EditorApplication.playModeStateChanged -= Editor_HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += Editor_HandlePlayModeStateChanged;

            EditorApplication.pauseStateChanged -= Editor_HandlePauseStateChanged;
            EditorApplication.pauseStateChanged += Editor_HandlePauseStateChanged;
        }

        private static void Editor_HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            playModeStateChange = state;
            //Debug.Log(GPUIConstants.LOG_PREFIX + "State: " + state + " IsPlaying: " + Application.isPlaying);
            if (!IsActive)
                return;
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    Instance.MaterialProvider.Reset();
                    Instance.CameraDataProvider.Reset();
                    Instance.LODGroupDataProvider.ClearNullValues();
                    Instance.LODGroupDataProvider.DestroyGeneratedLODGroups();
                    Instance.ParameterBuffer.ReleaseBuffers();
                    Instance.ParameterBufferIndexes.Clear();
                    Instance.CameraDataProvider.ClearEditModeCameraData();
                    editor_PlayModeFullRenderSceneViewCameras?.RemoveAll((c) => c == null);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    Instance.MaterialProvider.Reset();
                    Instance.CameraDataProvider.Reset();
                    Instance.LODGroupDataProvider.ClearNullValues();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    Instance.CameraDataProvider.ClearEditModeCameraData();
                    editor_PlayModeFullRenderSceneViewCameras?.RemoveAll((c) => c == null);
                    break;
            }

            if (state == PlayModeStateChange.ExitingPlayMode && editor_profileRollbackCache != null)
            {
                foreach (var item in editor_profileRollbackCache)
                {
                    if (item.Key != null && item.Value != null)
                    {
                        item.Key.CopyValuesFrom(item.Value);
                        Destroy(item.Value);
                    }
                }
                editor_profileRollbackCache = null;
            }

            if (state == PlayModeStateChange.EnteredPlayMode && editor_PlayModeFullRenderSceneViewCameras != null)
            {
                foreach (Camera camera in editor_PlayModeFullRenderSceneViewCameras)
                    Editor_AddSceneViewCameraData(camera);
            }

            Instance.IsPaused = false;
            Instance.MaterialProvider.checkForShaderModifications = true;

            Instance._lastDrawCallFrame = 0;
            Instance._lastDrawCallTime = 0;
            Instance._pendingLightProbeUpdateFrame = -1;
            Instance._lastLightProbeBufferUsedFrame = -1;
        }

        private static void Editor_HandlePauseStateChanged(PauseState state)
        {
            if (!IsActive)
                return;
            Instance.IsPaused = state == PauseState.Paused;
        }

        public static void Editor_CacheProfile(GPUIProfile profile)
        {
            if (!Application.isPlaying) return;
            editor_profileRollbackCache ??= new();
            if (editor_profileRollbackCache.ContainsKey(profile)) return;
            editor_profileRollbackCache[profile] = ScriptableObject.Instantiate(profile);
        }

        public static GPUICameraData Editor_AddSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return null;
            if (!Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
            {
                editModeCameraData = new GPUICameraData(camera);
                editModeCameraData.renderToSceneView = false;
                Instance.CameraDataProvider.AddEditModeCameraData(editModeCameraData);
                Instance.UpdateCommandBuffers(editModeCameraData);

            }
            editor_PlayModeFullRenderSceneViewCameras ??= new();
            if (!editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                editor_PlayModeFullRenderSceneViewCameras.Add(camera);
            return editModeCameraData;
        }

        public static void Editor_RemoveSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return;
            if (Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out GPUICameraData editModeCameraData))
                Instance.CameraDataProvider.RemoveEditModeCameraData(editModeCameraData);
            if (editor_PlayModeFullRenderSceneViewCameras != null && editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                editor_PlayModeFullRenderSceneViewCameras.Remove(camera);
        }

        public static bool Editor_ContainsSceneViewCameraData(Camera camera)
        {
            if (!IsActive || camera == null || camera.cameraType != CameraType.SceneView)
                return false;
            //if (Instance.CameraDataProvider.TryGetEditModeCameraData(camera.GetInstanceID(), out _))
            //    return true;
            if (editor_PlayModeFullRenderSceneViewCameras != null && editor_PlayModeFullRenderSceneViewCameras.Contains(camera))
                return true;
            return false;
        }

        private static void Editor_RenderEditModeCameras_Internal()
        {
            if (!Application.isPlaying && IsActive)
                Instance.CameraDataProvider.RenderEditModeCameras();
        }

        public static void Editor_RenderEditModeCameras()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall -= Editor_RenderEditModeCameras_Internal;
                EditorApplication.delayCall += Editor_RenderEditModeCameras_Internal;
            }
        }
#endif
        #endregion Editor Methods
    }

#if UNITY_EDITOR
    public struct GPUIRenderStatistics
    {
        public uint drawCount;
        public uint shadowDrawCount;
        public uint vertexCount;
        public uint shadowVertexCount;
    }
#endif

    public abstract class GPUISystemExtension : MonoBehaviour, IGPUIDisposable
    {
        public abstract void Dispose();

        public virtual void ReleaseBuffers() { }

        public virtual void OnCreatedRenderSourceGroup(GPUIRenderSourceGroup renderSourceGroup) { }
        public virtual void OnRemovedRenderSourceGroup(int renderSourceGroupKey) { }
        public virtual void OnRenderSourceGroupBufferSizeChanged(GPUIRenderSourceGroup renderSourceGroup) { }

        public virtual void OnCreatedRenderSource(GPUIRenderSource renderSource) { }
        public virtual void OnRemovedRenderSource(int key) { }
        public virtual void OnRenderSourceBufferSizeChanged(GPUIRenderSource renderSource, int previousBufferSize) { }

        public virtual void ExecuteOnPreCull(GPUICameraData cameraData) { }
        public virtual void ExecuteOnPreRender(GPUICameraData cameraData) { }
        public virtual void ExecuteOnPostRender(GPUICameraData cameraData) { }
    }
}
