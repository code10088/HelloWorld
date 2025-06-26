// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Rendering;

namespace GPUInstancerPro
{
    class GPUIShaderProcessor : IPreprocessShaders
    {
        public int callbackOrder { get { return 0; } }
        private ShaderKeyword _DOTS_INSTANCING_ON_Keyword;
        private ShaderKeyword _PROCEDURAL_INSTANCING_ON_Keyword;
        private ShaderKeyword _GPUI_OBJECT_MOTION_VECTOR_ON_Keyword;
        private ShaderKeyword _GPUI_PER_INSTANCE_LIGHTPROBES_ON_Keyword;

        public GPUIShaderProcessor()
        {
            _DOTS_INSTANCING_ON_Keyword = new ShaderKeyword("DOTS_INSTANCING_ON");
            _PROCEDURAL_INSTANCING_ON_Keyword = new ShaderKeyword("PROCEDURAL_INSTANCING_ON");
            _GPUI_OBJECT_MOTION_VECTOR_ON_Keyword = new ShaderKeyword(GPUIConstants.Kw_GPUI_OBJECT_MOTION_VECTOR_ON);
            _GPUI_PER_INSTANCE_LIGHTPROBES_ON_Keyword = new ShaderKeyword(GPUIConstants.Kw_GPUI_PER_INSTANCE_LIGHTPROBES_ON);
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            GPUIEditorSettings editorSettings = GPUIEditorSettings.Instance;
            bool isGPUIGenerated = shader.name.Contains("GPUInstancerPro");
            //bool isGPUIProShader = isGPUIGenerated || GPUIShaderBindings.Instance.IsShaderSetupForGPUIAnyExtension(shader.name);
            for (int i = 0; i < data.Count; ++i)
            {
                ShaderCompilerData compilerData = data[i];
                bool isProceduralInstancingOn = compilerData.shaderKeywordSet.IsEnabled(_PROCEDURAL_INSTANCING_ON_Keyword);
                if (editorSettings.stripDOTSInstancingVariants)
                {
                    // Remove variants with DOTS_INSTANCING_ON from GPUI shaders and remove variants with both DOTS_INSTANCING_ON and PROCEDURAL_INSTANCING_ON keyword
                    if (compilerData.shaderKeywordSet.IsEnabled(_DOTS_INSTANCING_ON_Keyword) && (isProceduralInstancingOn || isGPUIGenerated))
                    {
                        data.RemoveAt(i);
                        --i;
                        continue;
                    }
                }

                if (editorSettings.stripNonProceduralGPUIGeneratedVariants && isGPUIGenerated && !isProceduralInstancingOn)
                {
                    data.RemoveAt(i);
                    --i;
                    continue;
                }

                if ((editorSettings.stripObjectMotionVectorVariants && compilerData.shaderKeywordSet.IsEnabled(_GPUI_OBJECT_MOTION_VECTOR_ON_Keyword))
                    || (editorSettings.stripPerInstanceLightProbeVariants && compilerData.shaderKeywordSet.IsEnabled(_GPUI_PER_INSTANCE_LIGHTPROBES_ON_Keyword)))
                {
                    data.RemoveAt(i);
                    --i;
                    continue;
                }
            }
        }

        public static string GetEnabledKeywords(ShaderCompilerData data)
        {
            var keywordSet = data.shaderKeywordSet;
            var keywords = keywordSet.GetShaderKeywords();
            System.Text.StringBuilder result = new();

            foreach (var keyword in keywords)
            {
                if (keywordSet.IsEnabled(keyword))
                {
                    result.Append(keyword.name);
                    result.Append(" ");
                }
            }

            return result.ToString().Trim();
        }
    }
}
