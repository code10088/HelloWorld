using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// ScriptableAssetSingleton, the created asset can be added to 'Preloaded Assets' list in player settings automatically.
    /// </summary>
    public class ScriptableAssetSingleton<T> : ScriptableAsset where T : ScriptableAssetSingleton<T>
    {
        static T _instance;


        /// <summary>
        /// The asset instance.
        /// </summary>
        public static T instance
        {
            get
            {
                if (!_instance)
                {
#if UNITY_EDITOR
                    _instance = AssetUtilities.FindAsset<T>();
                    if (!_instance)
                    {
                        _instance = CreateInstance<T>();
                        Debug.LogWarning(string.Format("No asset of type {0} loaded, a temporary instance was created. Use {0}.CreateOrSelectAsset to create an asset.", typeof(T).Name));
                    }
#else
                    _instance = CreateInstance<T>();
                    Debug.LogWarning(string.Format("No asset of type {0} loaded, a temporary instance was created. Do you forget to add the asset to \"Preloaded Assets\" list?", typeof(T).Name));
#endif
                }
                return _instance;
            }
        }


        protected ScriptableAssetSingleton()
        {
            _instance = this as T;
        }


#if UNITY_EDITOR

        /// <summary>
        /// Create asset if it does not exist, or just select it if it exist.
        /// The created asset can be added to 'Preloaded Assets' list in player settings automatically.
        /// </summary>
        public static void CreateOrSelectAsset(bool addToPreloadedAssets = true)
        {
            bool needCreate = false;

            if (!_instance)
            {
                _instance = AssetUtilities.FindAsset<T>();
                if (!_instance)
                {
                    _instance = CreateInstance<T>();
                    needCreate = true;
                }
            }
            else needCreate = !AssetDatabase.IsNativeAsset(_instance);

            if (needCreate) AssetUtilities.CreateAsset(_instance);

            if (addToPreloadedAssets) AssetUtilities.AddPreloadedAsset(_instance);

            Selection.activeObject = instance;
        }

#endif

    } // class ScriptableAssetSingleton

} // namespace UnityExtensions