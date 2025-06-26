// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace GPUInstancerPro.TerrainModule
{
    [CustomEditor(typeof(GPUIDetailDensityModifier))]
    public class GPUIDetailDensityModifierEditor : GPUIEditor
    {
        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("detailManager"));
            SerializedProperty useBoundsSP = serializedObject.FindProperty("useBounds");
            EditorGUILayout.PropertyField(useBoundsSP);
            if (useBoundsSP.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundsSize"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedColliders"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyEveryUpdate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedPrototypeIndexes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("densityValue"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Detail Density Modifier";
        }
    }
}