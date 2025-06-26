// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancerPro.TerrainModule
{
    [CustomEditor(typeof(GPUIDetailMaterialDescription))]
    public class GPUIDetailMaterialDescriptionEditor : GPUIEditor
    {
        private GPUIDetailMaterialDescription _detailMaterialDescription;

        protected override void OnEnable()
        {
            base.OnEnable();

            _detailMaterialDescription = target as GPUIDetailMaterialDescription;
        }

        public override void DrawIMGUIContainer()
        {
            bool changed = false;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shader"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
            if (_detailMaterialDescription.shader != null && _detailMaterialDescription.shader != null)
            {
                string[] texturePropertyNames = _detailMaterialDescription.shader.GetPropertyNamesForType(ShaderPropertyType.Texture);
                string[] colorPropertyNames = _detailMaterialDescription.shader.GetPropertyNamesForType(ShaderPropertyType.Color);
                string[] floatPropertyNames = _detailMaterialDescription.shader.GetPropertyNamesForType(ShaderPropertyType.Float);

                int mainTextureSelectedIndex = 0;
                int healthyDryNoiseTextureSelectedIndex = 0;
                for (int i = 0; i < texturePropertyNames.Length; i++)
                {
                    if (_detailMaterialDescription.mainTextureProperty == texturePropertyNames[i])
                        mainTextureSelectedIndex = i;
                    if (_detailMaterialDescription.healthyDryNoiseTextureProperty == texturePropertyNames[i])
                        healthyDryNoiseTextureSelectedIndex = i;
                }

                int healthyColorSelectedIndex = 0;
                int dryColorSelectedIndex = 0;
                int wavingTintSelectedIndex = 0;
                for (int i = 0; i < colorPropertyNames.Length; i++)
                {
                    if (_detailMaterialDescription.healthyColorProperty == colorPropertyNames[i])
                        healthyColorSelectedIndex = i;
                    if (_detailMaterialDescription.dryColorProperty == colorPropertyNames[i])
                        dryColorSelectedIndex = i;
                    if (_detailMaterialDescription.wavingTintProperty == colorPropertyNames[i])
                        wavingTintSelectedIndex = i;
                }

                int isBillboardSelectedIndex = 0;
                for (int i = 0; i < floatPropertyNames.Length; i++)
                {
                    if (_detailMaterialDescription.isBillboardProperty == floatPropertyNames[i])
                    {
                        isBillboardSelectedIndex = i;
                        break;
                    }
                }

                EditorGUI.BeginChangeCheck();
                mainTextureSelectedIndex = EditorGUILayout.Popup("Main Texture Property", mainTextureSelectedIndex, texturePropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.mainTextureProperty = texturePropertyNames[mainTextureSelectedIndex];
                    changed = true;
                }

                EditorGUI.BeginChangeCheck();
                healthyColorSelectedIndex = EditorGUILayout.Popup("Healthy Color Property", healthyColorSelectedIndex, colorPropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.healthyColorProperty = colorPropertyNames[healthyColorSelectedIndex];
                    changed = true;
                }

                EditorGUILayout.BeginHorizontal();
                _detailMaterialDescription.isDryColorActive = EditorGUILayout.ToggleLeft("", _detailMaterialDescription.isDryColorActive, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(!_detailMaterialDescription.isDryColorActive);
                EditorGUI.BeginChangeCheck();
                dryColorSelectedIndex = EditorGUILayout.Popup("Dry Color Property", dryColorSelectedIndex, colorPropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.dryColorProperty = colorPropertyNames[dryColorSelectedIndex];
                    changed = true;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _detailMaterialDescription.isWavingTintActive = EditorGUILayout.ToggleLeft("", _detailMaterialDescription.isWavingTintActive, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(!_detailMaterialDescription.isWavingTintActive);
                EditorGUI.BeginChangeCheck();
                wavingTintSelectedIndex = EditorGUILayout.Popup("Waving Tint Property", wavingTintSelectedIndex, colorPropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.wavingTintProperty = colorPropertyNames[wavingTintSelectedIndex];
                    changed = true;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _detailMaterialDescription.isBillboardActive = EditorGUILayout.ToggleLeft("", _detailMaterialDescription.isBillboardActive, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(!_detailMaterialDescription.isBillboardActive);
                EditorGUI.BeginChangeCheck();
                isBillboardSelectedIndex = EditorGUILayout.Popup("Is Billboard Property", isBillboardSelectedIndex, floatPropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.isBillboardProperty = floatPropertyNames[isBillboardSelectedIndex];
                    changed = true;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _detailMaterialDescription.isHealthyDryNoiseTextureActive = EditorGUILayout.ToggleLeft("", _detailMaterialDescription.isHealthyDryNoiseTextureActive, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(!_detailMaterialDescription.isHealthyDryNoiseTextureActive);
                EditorGUI.BeginChangeCheck();
                healthyDryNoiseTextureSelectedIndex = EditorGUILayout.Popup("Healthy/Dry Noise Property", healthyDryNoiseTextureSelectedIndex, texturePropertyNames);
                if (EditorGUI.EndChangeCheck())
                {
                    _detailMaterialDescription.healthyDryNoiseTextureProperty = texturePropertyNames[healthyDryNoiseTextureSelectedIndex];
                    changed = true;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            if (changed)
            {
                EditorUtility.SetDirty(_detailMaterialDescription);
                if (GPUIRenderingSystem.IsActive)
                {
                    foreach (var item in GPUIRenderingSystem.Instance.ActiveGPUIManagers)
                    {
                        if (item is GPUIDetailManager detailManager)
                        {
                            if (detailManager.IsInitialized) // re-initialize detail manager
                                detailManager.Initialize();
                            break;
                        }
                    }
                }
            }
        }

        public override string GetTitleText()
        {
            return "GPUI Detail Material Description";
        }
    }
}
