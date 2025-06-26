// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomPropertyDrawer(typeof(GPUIRendererData))]
    public class GPUIRendererDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            Foldout foldout = new();
            foldout.text = property.displayName.Replace("Element", "Renderer");
            var optionalRendererNoSP = property.FindPropertyRelative("optionalRendererNo");
            if (optionalRendererNoSP.intValue > 0)
                foldout.text += " [Optional:" + optionalRendererNoSP.intValue + "]";
            //foldout.value = false;

            PropertyField meshField = new(property.FindPropertyRelative("rendererMesh"));
            meshField.label = "Mesh";
            foldout.Add(meshField);
            PropertyField materialsField = new(property.FindPropertyRelative("rendererMaterials"));
            materialsField.label = "Materials";
            foldout.Add(materialsField);
            if (!Application.isPlaying)
                materialsField.RegisterCallback<SerializedPropertyChangeEvent>(OnMaterialsChanged);
            if (Application.isPlaying)
            {
                GPUIRendererData rendererData = property.GetTargetObjectFromPath() as GPUIRendererData;
                if (rendererData.replacementMesh != null)
                {
                    ObjectField of = new("Replacement Mesh");
                    of.value = rendererData.replacementMesh;
                    foldout.Add(of);
                }
                if (rendererData.replacementMaterials != null && rendererData.replacementMaterials.Length > 0)
                {
                    Foldout replacementMatsFoldout = new Foldout();
                    replacementMatsFoldout.text = "Replacement Materials";
                    replacementMatsFoldout.value = false;
                    for (int i = 0; i < rendererData.replacementMaterials.Length; i++)
                    {
                        ObjectField of = new("Element " + i);
                        of.value = rendererData.replacementMaterials[i];
                        replacementMatsFoldout.Add(of);
                    }
                    foldout.Add(replacementMatsFoldout);
                }
            }
            PropertyField shadowCastingModeField = new(property.FindPropertyRelative("shadowCastingMode"));
            foldout.Add(shadowCastingModeField);

            LayerField layerField = new("Layer");
            layerField.BindProperty(property.FindPropertyRelative("layer"));
            foldout.Add(layerField);
            Foldout transformOffsetFoldout = new();
            transformOffsetFoldout.text = "Transform Offset";
            foldout.Add(GPUIEditorUtility.DrawMatrix4x4Fields(property.FindPropertyRelative("transformOffset")));

            container.Add(foldout);
            //container.SetEnabled(!Application.isPlaying);

            return container;
        }

        private void OnMaterialsChanged(SerializedPropertyChangeEvent evt)
        {
            if (evt.changedProperty.propertyType != SerializedPropertyType.ObjectReference || evt.changedProperty.objectReferenceValue == null)
                return;
            Material material = evt.changedProperty.objectReferenceValue as Material;
            if (material != null && material.shader != null)
                GPUIShaderUtility.SetupShaderForGPUI(material.shader, null, false);
        }
    }
}
