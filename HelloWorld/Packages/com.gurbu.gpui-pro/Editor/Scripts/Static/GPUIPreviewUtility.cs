// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GPUInstancerPro
{
    [InitializeOnLoad]
    public static class GPUIPreviewCache
    {
        public static Dictionary<int, Texture2D> PreviewCache { get; private set; }

        static GPUIPreviewCache()
        {
            if (PreviewCache == null)
                PreviewCache = new();
            else
                ClearEmptyPreviews();

            AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private static void DestroyPreview(Texture2D preview)
        {
            if (preview != null && preview.name.EndsWith("_GPUIPreview"))
                Object.DestroyImmediate(preview);
        }

        public static void AddPreview(int instanceID, Texture2D preview)
        {
            if (PreviewCache.TryGetValue(instanceID, out Texture2D cachedPreview))
            {
                if (preview == cachedPreview)
                    return;
                DestroyPreview(cachedPreview);
                PreviewCache[instanceID] = preview;
            }
            else
                PreviewCache.Add(instanceID, preview);
        }

        public static void RemovePreview(int instanceID)
        {
            if (PreviewCache.ContainsKey(instanceID))
            {
                DestroyPreview(PreviewCache[instanceID]);
                PreviewCache.Remove(instanceID);
            }
        }

        public static void ClearEmptyPreviews()
        {
            int[] instanceIDs = new int[PreviewCache.Count];
            PreviewCache.Keys.CopyTo(instanceIDs, 0);
            foreach (int instanceID in instanceIDs)
            {
                if (!PreviewCache[instanceID])
                    PreviewCache.Remove(instanceID);
            }
        }

        public static bool TryGetPreview(int key, out Texture2D preview)
        {
            if (PreviewCache.TryGetValue(key, out preview))
                return preview != null;
            return false;
        }

        public static void Cleanup()
        {
            if (PreviewCache == null) 
                return;

            foreach (Texture2D texture in PreviewCache.Values)
                Object.DestroyImmediate(texture);

            PreviewCache.Clear();
        }
    }

    public class GPUIPreviewDrawer
    {
        private Camera _camera;
        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private Light[] lights = new Light[2];
        private readonly int _sampleLayer = 31;
        public bool isVertexBased = true;

        public GPUIPreviewDrawer()
        {
            //InitializeCameraAndLights();
        }

        public void InitializeCameraAndLights()
        {
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            AddGameObject(camGO);
            camGO.transform.rotation = Quaternion.Euler(30, -135, 0);
            _camera = camGO.GetComponent<Camera>();
            _camera.cameraType = CameraType.Preview;
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.Depth;
            _camera.fieldOfView = 15;
            _camera.farClipPlane = 100;
            _camera.nearClipPlane = -100;
            _camera.renderingPath = RenderingPath.Forward;
            _camera.useOcclusionCulling = false;
            _camera.orthographic = true;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << _sampleLayer;
            _camera.allowHDR = false;

            lights = new Light[2];
            var l0 = CreateLight();
            AddGameObject(l0);
            lights[0] = l0.GetComponent<Light>();

            var l1 = CreateLight();
            AddGameObject(l1);
            lights[1] = l1.GetComponent<Light>();

            foreach (Light l in lights)
            {
                l.cullingMask = 1 << _sampleLayer;
            }

            lights[0].color = new Color(1f, 0.9568627f, 0.8392157f, 0f);
            lights[0].transform.rotation = Quaternion.Euler(30, -135, 0);
            lights[0].intensity = 1f;
            lights[1].color = new Color(.4f, .4f, .45f, 0f) * .7f;
            lights[1].transform.rotation = Quaternion.identity;
            lights[1].intensity = 0.5f;

            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                lights[0].color = lights[0].color.linear;
                lights[1].color = lights[1].color.linear;
            }
        }

        public void AddGameObject(GameObject go)
        {
            go.layer = _sampleLayer;
            if (_gameObjects.Contains(go))
                return;

            _gameObjects.Add(go);
        }

        public void Cleanup()
        {
            foreach (var go in _gameObjects)
                Object.DestroyImmediate(go);

            _gameObjects.Clear();
            lights = null;
        }

        public bool TryGetPreviewForPrototype(GPUIPrototype prototype, Vector2 size, Texture textureBG, out Texture2D previewTexture)
        {
            previewTexture = null;
            if (prototype != null)
            {
                if (prototype.prefabObject != null)
                {
                    if (!prototype.prefabObject.IsRenderersDisabled())
                    {
                        if (!AssetPreview.IsLoadingAssetPreview(prototype.prefabObject.GetInstanceID()))
                            previewTexture = AssetPreview.GetAssetPreview(prototype.prefabObject);
                        if (previewTexture != null)
                        {
                            Texture2D copy = new Texture2D(previewTexture.width, previewTexture.height, previewTexture.format, false);
                            Graphics.CopyTexture(previewTexture, copy);
                            copy.name = prototype.ToString() + "_GPUIPreview";
                            previewTexture = copy;
                            return true;
                        }
                        else
                        {
                            previewTexture = AssetPreview.GetMiniThumbnail(prototype.prefabObject);
                            return false;
                        }
                    }
                }
                else if (prototype.gpuiLODGroupData != null && Application.isPlaying)
                {
                    previewTexture = AssetPreview.GetMiniThumbnail(prototype.gpuiLODGroupData);
                    return true;
                }
                else if (prototype.prototypeMesh != null && Application.isPlaying)
                {
                    previewTexture = AssetPreview.GetMiniThumbnail(prototype.prototypeMesh);
                    return true;
                }
            }
            if (textureBG != null)
            {
                if (!AssetPreview.IsLoadingAssetPreview(textureBG.GetInstanceID()))
                    previewTexture = AssetPreview.GetAssetPreview(textureBG);
                if (previewTexture != null)
                    return true;
                else
                {
                    previewTexture = AssetPreview.GetMiniThumbnail(textureBG);
                    return false;
                }
            }

            if (Application.isPlaying)
                return false;

            #region Render with Camera
            RenderTexture renderTargetTexture = null;
            try
            {
                if (!_camera || lights == null)
                {
                    Cleanup();
                    InitializeCameraAndLights();
                }

                #region Initialize
                string textureName = "_GPUIPreview";
                float scaleFac = GetScaleFactor(size.x, size.y);

                int rtWidth = (int)(size.x * scaleFac);
                int rtHeight = (int)(size.y * scaleFac);

                renderTargetTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                renderTargetTexture.Create();
                _camera.targetTexture = renderTargetTexture;

                RenderTexture activeRT = RenderTexture.active;
                RenderTexture.active = renderTargetTexture;
                GL.Clear(true, true, Color.clear);

                foreach (var light in lights)
                    light.enabled = true;

                if (textureBG != null)
                {
                    GL.PushMatrix();
                    GL.LoadOrtho();
                    GL.LoadPixelMatrix(0, renderTargetTexture.width, renderTargetTexture.height, 0);
                    Graphics.DrawTexture(
                        new Rect(0, 0, renderTargetTexture.width, renderTargetTexture.height),
                        textureBG,
                        new Rect(0, 0, 1, 1),
                        0, 0, 0, 0);
                    GL.PopMatrix();
                    textureName = textureBG.name + "_GPUIPreview";
                }
                #endregion Initialize

                if (prototype != null)
                {
                    textureName = prototype.ToString() + "_GPUIPreview";

                    GameObject gameObject = prototype.prefabObject;
                    GPUILODGroupData lodGroupData = prototype.gpuiLODGroupData;
                    Mesh pMesh = prototype.prototypeMesh;
                    Material[] pMaterials = prototype.prototypeMaterials;
                    if (gameObject != null)
                    {
                        Renderer[] renderers;
                        if (gameObject.TryGetComponent(out LODGroup lodGroup))
                            renderers = lodGroup.GetLODs()[0].renderers;
                        else
                            renderers = gameObject.GetComponentsInChildren<Renderer>();
                        Bounds bounds = renderers.GetBounds(isVertexBased);

                        float maxBounds = Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z);

                        _camera.transform.position = bounds.center;
                        _camera.orthographicSize = maxBounds * 1.3f;

                        UnityEditorInternal.InternalEditorUtility.SetCustomLighting(lights, Color.gray);

                        foreach (Renderer renderer in renderers)
                        {
                            if (renderer == null)
                                continue;
                            Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
                            Mesh mesh = null;
                            if (renderer.GetComponent<MeshFilter>())
                            {
                                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                            }
                            else if (renderer is SkinnedMeshRenderer smr)
                            {
                                mesh = smr.sharedMesh;
                                matrix = Matrix4x4.TRS(matrix.rotation * matrix.GetPosition(), Quaternion.identity, matrix.lossyScale);
                            }
                            if (mesh != null && renderer.sharedMaterials != null)
                            {
                                int subMeshIndex = 0;
                                foreach (Material mat in renderer.sharedMaterials)
                                {
                                    Graphics.DrawMesh(mesh, matrix, mat, _sampleLayer, _camera, Math.Min(subMeshIndex, mesh.subMeshCount - 1),
                                        null, ShadowCastingMode.Off, false, null, false);
                                    subMeshIndex++;
                                }
                            }
                        }

                        _camera.Render();
                        UnityEditorInternal.InternalEditorUtility.RemoveCustomLighting();
                    }
                    else if (lodGroupData != null && lodGroupData.Length > 0 && lodGroupData[0].Length > 0)
                    {
                        float maxBounds = Mathf.Max(Mathf.Max(lodGroupData.bounds.extents.x, lodGroupData.bounds.extents.y), lodGroupData.bounds.extents.z);
                        _camera.transform.position = lodGroupData.bounds.center;
                        _camera.orthographicSize = maxBounds * 1.3f;

                        UnityEditorInternal.InternalEditorUtility.SetCustomLighting(lights, Color.gray);

                        for (int i = 0; i < lodGroupData[0].Length; i++)
                        {
                            var renderer = lodGroupData[0][i];
                            if (renderer.rendererMaterials != null)
                            {
                                int submeshIndex = 0;
                                foreach (Material mat in renderer.rendererMaterials)
                                {
                                    Graphics.DrawMesh(renderer.rendererMesh, renderer.transformOffset, mat, _sampleLayer, _camera, Math.Min(submeshIndex, renderer.rendererMesh.subMeshCount - 1),
                                        null, ShadowCastingMode.Off, false, null, false);
                                    submeshIndex++;
                                }
                            }
                        }

                        _camera.Render();
                        UnityEditorInternal.InternalEditorUtility.RemoveCustomLighting();
                    }
                    else if (pMesh != null && pMaterials != null)
                    {
                        float maxBounds = Mathf.Max(Mathf.Max(pMesh.bounds.extents.x, pMesh.bounds.extents.y), pMesh.bounds.extents.z);
                        _camera.transform.position = pMesh.bounds.center;
                        _camera.orthographicSize = maxBounds * 1.3f;

                        UnityEditorInternal.InternalEditorUtility.SetCustomLighting(lights, Color.gray);

                        for (int i = 0; i < pMaterials.Length; i++)
                        {
                            int submeshIndex = 0;
                            foreach (Material mat in pMaterials)
                            {
                                Graphics.DrawMesh(pMesh, Matrix4x4.identity, mat, _sampleLayer, _camera, Math.Min(submeshIndex, pMesh.subMeshCount - 1), null, ShadowCastingMode.Off, false, null, false);
                                submeshIndex++;
                            }
                        }

                        _camera.Render();
                        UnityEditorInternal.InternalEditorUtility.RemoveCustomLighting();
                    }
                }

                #region Generate Texture
                previewTexture = new Texture2D(renderTargetTexture.width, renderTargetTexture.height, TextureFormat.RGBA32, false);
                previewTexture.ReadPixels(new Rect(0, 0, renderTargetTexture.width, renderTargetTexture.height), 0, 0);
                previewTexture.name = textureName;
                previewTexture.Apply(false, true);
                #endregion Generate Texture

                #region End Preview
                RenderTexture.active = activeRT;

                foreach (var light in lights)
                    light.enabled = false;

                RenderTexture.ReleaseTemporary(renderTargetTexture);
                #endregion End Preview
            }
            catch (Exception e)
            {
                if (renderTargetTexture != null && renderTargetTexture.IsCreated())
                    RenderTexture.ReleaseTemporary(renderTargetTexture);
                Debug.LogError(e);
                return false;
            }
            #endregion Render with Camera

            return true;
        }

        public float GetScaleFactor(float width, float height)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            float result = Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
            return result;
        }

        protected static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.enabled = false;
            return lightGO;
        }
    }
}