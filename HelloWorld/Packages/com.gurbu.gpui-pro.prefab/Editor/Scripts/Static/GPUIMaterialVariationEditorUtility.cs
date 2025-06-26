// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    public static class GPUIMaterialVariationEditorUtility
    {
        public static bool IsValidVariationDefinition(GPUIMaterialVariationDefinition variationDefinition)
        {
            if (variationDefinition == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Variation definition is null!");
                return false;
            }
            if (variationDefinition.material == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Variation definition Material is null!");
                return false;
            }
            if (variationDefinition.material.shader == null || variationDefinition.material.shader.name == GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Variation definition Materials Shader is null!");
                return false;
            }
            return true;
        }

        public static void GenerateShader(GPUIMaterialVariationDefinition variationDefinition)
        {
            if (!IsValidVariationDefinition(variationDefinition))
                return;

            GenerateHLSLIncludeFile(variationDefinition);

            if (!GPUIShaderBindings.Instance.GetInstancedShader(variationDefinition.material.shader, null, out Shader shader))
                shader = variationDefinition.material.shader;

            string originalShaderPath = AssetDatabase.GetAssetPath(shader);
            if (originalShaderPath.EndsWith(".shadergraph"))
            {
                variationDefinition.replacementShader = shader;
                GenerateSubGraph(variationDefinition);
                return;
            }
            string createAtPath = null;
            if (variationDefinition.replacementShader != null)
                createAtPath = AssetDatabase.GetAssetPath(variationDefinition.replacementShader);
            if (originalShaderPath.StartsWith("Packages/") && string.IsNullOrEmpty(createAtPath))
                createAtPath = variationDefinition.GetAssetFolderPath() + variationDefinition.name.Replace("_GPUIVariationDefinition", "_Variation") + Path.GetExtension(originalShaderPath);

            string includeRelativePath = GPUIUtility.GetRelativePathForShader(createAtPath != null ? createAtPath : originalShaderPath, AssetDatabase.GetAssetPath(variationDefinition.shaderIncludeFile));

            string setupText = "#include_with_pragmas \"" + includeRelativePath + "\"";
            setupText += "\n#pragma instancing_options procedural:setupVariationGPUI";
            setupText += "\n#pragma multi_compile_instancing";
            setupText += "\n#pragma shader_feature_local _ " + GPUIPrefabConstants.Kw_GPUI_MATERIAL_VARIATION + "\n";

            variationDefinition.replacementShader = GPUIShaderUtility.CreateInstancedShader(shader, null, setupText, false, "GPUInstancerPro/Variation/" + variationDefinition.name.Replace("_GPUIVariationDefinition", "_"), variationDefinition.name.Replace("_GPUIVariationDefinition", "_Variation"), createAtPath);
            if (variationDefinition.replacementShader == null)
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_shaderConversion, shader.name), shader);

            EditorUtility.SetDirty(variationDefinition);
        }

        public static void GenerateSubGraph(GPUIMaterialVariationDefinition variationDefinition)
        {
            if (!IsValidVariationDefinition(variationDefinition))
                return;

            string subGraphText = GPUIPrefabEditorConstants.MaterialVariationsSubGraphTemplate;
            subGraphText = subGraphText.Replace(GPUIPrefabEditorConstants.PLACEHOLDER_IncludeFileGUID, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variationDefinition.shaderIncludeFile)));
            subGraphText = subGraphText.Replace(GPUIPrefabEditorConstants.PLACEHOLDER_MaterialVariationKeyword, GPUIPrefabConstants.Kw_GPUI_MATERIAL_VARIATION);

            string folderPath = variationDefinition.GetAssetFolderPath();
            string filePath = folderPath + variationDefinition.name.Replace("_GPUIVariationDefinition", "_GPUIVariationSetup") + ".shadersubgraph";

            subGraphText.SaveToTextFile(filePath);

            Debug.Log(GPUIConstants.LOG_PREFIX + "Sub Graph generated for the shader " + variationDefinition.material.shader.name + ". Please add this Sub Graph to the Shader Graph instead of the GPUI Setup Node to apply variations.", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath));
        }

        public static void GenerateHLSLIncludeFile(GPUIMaterialVariationDefinition variationDefinition)
        {
            if (!IsValidVariationDefinition(variationDefinition))
                return;

            string includeFileText = GPUIPrefabEditorConstants.MaterialVariationsHLSLTemplate;
            includeFileText = includeFileText.Replace(GPUIPrefabEditorConstants.PLACEHOLDER_VariationBufferName, variationDefinition.bufferName);
            includeFileText = includeFileText.Replace(GPUIPrefabEditorConstants.PLACEHOLDER_MaterialVariationKeyword, GPUIPrefabConstants.Kw_GPUI_MATERIAL_VARIATION);

            string variationSetupCode = "";
            int count = variationDefinition.items.Length;
            for (int i = 0; i < count; i++)
            {
                variationSetupCode += "\n    " + variationDefinition.items[i].propertyName + " = " + variationDefinition.bufferName + "[gpui_InstanceID * " + count + " + " + i + "]";
                if (variationDefinition.items[i].variationType == GPUIMaterialVariationType.Float || variationDefinition.items[i].variationType == GPUIMaterialVariationType.Integer)
                    variationSetupCode += ".x;";
                else
                    variationSetupCode += ";";
            }
            includeFileText = includeFileText.Replace(GPUIPrefabEditorConstants.PLACEHOLDER_VariationSetupCode, variationSetupCode);

            string filePath;
            if (variationDefinition.shaderIncludeFile != null)
                filePath = AssetDatabase.GetAssetPath(variationDefinition.shaderIncludeFile);
            else
            {
                string folderPath = variationDefinition.GetAssetFolderPath();
                filePath = folderPath + variationDefinition.name.Replace("Definition", "Include") + ".hlsl";
            }

            includeFileText.SaveToTextFile(filePath);

            variationDefinition.shaderIncludeFile = AssetDatabase.LoadAssetAtPath<ShaderInclude>(filePath);
            EditorUtility.SetDirty(variationDefinition);

            Debug.Log(GPUIConstants.LOG_PREFIX + "Shader include file has been successfully generated at path: " + filePath, variationDefinition.shaderIncludeFile);
        }
    }
}
