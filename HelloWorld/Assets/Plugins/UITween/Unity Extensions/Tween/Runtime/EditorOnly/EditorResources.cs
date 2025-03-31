#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Tween.Editor
{
    public class EditorResources : ScriptableAssetSingleton<EditorResources>
    {
        public Texture2D play;
        public Texture2D rightArrow;
        public Texture2D leftArrow;


        [MenuItem("Assets/Create/Unity Extensions/Editor/Tween Editor Resources")]
        static void CreateAsset()
        {
            CreateOrSelectAsset();
        }

    } // EditorResources

} // UnityExtensions.Tween.Editor

#endif