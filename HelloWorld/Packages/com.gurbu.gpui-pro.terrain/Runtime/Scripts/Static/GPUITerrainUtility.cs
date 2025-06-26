// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using static GPUInstancerPro.TerrainModule.GPUITerrainConstants;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    public static class GPUITerrainUtility
    {
        #region Create Detail Render Textures
        private const float DETAIL_DENSITY_VALUE_DIVIDER = 255f;

        public static RenderTexture CreateDetailRenderTexture(int resolution, string name)
        {
            RenderTexture result = new RenderTexture(resolution, resolution, 0, R8_RenderTextureFormat, RenderTextureReadWrite.Linear)
            {
                name = name,
                isPowerOfTwo = false,
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                useMipMap = false,
                autoGenerateMips = false,
            };
            result.Create();
            result.ClearRenderTexture(); // To make sure that we start with an empty texture and not with random data.

            return result;
        }

        public static void CaptureTerrainDetailToRenderTexture(TerrainData terrainData, int detailLayer, RenderTexture renderTexture, bool sampleTerrainHoles)
        {
            int detailResolution = terrainData.detailResolution;

            Profiler.BeginSample("TerrainData.GetDetailLayer");
            int[,] details = terrainData.GetDetailLayer(0, 0, detailResolution, detailResolution, detailLayer);
            Profiler.EndSample();

            CaptureTerrainDetailToRenderTexture(terrainData, detailResolution, details, renderTexture, sampleTerrainHoles);
        }

        public static void CaptureTerrainDetailToRenderTexture(TerrainData terrainData, int detailResolution, int[,] details, RenderTexture renderTexture, bool sampleTerrainHoles)
        {
            int bufferSize = detailResolution * detailResolution;

            GraphicsBuffer detailLayerBuffer = new(GraphicsBuffer.Target.Structured, bufferSize, 4 /* int */);
            detailLayerBuffer.SetData(details);

            ComputeShader cs = CS_TerrainDetailCapture;
            if (sampleTerrainHoles)
            {
                cs.EnableKeyword(Kw_GPUI_TERRAIN_HOLES);
                cs.SetTexture(0, PROP_terrainHoleTexture, terrainData.holesTexture);
            }
            else
                cs.DisableKeyword(Kw_GPUI_TERRAIN_HOLES);

            cs.SetTexture(0, PROP_terrainDetailTexture, renderTexture);
            cs.SetBuffer(0, PROP_detailLayerBuffer, detailLayerBuffer);
            cs.SetInt(PROP_detailResolution, detailResolution);
            cs.DispatchX(0, bufferSize);

            detailLayerBuffer.Dispose();
        }

        public static void CaptureTerrainDetailToRenderTextureWithComputeDetailInstanceTransforms(TerrainData terrainData, int detailLayer, float density, RenderTexture renderTexture, bool sampleTerrainHoles)
        {
            int detailPatchCount = terrainData.detailPatchCount;
            DetailInstanceTransform[] instanceTransformArray = new DetailInstanceTransform[0];
            int arraySize = 0;
            for (int patchY = 0; patchY < detailPatchCount; patchY++)
            {
                for (int patchX = 0; patchX < detailPatchCount; patchX++)
                {
                    DetailInstanceTransform[] patchInstanceTransforms = terrainData.ComputeDetailInstanceTransforms(patchX, patchY, detailLayer, density, out _);
                    int additionSize = patchInstanceTransforms.Length;
                    if (additionSize == 0)
                        continue;
                    Array.Resize(ref instanceTransformArray, arraySize + additionSize);
                    Array.Copy(patchInstanceTransforms, 0, instanceTransformArray, arraySize, additionSize);
                    arraySize += additionSize;
                }
            }

            if (arraySize == 0)
                return;

            int detailResolution = terrainData.detailResolution;

            ComputeShader cs = CS_TerrainDetailCaptureFromInstanceTransforms;

            GraphicsBuffer detailLayerBuffer = new(GraphicsBuffer.Target.Structured, detailResolution * detailResolution, 4 /* int */);
            GraphicsBuffer detailInstanceTransformBuffer = new(GraphicsBuffer.Target.Structured, arraySize, 4 * 6 /* float 6 */);
            detailInstanceTransformBuffer.SetData(instanceTransformArray);

            // Reset detail layer buffer values to zero
            cs.SetBuffer(1, PROP_detailLayerBuffer, detailLayerBuffer);
            cs.SetInt(GPUIConstants.PROP_bufferSize, detailLayerBuffer.count);
            cs.DispatchX(1, detailLayerBuffer.count);

            // Fill detail layer buffer
            cs.SetBuffer(0, PROP_detailLayerBuffer, detailLayerBuffer);
            cs.SetBuffer(0, "detailInstanceTransformBuffer", detailInstanceTransformBuffer);
            cs.SetInt(PROP_detailResolution, detailResolution);
            cs.SetVector(PROP_terrainSize, terrainData.size);
            cs.SetInt(GPUIConstants.PROP_bufferSize, arraySize);
            cs.DispatchX(0, arraySize);

            detailInstanceTransformBuffer.Dispose();

            // Set values to texture
            cs = CS_TerrainDetailCapture;
            if (sampleTerrainHoles)
            {
                cs.EnableKeyword(Kw_GPUI_TERRAIN_HOLES);
                cs.SetTexture(0, PROP_terrainHoleTexture, terrainData.holesTexture);
            }
            else
                cs.DisableKeyword(Kw_GPUI_TERRAIN_HOLES);

            cs.SetTexture(0, PROP_terrainDetailTexture, renderTexture);
            cs.SetBuffer(0, PROP_detailLayerBuffer, detailLayerBuffer);
            cs.SetInt(PROP_detailResolution, detailResolution);
            cs.DispatchX(0, detailResolution * detailResolution);

            detailLayerBuffer.Dispose();
        }

        public static void UpdateTerrainDetailWithRenderTexture(Terrain terrain, int detailLayer, RenderTexture renderTexture)
        {
            UpdateTerrainDetailWithRenderTexture(terrain.terrainData, detailLayer, renderTexture);
        }

        public static void UpdateTerrainDetailWithRenderTexture(TerrainData terrainData, int detailLayer, RenderTexture renderTexture, float densityMultiplier = 1f)
        {
            UpdateTerrainDetailWithTexture2D(terrainData, detailLayer, GPUITextureUtility.RenderTextureToTexture2D(renderTexture, TextureFormat.R8, true, FilterMode.Point), densityMultiplier);
        }

        public static void UpdateTerrainDetailWithTexture2D(TerrainData terrainData, int detailLayer, Texture2D detailTexture, float densityMultiplier = 1f)
        {
            Color[] pixels;
            if (detailTexture.isReadable)
                pixels = detailTexture.GetPixels();
            else
            {
                RenderTexture tempRT = CreateDetailRenderTexture(detailTexture.width, null);
                GPUITextureUtility.CopyTextureSamplerWithComputeShader(detailTexture, tempRT);
                detailTexture = GPUITextureUtility.RenderTextureToTexture2D(tempRT, TextureFormat.R8, true, FilterMode.Point);
                pixels = detailTexture.GetPixels();

                tempRT.DestroyRenderTexture();
                detailTexture.DestroyGeneric();
            }
            int detailResolution = terrainData.detailResolution;
            int[,] details = new int[detailResolution, detailResolution];
            for (int x = 0; x < detailResolution; x++)
            {
                for (int y = 0; y < detailResolution; y++)
                {
                    float val = pixels[y * detailResolution + x].r * 255f * densityMultiplier;
                    details[y, x] = (int)val;
                }
            }
            terrainData.SetDetailLayer(0, 0, detailLayer, details);
        }

        #endregion Create Detail Render Textures

        #region Mesh Utility Methods

        public static Mesh CreateCrossQuadsMesh(string name, int quadCount)
        {
            GameObject parent = new GameObject(name, typeof(MeshFilter));
            parent.hideFlags = HideFlags.HideAndDontSave;
            parent.transform.position = Vector3.zero;
            CombineInstance[] combinesInstances = new CombineInstance[quadCount];
            for (int i = 0; i < quadCount; i++)
            {
                GameObject child = new GameObject("quadToCombine_" + i, typeof(MeshFilter));

                Mesh mesh = GPUIUtility.GenerateQuadMesh(1, 1, new Rect(0.0f, 0.0f, 1.0f, 1.0f), true, 0, 0, true);

                // modify normals fit for grass
                for (int j = 0; j < mesh.normals.Length; j++)
                    mesh.normals[i] = Vector3.up;

                child.GetComponent<MeshFilter>().sharedMesh = mesh;
                child.transform.parent = parent.transform;
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity * Quaternion.AngleAxis((180.0f / quadCount) * i, Vector3.up);
                child.transform.localScale = Vector3.one;

                combinesInstances[i] = new CombineInstance
                {
                    mesh = child.GetComponent<MeshFilter>().sharedMesh,
                    transform = child.transform.localToWorldMatrix
                };
            }
            parent.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            parent.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combinesInstances, true, true);
            Mesh result = parent.GetComponent<MeshFilter>().sharedMesh;
            result.name = name;

            GameObject.DestroyImmediate(parent);
            return result;
        }

        public static Mesh GenerateBladeMesh(Vector2 size, int segmentCount, float bendMultiplier, float bendLowerAmount, AnimationCurve bladeBendCurve, AnimationCurve bladeWidthCurve)
        {
            Mesh mesh = new Mesh();
            mesh.name = "BladeMesh";

            int vertexCount = 4 + (2 * (segmentCount - 1)) + 1;
            float segmentHeight = size.y / (segmentCount + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segmentCount * 6 + 3];

            for (int i = 0; i < segmentCount; i++)
            {
                float segmentBend = bladeBendCurve.Evaluate((i + 1f) / (segmentCount + 1)) * size.y * bendMultiplier;
                float lowerAmount = segmentBend * bendLowerAmount;
                float segmentWidth = size.x * bladeWidthCurve.Evaluate((i + 1f) / segmentCount);
                vertices[(i + 1) * 2] = new Vector3(-segmentWidth, segmentHeight * (i + 1) - lowerAmount, segmentBend); // top left
                vertices[(i + 1) * 2 + 1] = new Vector3(segmentWidth, segmentHeight * (i + 1) - lowerAmount, segmentBend); // top right

                uvs[(i + 1) * 2] = new Vector2(0, (i + 1f) / (segmentCount + 1));
                uvs[(i + 1) * 2 + 1] = new Vector2(1, (i + 1f) / (segmentCount + 1));

                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = (i + 1) * 2;
                triangles[i * 6 + 2] = i * 2 + 1;
                triangles[i * 6 + 3] = i * 2 + 1;
                triangles[i * 6 + 4] = (i + 1) * 2;
                triangles[i * 6 + 5] = (i + 1) * 2 + 1;
            }

            float bottomWidth = size.x * bladeWidthCurve.Evaluate(0);
            vertices[0] = new Vector3(-bottomWidth, 0, 0); // bottom left
            vertices[1] = new Vector3(bottomWidth, 0, 0); // bottom right
            float topBend = bladeBendCurve.Evaluate(1) * size.y * bendMultiplier;
            vertices[vertexCount - 1] = new Vector3(0, size.y - topBend * bendLowerAmount, topBend); // top
            mesh.vertices = vertices;

            uvs[0] = Vector2.zero;
            uvs[1] = new Vector2(1, 0);
            uvs[vertexCount - 1] = Vector2.one;
            mesh.uv = uvs;

            triangles[triangles.Length - 3] = segmentCount * 2;
            triangles[triangles.Length - 2] = vertexCount - 1;
            triangles[triangles.Length - 1] = segmentCount * 2 + 1;

            mesh.triangles = triangles;

            Vector3 planeNormal = new Vector3(0, 0, -1);
            Vector4 planeTangent = new Vector4(1, 0, 0, -1);

            Vector3[] normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                normals[i] = planeNormal;
            mesh.normals = normals;

            Vector4[] tangents = new Vector4[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                tangents[i] = planeTangent;
            mesh.tangents = tangents;

            Color[] colors = new Color[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                colors[i] = Color.Lerp(Color.clear, Color.red, mesh.vertices[i].y / size.y);

            mesh.colors = colors;

            return mesh;
        }

        #endregion Mesh Utility Methods

        #region Runtime Modifications

        #region Detail Manager
        public static void SetDetailDensityInsideCollider(GPUIDetailManager detailManager, Collider collider, float valueToSet, float offset = 0, List<int> prototypeIndexFilter = null)
        {
            if (!detailManager.IsInitialized)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Detail Manager is not initialized. Can not modify the detail density!");
                return;
            }

            if (collider is BoxCollider boxCollider)
                SetDetailDensityInsideBoxCollider(detailManager, valueToSet, boxCollider, offset, prototypeIndexFilter);
            else if (collider is SphereCollider sphereCollider)
                SetDetailDensityInsideSphereCollider(detailManager, valueToSet, sphereCollider, offset, prototypeIndexFilter);
            else if (collider is CapsuleCollider capsuleCollider)
                SetDetailDensityInsideCapsuleCollider(detailManager, valueToSet, capsuleCollider, offset, prototypeIndexFilter);
            else
                SetDetailDensityInsideBounds(detailManager, valueToSet, collider.bounds, offset, prototypeIndexFilter);
            detailManager.RequireUpdate();
        }

        private static void SetCommonCSParams(ComputeShader cs, int kernelIndex, GPUITerrain gpuiTerrain, RenderTexture detailTexture, int textureSize, Texture heightmapTexture)
        {
            if (gpuiTerrain.HasTwoChannelHeightmap())
                cs.EnableKeyword(Kw_GPUI_TWO_CHANNEL_HEIGHTMAP);
            else
                cs.DisableKeyword(Kw_GPUI_TWO_CHANNEL_HEIGHTMAP);

            cs.SetTexture(kernelIndex, PROP_terrainDetailTexture, detailTexture);
            cs.SetTexture(kernelIndex, PROP_heightmapTexture, heightmapTexture);
            cs.SetInt(PROP_heightmapTextureSize, heightmapTexture.width);
            cs.SetInt(PROP_detailTextureSize, textureSize);
            cs.SetVector(PROP_terrainPosition, gpuiTerrain.GetPosition());
            cs.SetVector(PROP_terrainSize, gpuiTerrain.GetSize());
        }

        public static void SetDetailDensityInsideBounds(GPUIDetailManager detailManager, float valueToSet, Bounds bounds, float offset, List<int> prototypeIndexFilter)
        {
            if (!detailManager.IsInitialized)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Detail Manager is not initialized. Can not modify the detail density!");
                return;
            }
            valueToSet /= DETAIL_DENSITY_VALUE_DIVIDER;
            int count = detailManager.GetPrototypeCount();
            ComputeShader cs = CS_TerrainDetailDensityModifier;
            int kernelIndex = 0;

            foreach (GPUITerrain gpuiTerrain in detailManager.GetActiveTerrainValues())
            {
                if (!gpuiTerrain.GetTerrainWorldBounds().Intersects(bounds)) continue;

                Texture heightmapTexture = gpuiTerrain.GetHeightmapTexture();
                if (heightmapTexture == null)
                    continue;

                if (!gpuiTerrain.IsDetailDensityTexturesLoaded)
                    gpuiTerrain.CreateDetailTextures();

                for (int i = 0; i < count; i++)
                {
                    if (prototypeIndexFilter != null && prototypeIndexFilter.Count > 0 && !prototypeIndexFilter.Contains(i))
                        continue;

                    RenderTexture detailTexture = gpuiTerrain.GetDetailDensityTexture(gpuiTerrain.GetFirstTerrainDetailPrototypeIndex(i));
                    if (detailTexture == null)
                        continue;
                    int textureSize = detailTexture.width;

                    SetCommonCSParams(cs, kernelIndex, gpuiTerrain, detailTexture, textureSize, heightmapTexture);
                    cs.SetFloat(GPUIConstants.PROP_valueToSet, valueToSet);
                    cs.SetVector(GPUIConstants.PROP_boundsCenter, bounds.center);
                    cs.SetVector(GPUIConstants.PROP_boundsExtents, bounds.extents + Vector3.one * offset);
                    cs.DispatchXZ(kernelIndex, textureSize, textureSize);
                }
            }
            detailManager.RequireUpdate();
        }

        public static void SetDetailDensityInsideBoxCollider(GPUIDetailManager detailManager, float valueToSet, BoxCollider boxCollider, float offset, List<int> prototypeIndexFilter)
        {
            if (!detailManager.IsInitialized)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Detail Manager is not initialized. Can not modify the detail density!");
                return;
            }
            valueToSet /= DETAIL_DENSITY_VALUE_DIVIDER;
            int count = detailManager.GetPrototypeCount();
            ComputeShader cs = CS_TerrainDetailDensityModifier;
            int kernelIndex = 1;

            Vector3 center = boxCollider.center;
            Vector3 extents = boxCollider.size / 2 + Vector3.one * offset;
            Matrix4x4 modifierTransform = boxCollider.transform.localToWorldMatrix;

            Bounds colliderBounds = boxCollider.bounds;
            foreach (GPUITerrain gpuiTerrain in detailManager.GetActiveTerrainValues())
            {
                if (!gpuiTerrain.GetTerrainWorldBounds().Intersects(colliderBounds)) continue;

                Texture heightmapTexture = gpuiTerrain.GetHeightmapTexture();
                if (heightmapTexture == null)
                    continue;

                if (!gpuiTerrain.IsDetailDensityTexturesLoaded)
                    gpuiTerrain.CreateDetailTextures();

                for (int i = 0; i < count; i++)
                {
                    if (prototypeIndexFilter != null && prototypeIndexFilter.Count > 0 && !prototypeIndexFilter.Contains(i))
                        continue;

                    RenderTexture detailTexture = gpuiTerrain.GetDetailDensityTexture(gpuiTerrain.GetFirstTerrainDetailPrototypeIndex(i));
                    if (detailTexture == null)
                        continue;
                    int textureSize = detailTexture.width;

                    SetCommonCSParams(cs, kernelIndex, gpuiTerrain, detailTexture, textureSize, heightmapTexture);
                    cs.SetFloat(GPUIConstants.PROP_valueToSet, valueToSet);
                    cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                    cs.SetVector(GPUIConstants.PROP_boundsExtents, extents);
                    cs.SetMatrix(GPUIConstants.PROP_modifierTransform, modifierTransform);
                    cs.DispatchXZ(kernelIndex, textureSize, textureSize);
                }
            }
        }

        public static void SetDetailDensityInsideSphereCollider(GPUIDetailManager detailManager, float valueToSet, SphereCollider sphereCollider, float offset, List<int> prototypeIndexFilter)
        {
            if (!detailManager.IsInitialized)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Detail Manager is not initialized. Can not modify the detail density!");
                return;
            }
            valueToSet /= DETAIL_DENSITY_VALUE_DIVIDER;
            int count = detailManager.GetPrototypeCount();
            ComputeShader cs = CS_TerrainDetailDensityModifier;
            int kernelIndex = 2;

            Vector3 center = sphereCollider.center + sphereCollider.transform.position;
            Vector3 scale = sphereCollider.transform.lossyScale;
            float radius = sphereCollider.radius * Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z) + offset;

            Bounds colliderBounds = sphereCollider.bounds;
            foreach (GPUITerrain gpuiTerrain in detailManager.GetActiveTerrainValues())
            {
                if (!gpuiTerrain.GetTerrainWorldBounds().Intersects(colliderBounds)) continue;

                Texture heightmapTexture = gpuiTerrain.GetHeightmapTexture();
                if (heightmapTexture == null)
                    continue;

                if (!gpuiTerrain.IsDetailDensityTexturesLoaded)
                    gpuiTerrain.CreateDetailTextures();

                for (int i = 0; i < count; i++)
                {
                    if (prototypeIndexFilter != null && prototypeIndexFilter.Count > 0 && !prototypeIndexFilter.Contains(i))
                        continue;

                    RenderTexture detailTexture = gpuiTerrain.GetDetailDensityTexture(gpuiTerrain.GetFirstTerrainDetailPrototypeIndex(i));
                    if (detailTexture == null)
                        continue;
                    int textureSize = detailTexture.width;

                    SetCommonCSParams(cs, kernelIndex, gpuiTerrain, detailTexture, textureSize, heightmapTexture);
                    cs.SetFloat(GPUIConstants.PROP_valueToSet, valueToSet);
                    cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                    cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
                    cs.DispatchXZ(kernelIndex, textureSize, textureSize);
                }
            }
        }

        public static void SetDetailDensityInsideCapsuleCollider(GPUIDetailManager detailManager, float valueToSet, CapsuleCollider capsuleCollider, float offset, List<int> prototypeIndexFilter)
        {
            if (!detailManager.IsInitialized)
            {
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Detail Manager is not initialized. Can not modify the detail density!");
                return;
            }
            valueToSet /= DETAIL_DENSITY_VALUE_DIVIDER;
            int count = detailManager.GetPrototypeCount();
            ComputeShader cs = CS_TerrainDetailDensityModifier;
            int kernelIndex = 3;

            Vector3 center = capsuleCollider.center;
            Vector3 scale = capsuleCollider.transform.lossyScale;
            float radius = capsuleCollider.radius * Mathf.Max(Mathf.Max(
                capsuleCollider.direction == 0 ? 0 : scale.x, 
                capsuleCollider.direction == 1 ? 0 : scale.y), 
                capsuleCollider.direction == 2 ? 0 : scale.z) + offset;
            float height = capsuleCollider.height * (
                    capsuleCollider.direction == 0 ? scale.x : 0 +
                    capsuleCollider.direction == 1 ? scale.y : 0 +
                    capsuleCollider.direction == 2 ? scale.z : 0);

            Bounds colliderBounds = capsuleCollider.bounds;
            foreach (GPUITerrain gpuiTerrain in detailManager.GetActiveTerrainValues())
            {
                if (!gpuiTerrain.GetTerrainWorldBounds().Intersects(colliderBounds)) continue;

                Texture heightmapTexture = gpuiTerrain.GetHeightmapTexture();
                if (heightmapTexture == null)
                    continue;

                if (!gpuiTerrain.IsDetailDensityTexturesLoaded)
                    gpuiTerrain.CreateDetailTextures();

                for (int i = 0; i < count; i++)
                {
                    if (prototypeIndexFilter != null && !prototypeIndexFilter.Contains(i))
                        continue;

                    RenderTexture detailTexture = gpuiTerrain.GetDetailDensityTexture(gpuiTerrain.GetFirstTerrainDetailPrototypeIndex(i));
                    if (detailTexture == null)
                        continue;
                    int textureSize = detailTexture.width;

                    SetCommonCSParams(cs, kernelIndex, gpuiTerrain, detailTexture, textureSize, heightmapTexture);
                    cs.SetFloat(GPUIConstants.PROP_valueToSet, valueToSet);
                    cs.SetVector(GPUIConstants.PROP_boundsCenter, center);
                    cs.SetFloat(GPUIConstants.PROP_modifierRadius, radius);
                    cs.SetFloat(GPUIConstants.PROP_modifierHeight, height);
                    cs.DispatchXZ(kernelIndex, textureSize, textureSize);
                }
            }
        }
        #endregion Detail Manager

        #endregion Runtime Modifications

        #region Unity Terrain Methods

        public static Material GetDefaultTerrainMaterial()
        {
            Material terrainMaterial;
            if (GPUIRuntimeSettings.Instance.IsURP)
                terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            else if (GPUIRuntimeSettings.Instance.IsHDRP)
                terrainMaterial = new Material(Shader.Find("HDRP/TerrainLit"));
            else
                terrainMaterial = new Material(Shader.Find("Nature/Terrain/Standard"));

            return terrainMaterial;
        }

        #endregion Unity Terrain Methods

        #region Editor Methods

#if UNITY_EDITOR
        public static Texture2D SaveDetailDensityTexture(RenderTexture detailDensityTexture, string folderPath, string fileName = null)
        {
            return GPUITextureUtility.SaveRenderTextureToPNG(detailDensityTexture, TextureFormat.R8, folderPath, fileName, true, FilterMode.Point, TextureImporterType.SingleChannel, detailDensityTexture.width, false, false, false, TextureImporterCompression.Uncompressed, false);
        }

        public static Texture2D SaveDetailDensityTexture(Texture2D detailDensityTexture, string folderPath, string fileName = null)
        {
            return GPUITextureUtility.SaveTexture2DToPNG(detailDensityTexture, folderPath, fileName, FilterMode.Point, TextureImporterType.SingleChannel, detailDensityTexture.width, false, false, false, TextureImporterCompression.Uncompressed, false);
        }
#endif // UNITY_EDITOR

        #endregion Editor Methods
    }
}