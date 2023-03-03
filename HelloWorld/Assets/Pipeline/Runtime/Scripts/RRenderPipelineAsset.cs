using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/RenderPipeline/RRenderPipeline")]
public class RRenderPipelineAsset : RenderPipelineAsset
{
    public string[] legacyShaderTagIds = new string[] { "Always" };
    protected override RenderPipeline CreatePipeline()
    {
        return new RRenderPipeline(this);
    }
}
