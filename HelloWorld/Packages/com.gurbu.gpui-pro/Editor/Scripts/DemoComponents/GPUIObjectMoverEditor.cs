// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIObjectMover))]
    public class GPUIObjectMoverEditor : GPUIEditor
    {
        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty isOrbitingSP = serializedObject.FindProperty("isOrbiting");
            if (!isOrbitingSP.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardMove"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("upwardMove"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("positionChange"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationChange"));

                SerializedProperty isLoopingSP = serializedObject.FindProperty("isLooping");
                EditorGUILayout.PropertyField(isLoopingSP);
                if (isLoopingSP.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loopDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loopAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("loopChangeDirection"));
                    EditorGUI.indentLevel--;
                }

                SerializedProperty isRayCastingSP = serializedObject.FindProperty("isRayCasting");
                EditorGUILayout.PropertyField(isRayCastingSP);
                if (isRayCastingSP.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rayCastHeight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rayCastLayer"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rayCastMaxDistance"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(isOrbitingSP);
            if (isOrbitingSP.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitCenter"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("orbitSpeed"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Object Mover";
        }
    }
}