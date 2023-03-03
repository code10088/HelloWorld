using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RRenderPipeline : RenderPipeline
{
    private RRenderPipelineAsset asset;
    private static Material errorMaterial;
    private ShaderTagId[] legacyShaderTagIds;
    private const string bufferName = "Render Camera";
    private CommandBuffer buffer = new CommandBuffer() { name = bufferName };
    public RRenderPipeline(RRenderPipelineAsset asset)
    {
        this.asset = asset;
        errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        legacyShaderTagIds = new ShaderTagId[asset.legacyShaderTagIds.Length];
        for (int i = 0; i < asset.legacyShaderTagIds.Length; i++)
        {
            legacyShaderTagIds[i] = new ShaderTagId(asset.legacyShaderTagIds[i]);
        }
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //会有显示Scene视图的SceneCamera，点击Camera时显示Preview视图的PreviewCamera，以及场景中我们添加的Camera
        foreach (Camera camera in cameras)
        {
            Init(context, camera);
            DrawSceneUI(camera);
            DrawOpaque(context, camera);
            DrawSkybox(context, camera);
            DrawTransparent(context, camera);
            DrawUnsupportedShaders(context, camera);
            DrawGizmos(context, camera);
            Submit(context);
        }
    }
    private void Init(ScriptableRenderContext context, Camera camera)
    {
        //根据当前Camera，更新内置Shader的变量
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    private void Submit(ScriptableRenderContext context)
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        context.Submit();
    }
    private void DrawSceneUI(Camera camera)
    {
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
#endif
    }
    private void DrawOpaque(ScriptableRenderContext context, Camera camera)
    {
        //获取当前相机的剔除规则
        camera.TryGetCullingParameters(out var cullingParameters);
        CullingResults cullingResults = context.Cull(ref cullingParameters);
        //生成DrawingSettings,Opaque物体从前往后渲染
        SortingSettings sortingSettings = new SortingSettings(camera);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        DrawingSettings drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings);
        drawingSettings.SetShaderPassName(1, new ShaderTagId("CustomLightModeTag"));
        //生成FilteringSettings,过滤出Opaque物体
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    private void DrawSkybox(ScriptableRenderContext context, Camera camera)
    {
        context.DrawSkybox(camera);
    }
    private void DrawTransparent(ScriptableRenderContext context, Camera camera)
    {
        //获取当前相机的剔除规则
        camera.TryGetCullingParameters(out var cullingParameters);
        CullingResults cullingResults = context.Cull(ref cullingParameters);
        //生成DrawingSettings,Transparent物体从前往后渲染
        SortingSettings sortingSettings = new SortingSettings(camera);
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        DrawingSettings drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings);
        drawingSettings.SetShaderPassName(1, new ShaderTagId("CustomLightModeTag"));
        //生成FilteringSettings,过滤出Transparent物体
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    private void DrawUnsupportedShaders(ScriptableRenderContext context, Camera camera)
    {
        camera.TryGetCullingParameters(out var cullingParameters);
        CullingResults cullingResults = context.Cull(ref cullingParameters);
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIds[0], sortingSettings);
        drawingSettings.overrideMaterial = errorMaterial;
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    private void DrawGizmos(ScriptableRenderContext context, Camera camera)
    {
#if UNITY_EDITOR
        if (UnityEditor.Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
#endif
    }
}
