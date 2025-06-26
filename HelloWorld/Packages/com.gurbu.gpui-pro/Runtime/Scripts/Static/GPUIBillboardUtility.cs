// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public static class GPUIBillboardUtility
    {
        #region Billboard Methods

        public static GPUIBillboard GenerateBillboardData(GameObject prefabObject)
        {
            return GenerateBillboardData(prefabObject, 2048, 8, 0.5f, 0.5f);
        }

        public static GPUIBillboard GenerateBillboardData(GameObject prefabObject, int atlasResolution, int frameCount, float brightness, float cutoffOverride, float normalStrength = 1f)
        {
            Bounds objectBounds = prefabObject.GetBounds(true);
            objectBounds.size = Vector3.Scale(objectBounds.size, prefabObject.transform.lossyScale.Reciprocal());
            objectBounds.center = Vector3.Scale(objectBounds.center, prefabObject.transform.lossyScale.Reciprocal());
            Vector2 quadSize = new Vector2(Vector2.Distance(Vector2.zero, new Vector2(objectBounds.size.x, objectBounds.size.z)), objectBounds.size.y);
            float yPivotOffset = -objectBounds.min.y;

            GPUIBillboard billboard = ScriptableObject.CreateInstance<GPUIBillboard>();
            billboard.name = prefabObject.name + "_Billboard";
            billboard.prefabObject = prefabObject;
            billboard.atlasResolution = (GPUIBillboard.GPUIBillboardResolution)atlasResolution;
            billboard.frameCount = frameCount;
            billboard.brightness = brightness;
            billboard.cutoffOverride = cutoffOverride;
            billboard.normalStrength = normalStrength;
            billboard.quadSize = quadSize;
            billboard.yPivotOffset = yPivotOffset;
            billboard.billboardShaderType = GPUIBillboard.GPUIBillboardShaderType.Default;
            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                billboard.billboardShaderType = DetermineBillboardShaderType(prefabObject);

            return billboard;
        }

        private static GPUIBillboard.GPUIBillboardShaderType DetermineBillboardShaderType(GameObject prefabObject)
        {
            MeshRenderer[] meshRenderers = prefabObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                foreach (Material mat in meshRenderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null)
                        continue;

                    if (mat.shader.name.Contains("Tree Creator"))
                        return GPUIBillboard.GPUIBillboardShaderType.TreeCreator;
                    if (mat.shader.name.Contains("SpeedTree"))
                        return GPUIBillboard.GPUIBillboardShaderType.SpeedTree;
                    if (mat.shader.name.Contains("Tree Soft Occlusion"))
                        return GPUIBillboard.GPUIBillboardShaderType.SoftOcclusion;
                }
            }

            return GPUIBillboard.GPUIBillboardShaderType.Default;
        }

        public static bool GenerateBillboardDelayed(GPUIBillboard billboard, bool saveAsAsset = false)
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                GenerateBillboard(billboard, saveAsAsset);
                GPUIRenderingSystem.RegenerateRenderers();
            };
            return true;
#else
            bool result = GenerateBillboard(billboard, false);
            GPUIRenderingSystem.RegenerateRenderers();
            return result;
