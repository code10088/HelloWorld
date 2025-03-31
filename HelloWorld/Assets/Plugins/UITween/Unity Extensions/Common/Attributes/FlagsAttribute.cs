using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// Use on an enum field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class FlagsAttribute : PropertyAttribute
    {
        bool _includeObsolete;

        public FlagsAttribute(bool includeObsolete = false)
        {
            _includeObsolete = includeObsolete;
        }


#if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(FlagsAttribute))]
        class FlagsDrawer : BasePropertyDrawer<FlagsAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var value = (Enum)Enum.ToObject(fieldInfo.FieldType, property.intValue);

                using (var scope = ChangeCheckScope.New())
                {
                    value = EditorGUI.EnumFlagsField(position, label, value, attribute._includeObsolete);
                    if (scope.changed)
                    {
                        property.intValue = value.GetHashCode();
                    }
                }
            }

        } // class FlagsDrawer

#endif // UNITY_EDITOR

    } // class FlagsAttribute

} // namespace UnityExtensions