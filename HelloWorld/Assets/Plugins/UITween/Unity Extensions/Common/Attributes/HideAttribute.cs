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
    /// Hide a field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class HideAttribute : PropertyAttribute
    {
        public HideAttribute(string fieldOrProperty, bool hideValue, int indent = 0)
        {
#if UNITY_EDITOR
            _name = fieldOrProperty;
            _value = hideValue;
            _indent = indent;
#endif
        }


#if UNITY_EDITOR

        string _name;
        bool _value;
        int _indent;

        object _fieldOrProp;
        int _result; // 0-hide, 1-show, -1-error

        [CustomPropertyDrawer(typeof(HideAttribute))]
        class HideDrawer : BasePropertyDrawer<HideAttribute>
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

                object result = (attribute._fieldOrProp as FieldInfo)?.GetValue(property.serializedObject.targetObject);
                if (result == null) result = (attribute._fieldOrProp as PropertyInfo)?.GetValue(property.serializedObject.targetObject, null);

                if (result != null)
                {
                    if ((bool)result == attribute._value)
                    {
                        attribute._result = 0;
                        return -2f;
                    }
                    else
                    {
                        attribute._result = 1;
                        return base.GetPropertyHeight(property, label);
                    }
                }
                else
                {
                    attribute._result = -1;
                    return EditorGUIUtility.singleLineHeight;
                }
            }


            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (attribute._result == 1)
                {
                    using (IndentLevelScope.New(attribute._indent))
                        base.OnGUI(position, property, label);
                }
                else if (attribute._result == -1)
                {
                    EditorGUI.LabelField(position, label.text, "Field or Property has error!");
                }
            }

        } // class HideDrawer

#endif

    } // class HideAttribute

} // namespace UnityExtensions