#endif
        }

        public static bool GenerateBillboard(GPUIBillboard billboard, bool saveAsAsset = false)
        {
            //Debug.Log(GPUIConstants.LOG_PREFIX + "GenerateBillboard for " + billboard.prefabObject.name);
            if (billboard == null || billboard.prefabObject == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can no generate billboard. Prefab is null!");
                return false;
            }

            GameObject sample = null;
            GameObject billboardCameraPivot = null;
            int cachedMasterTextureLimit = QualitySettings.globalTextureMipmapLimit;
            QualitySettings.globalTextureMipmapLimit = 0;
            RenderPipelineAsset renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            RenderPipelineAsset qualityPipelineAsset = QualitySettings.renderPipeline;
            try
            {
                #region Create RenderTextures
                int frameResolution = (int)billboard.atlasResolution / billboard.frameCount;
                billboard.albedoAtlasRT = new RenderTexture((int)billboard.atlasResolution, frameResolution, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    enableRandomWrite = true,
                    wrapMode = TextureWrapMode.Repeat,
                    name = billboard.prefabObject.name + "_Albedo"
                };
                billboard.albedoAtlasRT.Create();

                billboard.normalAtlasRT = new RenderTexture((int)billboard.atlasResolution, frameResolution, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    enableRandomWrite = true,
                    wrapMode = TextureWrapMode.Repeat,
                    name = billboard.prefabObject.name + "_Normal"
                };
                billboard.albedoAtlasRT.Create();
                #endregion Create RenderTextures

                // instantiate an instance of the prefab to sample
                sample = GameObject.Instantiate(billboard.prefabObject, Vector3.zero, Quaternion.identity);
                sample.transform.localScale = Vector3.one;
                sample.hideFlags = HideFlags.DontSave;
                Bounds objectBounds = sample.GetBounds(true);
                billboard.quadSize = new Vector2(Vector2.Distance(Vector2.zero, new Vector2(objectBounds.size.x, objectBounds.size.z)), objectBounds.size.y);
                billboard.yPivotOffset = -objectBounds.min.y;

                int sampleLayer = 31;
                MeshRenderer[] sampleChildrenMRs = sample.GetComponentsInChildren<MeshRenderer>();

                if (sampleChildrenMRs == null || sampleChildrenMRs.Length == 0)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Cannot create GPU Instancer billboard for " + billboard.prefabObject.name + " : no mesh renderers found in prefab!");
                    GameObject.DestroyImmediate(sample);
                    return false;
                }
                for (int i = 0; i < sampleChildrenMRs.Length; i++)
                {
                    sampleChildrenMRs[i].gameObject.layer = sampleLayer;

                    for (int m = 0; m < sampleChildrenMRs[i].sharedMaterials.Length; m++)
                    {
                        Material mat = sampleChildrenMRs[i].sharedMaterials[m];
                        if (mat != null)
                        {
                            if (mat.HasProperty("_MainTexture"))
                                mat.SetTexture("_MainTex", mat.GetTexture("_MainTexture"));

                            if (mat.HasProperty("_BaseMap"))
                                mat.SetTexture("_MainTex", mat.GetTexture("_BaseMap"));
                            if (mat.HasProperty("_BaseColor"))
                                mat.SetColor("_Color", mat.GetColor("_BaseColor"));
                        }
                    }
                }

                Shader billboardAlbedoBakeShader = GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_ALBEDO_BAKER);
                Shader billboardNormalBakeShader = GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_NORMAL_BAKER);
                Shader.SetGlobalFloat("_GPUIBillboardBrightness", billboard.brightness);
                Shader.SetGlobalFloat("_GPUIBillboardCutoffOverride", billboard.cutoffOverride);
