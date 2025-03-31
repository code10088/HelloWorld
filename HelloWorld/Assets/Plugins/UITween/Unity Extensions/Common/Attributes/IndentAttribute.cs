using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// set indent of a field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IndentAttribute : PropertyAttribute
    {
        int _indentLevel;

        public IndentAttribute(int indentLevel = 1)
        {
            _indentLevel = indentLevel;
        }

#if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(IndentAttribute))]
        class IndentDrawer : BasePropertyDrawer<IndentAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.indentLevel += attribute._indentLevel;
                base.OnGUI(position, property, label);
                EditorGUI.indentLevel -= attribute._indentLevel;
            }
        }

#endif // UNITY_EDITOR

    } // class IndentAttribute

} // namespace UnityExtensions