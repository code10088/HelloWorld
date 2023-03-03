#if UNITY_EDITOR

using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// AssetUtilities
    /// </summary>
    public struct AssetUtilities
    {
        /// <summary>
        /// Get the active directory if it exists or "Assets" returned
        /// </summary>
        public static string activeDirectory
        {
            get
            {
                var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

                if (!RuntimeUtilities.IsNullOrEmpty(objects))
                {
                    string path;

                    foreach(var obj in objects)
                    {
                        path = AssetDatabase.GetAssetPath(obj);
                        if (Directory.Exists(path) && path.StartsWith("Assets")) return path;
                    }

                    path = AssetDatabase.GetAssetPath(objects[0]);
                    if (path.StartsWith("Assets")) return path.Substring(0, path.LastIndexOf('/'));
                }

                return "Assets";
            }
        }


        /// <summary>
        /// Create asset file.
        /// </summary>
        /// <param name="assetPath">
        /// A path relative to "Assets", for example: "Assets/MyFolder/MyAsset.asset"
        /// </param>
        public static void CreateAsset<T>(
            T unityObject,
            string assetPath,
            bool autoRename = true,
            bool autoCreateDirectory = true)
            where T : Object
        {
            if (autoCreateDirectory)
            {
                int index = assetPath.LastIndexOf('/');
                if (index >= 0)
                {
                    Directory.CreateDirectory(assetPath.Substring(0, index));
                }
            }

            if (autoRename)
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            AssetDatabase.CreateAsset(unityObject, assetPath);
        }


        /// <summary>
        /// Create asset file.
        /// </summary>
        public static void CreateAsset<T>(T unityObject) where T : Object
        {
            CreateAsset(unityObject, activeDirectory + '/' + typeof(T).Name + ".asset", true, false);
        }


        /// <summary>
        /// Find asset of specific type.
        /// </summary>
        public static T FindAsset<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).FullName);

            if (!RuntimeUtilities.IsNullOrEmpty(guids))
            {
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            return null;
        }


        /// <summary>
        /// Find all isolated assets of specific type. An isolated asset is stored in a individual file.
        /// </summary>
        public static List<T> FindIsolatedAssets<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
            List<T> results = new List<T>();

            foreach(var guid in guids)
            {
                results.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return results;
        }


        /// <summary>
        /// Find all assets of specific type.
        /// </summary>
        public static List<T> FindAssets<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
            List<T> results = new List<T>();

            foreach (var guid in guids)
            {
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    if (asset is T) results.Add(asset as T);
                }
            }

            return results;
        }


        public static void AddPreloadedAsset(Object assetObject)
        {
            if (!assetObject) return;

            var assets = PlayerSettings.GetPreloadedAssets();

            bool added = false;

            if (assets != null)
            {
                if (ArrayUtility.Contains(assets, assetObject)) return;

                for (int i = 0; i < assets.Length; i++)
                {
                    if (!assets[i])
                    {
                        assets[i] = assetObject;
                        added = true;
                        break;
                    }
                }
            }

            if (!added) ArrayUtility.Add(ref assets, assetObject);

            PlayerSettings.SetPreloadedAssets(assets);
        }

    } // struct AssetUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR