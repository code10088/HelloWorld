// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIMaterialProvider : GPUIDataProvider<int, Material>
    {
#if UNITY_EDITOR
        public struct GPUIShaderConversionData : IEquatable<GPUIShaderConversionData>
        {
            public Shader shader;
            public string extensionCode;

            public bool Equals(GPUIShaderConversionData other)
            {
                return other.shader == shader && other.extensionCode == extensionCode;
            }

            public override bool Equals(object obj)
            {
                if (obj is GPUIShaderConversionData other)
                    return Equals(other);
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                if (shader == null)
                    return 0;
                return shader.GetHashCode() + (extensionCode != null ? extensionCode.GetHashCode() : 0);
            }
        }

        private Queue<GPUIShaderConversionData> _shadersToConvert;
        private List<GPUIShaderConversionData> _failedShaderConversions;
        private Queue<Material> _materialVariants;
        public bool checkForShaderModifications;
#else
        private List<Shader> _missingBuildShaders;
#endif

        public override void Initialize()
        {
            base.Initialize();
#if UNITY_EDITOR
            _shadersToConvert = new Queue<GPUIShaderConversionData>();
            _failedShaderConversions = new List<GPUIShaderConversionData>();
            _materialVariants = new Queue<Material>();
#endif
        }

        public override void Dispose()
        {
            if (_dataDict != null)
            {
                foreach (Material mat in _dataDict.Values)
                {
                    if (mat != null && mat.name.EndsWith(GPUIShaderBindings.GPUI_REPLACEMENT_MATERIAL_NAME_SUFFIX))
                        GPUIUtility.DestroyGeneric(mat);
                }
            }
            base.Dispose();
#if UNITY_EDITOR
            _shadersToConvert = null;
            _failedShaderConversions = null;
            _materialVariants = null;
#endif
        }

        public bool TryGetReplacementMaterial(Material originalMat, List<string> keywords, string extensionCode, out Material replacementMat)
        {
            replacementMat = GPUIShaderBindings.Instance.ErrorMaterial;
            if (!IsInitialized ||originalMat == null || originalMat.shader == null)
                return false;
            int key = originalMat.GetInstanceID();
            if (!string.IsNullOrEmpty(extensionCode))
                key = GPUIUtility.GenerateHash(key, extensionCode.GetHashCode());
            if (TryGetData(key, out Material mat))
            {
                if (mat == null || mat.shader == null || mat.shader == GPUIConstants.ShaderUnityInternalError)
                    _dataDict.Remove(key);
                else
                {
                    if (keywords == null || keywords.Count == 0)
                    {
                        replacementMat = mat;
                        return true;
                    }
                    keywords.Sort();
                    int keyWithKeywords = GPUIUtility.GenerateHash(key, string.Concat(keywords).GetHashCode());
                    if (TryGetData(keyWithKeywords, out Material matWithKeyword))
                    {
                        if (matWithKeyword == null || matWithKeyword.shader == null || matWithKeyword.shader == GPUIConstants.ShaderUnityInternalError)
                            _dataDict.Remove(keyWithKeywords);
                        else
                        {
                            replacementMat = matWithKeyword;
                            return true;
                        }
                    }
                    bool isKeywordsAlreadyEnabled = true;
                    foreach (string keyword in keywords)
                    {
                        if (!mat.IsKeywordEnabled(keyword))
                        {
                            isKeywordsAlreadyEnabled = false; 
                            break;
                        }
                    }
                    if (isKeywordsAlreadyEnabled)
                        matWithKeyword = mat;
                    else
                    {
                        matWithKeyword = mat.CopyWithShader(mat.shader);
                        foreach (string keyword in keywords)
                            matWithKeyword.EnableKeyword(keyword);
                    }
                    _dataDict.Add(keyWithKeywords, matWithKeyword);
                    replacementMat = matWithKeyword;
                    return true;
                }
            }

            if (GPUIShaderBindings.Instance.GetInstancedMaterial(originalMat, extensionCode, out replacementMat))
            {
                _dataDict.Add(key, replacementMat);
                if (keywords != null && keywords.Count > 0)
                    return TryGetReplacementMaterial(originalMat, keywords, extensionCode, out replacementMat);
                return true;
            }
#if UNITY_EDITOR
            AddToShadersToConvert(originalMat.shader, extensionCode);
#else
            _missingBuildShaders ??= new();
            if (!_missingBuildShaders.Contains(originalMat.shader))
            {
                _missingBuildShaders.Add(originalMat.shader);
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find GPU Instancer Pro setup for shader: " + originalMat.shader.name);
            }
#endif

            return false;
        }

#if UNITY_EDITOR
        public bool TryGetShaderToConvert(out GPUIShaderConversionData shaderConversionData)
        {
            shaderConversionData = default;
            if (!IsInitialized)
                return false;
            while (_shadersToConvert.TryDequeue(out shaderConversionData))
            {
                if (shaderConversionData.shader != null)
                    return true;
            }
            return false;
        }

        public void AddToShadersToConvert(Shader shader, string extensionCode)
        {
            GPUIShaderConversionData shaderConversionData = new GPUIShaderConversionData() { shader = shader, extensionCode = extensionCode };
            if (!_shadersToConvert.Contains(shaderConversionData) && !_failedShaderConversions.Contains(shaderConversionData))
                _shadersToConvert.Enqueue(shaderConversionData);
        }

        public void AddToFailedShaderConversions(GPUIShaderConversionData shaderConversionData)
        {
            if (IsInitialized && shaderConversionData.shader != null && !_failedShaderConversions.Contains(shaderConversionData))
            {
                _failedShaderConversions.Add(shaderConversionData);
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Failed to convert shader: " + shaderConversionData.shader.name +
                    (string.IsNullOrEmpty(shaderConversionData.extensionCode) ? "" : " with Extension Code:" + shaderConversionData.extensionCode), shaderConversionData.shader);
#endif
            }
        }

        public bool IsFailedShaderConversion(Shader shader, string extensionCode)
        {
            GPUIShaderConversionData shaderConversionData = new GPUIShaderConversionData() { shader = shader, extensionCode = extensionCode };
            if (_failedShaderConversions != null && _failedShaderConversions.Contains(shaderConversionData))
                return true;
            return false;
        }

        public void ClearFailedShaderConversions()
        {
            if (!IsInitialized)
                return;
            _failedShaderConversions.Clear();
        }

        public void AddMaterialVariant(Material material)
        {
            if (!IsInitialized)
                return;
            _materialVariants.Enqueue(material);
        }

        public bool TryGetMaterialVariant(out Material material)
        {
            material = null;
            if (!IsInitialized)
                return false;
            while (_materialVariants.TryDequeue(out material))
            {
                if (material != null && material.shader != null)
                    return true;
            }
            return false;
        }
#endif // UNITY_EDITOR
    }
}