// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomPropertyDrawer(typeof(GPUIPrototype))]
    public class GPUIPrototypePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new IMGUIContainer(() => DrawPrototypeFieldsIMGUI(property));
        }

        private void DrawPrototypeFieldsIMGUI(SerializedProperty property)
        {
            if (property == null)
                return;
            int prototypeType = property.FindPropertyRelative("prototypeType").intValue;
            bool isAllowEdit = false;
            if (property.serializedObject.targetObject is GPUIManager manager)
                isAllowEdit = manager.Editor_IsAllowEditPrototype(prototypeType);
            EditorGUI.BeginDisabledGroup(Application.isPlaying || !isAllowEdit);
            EditorGUI.BeginChangeCheck();
            switch (prototypeType)
            {
                case 0:
                    GPUIEditorUtility.DrawIMGUISerializedProperty(property.FindPropertyRelative("prefabObject"), false);
                    break;
                case 1:
                    GPUIEditorUtility.DrawIMGUISerializedProperty(property.FindPropertyRelative("gpuiLODGroupData"), false);
                    break;
                case 2:
                    GPUIEditorUtility.DrawIMGUISerializedProperty(property.FindPropertyRelative("prototypeMesh"), false);
                    GPUIEditorUtility.DrawIMGUISerializedProperty(property.FindPropertyRelative("prototypeMaterials"), false);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                GPUIRenderingSystem.RegenerateRenderers();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