#if UNITY_EDITOR
                Shader.SetGlobalFloat("_IsLinearSpace", PlayerSettings.colorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
#endif

                float sampleBoundsMaxSize = Mathf.Max(billboard.quadSize.x, billboard.quadSize.y);

                // create the billboard snapshot camera
                billboardCameraPivot = new GameObject("GPUI_BillboardCameraPivot");
                Camera billboardCamera = new GameObject().AddComponent<Camera>();
                billboardCamera.transform.SetParent(billboardCameraPivot.transform);

                billboardCamera.gameObject.hideFlags = HideFlags.DontSave;
                billboardCamera.cullingMask = 1 << sampleLayer;
                billboardCamera.clearFlags = CameraClearFlags.SolidColor;
                billboardCamera.backgroundColor = Color.clear;
                billboardCamera.orthographic = true;
                billboardCamera.nearClipPlane = 0f;
                billboardCamera.farClipPlane = sampleBoundsMaxSize;
                billboardCamera.orthographicSize = sampleBoundsMaxSize * 0.5f;
                billboardCamera.allowMSAA = false;
                billboardCamera.enabled = false;
                billboardCamera.renderingPath = RenderingPath.Forward;
                billboardCamera.transform.localPosition = new Vector3(0, objectBounds.center.y, -sampleBoundsMaxSize / 2);

                int frameCount = billboard.frameCount;
                float rotateAngle = 360f / frameCount;

                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;

                // create render target for atlas frames (both albedo and normal will share the same target)
                RenderTexture frameTarget = RenderTexture.GetTemporary(frameResolution, frameResolution, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                frameTarget.enableRandomWrite = true;
                frameTarget.Create();
                billboardCamera.targetTexture = frameTarget;

                // render the frames into the atlas textures
                for (int f = 0; f < frameCount; f++)
                {
                    //sample.transform.rotation = Quaternion.AngleAxis(rotateAngle * -f, Vector3.up);
                    billboardCameraPivot.transform.rotation = Quaternion.AngleAxis(rotateAngle * f, Vector3.up);

                    billboardCamera.RenderWithShader(billboardAlbedoBakeShader, string.Empty);
                    GPUITextureUtility.CopyTextureWithComputeShader(frameTarget, billboard.albedoAtlasRT, f * frameResolution, 0, 0);

                    billboardCamera.RenderWithShader(billboardNormalBakeShader, string.Empty);
                    GPUITextureUtility.CopyTextureWithComputeShader(frameTarget, billboard.normalAtlasRT, f * frameResolution, 0, 0);
                }

                DilateBillboardTexture(billboard.albedoAtlasRT, frameCount, false);
                DilateBillboardTexture(billboard.normalAtlasRT, frameCount, true);

#if UNITY_EDITOR
                if (/*!Application.isPlaying &&*/ saveAsAsset)
                    SaveBillboardAsAsset(billboard);
#endif
            }
            catch (Exception e)
            {
                GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
                QualitySettings.renderPipeline = qualityPipelineAsset;
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Error on billboard generation for: " + billboard.prefabObject);
                QualitySettings.globalTextureMipmapLimit = cachedMasterTextureLimit;
                if (sample)
                    UnityEngine.Object.DestroyImmediate(sample);
                if (billboardCameraPivot)
                    UnityEngine.Object.DestroyImmediate(billboardCameraPivot);
                if (billboard.albedoAtlasRT)
                    UnityEngine.Object.DestroyImmediate(billboard.albedoAtlasRT);
                if (billboard.normalAtlasRT)
                    UnityEngine.Object.DestroyImmediate(billboard.normalAtlasRT);
                Debug.LogException(e);
            }
            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
            QualitySettings.renderPipeline = qualityPipelineAsset;
            QualitySettings.globalTextureMipmapLimit = cachedMasterTextureLimit;
            UnityEngine.Object.DestroyImmediate(sample);
            UnityEngine.Object.DestroyImmediate(billboardCameraPivot);

            return true;
        }


        public static GPUIBillboard FindBillboardAsset(GameObject prefabObject)
        {
            if (GPUIRuntimeSettings.Instance.billboardAssets != null)
            {
                foreach (var billboardAsset in GPUIRuntimeSettings.Instance.billboardAssets)
                {
                    if (billboardAsset != null && billboardAsset.prefabObject == prefabObject)
                        return billboardAsset;
                }
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return AssetDatabase.LoadAssetAtPath<GPUIBillboard>(GetBillboardFolderPath(prefabObject) + prefabObject.name + "_Billboard.asset");
#endif
            return null;
        }

#if UNITY_EDITOR
        public static void SaveBillboardAsAsset(GPUIBillboard billboard, string folderOverride = null)
        {
            if (/*!Application.isPlaying &&*/ billboard != null && AssetDatabase.Contains(billboard.prefabObject))
            {
                string folderPath = GetBillboardFolderPath(billboard.prefabObject);

                if (!string.IsNullOrEmpty(folderOverride))
                    folderPath = folderOverride;
                else if (AssetDatabase.Contains(billboard))
                    folderPath = billboard.GetAssetFolderPath();

                if (billboard.albedoAtlasRT != null)
                {
                    billboard.albedoAtlasTexture = GPUITextureUtility.SaveRenderTextureToPNG(billboard.albedoAtlasRT, TextureFormat.ARGB32, folderPath, null, false, FilterMode.Bilinear, TextureImporterType.Default, (int)billboard.atlasResolution, true, true, true);
                    billboard.albedoAtlasRT.DestroyRenderTexture();
                    billboard.albedoAtlasRT = null;
                }

                if (billboard.normalAtlasRT != null)
                {
                    billboard.normalAtlasTexture = GPUITextureUtility.SaveRenderTextureToPNG(billboard.normalAtlasRT, TextureFormat.ARGB32, folderPath, null, false, FilterMode.Bilinear, TextureImporterType.Default /*No need to save as Normal, complicates the system and creates inconsistencies between RenderTexture version*/, (int)billboard.atlasResolution);
                    billboard.normalAtlasRT.DestroyRenderTexture();
                    billboard.normalAtlasRT = null;
                }
                if (!AssetDatabase.Contains(billboard))
                    billboard.SaveAsAsset(folderPath, billboard.prefabObject.name + "_Billboard.asset");
                else
                    EditorUtility.SetDirty(billboard);
            }
        }

        private static string GetBillboardFolderPath(GameObject prefabObject)
        {
            return prefabObject.GetAssetFolderPath() + prefabObject.name + "_Billboard/";
        }

        public static void DeleteBillboard(GPUIBillboard billboard)
        {
            if (!Application.isPlaying && billboard != null && AssetDatabase.Contains(billboard))
            {
                string billboardPath = AssetDatabase.GetAssetPath(billboard);
                if (billboard.albedoAtlasTexture != null)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(billboard.albedoAtlasTexture));
                if (billboard.normalAtlasTexture != null)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(billboard.normalAtlasTexture));
                AssetDatabase.DeleteAsset(billboardPath);

                string folderPath = Path.GetDirectoryName(billboardPath);
                if (Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Length == 0)
                    AssetDatabase.DeleteAsset(folderPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#endif

        public static void DilateBillboardTexture(RenderTexture billboardTexture, int frameCount, bool isNormal)
        {
            RenderTexture result = new RenderTexture(billboardTexture.width, billboardTexture.height, billboardTexture.depth, billboardTexture.format, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                wrapMode = billboardTexture.wrapMode,
                name = billboardTexture.name
            };
            result.Create();

            ComputeShader dilationCompute = GPUIConstants.CS_Billboard;

            dilationCompute.SetTexture(0, "result", result);
            dilationCompute.SetTexture(0, "billboardSource", billboardTexture);
            dilationCompute.SetInts("billboardSize", new int[2] { billboardTexture.width, billboardTexture.height });
            dilationCompute.SetInt("frameCount", frameCount);
#if UNITY_EDITOR
            dilationCompute.SetBool("isLinearSpace", PlayerSettings.colorSpace == ColorSpace.Linear);
#endif
            dilationCompute.SetBool("isNormal", isNormal);
            dilationCompute.Dispatch(0, Mathf.CeilToInt(billboardTexture.width / (GPUIConstants.CS_THREAD_COUNT_2D * frameCount)),
                Mathf.CeilToInt(billboardTexture.height / GPUIConstants.CS_THREAD_COUNT_2D), frameCount);

            GPUITextureUtility.CopyTextureWithComputeShader(result, billboardTexture, 0, 0, 0);

            GPUITextureUtility.DestroyRenderTexture(result);
        }

        public static Material CreateBillboardMaterial(Texture albedo, Texture normal, float cutOff, int frameCount, float normalStrength, GPUIBillboard.GPUIBillboardShaderType shaderType)
        {
            Material material = new Material(GetBillboardShader(shaderType));
            material.SetTexture("_AlbedoAtlas", albedo);
            material.SetTexture("_NormalAtlas", normal);
            material.SetFloat("_CutOff_GPUI", cutOff);
            material.SetInt("_FrameCount_GPUI", frameCount);
            material.SetFloat("_NormalStrength_GPUI", normalStrength);
            if (QualitySettings.billboardsFaceCameraPosition)
                material.EnableKeyword(GPUIConstants.Kw_BILLBOARD_FACE_CAMERA_POS);
            else
                material.DisableKeyword(GPUIConstants.Kw_BILLBOARD_FACE_CAMERA_POS);

#if GPUI_HDRP
            if (GPUIRuntimeSettings.Instance.IsHDRP)
                UnityEngine.Rendering.HighDefinition.HDMaterial.SetAlphaClipping(material, true);
#endif

            return material;
        }

        public static Material CreateBillboardMaterial(GPUIBillboard billboard)
        {
            if (billboard._billboardMaterial != null)
                billboard._billboardMaterial.DestroyGeneric();
            Material billboardMaterial;
            if (billboard.albedoAtlasRT != null && billboard.normalAtlasRT != null)
                billboardMaterial = CreateBillboardMaterial(billboard.albedoAtlasRT, billboard.normalAtlasRT, billboard.cutoffOverride, billboard.frameCount, billboard.normalStrength, billboard.billboardShaderType);
            else
                billboardMaterial = CreateBillboardMaterial(billboard.albedoAtlasTexture, billboard.normalAtlasTexture, billboard.cutoffOverride, billboard.frameCount, billboard.normalStrength, billboard.billboardShaderType);

            if (billboard.billboardShaderType == GPUIBillboard.GPUIBillboardShaderType.SpeedTree)
            {
                Renderer spdMR = billboard.prefabObject.GetComponentInChildren<MeshRenderer>();

                if (spdMR != null)
                {
                    if (spdMR.sharedMaterial.IsKeywordEnabled("EFFECT_HUE_VARIATION"))
                    {
                        billboardMaterial.EnableKeyword("SPDTREE_HUE_VARIATION");
                        billboardMaterial.SetFloat("_UseSPDHueVariation", 1.0f);

                        if (spdMR.sharedMaterial.HasProperty("_HueVariation")) // SpeedTree 7
                            billboardMaterial.SetVector("_SPDHueVariation", spdMR.sharedMaterial.GetVector("_HueVariation"));

                        if (spdMR.sharedMaterial.HasProperty("_HueVariationColor")) // SpeedTree 8
                            billboardMaterial.SetVector("_SPDHueVariation", spdMR.sharedMaterial.GetVector("_HueVariationColor"));
                    }
                    else
                        billboardMaterial.DisableKeyword("SPDTREE_HUE_VARIATION");
                }
            }

#if UNITY_EDITOR
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.MaterialProvider.AddMaterialVariant(billboardMaterial);
#endif

            billboard._billboardMaterial = billboardMaterial;

            return billboardMaterial;
        }

        public static Mesh GenerateQuadMesh(GPUIBillboard billboard)
        {
            if (billboard._quadMesh != null)
                billboard._quadMesh.DestroyGeneric();
            Rect uvRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            if (billboard.quadSize.x < billboard.quadSize.y)
            {
                uvRect.width = billboard.quadSize.x / billboard.quadSize.y;
                uvRect.x = (1f - uvRect.width) / 2f;
            }
            else if (billboard.quadSize.x > billboard.quadSize.y)
            {
                uvRect.height = billboard.quadSize.y / billboard.quadSize.x;
                uvRect.y = (1f - uvRect.height) / 2f;
            }
            billboard._quadMesh = GPUIUtility.GenerateQuadMesh(billboard.quadSize.x, billboard.quadSize.y, uvRect, true, 0, billboard.yPivotOffset);
            return billboard._quadMesh;
        }

        public static Shader GetBillboardShader(GPUIBillboard.GPUIBillboardShaderType shaderType)
        {
            switch (GPUIRuntimeSettings.Instance.RenderPipeline)
            {
                case GPUIRenderPipeline.URP:
                    return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_URP);
                case GPUIRenderPipeline.HDRP:
                    return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_HDRP);
                default:
                    switch (shaderType)
                    {
                        case GPUIBillboard.GPUIBillboardShaderType.SpeedTree:
                            return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_Builtin_SpeedTree);
                        case GPUIBillboard.GPUIBillboardShaderType.TreeCreator:
                            return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_Builtin_TreeCreator);
                        case GPUIBillboard.GPUIBillboardShaderType.SoftOcclusion:
                            return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_Builtin_SoftOcclusion);
                        default:
                            return GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_BILLBOARD_Builtin);
                    }
            }
        }

#endregion Billboard Methods
    }
}