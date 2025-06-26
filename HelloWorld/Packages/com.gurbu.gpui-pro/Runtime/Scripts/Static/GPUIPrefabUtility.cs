// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GPUInstancerPro
{
    public static class GPUIPrefabUtility
    {
        public static T AddComponentToPrefab<T>(GameObject prefabObject) where T : Component
        {
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabObject);

            while (prefabType == PrefabAssetType.Variant)
            {
                GameObject correspondingPrefabOfVariant = GetCorrespondingPrefabOfVariant(prefabObject);
                prefabType = PrefabUtility.GetPrefabAssetType(correspondingPrefabOfVariant);
                if (prefabType == PrefabAssetType.Model)
                {
                    prefabType = PrefabAssetType.Regular; // If the parent is a Model treat it as Regular.
                    break;
                }
                prefabObject = correspondingPrefabOfVariant;
            }

            if (prefabType == PrefabAssetType.Regular)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
                if (string.IsNullOrEmpty(prefabPath))
                    return null;
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                prefabContents.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                return prefabObject.GetComponent<T>();
            }

            return prefabObject.AddComponent<T>();
        }

        public static T AddOrGetComponentToPrefab<T>(GameObject prefabObject) where T : Component
        {
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabObject);

            while (prefabType == PrefabAssetType.Variant)
            {
                GameObject correspondingPrefabOfVariant = GetCorrespondingPrefabOfVariant(prefabObject);
                prefabType = PrefabUtility.GetPrefabAssetType(correspondingPrefabOfVariant);
                if (prefabType == PrefabAssetType.Model)
                {
                    prefabType = PrefabAssetType.Regular; // If the parent is a Model treat it as Regular.
                    break;
                }
                prefabObject = correspondingPrefabOfVariant;
            }

            if (prefabType == PrefabAssetType.Regular)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
                if (string.IsNullOrEmpty(prefabPath))
                    return null;
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                prefabContents.AddOrGetComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                return prefabObject.GetComponent<T>();
            }

            return prefabObject.AddOrGetComponent<T>();
        }

        public static void SavePrefabAsset(GameObject prefabObject)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
            if (string.IsNullOrEmpty(prefabPath))
                return;
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        public static void RemoveComponentFromPrefab<T>(GameObject prefabObject) where T : Component
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
            if (string.IsNullOrEmpty(prefabPath))
                return;
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            T component = prefabContents.GetComponent<T>();
            if (component)
            {
                GameObject.DestroyImmediate(component, true);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        public static GameObject LoadPrefabContents(GameObject prefabObject)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
            if (string.IsNullOrEmpty(prefabPath))
                return null;
            return PrefabUtility.LoadPrefabContents(prefabPath);
        }

        public static void UnloadPrefabContents(GameObject prefabObject, GameObject prefabContents, bool saveChanges = true)
        {
            if (!prefabContents)
                return;
            if (saveChanges)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
                if (string.IsNullOrEmpty(prefabPath))
                    return;
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            }
            PrefabUtility.UnloadPrefabContents(prefabContents);
            if (prefabContents)
            {
                Debug.Log(GPUIConstants.LOG_PREFIX + "Destroying prefab contents...");
                GameObject.DestroyImmediate(prefabContents);
            }
        }

        public static GameObject GetCorrespondingPrefabOfVariant(GameObject variant)
        {
            GameObject result = variant;
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(result);
            if (prefabType == PrefabAssetType.Variant)
            {
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(result))
                    result = GetOutermostPrefabAssetRoot(result);

                prefabType = PrefabUtility.GetPrefabAssetType(result);
                if (prefabType == PrefabAssetType.Variant)
                    result = GetOutermostPrefabAssetRoot(result);
            }
            return result;
        }

        public static GameObject GetOutermostPrefabAssetRoot(GameObject prefabInstance)
        {
            GameObject result = prefabInstance;
            GameObject newPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(result);
            if (newPrefabObject != null)
            {
                while (newPrefabObject.transform.parent != null)
                    newPrefabObject = newPrefabObject.transform.parent.gameObject;
                result = newPrefabObject;
            }
            return result;
        }

        public static List<GameObject> GetCorrespondingPrefabAssetsOfGameObjects(GameObject[] gameObjects)
        {
            List<GameObject> result = new List<GameObject>();
            PrefabAssetType prefabType;
            GameObject prefabRoot;
            foreach (GameObject go in gameObjects)
            {
                prefabRoot = null;
                if (go != PrefabUtility.GetOutermostPrefabInstanceRoot(go))
                    continue;
                prefabType = PrefabUtility.GetPrefabAssetType(go);
                if (prefabType == PrefabAssetType.Regular)
                    prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(go);
                else if (prefabType == PrefabAssetType.Variant)
                    prefabRoot = GetCorrespondingPrefabOfVariant(go);

                if (prefabRoot != null)
                    result.Add(prefabRoot);
            }

            return result;
        }

        public static bool IsPrefabAsset(UnityEngine.Object asset, out GameObject prefabObject, bool acceptModelPrefab, string warningTextCode = null, Func<string, bool, bool> DisplayDialog = null)
        {
            prefabObject = null;
            if (asset == null)
                return false;

            if (!(asset is GameObject))
            {
                if (!string.IsNullOrEmpty(warningTextCode) && DisplayDialog != null)
                    DisplayDialog.Invoke(warningTextCode, false);
                return false;
            }

            prefabObject = asset as GameObject;
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabObject);

            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant || (acceptModelPrefab && prefabType == PrefabAssetType.Model))
            {
                GameObject newPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(prefabObject);
                if (newPrefabObject != null && PrefabUtility.GetPrefabInstanceStatus(prefabObject) == PrefabInstanceStatus.Connected)
                {
                    while (newPrefabObject.transform.parent != null)
                        newPrefabObject = newPrefabObject.transform.parent.gameObject;
                    prefabObject = newPrefabObject;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(warningTextCode) && DisplayDialog != null)
                    DisplayDialog(warningTextCode, false);
                prefabObject = null;
                return false;
            }
            return true;
        }

        public static GameObject[] FindAllInstancesOfPrefab(GameObject prefabObject, bool includeInactive = true)
        {
            GameObject[] prefabInstances = PrefabUtility.FindAllInstancesOfPrefab(prefabObject);
            if (!includeInactive && prefabInstances.Length > 0)
            {
                List<GameObject> instances = new List<GameObject>();
                for (int i = 0; i < prefabInstances.Length; i++)
                {
                    if (prefabInstances[i].activeInHierarchy)
                        instances.Add(prefabInstances[i]);
                }
                prefabInstances = instances.ToArray();
            }
            return prefabInstances;
        }

        public static void MergeAllPrefabInstances(GameObject prefabObject)
        {
            GameObject[] prefabInstances = FindAllInstancesOfPrefab(prefabObject);
            foreach (GameObject prefabInstance in prefabInstances)
            {
                //Debug.Log(GPUIConstants.LOG_PREFIX + "Merging: " + prefabInstance.name);
                PrefabUtility.MergePrefabInstance(prefabInstance);
            }
        }

        public static void RevertPropertyOnAllPrefabInstances<T>(GameObject prefabObject, string propertyName) where T : Component
        {
            GameObject[] prefabInstances = FindAllInstancesOfPrefab(prefabObject);
            foreach (GameObject prefabInstance in prefabInstances)
            {
                PrefabUtility.MergePrefabInstance(prefabInstance);
                if (!prefabInstance.TryGetComponent(out T component))
                    continue;
                var so = new SerializedObject(component);
                var sp = so.FindProperty(propertyName);
                if (sp == null)
                    continue;
                PrefabUtility.RevertPropertyOverride(sp, InteractionMode.AutomatedAction);
            }
        }

        public static GameObject InstantiatePrefab(GameObject prefabObject, Matrix4x4 matrix, Transform parent = null)
        {
            return InstantiatePrefab(prefabObject, matrix.GetPosition(), matrix.rotation, matrix.lossyScale, parent);
        }

        public static GameObject InstantiatePrefab(GameObject prefabObject, Vector3 position, Quaternion rotation, Vector3 localScale, Transform parent = null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabObject, parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = localScale;
            return instance;
        }

        public static bool IsPrefabInstancePropertyOverridden(SerializedProperty property)
        {
            if (property == null)
                return false;

            SerializedObject serializedObject = property.serializedObject;
            if (serializedObject == null)
                return false;

            UnityEngine.Object targetObject = serializedObject.targetObject;
            if (targetObject == null)
                return false;

            // Check if the object is part of a prefab instance
            if (PrefabUtility.GetPrefabInstanceStatus(targetObject) != PrefabInstanceStatus.Connected)
                return false;

            // Check if the property has an override
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(targetObject);
            if (modifications != null)
            {
                foreach (var modification in modifications)
                {
                    if (modification.propertyPath == property.propertyPath)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
#endif // UNITY_EDITOR