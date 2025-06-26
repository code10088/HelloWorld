// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace GPUInstancerPro.PrefabModule
{
    [CustomEditor(typeof(GPUIMaterialVariationDefinition))]
    public class GPUIMaterialVariationDefinitionEditor : GPUIEditor
    {
        private GPUIMaterialVariationDefinition _variationDefinition;
        private bool _propertiesFoldout = true;
        private static readonly List<ShaderPropertyType> IGNORE_TYPES = new List<ShaderPropertyType> { ShaderPropertyType.Texture };
        private static readonly string CUSTOM_OPTION = "<Custom>";

        protected override void OnEnable()
        {
            base.OnEnable();

            _variationDefinition = target as GPUIMaterialVariationDefinition;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            base.DrawContentGUI(contentElement);
            GPUIEditorTextUtility.TryGetGPUIText("materialVariationDefinition", out var gpuiText);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, contentElement);
        }

        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();
            _variationDefinition.material = (Material)EditorGUILayout.ObjectField("Material", _variationDefinition.material, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_variationDefinition);
            }
            if (_variationDefinition.material == null)
                return;
            EditorGUI.BeginChangeCheck();
            _variationDefinition.bufferName = EditorGUILayout.TextField("Buffer Name", _variationDefinition.bufferName);
            _variationDefinition.replacementShader = (Shader)EditorGUILayout.ObjectField("Variation Shader", _variationDefinition.replacementShader, typeof(Shader), false);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_variationDefinition);
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Include File", _variationDefinition.shaderIncludeFile, typeof(ShaderInclude), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.BeginHorizontal();
            GPUIEditorUtility.DrawColoredButton(new GUIContent("Generate Shader"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Normal, Rect.zero, GenerateShader);
            GPUIEditorUtility.DrawColoredButton(new GUIContent("Generate Include File"), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Normal, Rect.zero, GenerateHLSLIncludeFile);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
            _propertiesFoldout = EditorGUILayout.Foldout(_propertiesFoldout, "Variation Properties", true);

            if (_propertiesFoldout)
            {
                if (_variationDefinition.items == null)
                    _variationDefinition.items = new GPUIMVDefinitionItem[0];

                string[] propertyNames = _variationDefinition.material.shader.GetPropertyNames(IGNORE_TYPES);
                int customOptionIndex = propertyNames.Length;
                propertyNames = propertyNames.AddAndReturn(CUSTOM_OPTION);

                for (int i = 0; i < _variationDefinition.items.Length; i++)
                {
                    int selectedIndex = -1;
                    for (int p = 0; p < propertyNames.Length; p++)
                    {
                        if (propertyNames[p] == _variationDefinition.items[i].propertyName)
                        {
                            selectedIndex = p;
                            break;
                        }
                    }
                    if (selectedIndex < 0)
                        selectedIndex = customOptionIndex;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    selectedIndex = EditorGUILayout.Popup("", selectedIndex, propertyNames, GUILayout.MinWidth(10));
                    if (selectedIndex == customOptionIndex)
                        _variationDefinition.items[i].propertyName = EditorGUILayout.TextField(_variationDefinition.items[i].propertyName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selectedIndex != customOptionIndex)
                            _variationDefinition.items[i].propertyName = propertyNames[selectedIndex];

                        SetDefaultVariationType(_variationDefinition, i, GetPropertyIndex(_variationDefinition.material.shader, _variationDefinition.items[i].propertyName));
                        EditorUtility.SetDirty(_variationDefinition);
                    }
                    EditorGUI.BeginChangeCheck();
                    _variationDefinition.items[i].variationType = (GPUIMaterialVariationType)EditorGUILayout.EnumPopup("", _variationDefinition.items[i].variationType, GUILayout.MinWidth(10));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_variationDefinition);
                    }
                    int index = i;
                    GPUIEditorUtility.DrawColoredButton(new GUIContent("X"), GPUIEditorConstants.Colors.lightRed, Color.white, FontStyle.Normal, Rect.zero, () => _variationDefinition.items = _variationDefinition.items.RemoveAtAndReturn(index));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(4);
                Rect rect = GUILayoutUtility.GetRect(200, 20, GUILayout.ExpandWidth(false));
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Add Property"), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Normal, rect, AddProperty);
                EditorGUILayout.Space(4);
                GPUIEditorUtility.DrawIMGUIHelpText("Please make sure to click on the \"Generate Include File\" button after making changes to the properties for the changes to be applied to the shader.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        private void AddProperty()
        {
            int length = _variationDefinition.items.Length;
            Array.Resize(ref _variationDefinition.items, length + 1);

            SetDefaultVariationType(_variationDefinition, length, 0);
            EditorUtility.SetDirty(_variationDefinition);
        }

        public override string GetTitleText()
        {
            return "GPUI Material Variation Definition";
        }

        public static int GetPropertyIndex(Shader shader, string propertyName)
        {
            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyName(i) == propertyName)
                    return i;
            }
            return -1;
        }
        
        public static void SetDefaultVariationType(GPUIMaterialVariationDefinition variationDefinition, int variationIndex, int shaderPropertyIndex)
        {
            if (shaderPropertyIndex < 0 || variationIndex < 0 || variationDefinition.items.Length <= variationIndex)
                return;

            GPUIMVDefinitionItem definitionItem = variationDefinition.items[variationIndex];

            ShaderPropertyType propertyType = variationDefinition.material.shader.GetPropertyType(shaderPropertyIndex);
            definitionItem.propertyName = variationDefinition.material.shader.GetPropertyName(shaderPropertyIndex);
            switch (propertyType)
            {
                case ShaderPropertyType.Color:
                    definitionItem.variationType = GPUIMaterialVariationType.Color;
                    definitionItem.defaultValue = variationDefinition.material.GetColor(definitionItem.propertyName);
                    break;
                case ShaderPropertyType.Vector:
                    definitionItem.variationType = GPUIMaterialVariationType.Vector4;
                    definitionItem.defaultValue = variationDefinition.material.GetVector(definitionItem.propertyName);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    definitionItem.variationType = GPUIMaterialVariationType.Float;
                    definitionItem.defaultValue.x = variationDefinition.material.GetFloat(definitionItem.propertyName);
                    break;
                case ShaderPropertyType.Int:
                    definitionItem.variationType = GPUIMaterialVariationType.Integer;
                    definitionItem.defaultValue.x = variationDefinition.material.GetInt(definitionItem.propertyName);
                    break;
            }
            variationDefinition.items[variationIndex] = definitionItem;
            EditorUtility.SetDirty(variationDefinition);
        }

        private void GenerateShader()
        {
            GPUIMaterialVariationEditorUtility.GenerateShader(_variationDefinition);
        }

        private void GenerateHLSLIncludeFile()
        {
            GPUIMaterialVariationEditorUtility.GenerateHLSLIncludeFile(_variationDefinition);
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:BestPractices#Prefab_Manager_Material_Variations";
        }
    }
}
