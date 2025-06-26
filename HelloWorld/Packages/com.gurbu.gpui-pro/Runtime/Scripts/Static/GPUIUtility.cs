// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using System.IO;
using Unity.Mathematics;
using System.Reflection;
using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public static class GPUIUtility
    {
        #region Transform/GO Extensions

        public static bool HasComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() != null;
        }

        public static bool HasComponentInChildren<T>(this GameObject go) where T : Component
        {
            return go.GetComponentInChildren<T>() != null;
        }

        public static bool HasComponentInChildrenExceptParent<T>(this GameObject go) where T : Component
        {
            foreach (Transform childTransform in go.transform)
            {
                if (childTransform.GetComponentInChildren<T>() != null)
                    return true;
            }
            return false;
        }

        public static bool HasComponent<T>(this Transform transform) where T : Component
        {
            return transform.TryGetComponent<T>(out _);
        }

        public static Matrix4x4 GetTransformOffset(this Transform parentTransform, Transform childTransform)
        {
            Matrix4x4 transformOffset = Matrix4x4.identity;
            Transform currentTransform = childTransform;
            while (currentTransform != parentTransform)
            {
                transformOffset = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * transformOffset;
                currentTransform = currentTransform.parent;
            }
            return transformOffset;
        }

        public static void GetMeshRenderers(this Transform transform, List<Renderer> meshRenderers, bool includeSkinnedMeshRenderers)
        {
            if (meshRenderers == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "A list must be supplied to call GetMeshRenderers method.");
                return;
            }
            if (transform.TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderers.Add(meshRenderer);
            if (includeSkinnedMeshRenderers && transform.TryGetComponent(out SkinnedMeshRenderer smr))
                meshRenderers.Add(smr);

            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = transform.GetChild(i);
                if (!childTransform.TryGetComponent<GPUIPrefabDefinition>(out _))
                    childTransform.GetMeshRenderers(meshRenderers, includeSkinnedMeshRenderers);
            }
        }

        public static void GetPrefabRenderers(this Transform transform, List<Renderer> renderers)
        {
            if (transform.TryGetComponent(out Renderer renderer))
            {
                if (renderer is MeshRenderer || renderer is BillboardRenderer || renderer is SkinnedMeshRenderer)
                    renderers.Add(renderer);
            }

            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = transform.GetChild(i);
                if (!childTransform.TryGetComponent<GPUIPrefabDefinition>(out _))
                    childTransform.GetPrefabRenderers(renderers);
            }
        }

        public static void SetMatrixToTransform(this Transform transform, Matrix4x4 matrix)
        {
            transform.SetPositionAndRotation(matrix.GetPosition(), matrix.rotation);
            transform.localScale = matrix.lossyScale;
        }

        public static void DestroyGeneric(this UnityEngine.Object uObject)
        {
            if (!uObject)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(uObject);
            else
                UnityEngine.Object.DestroyImmediate(uObject);
        }

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {
            T result = gameObject.GetComponent<T>();
            if (result == null)
            {
                result = gameObject.AddComponent<T>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(gameObject);
#endif
            }
            return result;
        }

        public static T AddOrGetComponent<T>(this Component component) where T : Component
        {
            T result = component.GetComponent<T>();
            if (result == null)
            {
                result = component.gameObject.AddComponent<T>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(component.gameObject);
#endif
            }
            return result;
        }

        public static Bounds GetBounds(this GameObject gameObject, bool isVertexBased = false)
        {
            Renderer[] renderers;
            if (gameObject.TryGetComponent(out LODGroup lodGroup))
                renderers = lodGroup.GetLODs()[0].renderers;
            else
                renderers = gameObject.GetComponentsInChildren<Renderer>();
            return renderers.GetBounds(isVertexBased);
        }

        public static Bounds GetBounds(this Renderer[] renderers, bool isVertexBased = false)
        {
            Bounds bounds = new Bounds();
            bool isBoundsInitialized = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                Mesh mesh = null;
                if (renderer.TryGetComponent(out MeshFilter meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                }
                else if (renderer is SkinnedMeshRenderer smr)
                {
                    mesh = smr.sharedMesh;
                }

                if (mesh != null)
                {
                    if (isVertexBased && mesh.isReadable)
                    {
                        Matrix4x4 rendererLTW = renderer.transform.localToWorldMatrix;
                        Vector3[] verts = mesh.vertices;
                        if (!isBoundsInitialized && verts.Length > 0)
                        {
                            isBoundsInitialized = true;
                            bounds = new Bounds(rendererLTW.MultiplyPoint3x4(verts[0]), Vector3.zero);
                        }
                        for (var v = 0; v < verts.Length; v++)
                        {
                            bounds.Encapsulate(rendererLTW.MultiplyPoint3x4(verts[v]));
                        }
                    }
                    else
                    {
                        Bounds rendererBounds = renderer.bounds;
                        if (!isBoundsInitialized)
                        {
                            isBoundsInitialized = true;
                            bounds = new Bounds(rendererBounds.center, rendererBounds.size);
                        }
                        else
                        {
                            bounds.Encapsulate(rendererBounds);
                        }
                    }
                }
            }

            return bounds;
        }

        public static void SetLayer(this GameObject gameObject, int layer, bool includeChildren = true)
        {
            gameObject.layer = layer;
            if (includeChildren)
            {
                foreach (Transform childTransform in gameObject.transform.GetComponentsInChildren<Transform>(true))
                {
                    childTransform.gameObject.layer = layer;
                }
            }
        }

        public static bool EqualOrParentOf(this GameObject parent, GameObject child)
        {
            if (parent == child) return true;
            Transform pt = parent.transform;
            Transform ct = child.transform.parent;
            while (ct != null)
            {
                if (pt == ct) return true;
                ct = ct.transform.parent;
            }
            return false;
        }

        public static GameObject GetPrefabRoot(this GameObject go)
        {
            if (go == null) return null;
            return GetPrefabRoot(go.transform).gameObject;
        }

        public static Transform GetPrefabRoot(this Transform transform)
        {
            Transform parent = transform;
            while (parent != null)
            {
                transform = parent;
                parent = transform.parent;
            }
            return transform;
        }

        public static int GetLODCount(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out LODGroup lodGroup))
                return lodGroup.lodCount;
            else
                return 1;
        }

        public static int GetVertexCount(this Renderer[] renderers)
        {
            int vertexCount = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                Mesh mesh = null;
                if (renderer.TryGetComponent(out MeshFilter meshFilter))
                    mesh = meshFilter.sharedMesh;
                else if (renderer is SkinnedMeshRenderer smr)
                    mesh = smr.sharedMesh;

                if (mesh != null)
                    vertexCount += mesh.vertexCount;
            }
            return vertexCount;
        }

        public static bool IsRenderersDisabled(this GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                if (renderer.enabled) return false;
            return true;
        }

        public static bool HasShader(this GameObject gameObject, string shaderName)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            string gpuiShaderName = ConvertToGPUIShaderName(shaderName, null);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader != null && (mat.shader.name == shaderName || mat.shader.name == gpuiShaderName))
                        return true;
                }
            }
            return false;
        }

        public static GameObject InstantiateWithStrippedComponents(GameObject prefabGO, Vector3 position, Quaternion rotation, IEnumerable<Type> allowedComponentTypes)
        {
            bool isGOActive = prefabGO.activeSelf;
            if (isGOActive)
                prefabGO.SetActive(false);
            GameObject result = UnityEngine.Object.Instantiate(prefabGO, position, rotation);
            if (isGOActive)
                prefabGO.SetActive(true);
            StripUnwantedComponents(result, allowedComponentTypes);
            if (isGOActive)
                result.SetActive(true);
            return result;
        }

        public static void StripUnwantedComponents(GameObject gameObject, IEnumerable<Type> allowedComponentTypes)
        {
            List<Component> componentsInChildren = new List<Component>();
            gameObject.GetComponentsInChildren(true, componentsInChildren);
            componentsInChildren.RemoveAll(c => c is Transform);
            List<Component> components = new List<Component>();
            foreach (var component in componentsInChildren)
            {
                if (component == null)
                    continue;
                Type componentType = component.GetType();

                bool isAllowed = false;
                foreach (Type allowedType in allowedComponentTypes)
                {
                    if (componentType == allowedType)
                    {
                        isAllowed = true;
                        break;
                    }
                }
                if (isAllowed)
                    continue;
                component.GetComponents(components);
                StripUnwantedComponent(component, components);
            }
        }

        private static void StripUnwantedComponent(Component component, List<Component> components)
        {
            List<Component> dependentComponents = GetDependentComponents(component, components);
            foreach (var c in dependentComponents)
            {
                StripUnwantedComponent(c, components);
            }

            UnityEngine.Object.DestroyImmediate(component);
        }

        private static List<Component> GetDependentComponents(Component component, List<Component> components)
        {
            List<Component> dependentComponents = new List<Component>();
            Type componentType = component.GetType();
            for (int i = 0; i < components.Count; i++)
            {
                var dependentComponent = components[i];
                if (dependentComponent == null || dependentComponent == component || dependentComponent is Transform)
                    continue;
                Type dependentComponentType = dependentComponent.GetType();
                var requireComponentAttributes = dependentComponentType.GetCustomAttributes<RequireComponent>(true);
                foreach (RequireComponent rc in requireComponentAttributes)
                {
                    if (rc.m_Type0 == componentType || rc.m_Type1 == componentType || rc.m_Type2 == componentType)
                    {
                        dependentComponents.Add(dependentComponent);
                        break;
                    }
                }
            }
            return dependentComponents;
        }

        public static Transform FindDeepChild(this Transform parentTransform, string childName)
        {
            Transform result = parentTransform.Find(childName);
            if (result != null)
                return result;
            int childCount = parentTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                result = parentTransform.GetChild(i).FindDeepChild(childName);
                if (result != null)
                    return result;
            }
            return null;
        }
        #endregion Transform/GO Extensions

        #region Renderer Extensions

        public static bool IsShadowCasting(this Renderer renderer)
        {
            return renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        public static void SetValue(this MaterialPropertyBlock mpb, int nameID, object value)
        {
            if (value == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Given value is null! Can not apply override!");
                return;
            }
            if (value is Vector4 vector4)
                mpb.SetVector(nameID, vector4);
            else if (value is Vector3 vector3)
                mpb.SetVector(nameID, vector3);
            else if (value is Vector2 vector2)
                mpb.SetVector(nameID, vector2);
            else if (value is float f)
                mpb.SetFloat(nameID, f);
            else if (value is int i)
                mpb.SetInt(nameID, i);
            else if (value is Color c)
                mpb.SetColor(nameID, c);
            else if (value is GraphicsBuffer gBuffer)
                mpb.SetBuffer(nameID, gBuffer);
            else if (value is ComputeBuffer cBuffer)
                mpb.SetBuffer(nameID, cBuffer);
            else if (value is Texture texture)
                mpb.SetTexture(nameID, texture);
            else
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not set value to MaterialPropertyBlock! Type undefined: " + value.GetType());
                return;
            }
        }

        #endregion Renderer Extensions

        #region DateTime 

        public static string ToDateString(this DateTime dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool TryParseDateTime(this string dateTimeString, out DateTime result)
        {
            return DateTime.TryParseExact(dateTimeString, "MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result);
        }

        #endregion DateTime Extensions

        #region Editor Methods

#if UNITY_EDITOR
        public static bool SaveAsAsset(this UnityEngine.Object asset, string folderPath, string fileName, bool renameIfFileExists = false, bool saveInPlayMode = false)
        {
            if (EditorApplication.isUpdating)
                return false;
            if (!saveInPlayMode && Application.isPlaying)
                return false;

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (renameIfFileExists)
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                int count = 1;
                UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath + fileName);
                while (existingAsset != null)
                {
                    fileName = string.Format("{0}({1})", fileNameOnly, count++) + extension;
                    existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath + fileName);
                }
            }

            if (fileName.EndsWith(".prefab"))
            {
                GameObject go = (GameObject)asset;
                go.hideFlags = HideFlags.None;
                PrefabUtility.SaveAsPrefabAsset(go, folderPath + fileName);
            }
            else
                AssetDatabase.CreateAsset(asset, folderPath + fileName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        public static void RemoveSubAssets(this UnityEngine.Object baseAsset)
        {
            if (!Application.isPlaying)
            {
                string assetPath = AssetDatabase.GetAssetPath(baseAsset);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool requireImport = false;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset != baseAsset)
                    {
                        UnityEngine.Object.DestroyImmediate(asset, true);
                        requireImport = true;
                    }
                }
                if (requireImport)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        public static void AddObjectToAsset(this UnityEngine.Object baseAsset, UnityEngine.Object objectToAdd)
        {
            if (!Application.isPlaying)
            {
                string assetPath = AssetDatabase.GetAssetPath(baseAsset);
                AssetDatabase.AddObjectToAsset(objectToAdd, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        public static string GetAssetFolderPath(this UnityEngine.Object asset)
        {
            if (asset == null)
                return null;
            return GetFolderPath(AssetDatabase.GetAssetPath(asset));
        }

        public static string GetFolderPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
            string folderPath = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            if (!folderPath.EndsWith("/"))
                folderPath += "/";
            if (folderPath.StartsWith(Application.dataPath))
                folderPath = "Assets/" + folderPath.Substring(Application.dataPath.Length + 1);
            return folderPath;
        }

        public static void ReimportFilesInFolder(string folderPath, string searchPattern)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return;
                string[] files = Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string filePath = file.Replace("\\", "/");
#if GPUIPRO_DEVMODE
                    Debug.Log(GPUIConstants.LOG_PREFIX + "Reimporting file at path: " + filePath);
#endif
                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                }
            } 
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void ClearGameObjectSelectionsWithComponent(Type componentType)
        {
            UnityEngine.Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            List<UnityEngine.Object> selections = new();
            bool modified = false;
            for (int i = 0; i < selections.Count; i++)
            {
                if (selections[i] is GameObject go && go.GetComponent(componentType))
                    continue;
                else
                    selections.Add(selections[i]);
            }

            if (modified)
                Selection.objects = selections.ToArray();
        }
#endif

        #endregion Editor Methods

        #region Math Methods

        public static Vector3 Round(this Vector3 vector3, int decimals)
        {
            vector3.x = (float)Math.Round(vector3.x, decimals);
            vector3.y = (float)Math.Round(vector3.y, decimals);
            vector3.z = (float)Math.Round(vector3.z, decimals);
            return vector3;
        }

        public static int GenerateHash(params int[] numbers)
        {
            HashCode hashCode = new HashCode();
            for (int i = 0; i < numbers.Length; i++)
                hashCode.Add(numbers[i]);
            return hashCode.ToHashCode();
        }

        public static void SetPosition(this ref Matrix4x4 matrix, Vector3 position)
        {
            matrix.m03 = position.x;
            matrix.m13 = position.y;
            matrix.m23 = position.z;
        }

        public static void SetScale(this ref Matrix4x4 matrix, Vector3 scale)
        {
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(Vector3.Scale(scale, matrix.lossyScale.Reciprocal()));
            matrix *= scaleMatrix;
        }

        public static bool EqualsMatrix4x4(Matrix4x4 a, Matrix4x4 b)
        {
            return a.m00 == b.m00 && a.m01 == b.m01 && a.m02 == b.m02 && a.m03 == b.m03 &&
                   a.m10 == b.m10 && a.m11 == b.m11 && a.m12 == b.m12 && a.m13 == b.m13 &&
                   a.m20 == b.m20 && a.m21 == b.m21 && a.m22 == b.m22 && a.m23 == b.m23 &&
                   a.m30 == b.m30 && a.m31 == b.m31 && a.m32 == b.m32 && a.m33 == b.m33;
        }

        public static void MousePointsToPlanes(Camera cam, Vector2 p1, Vector2 p2, float farPlane, Plane[] planes)
        {
            Vector3 camPos = cam.transform.position;
            Vector2 min = Vector2.Min(p1, p2);
            Vector2 max = Vector2.Max(p1, p2);

            min.y = cam.pixelHeight - min.y;
            max.y = cam.pixelHeight - max.y;

            Ray bottomLeft = cam.ScreenPointToRay(min);
            Ray topLeft = cam.ScreenPointToRay(new Vector2(min.x, max.y));
            Ray bottomRight = cam.ScreenPointToRay(new Vector2(max.x, min.y));
            Ray topRight = cam.ScreenPointToRay(max);

            planes[0].Set3Points(camPos, bottomLeft.origin + bottomLeft.direction, topLeft.origin + topLeft.direction);
            planes[1].Set3Points(camPos, topRight.origin + topRight.direction, bottomRight.origin + bottomRight.direction);
            planes[2].Set3Points(camPos, topLeft.origin + topLeft.direction, topRight.origin + topRight.direction);
            planes[3].Set3Points(camPos, bottomRight.origin + bottomRight.direction, bottomLeft.origin + bottomLeft.direction);
            planes[4].Set3Points(topRight.origin - topRight.direction + camPos, bottomRight.origin - bottomRight.direction + camPos, bottomLeft.origin - bottomLeft.direction + camPos);
            planes[5].Set3Points(topLeft.origin + topLeft.direction * farPlane, bottomLeft.origin + bottomLeft.direction * farPlane, topRight.origin + topRight.direction * farPlane);
        }

        public static bool TestPlanesAABBComplete(Plane[] planes, Bounds bounds)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            foreach (Plane plane in planes)
            {
                if (!plane.GetSide(boundsMin) || !plane.GetSide(boundsMax))
                    return false;
            }
            return true;
        }

        public static string FormatNumberWithSuffix(this long num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.0M");
            if (num >= 10000)
                return (num / 1000D).ToString("0.0k");

            return num.ToString("#,0");
        }

        public static string FormatNumberWithSuffix(this int num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.0M");
            if (num >= 10000)
                return (num / 1000D).ToString("0.0k");

            return num.ToString("#,0");
        }

        public static string FormatNumberWithSuffix(this uint num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.0M");
            if (num >= 10000)
                return (num / 1000D).ToString("0.0k");

            return num.ToString("#,0");
        }

        public static bool Approximately(this Color color, Color other, bool includeAlpha = false, float errorMargin = 1f / 500f)
        {
            return math.abs(color.r - other.r) < errorMargin && math.abs(color.g - other.g) < errorMargin && math.abs(color.b - other.b) < errorMargin && (!includeAlpha || math.abs(color.a - other.a) < errorMargin);
        }

        public static bool Approximately(this Quaternion rotation, Quaternion other, float errorMargin = 1f / 500f)
        {
            return 1f - Mathf.Abs(Quaternion.Dot(rotation, other)) < errorMargin;
        }

        public static bool Approximately(this Vector3 position, Vector3 other, float errorMargin = 1f / 500f)
        {
            return Vector3.Distance(position, other) < errorMargin;
        }

        public static float BLerp(float c00, float c10, float c01, float c11, float u, float v)
        {
            return math.lerp(math.lerp(c00, c10, u), math.lerp(c01, c11, u), v);
        }

        public static int4 RoundToInt(float4 input)
        {
            return new int4(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), Mathf.RoundToInt(input.z), Mathf.RoundToInt(input.w));
        }

        public static float4 GetRGBAFromFloat(float v)
        {
            float4 kEncodeMul = new float4(1.0f, 255.0f, 65025.0f, 16581375.0f);
            float kEncodeBit = 1.0f / 255.0f;
            float4 enc = kEncodeMul * v;
            enc = math.frac(enc);
            enc -= enc.yzww * kEncodeBit;
            return enc;
        }

        public static Color32 GetColor32FromFloat(float v)
        {
            float4 decoded = GetRGBAFromFloat(v);
            return new Color32((byte)Mathf.RoundToInt(decoded.x * 255), (byte)Mathf.RoundToInt(decoded.y * 255), (byte)Mathf.RoundToInt(decoded.z * 255), (byte)Mathf.RoundToInt(decoded.w * 255));
        }

        public static float StoreRGBAInFloat(Vector4 rgba)
        {
            Vector4 kDecodeDot = new Vector4(1.0f, 1.0f / 255.0f, 1.0f / 65025.0f, 1.0f / 160581375.0f);
            return Vector4.Dot(rgba, kDecodeDot);
        }

        public static float StoreColor32InFloat(Color32 color32)
        {
            Vector4 kDecodeDot = new Vector4(1.0f, 1.0f / 255.0f, 1.0f / 65025.0f, 1.0f / 160581375.0f);
            return Vector4.Dot((Vector4)(Color)color32, kDecodeDot);
        }

        public static Vector3 Reciprocal(this Vector3 vector3)
        {
            return new Vector3(vector3.x == 0f ? 0f : 1f / vector3.x, vector3.y == 0f ? 0f : 1f / vector3.y, vector3.z == 0f ? 0f : 1f / vector3.z);
        }

        public static Bounds GetMatrixAppliedBounds(this Bounds bounds, Matrix4x4 matrix)
        {
            bounds.size = Vector3.Scale(bounds.size, matrix.lossyScale);
            bounds = bounds.GetRotationAppliedBounds(matrix.rotation);
            bounds.center += matrix.GetPosition();
            return bounds;
        }

        public static Bounds GetRotationAppliedBounds(this Bounds bounds, Quaternion rotation)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            bounds.size = Vector3.zero;
            bounds.Encapsulate(rotation * new Vector3(boundsMin.x, boundsMax.y, boundsMin.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMin.x, boundsMax.y, boundsMax.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMax.x, boundsMax.y, boundsMax.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMax.x, boundsMax.y, boundsMin.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMax.x, boundsMin.y, boundsMin.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMax.x, boundsMin.y, boundsMax.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMin.x, boundsMin.y, boundsMax.z));
            bounds.Encapsulate(rotation * new Vector3(boundsMin.x, boundsMin.y, boundsMin.z));
            return bounds;
        }

        /// <summary>
        /// Rotates a given position around a specified center point by a certain angle and axis.
        /// </summary>
        /// <param name="position">The point to be rotated.</param>
        /// <param name="center">The pivot point to rotate around.</param>
        /// <param name="rotation">Rotation.</param>
        /// <returns>The new position of the point after rotation.</returns>
        public static Vector3 RotatePositionAround(Vector3 position, Vector3 center, Quaternion rotation)
        {
            // Translate the position to the origin (relative to the center)
            Vector3 direction = position - center;

            // Rotate the direction vector
            Vector3 rotatedDirection = rotation * direction;

            // Translate the point back to its original space by adding the center
            Vector3 newPosition = center + rotatedDirection;

            // Return the new position
            return newPosition;
        }

        public static Vector3 RotateSize(Vector3 size, Quaternion rotation)
        {
            // Rotate each dimension of the size vector individually
            Vector3 rotatedX = rotation * Vector3.right * size.x;
            Vector3 rotatedY = rotation * Vector3.up * size.y;
            Vector3 rotatedZ = rotation * Vector3.forward * size.z;

            // Sum the absolute values of the rotated axes to get the rotated size
            return new Vector3(
                Mathf.Abs(rotatedX.x) + Mathf.Abs(rotatedY.x) + Mathf.Abs(rotatedZ.x),
                Mathf.Abs(rotatedX.y) + Mathf.Abs(rotatedY.y) + Mathf.Abs(rotatedZ.y),
                Mathf.Abs(rotatedX.z) + Mathf.Abs(rotatedY.z) + Mathf.Abs(rotatedZ.z)
            );
        }

        private static Quaternion FlipQuaternion(Quaternion q)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
            return q;
        }

        #endregion Math Methods

        #region String Extensions

        public static string CamelToTitleCase(string camelCaseText)
        {
            string result = "";
            while (camelCaseText.StartsWith("_"))
            {
                camelCaseText = camelCaseText.Substring(1);
            }
            if (camelCaseText.StartsWith("editor_"))
            {
                camelCaseText = camelCaseText.Substring(7);
            }
            if (camelCaseText.StartsWith("gpui"))
            {
                result += "GPUI ";
                camelCaseText = camelCaseText.Substring(4);
            }
            camelCaseText = camelCaseText.Substring(0, 1).ToUpper() + camelCaseText.Substring(1);
            return result += Regex.Replace(Regex.Replace(camelCaseText, @"([A-Z])([a-z])", @" $1$2"), @"([a-z])([A-Z])", @"$1 $2").Trim();
        }

        public static bool CompareExtensionCode(string c1, string c2)
        {
            if (string.IsNullOrEmpty(c1) && string.IsNullOrEmpty(c2))
                return true;
            return string.Equals(c1, c2);
        }

        public static string ConvertToGPUIShaderName(string originalShaderName, string extensionCode, string shaderNamePrefix = null)
        {
            string defaultPrefix = GPUIConstants.GetShaderNamePrefix(extensionCode);
            if (string.IsNullOrEmpty(shaderNamePrefix))
                shaderNamePrefix = defaultPrefix;
            bool isHidden = originalShaderName.StartsWith("Hidden/");
            if (isHidden)
                originalShaderName = originalShaderName.Substring(7);
            if (originalShaderName.StartsWith(defaultPrefix))
                originalShaderName = originalShaderName.Substring(defaultPrefix.Length, originalShaderName.Length - defaultPrefix.Length);
            string newShaderName = shaderNamePrefix + originalShaderName;
            if (isHidden)
                newShaderName = "Hidden/" + newShaderName;

            return newShaderName;
        }

        public static string RemoveSpacesAndLimitSize(this string input, int maxSize)
        {
            // Remove all empty spaces from the input string
            string result = input.Replace(" ", "");

            // Limit the size of the string if it exceeds maxSize
            if (result.Length > maxSize)
            {
                result = result.Substring(0, maxSize);
            }

            return result;
        }

        public static string Matrix4x4ToString(Matrix4x4 matrix4x4)
        {
            return Regex.Replace(matrix4x4.ToString(), @"[\r\n\t]+", ";");
        }

        public static bool TryParseMatrix4x4(string matrixStr, out Matrix4x4 matrix4x4)
        {
            matrix4x4 = new Matrix4x4();
            if (string.IsNullOrEmpty(matrixStr))
                return false;
            string[] floatStrArray = matrixStr.Split(';');
            if (floatStrArray.Length < 16)
                return false;
            for (int i = 0; i < 16; i++)
            {
                if (float.TryParse(floatStrArray[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
                    matrix4x4[i / 4, i % 4] = val;
                else
                    return false;
            }
            return true;
        }

        public static Matrix4x4 Matrix4x4FromString(string matrixStr)
        {
            Matrix4x4 matrix4x4 = new Matrix4x4();
            string[] floatStrArray = matrixStr.Split(';');
            for (int i = 0; i < 16; i++)
            {
                matrix4x4[i / 4, i % 4] = float.Parse(floatStrArray[i], System.Globalization.CultureInfo.InvariantCulture);
            }
            return matrix4x4;
        }

        public static string ReadTextFileAtPath(string filePath)
        {
            string result = null;
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                    result = reader.ReadToEnd();
            }
            return result;
        }

        public static string GetRelativePathForShader(string shaderPathString, string includeFilePathString)
        {
            if (string.IsNullOrEmpty(shaderPathString) || string.IsNullOrEmpty(includeFilePathString))
                return string.Empty;
            if (shaderPathString.StartsWith("Packages/"))
                shaderPathString = GPUIConstants.GetGeneratedShaderPath();
            string relativePath = Path.GetRelativePath(Path.GetDirectoryName(shaderPathString), includeFilePathString).Replace("\\", "/");
            if (!relativePath.StartsWith("."))
                relativePath = "./" + relativePath;
            return relativePath;
        }

        public static string UintToBinaryString(uint value, int length = 32)
        {
            if (length < 1 || length > 32)
                length = 32;

            string binaryString = Convert.ToString(value, 2).PadLeft(32, '0'); // Ensure 32-bit representation
            return binaryString.Substring(32 - length, length); // Extract the last 'length' bits
        }

        #endregion String Extensions

        #region GraphicsBuffer Extensions

        public static void SetData(this GraphicsBuffer targetBuffer, GraphicsBuffer sourceBuffer, int sourceStartIndex, int targetStartIndex, int count)
        {
            if (sourceBuffer == null || targetBuffer == null) return;
            if (count <= 0 || targetBuffer.count < targetStartIndex + count || sourceBuffer.count < sourceStartIndex + count) return;
            //Debug.Log(GPUIConstants.LOG_PREFIX + "Setting data sourceStartIndex: " + sourceStartIndex + " targetStartIndex: " + targetStartIndex + " count: " + count);
            ComputeShader cs = GPUIConstants.CS_GraphicsBufferUtility;
            int kernelIndex = 0;
            switch (targetBuffer.stride)
            {
                case 64: // Matrix4x4
                    cs.DisableKeyword(GPUIConstants.Kw_GPUI_TRANSFORM_DATA);
                    cs.DisableKeyword(GPUIConstants.Kw_GPUI_FLOAT4_BUFFER);
                    break;
                case 40: // GPUITransformData
                    cs.EnableKeyword(GPUIConstants.Kw_GPUI_TRANSFORM_DATA);
                    cs.DisableKeyword(GPUIConstants.Kw_GPUI_FLOAT4_BUFFER);
                    break;
                case 16: // float4
                    cs.DisableKeyword(GPUIConstants.Kw_GPUI_TRANSFORM_DATA);
                    cs.EnableKeyword(GPUIConstants.Kw_GPUI_FLOAT4_BUFFER);
                    break;
                default:
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Unknown stride size for buffer: " + targetBuffer.stride);
                    return;
            }

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_sourceBuffer, sourceBuffer);
            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_targetBuffer, targetBuffer);
            cs.SetInt(GPUIConstants.PROP_sourceStartIndex, sourceStartIndex);
            cs.SetInt(GPUIConstants.PROP_targetStartIndex, targetStartIndex);
            cs.SetInt(GPUIConstants.PROP_count, count);
            cs.DispatchX(kernelIndex, count);
        }

        public static void SetAllDataTo(this GraphicsBuffer buffer, Matrix4x4 value)
        {
            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            cs.SetBuffer(8, GPUIConstants.PROP_gpuiTransformBuffer, buffer);
            cs.SetInt(GPUIConstants.PROP_bufferSize, buffer.count);
            cs.SetMatrix(GPUIConstants.PROP_matrix44, value);
            cs.DispatchX(8, buffer.count);
        }

        public static void ClearBufferData(this GraphicsBuffer buffer)
        {
            if (buffer.stride % 4 != 0) 
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Given buffer stride size is not divisible by 4. Stride: " + buffer.stride);
                return;
            }
            int startIndex = 0;
            int count = buffer.stride * buffer.count / 4;

            ComputeShader cs = GPUIConstants.CS_GraphicsBufferUtility;
            int kernelIndex = 1;
            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_destination, buffer);
            cs.SetInt(GPUIConstants.PROP_targetStartIndex, startIndex);
            cs.SetInt(GPUIConstants.PROP_count, count);
            cs.DispatchX(kernelIndex, count);
        }

        #endregion GraphicsBuffer Extensions

        #region Array/List Extensions

        public static T[] RemoveAtAndReturn<T>(this T[] array, int toRemove)
        {
            if (array == null || toRemove >= array.Length)
                return array;
            T[] result = new T[array.Length - 1];
            if (toRemove > 0)
                Array.Copy(array, 0, result, 0, toRemove);
            if (toRemove < array.Length - 1)
                Array.Copy(array, toRemove + 1, result, toRemove, array.Length - toRemove - 1);

            return result;
        }

        public static T[] AddAndReturn<T>(this T[] array, T toAdd)
        {
            T[] result = new T[array.Length + 1];
            Array.Copy(array, 0, result, 0, array.Length);
            result[array.Length] = toAdd;
            return result;
        }

        public static T[] MirrorAndFlatten<T>(this T[,] array2D)
        {
            T[] resultArray1D = new T[array2D.GetLength(0) * array2D.GetLength(1)];

            for (int y = 0; y < array2D.GetLength(0); y++)
            {
                for (int x = 0; x < array2D.GetLength(1); x++)
                {
                    resultArray1D[x + y * array2D.GetLength(0)] = array2D[y, x];
                }
            }

            return resultArray1D;
        }

        public static bool Contains<T>(this T[] array, T element)
        {
            if (array == null)
                return false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    if (element == null)
                        return true;
                    return false;
                }
                if (array[i].Equals(element))
                    return true;
            }
            return false;
        }

        public static void RemoveNullValues<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TValue : UnityEngine.Object // It has to be UnityEngine.Object otherwise null check will not work.
        {
            var keysToRemove = new List<TKey>();
            foreach (var kvp in dictionary)
                if (kvp.Value == null)
                    keysToRemove.Add(kvp.Key);

            // Remove keys outside of enumeration to avoid modifying collection
            foreach (var key in keysToRemove)
                dictionary.Remove(key);
        }

        #endregion Array/List Extensions

        #region NativeArray Extensions

        public static void ResizeNativeArray<T>(this ref NativeArray<T> array, int newSize, Allocator allocator) where T : struct
        {
            NativeArray<T> previousArray = array;
            array = new NativeArray<T>(newSize, allocator);
            if (previousArray.IsCreated)
            {
                int count = Math.Min(previousArray.Length, newSize);
                var arraySlice = new NativeSlice<T>(array, 0, count);
                var previousArraySlice = new NativeSlice<T>(previousArray, 0, count);
                arraySlice.CopyFrom(previousArraySlice);
                //for (int i = 0; i < count; i++)
                //    array[i] = previousArray[i];
                previousArray.Dispose();
            }
        }

        #endregion NativeArray Extensions

        #region Gizmo Extensions

        public static void GizmoDrawWireMesh(GPUIPrototype prototype, Matrix4x4 matrix, bool drawBounds = true)
        {
            if (GPUIRenderingSystem.IsActive && GPUIRenderingSystem.Instance.LODGroupDataProvider.TryGetData(prototype.GetKey(), out GPUILODGroupData lodGroupData))
            {
                GizmoDrawWireMesh(lodGroupData, matrix, drawBounds);
                return;
            }

            if (prototype.prototypeType == GPUIPrototypeType.Prefab)
            {
                GameObject go = prototype.prefabObject;

                if (drawBounds)
                    GizmoDrawWireMesh(go.GetBounds(), matrix);
                else
                {
                    if (go.TryGetComponent(out LODGroup lodGroup))
                    {
                        LOD[] lods = lodGroup.GetLODs();
                        if (lods.Length > 0)
                        {
                            foreach (Renderer renderer in lods[0].renderers)
                            {
                                if (renderer.TryGetComponent(out MeshFilter mf))
                                {
                                    matrix *= mf.transform.localToWorldMatrix * go.transform.localToWorldMatrix.inverse;
                                    for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
                                    {
                                        Gizmos.DrawWireMesh(mf.sharedMesh, i, matrix.GetPosition(), matrix.rotation, matrix.lossyScale);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
                        foreach (var mf in meshFilters)
                        {
                            matrix *= mf.transform.localToWorldMatrix * go.transform.localToWorldMatrix.inverse;
                            for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
                            {
                                Gizmos.DrawWireMesh(mf.sharedMesh, i, matrix.GetPosition(), matrix.rotation, matrix.lossyScale);
                            }
                        }
                    }
                }
            }
            else if (prototype.prototypeType == GPUIPrototypeType.LODGroupData)
                GizmoDrawWireMesh(prototype.gpuiLODGroupData, matrix, drawBounds);
        }

        public static void GizmoDrawWireMesh(GPUILODGroupData lodGroupData, Matrix4x4 matrix, bool drawBounds = true)
        {
            if (drawBounds)
                GizmoDrawWireMesh(lodGroupData.bounds, matrix);
            else
            {
                GPUILODData renderers = lodGroupData[0];
                for (int r = 0; r < renderers.Length; r++)
                {
                    GPUIRendererData renderer = renderers[r];
                    Matrix4x4 ltw = matrix * renderer.transformOffset;
                    for (int i = 0; i < renderer.rendererMesh.subMeshCount; i++)
                    {
                        Gizmos.DrawWireMesh(renderer.rendererMesh, i, ltw.GetPosition(), ltw.rotation, ltw.lossyScale);
                    }
                }
            }
        }

        public static void GizmoDrawWireMesh(Bounds bounds, Matrix4x4 matrix)
        {
            Gizmos.matrix = matrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        #endregion Gizmo Extensions

        #region Layer Extensions

        public static bool IsInLayer(int layerMask, int layer)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        #endregion Layer Extensions

        #region Compute Shader Extensions

        public static void SetBuffer<T>(this ComputeShader cs, int kernelIndex, int nameID, GPUIDataBuffer<T> gpuiDataBuffer) where T : struct
        {
            cs.SetBuffer(kernelIndex, nameID, gpuiDataBuffer.Buffer);
        }

        public static void DispatchX(this ComputeShader cs, int kernelIndex, int size)
        {
            if (GPUIConstants.CS_THREAD_COUNT == 0)
                GPUIRuntimeSettings.Instance.DetermineOperationMode();
            cs.Dispatch(kernelIndex, Mathf.CeilToInt(size / GPUIConstants.CS_THREAD_COUNT), 1, 1);
        }

        public static void DispatchXHeavy(this ComputeShader cs, int kernelIndex, int size)
        {
            if (GPUIConstants.CS_THREAD_COUNT_HEAVY == 0)
                GPUIRuntimeSettings.Instance.DetermineOperationMode();
            cs.Dispatch(kernelIndex, Mathf.CeilToInt(size / GPUIConstants.CS_THREAD_COUNT_HEAVY), 1, 1);
        }

        public static void DispatchXY(this ComputeShader cs, int kernelIndex, int sizeX, int sizeY)
        {
            if (GPUIConstants.CS_THREAD_COUNT == 0)
                GPUIRuntimeSettings.Instance.DetermineOperationMode();
            cs.Dispatch(kernelIndex, Mathf.CeilToInt(sizeX / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sizeY / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

        public static void DispatchXZ(this ComputeShader cs, int kernelIndex, int sizeX, int sizeZ)
        {
            if (GPUIConstants.CS_THREAD_COUNT == 0)
                GPUIRuntimeSettings.Instance.DetermineOperationMode();
            cs.Dispatch(kernelIndex, Mathf.CeilToInt(sizeX / GPUIConstants.CS_THREAD_COUNT_2D), 1, Mathf.CeilToInt(sizeZ / GPUIConstants.CS_THREAD_COUNT_2D));
        }

        public static void DispatchXYZ(this ComputeShader cs, int kernelIndex, int sizeX, int sizeY, int sizeZ)
        {
            if (GPUIConstants.CS_THREAD_COUNT == 0)
                GPUIRuntimeSettings.Instance.DetermineOperationMode();
            cs.Dispatch(kernelIndex, Mathf.CeilToInt(sizeX / GPUIConstants.CS_THREAD_COUNT_3D), Mathf.CeilToInt(sizeY / GPUIConstants.CS_THREAD_COUNT_3D), Mathf.CeilToInt(sizeZ / GPUIConstants.CS_THREAD_COUNT_3D));
        }

#if UNITY_EDITOR
        public static bool ComputeShaderHasCompilerErrors(ComputeShader computeShader)
        {
            if (computeShader == null) return false;
            ShaderMessage[] shaderMessages = ShaderUtil.GetComputeShaderMessages(computeShader);
            foreach (ShaderMessage shaderMessage in shaderMessages)
            {
                if (shaderMessage.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    return true;
            }
            return false;
        }
#endif

        #endregion Compute Shader Extensions

        #region Shader Extensions

        public static string[] GetPropertyNames(this Shader shader, List<ShaderPropertyType> ignoreTypes = null)
        {
            int propertyCount = shader.GetPropertyCount();
            List<string> result = new List<string>();
            for (int i = 0; i < propertyCount; i++)
            {
                if (ignoreTypes == null || !ignoreTypes.Contains(shader.GetPropertyType(i)))
                    result.Add(shader.GetPropertyName(i));
            }
            return result.ToArray();
        }

        public static string[] GetPropertyNamesForType(this Shader shader, ShaderPropertyType propertyType)
        {
            int propertyCount = shader.GetPropertyCount();
            List<string> result = new List<string>();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) == propertyType)
                    result.Add(shader.GetPropertyName(i));
            }
            return result.ToArray();
        }

        public static Material CopyWithShader(this Material originalMaterial, Shader instancedShader)
        {
            Material replacementMat = new Material(instancedShader);
            replacementMat.CopyPropertiesFromMaterial(originalMaterial);
            string name = originalMaterial.name;
            if (!name.EndsWith(GPUIShaderBindings.GPUI_REPLACEMENT_MATERIAL_NAME_SUFFIX))
                name += GPUIShaderBindings.GPUI_REPLACEMENT_MATERIAL_NAME_SUFFIX;
            replacementMat.name = name;
            replacementMat.hideFlags = HideFlags.HideAndDontSave;
            return replacementMat;
        }

        #endregion Shader Extensions

        #region Graphics Extensions

        public static void RenderMeshIndirect(in RenderParams rparams, Mesh mesh, GPUIDataBuffer<GraphicsBuffer.IndirectDrawIndexedArgs> commandBuffer, int commandCount = 1, int startCommand = 0)
        {
            Graphics.RenderMeshIndirect(rparams, mesh, commandBuffer.Buffer, commandCount, startCommand);
            //Graphics.DrawMeshInstancedIndirect(mesh, 0, rparams.material, rparams.worldBounds, commandBuffer.Buffer, startCommand * 4 * 5, rparams.matProps, rparams.shadowCastingMode, true, rparams.layer, rparams.camera, rparams.lightProbeUsage);
        }

        public static bool IsDepthTextureAvailable(this Camera camera)
        {
            return camera.depthTextureMode.HasFlag(DepthTextureMode.Depth) || camera.actualRenderingPath == RenderingPath.DeferredShading;
        } 

        #endregion Graphics Extensions

        #region Mesh Utility Methods

        public static Mesh GenerateQuadMesh(float width, float height, Rect? uvRect = null, bool centerPivotAtBottom = false, float pivotOffsetX = 0f, float pivotOffsetY = 0f, bool setVertexColors = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = "QuadMesh_GPUI";

            mesh.vertices = new Vector3[]
            {
                new Vector3(centerPivotAtBottom ? -width/2-pivotOffsetX : -pivotOffsetX, -pivotOffsetY, 0), // bottom left
                new Vector3(centerPivotAtBottom ? -width/2-pivotOffsetX : -pivotOffsetX, height-pivotOffsetY, 0), // top left
                new Vector3(centerPivotAtBottom ? width/2-pivotOffsetX : width-pivotOffsetX, height-pivotOffsetY, 0), // top right
                new Vector3(centerPivotAtBottom ? width/2-pivotOffsetX : width-pivotOffsetX, -pivotOffsetY, 0) // bottom right
            };


            if (uvRect != null)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(uvRect.Value.x, uvRect.Value.y),
                    new Vector2(uvRect.Value.x, uvRect.Value.y + uvRect.Value.height),
                    new Vector2(uvRect.Value.x + uvRect.Value.width, uvRect.Value.y + uvRect.Value.height),
                    new Vector2(uvRect.Value.x + uvRect.Value.width, uvRect.Value.y)
                };
            }
            else
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                };
            }

            mesh.triangles = new int[] { 0, 1, 3, 1, 2, 3 };

            Vector3 planeNormal = new Vector3(0, 0, -1);
            Vector4 planeTangent = new Vector4(1, 0, 0, -1);

            mesh.normals = new Vector3[4]
            {
                planeNormal,
                planeNormal,
                planeNormal,
                planeNormal
            };

            mesh.tangents = new Vector4[4]
            {
                planeTangent,
                planeTangent,
                planeTangent,
                planeTangent
            };

            if (setVertexColors)
            {
                Color[] colors = new Color[mesh.vertices.Length];

                for (int i = 0; i < mesh.vertices.Length; i++)
                    colors[i] = Color.Lerp(Color.clear, Color.white, mesh.vertices[i].y);

                mesh.colors = colors;
            }

            return mesh;
        }

        #endregion Mesh Utility Methods

        #region Resource Management

        public static Shader FindShader(string shaderName)
        {
            Shader result = Shader.Find(shaderName);
#if GPUI_ADDRESSABLES
            if (GPUIRuntimeSettings.Instance.loadShadersFromAddressables && result == null)
            {
                try
                {
                    var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Shader>(shaderName);
                    result = handle.WaitForCompletion();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
#endif
            return result;
        }

        public static T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            T result = Resources.Load<T>(path);
#if GPUI_ADDRESSABLES
            if (GPUIRuntimeSettings.Instance.loadResourcesFromAddressables && result == null)
            {
                try
                {
                    var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(path);
                    result = handle.WaitForCompletion();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
#endif
            return result;
        }

        #endregion Resource Management

        #region Reflection
        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
        {
            return (T)methodInfo.CreateDelegate(typeof(T));
        }

        private static readonly FieldInfo _listItemsField = typeof(List<>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);

        public static T[] GetListInternalArray<T>(List<T> list)
        {
            return (T[])_listItemsField.GetValue(list);
        }
        #endregion Reflection

        #region Light Probes
        public static unsafe void CalculateInterpolatedLightAndOcclusionProbes(ref GraphicsBuffer perInstanceLightProbesBuffer, void* p_matrices, int managedBufferStartIndex, int graphicsBufferStartIndex, int count, int rsgBufferSize,
            List<Vector3> positions, List<SphericalHarmonicsL2> sphericalHarmonics, List<Vector4> occlusionProbes, GraphicsBuffer sphericalHarmonicsBuffer, GraphicsBuffer occlusionProbesBuffer, Vector3 positionOffset)
        {
            Profiler.BeginSample("GPUIUtility.CalculateInterpolatedLightAndOcclusionProbes");
            positions.Clear();
            if (positions.Capacity < count)
                positions.Capacity = count;
            sphericalHarmonics.Clear();
            if (sphericalHarmonics.Capacity < count)
                sphericalHarmonics.Capacity = count;
            occlusionProbes.Clear();
            if (occlusionProbes.Capacity < count)
                occlusionProbes.Capacity = count;

            Vector3 pos = Vector3.zero;
            if (positionOffset == Vector3.zero)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector4 m = UnsafeUtility.ReadArrayElementWithStride<Vector4>(p_matrices, (i + managedBufferStartIndex) * 4 + 3, 16);
                    pos.x = m.x;
                    pos.y = m.y;
                    pos.z = m.z;
                    positions.Add(pos);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Matrix4x4 m = UnsafeUtility.ReadArrayElementWithStride<Matrix4x4>(p_matrices, (i + managedBufferStartIndex), 64);
                    pos.x = m.m03 + positionOffset.x * (float)Math.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
                    pos.y = m.m13 + positionOffset.y * (float)Math.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m22 * m.m22);
                    pos.z = m.m23 + positionOffset.z * (float)Math.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
                    positions.Add(pos);
                }
            }

            LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, sphericalHarmonics, occlusionProbes);

            int bufferSize = rsgBufferSize * 8;
            if (perInstanceLightProbesBuffer != null)
            {
                if (bufferSize != perInstanceLightProbesBuffer.count)
                {
                    var previousBuffer = perInstanceLightProbesBuffer;
                    perInstanceLightProbesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4 * 4);
                    perInstanceLightProbesBuffer.SetData(previousBuffer, 0, 0, Mathf.Min(bufferSize, previousBuffer.count));
                    previousBuffer.Dispose();
                }
            }
            else
                perInstanceLightProbesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 4 * 4);

            sphericalHarmonicsBuffer.SetData(sphericalHarmonics, 0, 0, count);
            occlusionProbesBuffer.SetData(occlusionProbes, 0, 0, count);

            ComputeShader cs = GPUIConstants.CS_LightProbeUtility;
            int kernelIndex = 0;
            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_gpuiPerInstanceLightProbesBuffer, perInstanceLightProbesBuffer);
            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_sphericalHarmonicsBuffer, sphericalHarmonicsBuffer);
            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_occlusionProbesBuffer, occlusionProbesBuffer);
            cs.SetInt(GPUIConstants.PROP_count, count);
            cs.SetInt(GPUIConstants.PROP_startIndex, graphicsBufferStartIndex);
            cs.DispatchX(kernelIndex, count);

            Profiler.EndSample();
        }
        #endregion Light Probes
    }
}