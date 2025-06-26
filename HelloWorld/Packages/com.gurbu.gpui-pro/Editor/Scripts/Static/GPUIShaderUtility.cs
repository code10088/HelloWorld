// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

namespace GPUInstancerPro
{
    public static class GPUIShaderUtility
    {
        private static string INCLUDE_BUILTIN_FILE = "\"UnityCG.cginc\"";
        private static string INCLUDE_BUILTIN = "#include " + INCLUDE_BUILTIN_FILE + " // Added by GPUIPro\n";
        private static string INCLUDE_URP = "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\" // Added by GPUIPro\n";
        private static string INCLUDE_HDRP = "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl\" // Added by GPUIPro\n#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl\" // Added by GPUIPro\n";

        private static List<string> FILES_INCLUDING_INSTANCING = new List<string>
        {
            "\"UnityCG.cginc\"",
            "\"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"",
            "\"Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl\"",
            "\"Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl\"",
        };

        private static string BUILTIN_START_DEFINES = "";
        private static string URP_START_DEFINES = "#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro\n";
        private static string HDRP_START_DEFINES = "";

        private static string SEARCH_START_CGPROGRAM = "CGPROGRAM";
        private static string SEARCH_END_ENDCG = "ENDCG";
        private static string SEARCH_START_HLSLPROGRAM = "HLSLPROGRAM";
        private static string SEARCH_END_ENDHLSL = "ENDHLSL";
        private static string PRAGMA_GPUI_SETUP = "#pragma instancing_options procedural:setupGPUI\n#pragma multi_compile_instancing\n";
        private static string PRAGMA_URP_OBJECTMOTIONVECTORS = "#include_with_pragmas \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl\"";
        private static string FILE_NAME_SUFFIX = "_GPUIPro";

        private static List<string> CROWD_WHITE_LIST = new List<string>
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Complex Lit",
            "HDRP/Lit",
            "HDRP/LayeredLit",
            "HDRP/Unlit",
        };

        public static void AutoShaderConverterUpdate()
        {
            GPUIMaterialProvider materialProvider = GPUIRenderingSystem.Instance.MaterialProvider;
            if (materialProvider.checkForShaderModifications)
            {
                materialProvider.checkForShaderModifications = false;
                CheckForShaderModifications();
            }
            if (materialProvider.TryGetShaderToConvert(out GPUIMaterialProvider.GPUIShaderConversionData shaderConversionData))
            {
                if (!SetupShaderForGPUI(shaderConversionData.shader, shaderConversionData.extensionCode, false, true))
                    materialProvider.AddToFailedShaderConversions(shaderConversionData);
            }
            if (materialProvider.TryGetMaterialVariant(out Material material) && GPUIEditorSettings.Instance.isGenerateShaderVariantCollection)
                AddShaderVariantToCollection(material.shader, material.shaderKeywords);
        }

