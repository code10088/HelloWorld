using System;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// DisableAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DisableAttribute : PropertyAttribute
    {
        public DisableAttribute(string fieldOrProperty, bool disableValue, int indent = 0)
        {
#if UNITY_EDITOR
            _name = fieldOrProperty;
            _value = disableValue;
            _indent = indent;
#endif
        }


#if UNITY_EDITOR

        string _name;
        bool _value;
        int _indent;
        object _fieldOrProp;

        [CustomPropertyDrawer(typeof(DisableAttribute))]
        class DisableDrawer : BasePropertyDrawer<DisableAttribute>
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (attribute._fieldOrProp == null)
                {
                    var field = property.serializedObject.targetObject.GetType().GetInstanceField(attribute._name);
                    if (field?.FieldType == typeof(bool))
                    {
                        attribute._fieldOrProp = field;
                    }
                    else
                    {
                        var prop = property.serializedObject.targetObject.GetType().GetInstanceProperty(attribute._name);
                        if (prop?.PropertyType == typeof(bool) && prop.CanRead)
                        {
                            attribute._fieldOrProp = prop;
                        }
                    }
                }

                return attribute._fieldOrProp == null ? EditorGUIUtility.singleLineHeight : base.GetPropertyHeight(property, label);
            }


            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                object result = (attribute._fieldOrProp as FieldInfo)?.GetValue(property.serializedObject.targetObject);
                if (result == null) result = (attribute._fieldOrProp as PropertyInfo)?.GetValue(property.serializedObject.targetObject, null);

                if (result != null)
                {
                    using (DisabledScope.New((bool)result == attribute._value))
                    {
                        using (IndentLevelScope.New(attribute._indent))
                            base.OnGUI(position, property, label);
                    }
                }
                else
                {
                    EditorGUI.LabelField(position, label.text, "Field or Property has error!");
                }
            }

        } // class DisableDrawer

#endif // UNITY_EDITOR

    } // class DisableAttribute

} // namespace UnityExtensions