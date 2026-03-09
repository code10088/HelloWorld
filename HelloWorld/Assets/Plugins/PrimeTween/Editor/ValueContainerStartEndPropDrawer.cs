using System;
using System.Runtime.CompilerServices;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using Mathf = UnityEngine.Mathf;
using TweenType = PrimeTween.TweenAnimation.TweenType;

[CustomPropertyDrawer(typeof(ValueContainerStartEnd))]
public class ValueContainerStartEndPropDrawer : PropertyDrawer {
    readonly GUIContent _startValueGuiContent = new GUIContent(ObjectNames.NicifyVariableName(nameof(TweenSettings<float>.startValue)));
    readonly GUIContent _endValueGuiContent = new GUIContent(ObjectNames.NicifyVariableName(nameof(TweenSettings<float>.endValue)));

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
        prop.Next(true);
        var tweenType = (TweenType)prop.enumValueIndex;
        prop.Next(false);
        return GetHeight(prop, label, tweenType);
    }

    internal static float GetHeight(SerializedProperty prop, GUIContent label, TweenType tweenType) {
        var propType = Utils.TweenTypeToTweenData(tweenType).Item1;
        Assert.AreNotEqual(PropType.None, propType);
        float height = GetSingleItemHeight(propType, label) * 2f + EditorGUIUtility.standardVerticalSpacing;
        if (!UNITY_2020_3_OR_NEWER) {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        return height;
    }

    static float GetSingleItemHeight(PropType propType, GUIContent label) {
        return EditorGUI.GetPropertyHeight(ToSerializedPropType(), label);
        SerializedPropertyType ToSerializedPropType() {
            switch (propType) {
                case PropType.Double:
                case PropType.Float:
                    return SerializedPropertyType.Float;
                case PropType.Color:
                    return SerializedPropertyType.Color;
                case PropType.Vector2:
                    return SerializedPropertyType.Vector2;
                case PropType.Vector3:
                    return SerializedPropertyType.Vector3;
                case PropType.Vector4:
                case PropType.Quaternion:
                    return SerializedPropertyType.Vector4;
                case PropType.Rect:
                    return SerializedPropertyType.Rect;
                case PropType.Int:
                    return SerializedPropertyType.Integer;
                case PropType.None:
                default:
                    throw new Exception();
            }
        }
    }

    static bool UNITY_2020_3_OR_NEWER {
        get {
            #if UNITY_2020_3_OR_NEWER
            return true;
            #else
            return false;
            #endif
        }
    }

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label) {
        prop.Next(true);
        var tweenType = (TweenType)prop.enumValueIndex;
        prop.Next(false);
        Draw(ref pos, prop, tweenType, true, true, _startValueGuiContent, _endValueGuiContent);
    }

    internal static void Draw(ref Rect pos, SerializedProperty prop, TweenType tweenType, bool drawStartFromCurrentToggle, bool invert, GUIContent startValueLabel, GUIContent endValueLabel) {
        var propType = Utils.TweenTypeToTweenData(tweenType).Item1;
        Assert.AreNotEqual(PropType.None, propType);

        const float toggleWidth = 18f;
        EditorGUIUtility.labelWidth -= toggleWidth;

        // startFromCurrent toggle
        bool newStartFromCurrent = false;
        if (drawStartFromCurrentToggle) {
            Rect togglePos;
            if (UNITY_2020_3_OR_NEWER) {
                togglePos = new Rect(pos.x + 2, pos.y, toggleWidth - 2, EditorGUIUtility.singleLineHeight);
            } else {
                togglePos = pos;
            }
            using (var scope = new CustomPropertyScope(togglePos, null, prop)) {
                if (invert) {
                    newStartFromCurrent = !EditorGUI.ToggleLeft(togglePos, scope.content, !prop.boolValue);
                } else {
                    newStartFromCurrent = EditorGUI.ToggleLeft(togglePos, scope.content, prop.boolValue);
                }
                if (scope.EndChangeCheck()) {
                    prop.boolValue = newStartFromCurrent;
                }
            }
            if (!UNITY_2020_3_OR_NEWER) {
                pos.y += pos.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        pos.x += toggleWidth;
        pos.width -= toggleWidth;

        prop.NextVisible(nameof(ValueContainerStartEnd.startValue));
        bool disableGui = false;
        if (drawStartFromCurrentToggle) {
            disableGui = newStartFromCurrent ^ !invert;
        }
        float height = GetSingleItemHeight(propType, startValueLabel);
        using (new EditorGUI.DisabledScope(disableGui)) {
            DrawValueContainer(ref pos, prop, propType, startValueLabel, height);
            prop.Next(false);
        }

        pos.y += pos.height + EditorGUIUtility.standardVerticalSpacing;
        DrawValueContainer(ref pos, prop, propType, endValueLabel, height);
        pos.y += pos.height + EditorGUIUtility.standardVerticalSpacing;

        pos.x -= toggleWidth;
        pos.width += toggleWidth;
    }

    static void DrawValueContainer(ref Rect pos, SerializedProperty prop, PropType propType, GUIContent guiContent, float height) {
        Assert.IsNotNull(guiContent);
        var root = prop.Copy();
        prop.Next(true);
        TweenAnimation.ValueWrapper valueContainer = default;
        const int length = 4;
        for (int i = 0; i < length; i++) {
            if (i != 0) {
                prop.Next(false);
            }
            valueContainer[i] = prop.floatValue;
        }
        pos.height = height;

        using (var scope = new CustomPropertyScope(pos, guiContent, root)) {
            TweenAnimation.ValueWrapper newVal = DrawField(pos);
            TweenAnimation.ValueWrapper DrawField(Rect position) {
                switch (propType) {
                    case PropType.Float:
                        return EditorGUI.FloatField(position, scope.content, valueContainer.single).ToContainer();
                    case PropType.Color:
                        return EditorGUI.ColorField(position, scope.content, valueContainer.color).ToContainer();
                    case PropType.Vector2:
                        return EditorGUI.Vector2Field(position, scope.content, valueContainer.vector2).ToContainer();
                    case PropType.Vector3:
                        return EditorGUI.Vector3Field(position, scope.content, valueContainer.vector3).ToContainer();
                    case PropType.Vector4:
                    case PropType.Quaternion: // p2 todo don't draw quaternion. Or draw it as Vector3 euler angles?
                        return EditorGUI.Vector4Field(position, scope.content, valueContainer.vector4).ToContainer();
                    case PropType.Rect:
                        return EditorGUI.RectField(position, scope.content, valueContainer.rect).ToContainer();
                    case PropType.Int:
                        var newIntVal = EditorGUI.IntField(position, scope.content, Mathf.RoundToInt(valueContainer.single));
                        return ((float)newIntVal).ToContainer();
                    case PropType.Double: // should be used for display only. Unity serializes floats to text, not binary, so it's not possible to serialize two floats as one double
                        return EditorGUI.DoubleField(position, scope.content, valueContainer.DoubleVal).ToContainer();
                    case PropType.None:
                    default:
                        throw new Exception();
                }
            }

            if (scope.EndChangeCheck()) {
                root.Next(true);
                for (int i = 0; i < length; i++) {
                    if (i != 0) {
                        root.Next(false);
                    }
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (root.floatValue != newVal[i]) {
                        root.floatValue = newVal[i];
                    }
                }
            }
        }
    }
}

internal static class SerializedPropertyExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Next(this SerializedProperty prop, string expectedName, bool enterChildren = false) {
        prop.Next(enterChildren);
        CheckName(prop, expectedName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void NextVisible(this SerializedProperty prop, string expectedName, bool enterChildren = false) {
        prop.NextVisible(enterChildren);
        CheckName(prop, expectedName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckName(this SerializedProperty prop, string expectedName) {
        // #if PRIME_TWEEN_SAFETY_CHECKS
        // Assert.AreEqual(expectedName, prop.name);
        // #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ExitCurrentDepth(this SerializedProperty prop) {
        int depth = prop.depth;
        while (prop.depth >= depth) {
            prop.NextVisible(false);
        }
    }

    internal static bool SetIntChecked(this SerializedProperty prop, int value) {
        if (!prop.hasMultipleDifferentValues) {
            prop.intValue = value;
            return true;
        }
        return false;
    }

    internal static void SetFloatChecked(this SerializedProperty prop, float value) {
        if (!prop.hasMultipleDifferentValues) {
            prop.floatValue = value;
        }
    }

    internal static void SetObjectReferenceChecked(this SerializedProperty prop, UnityEngine.Object value) {
        if (!prop.hasMultipleDifferentValues) {
            prop.objectReferenceValue = value;
        }
    }

    internal static void SetArraySizeChecked(this SerializedProperty prop, int size) {
        if (!prop.hasMultipleDifferentValues) {
            prop.arraySize = size;
        }
    }

    internal static void SetBoolChecked(this SerializedProperty prop, bool value) {
        if (!prop.hasMultipleDifferentValues) {
            prop.boolValue = value;
        }
    }
}

internal struct CustomPropertyScope : IDisposable {
    internal readonly GUIContent content;
    bool _changeCheckEnded;

    internal CustomPropertyScope(Rect pos, GUIContent label, SerializedProperty prop) {
        content = EditorGUI.BeginProperty(pos, label, prop);
        Assert.IsNotNull(content);
        EditorGUI.BeginChangeCheck();
        _changeCheckEnded = false;
    }

    internal bool EndChangeCheck() {
        Assert.IsFalse(_changeCheckEnded);
        _changeCheckEnded = true;
        return EditorGUI.EndChangeCheck();
    }

    void IDisposable.Dispose() {
        #if PRIME_TWEEN_SAFETY_CHECKS
        if (!_changeCheckEnded) {
            Debug.Log($"{nameof(CustomPropertyScope)} was disposed without calling {nameof(EndChangeCheck)} first. This can happen during normal operation if a drawing function throws {nameof(ExitGUIException)}. For example, selecting multiple objects, the opening color selector or object reference picker results in this error.");
        }
        #endif
        EditorGUI.EndProperty();
    }
}