        public static bool SetupShaderForGPUI(Shader shader, string extensionCode, bool logIfExists = true, bool logError = true, bool isCheckWhiteList = true)
        {
            if (shader == null || shader.name == GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
            {
                if (logError)
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find shader! Please make sure that the material has a shader assigned.");
                return false;
            }
            GPUIShaderBindings.Instance.ClearEmptyShaderInstances();
            if (!GPUIShaderBindings.Instance.IsShaderSetupForGPUI(shader.name, extensionCode))
            {
                if (IsShaderInstanced(shader, extensionCode))
                {
                    GPUIShaderBindings.Instance.AddShaderInstance(shader.name, shader, extensionCode, true);
                    if (logIfExists)
                        Debug.Log(GPUIConstants.LOG_PREFIX + "Shader setup for " + shader.name + " has been successfully completed.", shader);
                    return true;
                }
                else
                {
                    Shader namedShader = Shader.Find(GPUIUtility.ConvertToGPUIShaderName(shader.name, extensionCode));
                    if (namedShader == null && ProcessShaderImports(shader, extensionCode))
                        return true;

                    if (namedShader != null && IsShaderInstanced(namedShader, extensionCode))
                    {
                        GPUIShaderBindings.Instance.AddShaderInstance(shader.name, namedShader, extensionCode, false);
                        return true;
                    }

                    Shader instancedShader = CreateInstancedShader(shader, extensionCode, null, false, null, null, null, isCheckWhiteList);
                    if (instancedShader != null && !string.IsNullOrEmpty(instancedShader.name))
                    {
                        GPUIShaderBindings.Instance.AddShaderInstance(shader.name, instancedShader, extensionCode);
                        return true;
                    }
                    else
                    {
                        if (logError)
                            LogShaderError(shader, extensionCode);

                        return false;
                    }
                }
            }
            else
            {
                if (logIfExists)
                    Debug.Log(GPUIConstants.LOG_PREFIX + shader.name + " shader has already been setup for GPUI.", shader);
                return true;
            }
        }

        private static readonly string SPEEDTREE8_SHADER_PACKAGE = "Packages/com.gurbu.gpui-pro/Editor/Extras/Shader_SpeedTree8_GPUIPro.unitypackage";
        private static readonly string SPEEDTREE9_SHADER_PACKAGE = "Packages/com.gurbu.gpui-pro/Editor/Extras/Shader_SpeedTree9_GPUIPro.unitypackage";
        private static bool SPEEDTREE8_SHADER_IMPORTED = false;
        private static bool SPEEDTREE9_SHADER_IMPORTED = false;
        private static bool ProcessShaderImports(Shader shader, string extensionCode)
        {
            if (Application.isPlaying)
                return false;
            string shaderName = shader.name;
            if (shaderName == GPUIConstants.SHADER_UNITY_SPEEDTREE8)
            {
                if (SPEEDTREE8_SHADER_IMPORTED)
                    return true;
                SPEEDTREE8_SHADER_IMPORTED = true;
                ImportShaderPackage(SPEEDTREE8_SHADER_PACKAGE, shaderName);
                return true;
            }
            else if (shaderName == GPUIConstants.SHADER_UNITY_SPEEDTREE9)
            {
                if (SPEEDTREE9_SHADER_IMPORTED)
                    return true;
                SPEEDTREE9_SHADER_IMPORTED = true;
                ImportShaderPackage(SPEEDTREE9_SHADER_PACKAGE, shaderName);
                return true;
            }
            return false;
        }

        private static void ImportShaderPackage(string shaderPackage, string shaderName)
        {
            Debug.Log(GPUIConstants.LOG_PREFIX + "Importing GPUI shader package: " + shaderPackage);
            EditorUtility.DisplayCancelableProgressBar("Importing Package", "Importing GPUI shader for " + shaderName + "...", 0.11f);
            AssetDatabase.ImportPackage(shaderPackage, false);
            EditorUtility.ClearProgressBar();
        }

        private static void LogShaderError(Shader shader, string extensionCode)
        {
            string originalAssetPath = AssetDatabase.GetAssetPath(shader).ToLower();
            if (originalAssetPath.EndsWith(".shadergraph"))
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_shaderGraph, shader.name), shader);
            else if (originalAssetPath.EndsWith(".surfshader"))
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_surfshader, shader.name), shader);
            else if (originalAssetPath.EndsWith(".stackedshader"))
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_stackedshader, shader.name), shader);
            else if (extensionCode == GPUIConstants.EXTENSION_CODE_CROWD)
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_shaderConversionWithCrowd, shader.name, extensionCode), shader);
            else
                Debug.LogError(GPUIConstants.LOG_PREFIX + string.Format(GPUIEditorConstants.ERRORTEXT_shaderConversion, shader.name), shader);
        }

        public static bool IsShaderInstanced(Shader shader, string extensionCode)
        {
            if (shader == null || shader.name == GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find shader! Please make sure that the material has a shader assigned.");
                return false;
            }
            string originalAssetPath = AssetDatabase.GetAssetPath(shader);
            string originalShaderText;
            try
            {
                originalShaderText = File.ReadAllText(originalAssetPath);
            }
            catch (Exception)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(originalShaderText))
            {
                switch (extensionCode)
                {
#if GPUI_CROWD
                    case GPUIConstants.EXTENSION_CODE_CROWD:
                        if (originalAssetPath.ToLower().EndsWith(".shadergraph"))
                            return originalShaderText.Contains("GPUI Pro Crowd Setup");
                        else if (originalAssetPath.ToLower().EndsWith(".stackedshader"))
                            return originalShaderText.Contains("08196cc3a5cc8d44698d705c7fde3b68"); // guid for GPUI Stackable for Better Shaders
                        else
                            return originalShaderText.Contains("GPUICrowdSetup.hlsl");
#endif
                    default:
                        if (originalAssetPath.ToLower().EndsWith(".shadergraph"))
                            return originalShaderText.Contains("GPU Instancer Pro Setup") || originalShaderText.Contains("GPUIVariationSetup") || originalShaderText.Contains("GPUI Pro Crowd Setup");
                        else if (originalAssetPath.ToLower().EndsWith(".stackedshader"))
                            return originalShaderText.Contains("08196cc3a5cc8d44698d705c7fde3b68"); // guid for GPUI Stackable for Better Shaders
                        else
                            return originalShaderText.Contains("GPUInstancerSetup.hlsl");
                }
            }
            return false;
        }

        public static void RegenerateShaders()
        {
            GPUIShaderBindings.Instance.ClearEmptyShaderInstances();
            List<GPUIShaderInstance> shaderInstances = GPUIShaderBindings.Instance.shaderInstances;
            if (shaderInstances != null)
            {
                for (int i = 0; i < shaderInstances.Count; i++)
                {
                    GPUIShaderInstance shaderInstance = shaderInstances[i];
                    Shader originalShader = shaderInstance.originalShader;
                    if (originalShader == null) continue;
                    if (!IsShaderInstanced(originalShader, shaderInstance.extensionCode))
                    {
                        Shader replacementShader = CreateInstancedShader(originalShader, shaderInstance.extensionCode);
                        if (replacementShader == null)
                        {
#if GPUIPRO_DEVMODE
                            Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can not regenerate shader: " + shaderInstance.shaderName, originalShader);
#endif
                            continue;
                        }
                        shaderInstance.replacementShaderName = replacementShader.name;
                        shaderInstance.modifiedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        shaderInstance.isUseOriginal = true;
                        shaderInstance.replacementShaderName = shaderInstance.shaderName;
                    }
                }
                EditorUtility.SetDirty(GPUIShaderBindings.Instance);
            }
        }

        public static void RegenerateShader(string originalShaderName)
        {
            if (string.IsNullOrEmpty(originalShaderName))
                return;
            List<GPUIShaderInstance> shaderInstances = GPUIShaderBindings.Instance.shaderInstances;
            Shader originalShader = Shader.Find(originalShaderName);
            if (shaderInstances != null && originalShader != null)
            {
                for (int i = 0; i < shaderInstances.Count; i++)
                {
                    GPUIShaderInstance shaderInstance = shaderInstances[i];
                    if (shaderInstance.shaderName == originalShaderName)
                    {
                        if (!IsShaderInstanced(originalShader, shaderInstance.extensionCode))
                        {
                            Shader replacementShader = CreateInstancedShader(originalShader, shaderInstance.extensionCode);
                            if (replacementShader == null)
                            {
                                LogShaderError(originalShader, shaderInstance.extensionCode);
                                return;
                            }
                            shaderInstance.replacementShaderName = replacementShader.name;
                            shaderInstance.modifiedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                            shaderInstance.isUseOriginal = true;
                    }
                }
                EditorUtility.SetDirty(GPUIShaderBindings.Instance);
            }
        }

        public static void RemoveShaderInstance(string originalShaderName)
        {
            if (string.IsNullOrEmpty(originalShaderName))
                return;
            List<GPUIShaderInstance> shaderInstances = GPUIShaderBindings.Instance.shaderInstances;
            if (shaderInstances != null)
            {
                for (int i = 0; i < shaderInstances.Count; i++)
                {
                    GPUIShaderInstance shaderInstance = shaderInstances[i];
                    if (shaderInstance.shaderName == originalShaderName)
                    {
                        shaderInstances.RemoveAt(i);
                        break;
                    }
                }
                EditorUtility.SetDirty(GPUIShaderBindings.Instance);
            }
        }

        public static void CheckForShaderModifications()
        {
            List<GPUIShaderInstance> shaderInstances = GPUIShaderBindings.Instance.shaderInstances;
            if (shaderInstances != null)
            {
#if UNITY_EDITOR
                bool modified = false;

                modified |= shaderInstances.RemoveAll(si => si == null || si.replacementShader == null || string.IsNullOrEmpty(si.shaderName)) > 0;

                if (GPUIEditorSettings.Instance.isAutoShaderConversion)
                {
                    for (int i = 0; i < shaderInstances.Count; i++)
                    {
                        GPUIShaderInstance shaderInstance = shaderInstances[i];
                        if (shaderInstance.isUseOriginal)
                            continue;

                        Shader originalShader = Shader.Find(shaderInstance.shaderName);
                        if (originalShader == null)
                        {
                            modified = true;
                            shaderInstances.RemoveAt(i);
                            i--;
                            continue;
                        }
                        string originalAssetPath = AssetDatabase.GetAssetPath(originalShader);
                        DateTime lastWriteTime = File.GetLastWriteTime(originalAssetPath);
                        if (lastWriteTime >= DateTime.Now)
                            continue;

                        DateTime instancedTime = DateTime.MinValue;
                        bool isValidDate = false;
                        if (!string.IsNullOrEmpty(shaderInstance.modifiedDate))
                            isValidDate = DateTime.TryParseExact(shaderInstance.modifiedDate, "MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out instancedTime);
                        if (!isValidDate || lastWriteTime > Convert.ToDateTime(shaderInstance.modifiedDate, System.Globalization.CultureInfo.InvariantCulture))
                        {
                            modified = true;
                            if (!IsShaderInstanced(originalShader, shaderInstance.extensionCode))
                            {
                                shaderInstance.replacementShaderName = CreateInstancedShader(originalShader, shaderInstance.extensionCode).name;
                                shaderInstance.modifiedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                                shaderInstance.isUseOriginal = true;
                        }
                    }
                }

                // remove non unique instances
                for (int i = 0; i < shaderInstances.Count; i++)
                {
                    for (int j = 0; j < shaderInstances.Count; j++)
                    {
                        if (i == j)
                            continue;
                        if (shaderInstances[i].shaderName == shaderInstances[j].shaderName && shaderInstances[i].extensionCode == shaderInstances[j].extensionCode)
                        {
                            shaderInstances.RemoveAt(i);
                            i--;
                            modified = true;
                            break;
                        }
                    }
                }

                if (modified)
                    EditorUtility.SetDirty(GPUIShaderBindings.Instance);
#endif
            }
        }

        #region Auto Shader Conversion

        #region Auto Shader Conversion Helper Methods

        private static string GetShaderIncludePath(string originalAssetPath, bool createInDefaultFolder, out string newAssetPath)
        {
            string includePath = GPUIConstants.GetPackagesPath() + "Runtime/Shaders/Include/GPUInstancerSetup.hlsl";
            newAssetPath = originalAssetPath;
            string[] oapSplit = originalAssetPath.Split('/');
            if (createInDefaultFolder)
            {
                string generatedShaderPath = GPUIConstants.GetGeneratedShaderPath();
                if (!Directory.Exists(generatedShaderPath))
                    Directory.CreateDirectory(generatedShaderPath);

                newAssetPath = generatedShaderPath + oapSplit[oapSplit.Length - 1];
            }

            return includePath;
        }

        private static string AddIncludeAndPragmaDirectives(string includePath, string newShaderText, string setupText, bool doubleEscape, string extensionCode, string shaderDirectory)
        {
            int searchOffset = 0;
            string searchStart;
            int blockStartIndex;

            string additionTextEnd = "\n";
            string additionTextStart = "\n";
            string rpInclude = INCLUDE_BUILTIN;

            switch (GPUIRuntimeSettings.Instance.RenderPipeline)
            {
                case GPUIRenderPipeline.BuiltIn:
                    additionTextStart += BUILTIN_START_DEFINES;
                    rpInclude = INCLUDE_BUILTIN;
                    break;
                case GPUIRenderPipeline.URP:
                    additionTextStart += URP_START_DEFINES;
                    rpInclude = INCLUDE_URP;
                    break;
                case GPUIRenderPipeline.HDRP:
                    additionTextStart += HDRP_START_DEFINES;
                    rpInclude = INCLUDE_HDRP;
                    break;
            }

            additionTextEnd += "#include_with_pragmas \"" + includePath + "\"\n";
            if (extensionCode == GPUIConstants.EXTENSION_CODE_CROWD)
                additionTextEnd += "#include_with_pragmas \"Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl\"\n";
            additionTextEnd += setupText;

            if (GPUIRuntimeSettings.Instance.IsURP && newShaderText.Contains(PRAGMA_URP_OBJECTMOTIONVECTORS))
            {
                string includeContent = File.ReadAllText("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl");
                includeContent = includeContent.Replace("#pragma multi_compile_instancing", "");
                int indexOfURPMotionVectorsHLSL = newShaderText.IndexOf(PRAGMA_URP_OBJECTMOTIONVECTORS);
                while (indexOfURPMotionVectorsHLSL > 0)
                {
                    newShaderText = newShaderText.Remove(indexOfURPMotionVectorsHLSL, PRAGMA_URP_OBJECTMOTIONVECTORS.Length);
                    newShaderText = newShaderText.Insert(indexOfURPMotionVectorsHLSL, includeContent);
                    indexOfURPMotionVectorsHLSL = newShaderText.IndexOf(PRAGMA_URP_OBJECTMOTIONVECTORS, indexOfURPMotionVectorsHLSL + 1);
                }
            }

            string newLineString = "\n";
            if (doubleEscape)
            {
                additionTextStart = additionTextStart.Replace("\n", "\\n").Replace("\"", "\\\"");
                additionTextEnd = additionTextEnd.Replace("\n", "\\n").Replace("\"", "\\\"");
                newLineString = "\\n";
            }

            while (true)
            {
                int nextHLSLIndex = newShaderText.IndexOf(SEARCH_START_HLSLPROGRAM, searchOffset);
                int nextCGIndex = newShaderText.IndexOf(SEARCH_START_CGPROGRAM, searchOffset);

                if (nextHLSLIndex >= 0 && nextCGIndex >= 0)
                {
                    if (nextHLSLIndex < nextCGIndex)
                    {
                        blockStartIndex = nextHLSLIndex;
                        searchStart = SEARCH_START_HLSLPROGRAM;
                    }
                    else
                    {
                        blockStartIndex = nextCGIndex;
                        searchStart = SEARCH_START_CGPROGRAM;
                    }
                }
                else if (nextCGIndex >= 0)
                {
                    blockStartIndex = nextCGIndex;
                    searchStart = SEARCH_START_CGPROGRAM;
                }
                else if (nextHLSLIndex >= 0)
                {
                    blockStartIndex = nextHLSLIndex;
                    searchStart = SEARCH_START_HLSLPROGRAM;
                }
                else
                    break;

                int insertPointStart = blockStartIndex + searchStart.Length;
                newShaderText = newShaderText.Insert(insertPointStart, additionTextStart);

                searchOffset = insertPointStart + additionTextStart.Length;

                int foundEndCG = newShaderText.IndexOf(SEARCH_END_ENDCG, searchOffset);
                int foundEndHLSL = newShaderText.IndexOf(SEARCH_END_ENDHLSL, searchOffset);

                int blockEndIndex;
                if (foundEndCG >= 0 && foundEndHLSL >= 0)
                    blockEndIndex = Math.Min(foundEndCG, foundEndHLSL);
                else if (foundEndCG >= 0)
                    blockEndIndex = foundEndCG;
                else if (foundEndHLSL >= 0)
                    blockEndIndex = foundEndHLSL;
                else
                    break;

                int blockTextLength = blockEndIndex - blockStartIndex;
                string blockText = newShaderText.Substring(blockStartIndex, blockTextLength);

                string additionTextEndCache = additionTextEnd;

                searchOffset = ProcessShaderPass(ref newShaderText, additionTextEnd, rpInclude, blockStartIndex, blockEndIndex, blockText, extensionCode, shaderDirectory, newLineString, out bool isSuccessful);
                if (!isSuccessful)
                    return null;

                additionTextEnd = additionTextEndCache;
            }

            return newShaderText;
        }

        private static int ProcessShaderPass(ref string newShaderText, string additionTextEnd, string rpInclude, int blockStartIndex, int blockEndIndex, string blockText, string extensionCode, string shaderDirectory, string newLineString, out bool isSuccessful)
        {
            isSuccessful = true;
            bool isCrowd = extensionCode == GPUIConstants.EXTENSION_CODE_CROWD;
            string wrapperCode = null;
            if (isCrowd)
            {
                if (!TryGetCrowdVertexMethodWrapper(blockText, shaderDirectory, newLineString, out wrapperCode, out string vertexMethodName))
                {
                    if (GPUIRuntimeSettings.Instance.IsHDRP && blockText.Contains("#pragma raytracing"))
                    {
                        isCrowd = false;
                    }
                    else
                    {
                        isSuccessful = false;
                        return -1;
                    }
                }
                else
                {
                    // Replace #pragma vertex directive
                    int pragmaIndex = blockText.IndexOf("#pragma vertex");
                    if (pragmaIndex != -1)
                    {
                        int lineEnd = blockText.IndexOf('\n', pragmaIndex);
                        if (lineEnd == -1)
                            lineEnd = blockText.Length;

                        string originalLine = blockText.Substring(pragmaIndex, lineEnd - pragmaIndex);
                        string trimmedLine = originalLine.Trim();

                        string[] parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 3)
                        {
                            string newLine = "#pragma vertex " + parts[2] + "GPUICA";
                            blockText = blockText.Substring(0, pragmaIndex) + newLine + blockText.Substring(lineEnd);
                        }
                    }

                    // Replace the original block with the updated blockText (containing modified pragma)
                    newShaderText = newShaderText.Remove(blockStartIndex, blockEndIndex - blockStartIndex);
                    newShaderText = newShaderText.Insert(blockStartIndex, blockText);

                    // Adjust blockEndIndex after replacement (in case size changed)
                    blockEndIndex = blockStartIndex + blockText.Length;

                    // Here replace the original vertex method with GPUICA
                    additionTextEnd += "#ifdef GPUI_PRO_CROWD_ACTIVE" + newLineString + "    #undef UNITY_SETUP_INSTANCE_ID" + newLineString + "    #define UNITY_SETUP_INSTANCE_ID(input)" + newLineString + "#endif" + newLineString;
                }
            }

            string matchedInclude = null;

            int includeIndex = -1;
            foreach (string item in FILES_INCLUDING_INSTANCING)
            {
                includeIndex = blockText.IndexOf(item);
                if (includeIndex != -1)
                {
                    matchedInclude = item;
                    break;
                }
            }

            int searchOffset = blockEndIndex + additionTextEnd.Length + 1;
            if (matchedInclude != null)
            {
                int globalIncludeIndex = blockStartIndex + includeIndex + matchedInclude.Length;
                newShaderText = newShaderText.Insert(globalIncludeIndex, additionTextEnd);
                blockEndIndex += additionTextEnd.Length;
            }
            else
            {
                newShaderText = newShaderText.Insert(blockEndIndex, newLineString + rpInclude + additionTextEnd);
                searchOffset += rpInclude.Length + 2;
                blockEndIndex += additionTextEnd.Length + rpInclude.Length + newLineString.Length;
            }

            if (isCrowd)
            {
                newShaderText = newShaderText.Insert(blockEndIndex, wrapperCode);
                searchOffset += wrapperCode.Length;
            }

            return searchOffset;
        }

        /// <param name="blockText">Shader pass code between HLSLPROGRAM-ENDHLSL or CGPROGRAM-ENGCG</param>
        /// <param name="wrapperCode">Vertex method wrapper for Crowd Animations</param>
        /// <returns>True, if successfully created wrapper method string.</returns>
        private static bool TryGetCrowdVertexMethodWrapper(string blockText, string shaderDirectory, string newLineString, out string wrapperCode, out string methodName)
        {
            wrapperCode = string.Empty;
            if (!TryFindVertexMethodName(blockText, out methodName))
            {
                //if (!TryFindVertexMethodNameInIncludes(blockText, shaderDirectory, out methodName)) // TODO Vertex pragma needs to be on the original file so we can modify it. Inline?
                    return false;
            }

            if (!TryFindMethodSignatureInText(methodName, blockText, out string returnType, out string inputType))
            {
                if (!TryFindMethodSignatureInIncludes(blockText, methodName, shaderDirectory, out returnType, out inputType))
                    return false;
            }

            if (!TryFindVertexStructFields(inputType, blockText, shaderDirectory, out string vertexField, out string normalField, out string tangentField))
                return false;

            string crowdMethod = string.Empty;
            if (GPUIRuntimeSettings.Instance.IsHDRP)
            {
                if (blockText.Contains("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"))
                {
                    normalField = null;
                    tangentField = null;
                }
                else if (blockText.Contains("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"))
                {
                    crowdMethod = "        float3 t; GPUI_CROWD_VERTEX(vertexID, input." + vertexField + ".xyz, input." + normalField + ".xyz, t);";
                    wrapperCode = newLineString + returnType + " " + methodName + "GPUICA(" + inputType + " input, AttributesPass inputPass, uint vertexID : SV_VertexID)" + newLineString +
    "{" + newLineString +
    "    #ifdef GPUI_PRO_CROWD_ACTIVE" + newLineString +
    "        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));" + newLineString +
    "        setupGPUI();" + newLineString +
    crowdMethod + newLineString +
    "    #endif" + newLineString +
    "    return " + methodName + "(input, inputPass);" + newLineString +
    "}" + newLineString;
                    return true;
                }
            }

            if (normalField != null && tangentField != null)
            {
                crowdMethod = "        GPUI_CROWD_VERTEX(vertexID, input." + vertexField + ".xyz, input." + normalField + ".xyz, input." + tangentField + ".xyz);";
            }
            else if (normalField == null)
            {
                crowdMethod = "        GPUI_CROWD_VERTEX_V(vertexID, input." + vertexField + ".xyz);";
            }
            else if (tangentField == null)
            {
                crowdMethod = "        GPUI_CROWD_VERTEX_VN(vertexID, input." + vertexField + ".xyz, input." + normalField + ".xyz);";
            }

            // 5. Build wrapper code
            wrapperCode = newLineString + returnType + " " + methodName + "GPUICA(" + inputType + " input, uint vertexID : SV_VertexID)" + newLineString +
    "{" + newLineString +
    "    #ifdef GPUI_PRO_CROWD_ACTIVE" + newLineString +
    "        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));" + newLineString +
    "        setupGPUI();" + newLineString +
    crowdMethod + newLineString +
    "    #endif" + newLineString +
    "    return " + methodName + "(input);" + newLineString +
    "}" + newLineString;

            return true;
        }

        private static bool TryFindVertexMethodName(string blockText, out string methodName)
        {
            methodName = string.Empty;

            // 1. Find the #pragma vertex line
            var lines = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string pragmaLine = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("#pragma vertex"))
                {
                    pragmaLine = lines[i];
                    break;
                }
            }
            if (pragmaLine == null)
                return false;

            string[] pragmaParts = pragmaLine.Trim().Split(' ');
            methodName = pragmaParts[pragmaParts.Length - 1];

            return true;
        }

        private static bool TryFindVertexMethodNameInIncludes(string blockText, string shaderDirectory, out string methodName, HashSet<string> visitedFiles = null)
        {
            methodName = string.Empty;

            if (visitedFiles == null)
                visitedFiles = new HashSet<string>();

            string[] lines = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].TrimStart();

                if (line.StartsWith("#include") || line.StartsWith("#include_with_pragmas"))
                {
                    int quoteStart = line.IndexOf('\"');
                    int quoteEnd = line.LastIndexOf('\"');

                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        string includePath = line.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        string fullPath = ResolveIncludePath(includePath, shaderDirectory);

                        if (!visitedFiles.Contains(fullPath) && File.Exists(fullPath))
                        {
                            visitedFiles.Add(fullPath);

                            string includeContent = File.ReadAllText(fullPath);

                            // Check current include content
                            if (TryFindVertexMethodName(includeContent, out methodName))
                                return true;

                            // Recurse into nested includes
                            if (TryFindVertexMethodNameInIncludes(includeContent, shaderDirectory, out methodName, visitedFiles))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryFindMethodSignatureInIncludes(string blockText, string methodName, string shaderDirectory, out string returnType, out string inputType, HashSet<string> visitedFiles = null)
        {
            returnType = "Varyings";
            inputType = "Attributes";

            if (visitedFiles == null)
                visitedFiles = new HashSet<string>();

            string[] lines = blockText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].TrimStart();

                if (line.StartsWith("#include") || line.StartsWith("#include_with_pragmas"))
                {
                    int quoteStart = line.IndexOf('\"');
                    int quoteEnd = line.LastIndexOf('\"');

                    if (quoteStart >= 0 && quoteEnd > quoteStart)
                    {
                        string includePath = line.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        string fullPath = ResolveIncludePath(includePath, shaderDirectory);

                        if (!visitedFiles.Contains(fullPath) && File.Exists(fullPath))
                        {
                            visitedFiles.Add(fullPath);

                            string includeContent = File.ReadAllText(fullPath);

                            // Check current include content
                            if (TryFindMethodSignatureInText(methodName, includeContent, out returnType, out inputType))
                                return true;

                            // Recurse into nested includes
                            if (TryFindMethodSignatureInIncludes(includeContent, methodName, shaderDirectory, out returnType, out inputType, visitedFiles))
                                return true;
                        }
                    }
                }
            }

            return false;
        }


        private static bool TryFindMethodSignatureInText(string methodName, string text, out string returnType, out string inputType)
        {
            returnType = null;
            inputType = null;

            string[] lines = text.Split('\n');
            StringBuilder candidate = new StringBuilder();
            bool insideCommentBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip block comments
                if (line.Contains("/*")) insideCommentBlock = true;
                if (insideCommentBlock)
                {
                    if (line.Contains("*/")) insideCommentBlock = false;
                    continue;
                }

                if (line.StartsWith("//")) continue;

                candidate.Append(line).Append(" ");

                // Check if this accumulated block has a potential full method header
                string candidateText = candidate.ToString();
                int methodIndex = candidateText.IndexOf(methodName);
                while (methodIndex >= 0)
                {
                    // Ensure this is a valid method name match (boundary check)
                    bool validStart = methodIndex == 0 || !char.IsLetterOrDigit(candidateText[methodIndex - 1]);
                    int nextIndex = methodIndex + methodName.Length;
                    bool validEnd = nextIndex < candidateText.Length && candidateText[nextIndex] == '(';

                    if (validStart && validEnd)
                    {
                        // Ensure we reached the opening brace
                        if (candidateText.Contains("{"))
                        {
                            // Try to parse: returnType methodName(inputType ...)
                            int parenStart = candidateText.IndexOf('(', methodIndex);
                            int returnTypeEnd = candidateText.LastIndexOf(' ', methodIndex - 1);
                            if (returnTypeEnd > 0)
                            {
                                string[] split = candidateText.Substring(0, returnTypeEnd).Trim().Split(' ');
                                returnType = split[split.Length - 1];
                                string paramBlock = candidateText.Substring(parenStart + 1);
                                int parenEnd = paramBlock.IndexOf(')');
                                if (parenEnd >= 0)
                                {
                                    string[] paramParts = paramBlock.Substring(0, parenEnd).Split(',');
                                    if (paramParts.Length > 0)
                                    {
                                        string[] paramTokens = paramParts[0].Trim().Split(' ');
                                        inputType = paramTokens.Length > 1 ? paramTokens[0].Trim() : null;
                                        return returnType != null && inputType != null;
                                    }
                                }
                            }
                        }
                    }

                    methodIndex = candidateText.IndexOf(methodName, methodIndex + 1);
                }
            }

            return false;
        }

        private static string ResolveIncludePath(string includePath, string shaderDirectory)
        {
            // If the path already starts with "Packages", assume it's absolute in Unity's project layout
            if (includePath.StartsWith("Packages"))
                return includePath;

            // Otherwise, resolve it relative to the shader directory
            string combined = Path.Combine(shaderDirectory, includePath);
            if (File.Exists(combined))
                return combined;

            // Fallback to raw path (caller should still check existence)
            return includePath;
        }

        private static bool TryFindVertexStructFields(string structName, string baseText, string shaderDirectory, out string positionField, out string normalField, out string tangentField, HashSet<string> visitedFiles = null)
        {
            positionField = normalField = tangentField = null;

            if (visitedFiles == null)
                visitedFiles = new HashSet<string>();

            // Search for struct declaration
            string[] lines = baseText.Split('\n');
            bool insideStruct = false;
            bool foundStruct = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (!insideStruct && trimmed.StartsWith("struct ") && trimmed.Contains(structName))
                {
                    insideStruct = true;
                    foundStruct = true;
                    continue;
                }

                if (insideStruct)
                {
                    if (trimmed.StartsWith("}"))
                        break;

                    if (trimmed.Contains(":"))
                    {
                        if (trimmed.Contains(": POSITION"))
                            positionField = GetFieldName(trimmed);
                        else if (trimmed.Contains(": NORMAL"))
                            normalField = GetFieldName(trimmed);
                        else if (trimmed.Contains(": TANGENT"))
                            tangentField = GetFieldName(trimmed);
                    }
                }
            }

            if (foundStruct && positionField != null)
                return true;

            // Look into includes recursively
            for (int i = lines.Length -1; i >= 0; i--)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#include") || trimmed.StartsWith("#include_with_pragmas"))
                {
                    int quoteStart = trimmed.IndexOf('"');
                    int quoteEnd = trimmed.LastIndexOf('"');

                    if (quoteStart != -1 && quoteEnd > quoteStart)
                    {
                        string includePath = trimmed.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        string resolvedPath = ResolveIncludePath(includePath, shaderDirectory);

                        if (!visitedFiles.Contains(resolvedPath) && File.Exists(resolvedPath))
                        {
                            visitedFiles.Add(resolvedPath);
                            string includedText = File.ReadAllText(resolvedPath);
                                if (TryFindVertexStructFields(structName, includedText, shaderDirectory, out positionField, out normalField, out tangentField, visitedFiles))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static string GetFieldName(string line)
        {
            string[] tokens = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2)
                return tokens[1].TrimEnd(';');
            return null;
        }

        private static Shader SaveInstancedShader(string newShaderText, string newAssetPath, string newShaderName)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(newShaderText);
            GPUIEditorUtility.VersionControlCheckout(newAssetPath);
            FileStream fs = File.Create(newAssetPath);
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();
            EditorUtility.DisplayProgressBar("GPU Instancer Shader Conversion", "Importing instanced shader...", 0.8f);
            AssetDatabase.ImportAsset(newAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Shader instancedShader = AssetDatabase.LoadAssetAtPath<Shader>(newAssetPath);
            if (instancedShader == null)
                instancedShader = Shader.Find(newShaderName);

            return instancedShader;
        }

        private static bool IsIncludeLine(string line)
        {
            return !line.Contains("#pragma instancing_options")
                    && !line.Contains("#pragma multi_compile_instancing")
                    && !line.Contains("DOTS_INSTANCING_ON")
                    && !line.Contains("GPUInstancerSetup.hlsl")
                    && !line.Contains("GPUICrowdSetup.hlsl")
                    && !line.Contains("DOTS.hlsl")
                    && !line.Contains("GPUInstancerInclude.cginc")
                    && !line.Contains("// Added by GPUIPro");
        }

        #endregion Auto Shader Conversion Helper Methods

        public static Shader CreateInstancedShader(Shader originalShader, string extensionCode, string setupText = null, bool useOriginal = false, string shaderNamePrefix = null, string fileNameSuffix = null, string createAtPath = null, bool isCheckWhiteList = true)
        {
            if (originalShader == null || originalShader.name == GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find shader! Please make sure that the material has a shader assigned.");
                return null;
            }
            if (!string.IsNullOrEmpty(extensionCode))
            {
                if (extensionCode == GPUIConstants.EXTENSION_CODE_CROWD)
                {
                    if (isCheckWhiteList && !CROWD_WHITE_LIST.Contains(originalShader.name))
                        return null;
                }
                else
                    return null;
            }
            try
            {
                string originalShaderName = originalShader.name;
                Shader originalShaderRef = Shader.Find(originalShaderName);
                string originalAssetPath = AssetDatabase.GetAssetPath(originalShaderRef);
                string extension = Path.GetExtension(originalAssetPath);

                bool isDoubleEscape = false;
                // can not work with ShaderGraph or other non shader code
                if (extension != ".shader")
                {
                    if (extension.EndsWith("pack"))
                        isDoubleEscape = true;
                    else
                        return null;
                }

                if (string.IsNullOrEmpty(setupText))
                    setupText = PRAGMA_GPUI_SETUP;
                if (string.IsNullOrEmpty(shaderNamePrefix))
                    shaderNamePrefix = GPUIConstants.GetShaderNamePrefix(extensionCode);
                if (string.IsNullOrEmpty(fileNameSuffix))
                    fileNameSuffix = FILE_NAME_SUFFIX;
                if (extensionCode == GPUIConstants.EXTENSION_CODE_CROWD)
                    fileNameSuffix += "CA";

                EditorUtility.DisplayProgressBar("GPU Instancer Shader Conversion", "Creating instanced shader for " + originalShaderName + ". Please wait...", 0.1f);

                #region Remove Existing procedural setup
                StringBuilder sb = new StringBuilder();
                using (StreamReader sr = new StreamReader(originalAssetPath))
                {
                    if (isDoubleEscape)
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            string[] lineSplit = line.Split("\\n");
                            for (int i = 0; i < lineSplit.Length; i++)
                            {
                                line = lineSplit[i];
                                if (IsIncludeLine(line))
                                {
                                    sb.Append(line);
                                    if (i + 1 < lineSplit.Length)
                                        sb.Append("\\n");
                                }
                            }
                            if (!sr.EndOfStream)
                                sb.Append("\n");
                        }
                    }
                    else
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (IsIncludeLine(line))
                            {
                                sb.Append(line);
                                if (!sr.EndOfStream)
                                    sb.Append("\n");
                            }
                        }
                    }
                }
                string originalShaderText = sb.ToString();
                if (string.IsNullOrEmpty(originalShaderText))
                {
                    EditorUtility.ClearProgressBar();
                    return null;
                }
                #endregion Remove Existing procedural setup

                bool createInDefaultFolder = false;
                // create shader versions for packages inside GPUI folder
                if (originalAssetPath.StartsWith("Packages/") && string.IsNullOrEmpty(createAtPath))
                    createInDefaultFolder = true;

                EditorUtility.DisplayProgressBar("GPU Instancer Shader Conversion", "Creating instanced shader for " + originalShaderName + ".  Please wait...", 0.5f);

                string newShaderName = useOriginal ? originalShaderName : GPUIUtility.ConvertToGPUIShaderName(originalShaderName, null, shaderNamePrefix);
                string newShaderText = originalShaderText.Replace("\r\n", "\n");
                newShaderText = useOriginal ? newShaderText : newShaderText.Replace("\"" + originalShaderName + "\"", "\"" + newShaderName + "\"");
                if (isDoubleEscape && !useOriginal)
                    newShaderText = newShaderText.Replace("\\\"" + originalShaderName + "\\\"", "\\\"" + newShaderName + "\\\"");

                string includePath = GetShaderIncludePath(originalAssetPath, createInDefaultFolder, out string newAssetPath);

                string shaderDirectory = Path.GetDirectoryName(originalAssetPath).Replace("\\", "/");
                // Include paths fix
                if (createInDefaultFolder)
                {
                    string includeAddition = shaderDirectory + "/";

                    int lastIndex = 0;
                    string searchStart = "";
                    string searchEnd = "";
                    int foundIndex = -1;

                    lastIndex = 0;
                    searchStart = "#include \"";
                    searchEnd = "\"";
                    string restOfText;

                    foundIndex = -1;
                    while (true)
                    {
                        foundIndex = newShaderText.IndexOf(searchStart, lastIndex);
                        if (foundIndex == -1)
                            break;
                        lastIndex = foundIndex + searchStart.Length + 1;

                        restOfText = newShaderText.Substring(foundIndex + searchStart.Length, newShaderText.Length - foundIndex - searchStart.Length);
                        if (!restOfText.StartsWith("Packages") && !(restOfText.StartsWith("Unity") && GPUIRuntimeSettings.Instance.IsBuiltInRP))
                        {
                            newShaderText = newShaderText.Substring(0, foundIndex + searchStart.Length) + includeAddition + restOfText;
                            lastIndex += includeAddition.Length;
                        }

                        foundIndex = newShaderText.IndexOf(searchEnd, lastIndex);
                        lastIndex = foundIndex;
                    }
                }

                newShaderText = AddIncludeAndPragmaDirectives(includePath, newShaderText, setupText, isDoubleEscape, extensionCode, shaderDirectory);

                if (newShaderText == null)
                {
                    EditorUtility.ClearProgressBar();
                    return null;
                }

                string originalFileName = Path.GetFileName(newAssetPath);
                newAssetPath = useOriginal ? newAssetPath : newAssetPath.Replace(originalFileName, originalFileName.Replace(FILE_NAME_SUFFIX, "").Replace(extension, fileNameSuffix + extension));
                if (!string.IsNullOrEmpty(createAtPath))
                    newAssetPath = createAtPath;
                Shader instancedShader = SaveInstancedShader(newShaderText, newAssetPath, newShaderName);

                if (instancedShader != null)
                {
                    if(extensionCode == GPUIConstants.EXTENSION_CODE_CROWD)
                        Debug.Log(GPUIConstants.LOG_PREFIX + "Generated a GPUI Pro - Crowd Animations support enabled version of the shader: " + originalShaderName, instancedShader);
                    else
                        Debug.Log(GPUIConstants.LOG_PREFIX + "Generated a GPUI Pro support enabled version of the shader: " + originalShaderName, instancedShader);
                }
                EditorUtility.ClearProgressBar();

                return instancedShader;
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException && e.Message.ToLower().Contains("unity_builtin_extra"))
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "\"" + originalShader.name + "\" shader is a built-in shader which is not included in GPUI package. Please download the original shader file from Unity Archive to enable auto-conversion for this shader. Check prototype settings on the Manager for instructions.");
                else
                    Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
            return null;
        }

        #endregion Auto Shader Conversion

        #region Shader Variant Collection
        public static void AddShaderVariantToCollection(Material material, string extensionCode)
        {
            if (!GPUIEditorSettings.Instance.isGenerateShaderVariantCollection)
                return;

            if (material != null && material.shader != null)
            {
                if (GPUIShaderBindings.Instance.GetInstancedShader(material.shader.name, extensionCode, out Shader instancedShader))
                    AddShaderVariantToCollection(instancedShader, material.shaderKeywords);
            }
        }

        public static void AddShaderVariantToCollection(string shaderName, string extensionCode)
        {
            if (!GPUIEditorSettings.Instance.isGenerateShaderVariantCollection)
                return;

            if (GPUIShaderBindings.Instance.GetInstancedShader(shaderName, extensionCode, out Shader instancedShader))
                AddShaderVariantToCollection(instancedShader);
        }

        public static void AddShaderVariantToCollection(Shader shader, string[] keywords = null)
        {
            if (shader != null)
            {
                ShaderVariantCollection.ShaderVariant shaderVariant = new ShaderVariantCollection.ShaderVariant()
                {
                    shader = shader,
                    keywords = keywords
                };
                GPUIRuntimeSettings.Instance.VariantCollection.Add(shaderVariant);
            }
        }

        public static void AddDefaultShaderVariants()
        {
            if (!GPUIEditorSettings.Instance.isGenerateShaderVariantCollection)
                return;
            AddShaderVariantToCollection(Shader.Find(GPUIConstants.SHADER_GPUI_ERROR));
            AddShaderVariantToCollection(Shader.Find(GPUIConstants.SHADER_GPUI_TREE_PROXY));
        }

        public static void ClearShaderVariantCollection()
        {
            GPUIRuntimeSettings.Instance.VariantCollection.Clear();
            EditorUtility.SetDirty(GPUIRuntimeSettings.Instance.VariantCollection);
            AddDefaultShaderVariants();
        }
        #endregion Shader Variant Collection
    }
}