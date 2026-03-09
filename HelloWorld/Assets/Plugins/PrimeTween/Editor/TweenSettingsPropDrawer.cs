using JetBrains.Annotations;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using Mathf = UnityEngine.Mathf;

[CustomPropertyDrawer(typeof(TweenSettings))]
internal class TweenSettingsPropDrawer : PropertyDrawer {
    static GUIContent updateTypeGuiContent;

    public override float GetPropertyHeight([NotNull] SerializedProperty property, GUIContent label) {
        if (!property.isExpanded) {
            return EditorGUIUtility.singleLineHeight;
        }
        return getPropHeight(property);
    }

    internal static float getPropHeight([NotNull] SerializedProperty property) {
        var count = 1;
        count++; // duration
        count++; // ease
        var easeIndex = property.FindPropertyRelative(nameof(TweenSettings.ease)).intValue;
        if (easeIndex == (int)Ease.Custom) {
            count++; // customEase
        }
        count++; // cycles
        var cycles = property.FindPropertyRelative(nameof(TweenSettings.cycles)).intValue;
        if (cycles != 0 && cycles != 1) {
            count++; // cycleMode
        }
        count++; // startDelay
        count++; // endDelay
        count++; // useUnscaledTime
        count++; // useFixedUpdate
        var result = EditorGUIUtility.singleLineHeight * count + EditorGUIUtility.standardVerticalSpacing * (count - 1);
        result += EditorGUIUtility.standardVerticalSpacing * 4; // extra spacing
        return result;
    }

    public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label) {
        var rect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
        EditorGUI.PropertyField(rect, property, label);
        if (!property.isExpanded) {
            return;
        }
        moveToNextLine(ref rect);
        EditorGUI.indentLevel++;
        { // duration
            property.NextVisible(true);
            DrawDuration(rect, property);
            moveToNextLine(ref rect);
        }
        drawEaseTillEnd(property, ref rect);
        EditorGUI.indentLevel--;
    }

    internal static void DrawDuration(Rect rect, [NotNull] SerializedProperty property) { // p2 todo allow duration to be 0f in Inspector? need to change how defaultValue behaves below
        EditorGUI.PropertyField(rect, property);
        ClampProperty(property, 1f);
    }

    internal static void ClampProperty(SerializedProperty prop, float defaultValue, float min = 0.01f, float max = float.MaxValue) {
        float current = prop.floatValue;
        float clamped = current == 0f ? defaultValue : Mathf.Clamp(current, min, max);
        prop.SetFloatChecked(clamped);
    }

    internal static void drawEaseTillEnd([NotNull] SerializedProperty property, ref Rect rect) {
        DrawEaseAndCycles(out _, property, ref rect);
        drawStartDelayTillEnd(ref rect, property);
    }

    internal static void DrawEaseAndCycles(out int cycles, SerializedProperty property, ref Rect rect, bool addSpace = true, bool draw = true, bool allowInfiniteCycles = true) {
        { // ease
            property.NextVisible(true);
            if (draw)
                EditorGUI.PropertyField(rect, property);
            moveToNextLine(ref rect);
            // customEase
            bool isCustom = property.intValue == (int) Ease.Custom;
            property.NextVisible(true);
            if (isCustom) {
                if (draw)
                    EditorGUI.PropertyField(rect, property);
                moveToNextLine(ref rect);
            } else {
                if (!property.hasMultipleDifferentValues) {
                    property.animationCurveValue = new AnimationCurve();
                }
            }
        }
        if (addSpace) {
            rect.y += EditorGUIUtility.standardVerticalSpacing * 4;
        }
        { // cycles
            property.NextVisible(nameof(TweenSettings.cycles));
            if (draw) {
                EditorGUI.PropertyField(rect, property);
                ClampCycles(property, allowInfiniteCycles);
            }
            moveToNextLine(ref rect);
            cycles = property.intValue;

            // cycleMode
            property.NextVisible(true);
            if (cycles != 0 && cycles != 1) {
                if (draw)
                    EditorGUI.PropertyField(rect, property);
                moveToNextLine(ref rect);
            }
        }
    }

    internal static void drawStartDelayTillEnd(ref Rect rect, [NotNull] SerializedProperty property) {
        { // startDelay, endDelay
            for (int _ = 0; _ < 2; _++) {
                property.NextVisible(true);
                EditorGUI.PropertyField(rect, property);
                if (property.floatValue < 0f) {
                    property.SetFloatChecked(0f);
                }
                moveToNextLine(ref rect);
            }
        }
        { // useUnscaledTime
            property.NextVisible(true);
            EditorGUI.PropertyField(rect, property);
            moveToNextLine(ref rect);
        }
        { // useFixedUpdate
            property.Next(false);
            bool useFixedUpdateObsolete = property.boolValue;
            var useFixedUpdateObsoleteProp = property.Copy();

            // _updateType
            property.NextVisible(false);
            var current = (_UpdateType)property.enumValueIndex;
            if (useFixedUpdateObsolete && current != _UpdateType.FixedUpdate && !property.serializedObject.isEditingMultipleObjects) {
                property.serializedObject.Update();
                useFixedUpdateObsoleteProp.boolValue = false;
                property.enumValueIndex = (int)_UpdateType.FixedUpdate;
                property.serializedObject.ApplyModifiedProperties();
            } else {
                if (updateTypeGuiContent == null) {
                    updateTypeGuiContent = new GUIContent(property.displayName, property.tooltip);
                }
                using (var scope = new CustomPropertyScope(rect, updateTypeGuiContent, property)) {
                    var newUpdateType = (_UpdateType)EditorGUI.EnumPopup(rect, scope.content, current);
                    if (scope.EndChangeCheck()) {
                        property.enumValueIndex = (int)newUpdateType;

                        // Updating _useFixedUpdate breaks the Prefab "Revert" button, so I commented it out.
                        // Updating the _useFixedUpdate was necessary before so users can safely downgrade from 1.3.0 to a previous version.
                        // But 1.3.0 was released 8 months ago, so we no longer need to support downgrading to such an old version.
                        // useFixedUpdateProp.boolValue = newUpdateType == _UpdateType.FixedUpdate;
                    }
                }
                moveToNextLine(ref rect);
            }
        }
    }

    internal static void ClampCycles(SerializedProperty property, bool allowInfiniteCycles = true) {
        int val = property.intValue;
        if (val == 0) {
            property.SetIntChecked(1);
        } else if (val < 0) {
            property.SetIntChecked(allowInfiniteCycles ? -1 : 1);
        }
    }

    static void moveToNextLine(ref Rect rect) {
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}

[CustomPropertyDrawer(typeof(UpdateType))]
class UpdateTypePropDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label) {
        prop.Next(true);
        EditorGUI.PropertyField(pos, prop, label);
    }
}
