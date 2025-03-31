#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Paths.Editor
{
    public class EditorResources : ScriptableAssetSingleton<EditorResources>
    {
        public Texture2D moveToolPan;
        public Texture2D moveTool3D;
        public Texture2D RotateTool;
        public Texture2D addNodeForward;
        public Texture2D addNodeBack;
        public Texture2D removeNode;


        [MenuItem("Assets/Create/Unity Extensions/Editor/Paths Editor Resources")]
        static void CreateAsset()
        {
            CreateOrSelectAsset();
        }

    } // EditorResources

} // UnityExtensions.Paths.Editor

#endif