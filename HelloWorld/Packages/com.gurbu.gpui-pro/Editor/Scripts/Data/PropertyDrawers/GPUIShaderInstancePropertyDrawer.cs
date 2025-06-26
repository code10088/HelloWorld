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
    [CustomPropertyDrawer(typeof(GPUIShaderInstance))]
    public class GPUIShaderInstancePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            Foldout foldout = new Foldout();
            foldout.text = property.FindPropertyRelative("shaderName").stringValue;
            foldout.value = false;
            foldout.Add(new IMGUIContainer(() => DrawPrototypeFields(property)));
            container.Add(foldout);
            return container;
        }

        private void DrawPrototypeFields(SerializedProperty property)
        {
            if (property == null) 
                return;
            string originalShaderName = property.FindPropertyRelative("shaderName").stringValue;
            Shader originalShader = Shader.Find(originalShaderName);
            if (originalShader == null)
                return;
            string originalShaderPath = AssetDatabase.GetAssetPath(originalShader);
            string extension = System.IO.Path.GetExtension(originalShaderPath);
            bool canConvert = extension == ".shader" || extension.EndsWith("pack");

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Original Shader", originalShader, typeof(Shader), false);
            bool isUseOriginal = property.FindPropertyRelative("isUseOriginal").boolValue;
            if (!isUseOriginal)
                EditorGUILayout.ObjectField("Replacement Shader", Shader.Find(property.FindPropertyRelative("replacementShaderName").stringValue), typeof(Shader), false);
            string extensionCode = property.FindPropertyRelative("extensionCode").stringValue;
            if (!string.IsNullOrEmpty(extensionCode))
                EditorGUILayout.TextField("Extension Code", extensionCode);
            EditorGUI.EndDisabledGroup();

            if (!isUseOriginal && canConvert)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Modified Date", DateTime.Parse(property.FindPropertyRelative("modifiedDate").stringValue).ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space(5);
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Regenerate Shader"), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Normal, EditorGUILayout.GetControlRect(GUILayout.Width(150)), () => GPUIShaderUtility.RegenerateShader(originalShaderName));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(4);
        }
    }
}
