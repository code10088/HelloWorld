// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace GPUInstancerPro.PrefabModule
{
    [CustomEditor(typeof(GPUIInstanceSpawner))]
    public class GPUIInstanceSpawnerEditor : GPUIEditor
    {
        private bool _uiToggle;

        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();

            SerializedProperty isRandomSeedSP = serializedObject.FindProperty("isRandomSeed");
            EditorGUILayout.PropertyField(isRandomSeedSP);
            if (!isRandomSeedSP.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startInstanceCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabObjects"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("removeSpeed"));

            SerializedProperty addInstantlySP = serializedObject.FindProperty("addInstantly");
            EditorGUILayout.PropertyField(addInstantlySP);
            if (addInstantlySP.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAddCount"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("addSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("addAsChildGameObject"));

            SerializedProperty randomRotationSP = serializedObject.FindProperty("randomRotation");
            EditorGUILayout.PropertyField(randomRotationSP);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));

            EditorGUILayout.Space(10);
            SerializedProperty spawnModeSP = serializedObject.FindProperty("spawnMode");
            EditorGUILayout.PropertyField(spawnModeSP);
            if (spawnModeSP.intValue == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("randomPosition"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("center"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
                if (spawnModeSP.intValue == 2)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceFromCenter"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minMaxScale"));

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("instantiatedPrefabEvent"));

            EditorGUILayout.Space(10);
            _uiToggle = EditorGUILayout.Foldout(_uiToggle, "UI", true);
            if (_uiToggle)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("instanceCountText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentInstanceCountText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("instanceCountSlider"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingPanel"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Instance Spawner";
        }
    }
}