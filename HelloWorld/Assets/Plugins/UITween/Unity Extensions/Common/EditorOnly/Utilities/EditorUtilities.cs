#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Utilities for editor.
    /// </summary>
    public struct EditorUtilities
    {
        static float _unscaledDeltaTime;
        static double _lastTimeSinceStartup = -0.01;


        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.update += () =>
            {
                _unscaledDeltaTime = (float)(EditorApplication.timeSinceStartup - _lastTimeSinceStartup);
                _lastTimeSinceStartup = EditorApplication.timeSinceStartup;
            };
        }


        public static float unscaledDeltaTime
        {
            get { return _unscaledDeltaTime; }
        }


        public static PlayModeStateChange playMode
        {
            get
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (EditorApplication.isPlaying) return PlayModeStateChange.EnteredPlayMode;
                    else return PlayModeStateChange.ExitingEditMode;
                }
                else
                {
                    if (EditorApplication.isPlaying) return PlayModeStateChange.ExitingPlayMode;
                    else return PlayModeStateChange.EnteredEditMode;
                }
            }
        }

    } // struct EditorUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR