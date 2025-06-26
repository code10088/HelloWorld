// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [CustomEditor(typeof(GPUIMaterialVariationInstance))]
    public class GPUIMaterialVariationInstanceEditor : GPUIEditor
    {
        private GPUIMaterialVariationInstance _variationInstance;

        protected override void OnEnable()
        {
            base.OnEnable();

            _variationInstance = target as GPUIMaterialVariationInstance;
        }

        public override void DrawIMGUIContainer()
        {
            EditorGUI.BeginChangeCheck();
            DrawIMGUISerializedProperty(serializedObject.FindProperty("variationDefinition"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            if (_variationInstance.variationDefinition != null)
            {
                if (_variationInstance.variationDefinition.items == null)
                    _variationInstance.variationDefinition.items = new GPUIMVDefinitionItem[0];
                if (_variationInstance.values == null)
                    _variationInstance.values = new Vector4[0];

                if (_variationInstance.variationDefinition.items != null && _variationInstance.variationDefinition.items.Length > _variationInstance.values.Length)
                {
                    int currentLength = _variationInstance.values.Length;
                    Array.Resize(ref _variationInstance.values, _variationInstance.variationDefinition.items.Length);
                    for (int i = currentLength; i < _variationInstance.values.Length; i++)
                        _variationInstance.values[i] = _variationInstance.variationDefinition.items[i].defaultValue;
                    EditorUtility.SetDirty(_variationInstance);
                }

                for (int i = 0; i < _variationInstance.variationDefinition.items.Length; i++)
                {
                    GPUIMVDefinitionItem definitionItem = _variationInstance.variationDefinition.items[i];
                    Vector4 value = _variationInstance.values[i];
                    EditorGUI.BeginChangeCheck();
                    switch (definitionItem.variationType)
                    {
                        case GPUIMaterialVariationType.Vector4:
                            value = EditorGUILayout.Vector4Field(definitionItem.propertyName, value);
                            break;
                        case GPUIMaterialVariationType.Color:
                            value = EditorGUILayout.ColorField(definitionItem.propertyName, value);
                            break;
                        case GPUIMaterialVariationType.Integer:
                            value.x = EditorGUILayout.IntField(definitionItem.propertyName, (int)value.x);
                            break;
                        case GPUIMaterialVariationType.Float:
                            value.x = EditorGUILayout.FloatField(definitionItem.propertyName, value.x);
                            break;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_variationInstance, "Variation value changed.");
                        _variationInstance.values[i] = value;
                        EditorUtility.SetDirty(_variationInstance);
                        _variationInstance.ApplyVariation();
                    }
                }
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Material Variation Instance";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:BestPractices#Prefab_Manager_Material_Variations";
        }
    }
}
