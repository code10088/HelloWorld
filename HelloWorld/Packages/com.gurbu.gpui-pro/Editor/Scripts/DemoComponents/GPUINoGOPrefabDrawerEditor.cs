// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUINoGOPrefabDrawer))]
    public class GPUINoGOPrefabDrawerEditor : GPUIEditor
    {
        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("profile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("instanceCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            SerializedProperty enableColorVariationsSP = serializedObject.FindProperty("enableColorVariations");
            EditorGUILayout.PropertyField(enableColorVariationsSP);
            if (enableColorVariationsSP.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("variationKeyword"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("variationBufferName"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("instanceCountText"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        public override string GetTitleText()
        {
            return "GPUI No-GameObject Prefab Drawer";
        }
    }
}