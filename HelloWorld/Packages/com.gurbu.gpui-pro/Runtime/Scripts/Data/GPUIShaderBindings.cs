// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public class GPUIShaderBindings : ScriptableObject
    {
        public static readonly string GPUI_REPLACEMENT_MATERIAL_NAME_SUFFIX = "_GPUIReplacement";

        [SerializeField]
        public List<GPUIShaderInstance> shaderInstances;
        [SerializeField]
        public bool stripObjectMotionVectorVariants = false;
        [SerializeField]
        public bool stripPerInstanceLightProbeVariants = false;

        private static GPUIShaderBindings _instance;
        public static GPUIShaderBindings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetDefaultShaderBindings();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static GPUIShaderBindings GetDefaultShaderBindings()
        {
            GPUIShaderBindings shaderBindings = GPUIUtility.LoadResource<GPUIShaderBindings>(GPUIConstants.FILE_SHADER_BINDINGS);

            if (shaderBindings == null)
            {
                shaderBindings = ScriptableObject.CreateInstance<GPUIShaderBindings>();
#if UNITY_EDITOR
                shaderBindings.SaveAsAsset(GPUIConstants.GetDefaultUserDataPath() + GPUIConstants.PATH_RESOURCES, GPUIConstants.FILE_SHADER_BINDINGS + ".asset");
#endif
            }
            return shaderBindings;
        }

        public bool IsShaderSetupForGPUI(Shader shader, string extensionCode)
        {
            if (shader != null)
                return IsShaderSetupForGPUI(shader.name, extensionCode);
            return false;
        }

        public bool IsShaderSetupForGPUI(string shaderName, string extensionCode)
        {
            foreach (GPUIShaderInstance si in shaderInstances)
            {
                if ((si.shaderName.Equals(shaderName) || si.replacementShaderName.Equals(shaderName)) && IsExtensionEqual(si, extensionCode))
                    return true;
            }
            return false;
        }

        public bool IsShaderSetupForGPUIAnyExtension(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName)) return false;
            foreach (GPUIShaderInstance si in shaderInstances)
            {
                if (si.shaderName.Equals(shaderName) || si.replacementShaderName.Equals(shaderName))
                    return true;
            }
            return false;
        }

        public virtual bool GetInstancedShader(Shader shader, string extensionCode, out Shader resultShader)
        {
            resultShader = ErrorShader;
            if (shader != null)
                return GetInstancedShader(shader.name, extensionCode, out resultShader);
            return false;
        }

        public virtual bool GetInstancedShader(string shaderName, string extensionCode, out Shader resultShader)
        {
            ClearEmptyShaderInstances();
            resultShader = ErrorShader;
            if (string.IsNullOrEmpty(shaderName))
                return false;

            if (shaderInstances == null)
                shaderInstances = new List<GPUIShaderInstance>();

            foreach (GPUIShaderInstance si in shaderInstances)
            {
                if ((si.shaderName.Equals(shaderName) || si.replacementShaderName.Equals(shaderName)) && IsExtensionEqual(si, extensionCode))
                {
                    resultShader = si.replacementShader;
                    return true;
                }
            }

            Shader namedShader = Shader.Find(GPUIUtility.ConvertToGPUIShaderName(shaderName, extensionCode));
            if (namedShader != null)
            {
                AddShaderInstance(shaderName, namedShader, extensionCode);
                resultShader = namedShader;
                return true;
            }

            return false;
        }

        public virtual bool GetInstancedMaterial(Material originalMaterial, string extensionCode, out Material replacementMat)
        {
            replacementMat = ErrorMaterial;
            if (originalMaterial == null || originalMaterial.shader == null)
            {
                if (Application.isPlaying)
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "One of the GPUI Renderers is missing material reference! Check the Material references in MeshRenderer.");
                return false;
            }
            else if (GetInstancedShader(originalMaterial.shader, extensionCode, out Shader instancedShader) && instancedShader != null)
            {
                if (originalMaterial.shader == instancedShader)
                    replacementMat = originalMaterial;
                else
                    replacementMat = originalMaterial.CopyWithShader(instancedShader);
                //Debug.Log(GPUIConstants.LOG_PREFIX + "Generating material: " + replacementMat.name);
                return true;
            }
            return false;
        }

        public void AddShaderInstance(string shaderName, Shader replacementShader, string extensionCode, bool isUseOriginal = false)
        {
            if (shaderInstances == null)
                shaderInstances = new List<GPUIShaderInstance>();
            if (shaderName == GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
                return;
            for (int i = 0; i < shaderInstances.Count; i++)
            {
                if (shaderInstances[i].shaderName == shaderName && IsExtensionEqual(shaderInstances[i], extensionCode))
                {
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Shader Instance already exists for shader: " + shaderName);
                    return;
                }
            }
            shaderInstances.Add(new GPUIShaderInstance()
            {
                shaderName = shaderName,
                replacementShaderName = replacementShader.name,
                extensionCode = extensionCode,
                isUseOriginal = isUseOriginal,
                modifiedDate = DateTime.Now.ToDateString()
            });
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public virtual void ClearEmptyShaderInstances()
        {
            if (shaderInstances != null)
            {
                int count = shaderInstances.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    var si = shaderInstances[i];
                    if (si != null)
                    {
                        Shader os = si.originalShader;
                        Shader rs = si.replacementShader;
                        if (os != null && os.name != GPUIConstants.SHADER_UNITY_INTERNAL_ERROR && rs != null && rs.name != GPUIConstants.SHADER_UNITY_INTERNAL_ERROR)
                            continue;
                    }
                    shaderInstances.RemoveAt(i);
                }
#if UNITY_EDITOR
                if (count != shaderInstances.Count)
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public static bool IsExtensionEqual(GPUIShaderInstance shaderInstance, string extensionCode)
        {
            return (string.IsNullOrEmpty(extensionCode) && string.IsNullOrEmpty(shaderInstance.extensionCode))
                || (extensionCode != null && extensionCode.Equals(shaderInstance.extensionCode));
        }

        private Shader _errorShader;
        public Shader ErrorShader
        {
            get
            {
                if (_errorShader == null)
                    _errorShader = GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_ERROR);
                return _errorShader;
            }
        }

        private Material _errorMaterial;
        public Material ErrorMaterial
        {
            get
            {
                if (_errorMaterial == null && ErrorShader != null)
                {
                    _errorMaterial = new Material(ErrorShader);
                    _errorMaterial.name = "GPUIInternalErrorMaterial";
                }
                return _errorMaterial;
            }
        }
    }

    [Serializable]
    public class GPUIShaderInstance
    {
        public string shaderName;
        public string replacementShaderName;
        public Shader originalShader { get { return !string.IsNullOrEmpty(shaderName) ? GPUIUtility.FindShader(shaderName) : null; } }
        public Shader replacementShader { get { return !string.IsNullOrEmpty(replacementShaderName) ? GPUIUtility.FindShader(replacementShaderName) : null; } }
        public string extensionCode;
        public bool isUseOriginal;
        public string modifiedDate;
    }
}