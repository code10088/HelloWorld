using JetBrains.Annotations;
using PrimeTween;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShakeSettings))]
internal class ShakeSettingsPropDrawer : PropertyDrawer {
    public override float GetPropertyHeight([NotNull] SerializedProperty property, GUIContent label) {
        if (!property.isExpanded) {
            return EditorGUIUtility.singleLineHeight;
        }
        property.NextVisible(true);
        float result = EditorGUI.GetPropertyHeight(property, true); // strength
        var count = 1;
        count++; // frequency
        count++; // duration
        count++; // enableFalloff
        property.NextVisible(false);
        property.NextVisible(false);
        property.NextVisible(false); // enableFalloff
        if (property.boolValue) {
            count++; // falloffEase
            property.NextVisible(false);
            if (property.intValue == -1) {
                count++; // strengthOverTime
            }
        }
        count++; // asymmetry
        count++; // easeBetweenShakes
        count++; // cycles
        count++; // startDelay
        count++; // endDelay
        count++; // useUnscaledTime
        count++; // useFixedUpdate
        result += EditorGUIUtility.singleLineHeight * count + EditorGUIUtility.standardVerticalSpacing * (count - 1);
        result += EditorGUIUtility.standardVerticalSpacing * 2; // extra space
        return result;
    }

    public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label) {
        var rect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
        EditorGUI.PropertyField(rect, property, label);
        if (!property.isExpanded) {
            return;
        }
        moveToNextLine();
        EditorGUI.indentLevel++;
        property.NextVisible(true);
        { // strength
            EditorGUI.PropertyField(rect, property);
            rect.y += EditorGUI.GetPropertyHeight(property, true);
        }
        { // duration
            property.NextVisible(false);
            TweenSettingsPropDrawer.DrawDuration(rect, property);
            moveToNextLine();
        }
        { // frequency
            property.NextVisible(false);
            propertyField();
            ClampFrequency(property);
        }
        { // enableFalloff
            property.NextVisible(false);
            propertyField();
            var enableFalloff = property.boolValue;
            property.NextVisible(false);
            if (enableFalloff) {
                // falloffEase
                propertyField();
                // strengthOverTime
                var customFalloffEase = property.intValue == (int)Ease.Custom;
                property.NextVisible(false);
                if (customFalloffEase) {
                    propertyField();
                }
            } else {
                // skipped strengthOverTime
                property.NextVisible(false);
            }
        }
        // extra space
        rect.y += EditorGUIUtility.standardVerticalSpacing * 2;
        { // asymmetry
            property.NextVisible(false);
            propertyField();
        }
        { // easeBetweenShakes
            property.NextVisible(false);
            propertyField();
            if (property.intValue == (int)Ease.Custom && property.SetIntChecked((int)Ease.Default)) {
                Debug.LogWarning($"Ease.Custom is not supported for {nameof(ShakeSettings.easeBetweenShakes)}.");
            }
        }

        // cycles
        property.NextVisible(false);
        propertyField();
        TweenSettingsPropDrawer.ClampCycles(property);

        // startDelay
        TweenSettingsPropDrawer.drawStartDelayTillEnd(ref rect, property);
        EditorGUI.indentLevel--;

        void propertyField() {
            EditorGUI.PropertyField(rect, property);
            moveToNextLine();
        }

        void moveToNextLine() {
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    internal static void ClampFrequency(SerializedProperty prop) {
        TweenSettingsPropDrawer.ClampProperty(prop, ShakeSettings.defaultFrequency, 0.15f);
    }
}
