// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License
using UnityEngine;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
using Sirenix.Serialization;
using Sirenix.OdinInspector.Editor;

namespace UnityEditor
{
    [CustomEditor(typeof(TextAsset))]
    [CanEditMultipleObjects]
    internal class TextAssetInspector : Editor
    {
        private const int kMaxChars = 7000;
        [NonSerialized]
        private GUIStyle m_TextStyle;
        private TextAsset m_TextAsset;
        private GUIContent m_CachedPreview;
        private string m_AssetGUID;
        private Hash128 m_LastDependencyHash;
        private PropertyTree bytesPropertyTree;

        public virtual void OnEnable()
        {
            m_TextAsset = target as TextAsset;
            m_AssetGUID = AssetDatabase.GetAssetPath(m_TextAsset);
            CachePreview();
        }

        public override void OnInspectorGUI()
        {
            if (m_TextStyle == null)
                m_TextStyle = "ScriptText";

            Hash128 dependencyHash = AssetDatabase.GetAssetDependencyHash(m_AssetGUID);
            if (m_LastDependencyHash != dependencyHash)
            {
                CachePreview();
                m_LastDependencyHash = dependencyHash;
            }

            bool enabledTemp = GUI.enabled;
            GUI.enabled = true; 
            if (bytesPropertyTree != null)
            {
                EditorGUILayout.Space(10f);
                bytesPropertyTree.Draw(false);
            }
            else if (m_TextAsset != null)
            {
                Rect rect = GUILayoutUtility.GetRect(m_CachedPreview, m_TextStyle);
                rect.x = 0;
                rect.y -= 3;
                //rect.width = GUIClip.visibleRect.width + 1;
                GUI.Box(rect, m_CachedPreview, m_TextStyle);
            }
            GUI.enabled = enabledTemp;
        }

        void CachePreview()
        {
            string text = string.Empty;

            if (m_TextAsset != null)
            {
                if (targets.Length > 1)
                {
                    //text = targetTitle;
                }
                else
                {
                    string ext = Path.GetExtension(m_AssetGUID);
                    if (ext == ".txt")
                    {
                        text = m_TextAsset.text;
                        if (text.Length >= kMaxChars)
                            text = text.Substring(0, kMaxChars) + "...\n\n<...etc...>";
                    }
                    else if (ext == ".bytes")
                    {
                        try
                        {
                            byte[] bytes = m_TextAsset.bytes;
                            BytesDecode bd = new BytesDecode(bytes);
                            string name = bd.ToStr();
                            Type type = Assembly.GetExecutingAssembly().GetType(name);
                            var mi = type.GetMethod("Deserialize");
                            object obj = Activator.CreateInstance(type);
                            mi.Invoke(obj, new object[] { bd });
                            bytesPropertyTree = PropertyTree.Create(obj);
                        }
                        catch
                        {

                        }
                    }
                }
            }

            m_CachedPreview = new GUIContent(text);
        }
    }
}