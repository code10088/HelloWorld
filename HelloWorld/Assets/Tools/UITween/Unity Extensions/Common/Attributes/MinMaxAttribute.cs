using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// Set minimum & maximum allowed values for int & float field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class MinMaxAttribute : PropertyAttribute
    {
        /// <summary>
        /// Set minimum & maximum allowed values for int & float field
        /// </summary>
        public MinMaxAttribute(float min, float max)
        {
#if UNITY_EDITOR
            _min = min;
            _max = max;
#endif
        }

#if UNITY_EDITOR

        float _min;
        float _max;

        [CustomPropertyDrawer(typeof(MinMaxAttribute))]
        class MinMaxDrawer : BasePropertyDrawer<MinMaxAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        {
                            property.floatValue = Mathf.Clamp(
                                EditorGUI.FloatField(position, label, property.floatValue),
                                attribute._min,
                                attribute._max);
                            break;
                        }
                    case SerializedPropertyType.Integer:
                        {
                            property.intValue = Mathf.Clamp(
                                EditorGUI.IntField(position, label, property.intValue),
                                (int)attribute._min,
                                (int)attribute._max);
                            break;
                        }
                    default:
                        {
                            EditorGUI.LabelField(position, label.text, "Use MinMax with float or int.");
                            break;
                        }
                }
            }

        } // class MinMaxDrawer

#endif // UNITY_EDITOR

    } // class MinMaxAttribute

} // namespace UnityExtensions