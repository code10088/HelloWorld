// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIProfile))]
    public class GPUIProfileEditor : GPUIEditor
    {
        private GPUIProfile _profile;

        protected override void OnEnable()
        {
            base.OnEnable();

            _profile = (GPUIProfile)target;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawContentGUI(contentElement, serializedObject, _helpBoxes);

            if (!Application.isPlaying)
            {
                Button createProfileButton = new(() => CreateNewProfile(_profile));
                createProfileButton.text = "Create New Profile";
                createProfileButton.enableRichText = true;
                createProfileButton.style.marginLeft = 10;
                createProfileButton.style.backgroundColor = GPUIEditorConstants.Colors.green;
                createProfileButton.style.color = Color.white;
                createProfileButton.focusable = false;
                contentElement.Add(createProfileButton);
            }
        }

        public static void DrawContentGUI(VisualElement contentElement, SerializedObject serializedObject, List<GPUIHelpBox> helpBoxes)
        {
            bool isEnabled = !serializedObject.FindProperty("isDefaultProfile").boolValue;
            if (!isEnabled)
                contentElement.Add(new GPUIHelpBox("Create a new Profile to edit the settings.", HelpBoxMessageType.Info));

            VisualTreeAsset profileUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIProfileUI.uxml");

            VisualElement profileVE = new();
            profileUITemplate.CloneTree(profileVE);
            profileVE.SetEnabled(isEnabled);
            contentElement.Add(profileVE);

            VisualElement cullingContent = profileVE.Q("CullingContent");
            VisualElement shadowCullingContent = profileVE.Q("ShadowCullingContent");
            VisualElement shadowCullingContentChildren = new VisualElement();
            VisualElement lodContent = profileVE.Q("LODContent");
            VisualElement otherContent = profileVE.Q("OtherContent");

            #region Culling
            cullingContent.Add(DrawSerializedProperty(serializedObject.FindProperty("isDistanceCulling"), helpBoxes, out PropertyField isDistanceCullingPF));
            VisualElement minMaxDistanceVE = DrawSerializedProperty(serializedObject.FindProperty("minMaxDistance"), helpBoxes);
            cullingContent.Add(minMaxDistanceVE);
            isDistanceCullingPF.RegisterValueChangeCallback((evt) => {
                minMaxDistanceVE.SetVisible(evt.changedProperty.boolValue);
            });

            SerializedProperty isFrustumCullingSP = serializedObject.FindProperty("isFrustumCulling");
            cullingContent.Add(DrawSerializedProperty(isFrustumCullingSP, helpBoxes, out PropertyField isFrustumCullingPF));
            VisualElement frustumOffsetVE = DrawSerializedProperty(serializedObject.FindProperty("frustumOffset"), helpBoxes);
            cullingContent.Add(frustumOffsetVE);

            SerializedProperty isOcclusionCullingSP = serializedObject.FindProperty("isOcclusionCulling");
            cullingContent.Add(DrawSerializedProperty(isOcclusionCullingSP, helpBoxes, out PropertyField isOcclusionCullingPF));
            VisualElement occlusionOffsetVE = DrawSerializedProperty(serializedObject.FindProperty("occlusionOffset"), helpBoxes);
            cullingContent.Add(occlusionOffsetVE);
            VisualElement occlusionOffsetSizeMultiplierVE = DrawSerializedProperty(serializedObject.FindProperty("occlusionOffsetSizeMultiplier"), helpBoxes);
            cullingContent.Add(occlusionOffsetSizeMultiplierVE);
            VisualElement occlusionAccuracyVE = DrawSerializedProperty(serializedObject.FindProperty("occlusionAccuracy"), helpBoxes);
            cullingContent.Add(occlusionAccuracyVE);

            cullingContent.Add(DrawSerializedProperty(serializedObject.FindProperty("minCullingDistance"), helpBoxes));
            cullingContent.Add(DrawSerializedProperty(serializedObject.FindProperty("boundsOffset"), helpBoxes, out PropertyField boundsOffsetPF));
            boundsOffsetPF.RegisterValueChangeCallback((evt) =>
            {
                if (GPUIRenderingSystem.IsActive)
                    GPUIRenderingSystem.Instance.LODGroupDataProvider.RecalculateLODGroupBounds();
            });
            #endregion Culling

            #region Shadow Culling
            shadowCullingContent.Add(DrawSerializedProperty(serializedObject.FindProperty("isShadowCasting"), helpBoxes, out PropertyField isShadowCastingPF));
            shadowCullingContent.Add(shadowCullingContentChildren);
            isShadowCastingPF.RegisterValueChangeCallback((evt) => {
                shadowCullingContentChildren.SetVisible(evt.changedProperty.boolValue);
            });

            VisualElement isShadowDistanceCullingVE = DrawSerializedProperty(serializedObject.FindProperty("isShadowDistanceCulling"), helpBoxes, out PropertyField isShadowDistanceCullingPF);
            shadowCullingContentChildren.Add(isShadowDistanceCullingVE);
            VisualElement customShadowDistanceVE = DrawSerializedProperty(serializedObject.FindProperty("customShadowDistance"), helpBoxes);
            shadowCullingContentChildren.Add(customShadowDistanceVE);
            isShadowDistanceCullingPF.RegisterValueChangeCallback((evt) => {
                customShadowDistanceVE.SetVisible(evt.changedProperty.boolValue);
            });

            SerializedProperty isShadowFrustumCullingSP = serializedObject.FindProperty("isShadowFrustumCulling");
            VisualElement isShadowFrustumCullingVE = DrawSerializedProperty(isShadowFrustumCullingSP, helpBoxes, out PropertyField isShadowFrustumCullingPF);
            shadowCullingContentChildren.Add(isShadowFrustumCullingVE);
            VisualElement shadowFrustumOffsetVE = DrawSerializedProperty(serializedObject.FindProperty("shadowFrustumOffset"), helpBoxes);
            shadowCullingContentChildren.Add(shadowFrustumOffsetVE);
            isShadowFrustumCullingPF.RegisterValueChangeCallback((evt) => {
                shadowFrustumOffsetVE.SetVisible(evt.changedProperty.boolValue);
            });

            SerializedProperty isShadowOcclusionCullingSP = serializedObject.FindProperty("isShadowOcclusionCulling");
            VisualElement isShadowOcclusionCullingVE = DrawSerializedProperty(isShadowOcclusionCullingSP, helpBoxes, out PropertyField isShadowOcclusionCullingPF);
            shadowCullingContentChildren.Add(isShadowOcclusionCullingVE);
            VisualElement shadowOcclusionOffsetVE = DrawSerializedProperty(serializedObject.FindProperty("shadowOcclusionOffset"), helpBoxes);
            shadowCullingContentChildren.Add(shadowOcclusionOffsetVE);
            VisualElement shadowOcclusionOffsetSizeMultiplierVE = DrawSerializedProperty(serializedObject.FindProperty("shadowOcclusionOffsetSizeMultiplier"), helpBoxes);
            shadowCullingContentChildren.Add(shadowOcclusionOffsetSizeMultiplierVE);

            isShadowOcclusionCullingPF.RegisterValueChangeCallback((evt) => {
                shadowOcclusionOffsetVE.SetVisible(evt.changedProperty.boolValue);
                shadowOcclusionOffsetSizeMultiplierVE.SetVisible(evt.changedProperty.boolValue);
            });

            VisualElement minShadowCullingDistanceVE = DrawSerializedProperty(serializedObject.FindProperty("minShadowCullingDistance"), helpBoxes);
            shadowCullingContentChildren.Add(minShadowCullingDistanceVE);
            #endregion Shadow Culling

            isFrustumCullingPF.RegisterValueChangeCallback((evt) => {
                frustumOffsetVE.SetVisible(evt.changedProperty.boolValue);
                shadowFrustumOffsetVE.SetVisible(evt.changedProperty.boolValue && isShadowFrustumCullingSP.boolValue);
            });
            isOcclusionCullingPF.RegisterValueChangeCallback((evt) => {
                occlusionOffsetVE.SetVisible(evt.changedProperty.boolValue);
                occlusionOffsetSizeMultiplierVE.SetVisible(evt.changedProperty.boolValue);
                occlusionAccuracyVE.SetVisible(evt.changedProperty.boolValue);
                shadowOcclusionOffsetVE.SetVisible(evt.changedProperty.boolValue && isShadowFrustumCullingSP.boolValue);
                shadowOcclusionOffsetSizeMultiplierVE.SetVisible(evt.changedProperty.boolValue && isShadowFrustumCullingSP.boolValue);
            });

            #region LOD
            SerializedProperty isLODCrossFadeSP = serializedObject.FindProperty("isLODCrossFade");
            lodContent.Add(DrawSerializedProperty(isLODCrossFadeSP, helpBoxes, out PropertyField isLODCrossFadePF));
            SerializedProperty isAnimateCrossFadeSP = serializedObject.FindProperty("isAnimateCrossFade");
            VisualElement isAnimateCrossFadeVE = DrawSerializedProperty(isAnimateCrossFadeSP, helpBoxes, out PropertyField isAnimateCrossFadePF);
            lodContent.Add(isAnimateCrossFadeVE);
            VisualElement lodCrossFadeAnimateSpeedVE = DrawSerializedProperty(serializedObject.FindProperty("lodCrossFadeAnimateSpeed"), helpBoxes);
            lodContent.Add(lodCrossFadeAnimateSpeedVE);
            isAnimateCrossFadePF.RegisterValueChangeCallback((evt) => {
                lodCrossFadeAnimateSpeedVE.SetVisible(isLODCrossFadeSP.boolValue && evt.changedProperty.boolValue);
            });
            isLODCrossFadePF.RegisterValueChangeCallback((evt) => {
                isAnimateCrossFadeVE.SetVisible(evt.changedProperty.boolValue);
                lodCrossFadeAnimateSpeedVE.SetVisible(evt.changedProperty.boolValue && isAnimateCrossFadeSP.boolValue);
            });
            lodContent.Add(DrawSerializedProperty(serializedObject.FindProperty("lodBiasAdjustment"), helpBoxes));
            lodContent.Add(DrawSerializedProperty(serializedObject.FindProperty("maximumLODLevel"), helpBoxes));

            if (isEnabled)
            {
                GPUIEditorTextUtility.TryGetGPUIText("shadowLODMap", out GPUIEditorTextUtility.GPUIText gpuiText);

                SerializedProperty shadowLODMapSP = serializedObject.FindProperty(gpuiText.codeText);
                Foldout shadowLODMapFoldout = new();
                shadowLODMapFoldout.text = gpuiText.title;
                shadowLODMapFoldout.tooltip = gpuiText.tooltip;
                shadowLODMapFoldout.value = false;
                for (int i = 0; i < 8; i++)
                {
                    SerializedProperty element = shadowLODMapSP.GetArrayElementAtIndex(i);
                    IntegerField field = new("LOD " + i + " Shadow");
                    field.value = (int)element.floatValue;
                    field.RegisterValueChangedCallback((evt) => SetShadowLODMapValue(evt, element));
                    shadowLODMapFoldout.Add(field);
                }
                lodContent.Add(shadowLODMapFoldout);
                GPUIEditorUtility.DrawHelpText(helpBoxes, gpuiText, lodContent);
            }
            #endregion LOD

            #region Other
            var lightProbeSettingSP = serializedObject.FindProperty("lightProbeSetting");
            int lightProbeSettingValue = lightProbeSettingSP.intValue;
            otherContent.Add(DrawSerializedProperty(lightProbeSettingSP, helpBoxes, out var lightProbeSettingPF));

            var lightProbePositionOffsetSP = serializedObject.FindProperty("lightProbePositionOffset");
            Vector3 lightProbePositionOffsetValue = lightProbePositionOffsetSP.vector3Value;
            var lightProbePositionOffsetVE = DrawSerializedProperty(lightProbePositionOffsetSP, helpBoxes, out var lightProbePositionOffsetPF);
            otherContent.Add(lightProbePositionOffsetVE);
            lightProbePositionOffsetVE.SetVisible(lightProbeSettingValue == 2);
            var perInstanceLPAPVWarning = GPUIEditorUtility.CreateGPUIHelpBox("perInstanceLightProbesAPVWarning", null, null, HelpBoxMessageType.Warning);
            otherContent.Add(perInstanceLPAPVWarning);
            perInstanceLPAPVWarning.SetVisible(lightProbeSettingValue == 2 && GPUIRuntimeSettings.IsAdaptiveProbeVolumesEnabled());
            var perInstanceLPDisabledWarning = GPUIEditorUtility.CreateGPUIHelpBox("perInstanceLightProbesDisabledWarning", GPUIEditorSettings.Instance, null, HelpBoxMessageType.Warning);
            otherContent.Add(perInstanceLPDisabledWarning);
            perInstanceLPDisabledWarning.SetVisible(lightProbeSettingValue == 2 && GPUIEditorSettings.Instance.stripPerInstanceLightProbeVariants);
            lightProbeSettingPF.RegisterValueChangeCallback((evt) =>
            {
                if (evt.target == lightProbeSettingPF && lightProbeSettingSP.intValue != lightProbeSettingValue)
                {
                    GPUIRenderingSystem.OnLightProbesUpdated();
                    lightProbeSettingValue = lightProbeSettingSP.intValue;
                    lightProbePositionOffsetVE.SetVisible(lightProbeSettingValue == 2);
                    perInstanceLPAPVWarning.SetVisible(lightProbeSettingValue == 2 && GPUIRuntimeSettings.IsAdaptiveProbeVolumesEnabled());
                    perInstanceLPDisabledWarning.SetVisible(lightProbeSettingValue == 2 && GPUIEditorSettings.Instance.stripPerInstanceLightProbeVariants);
                }
            });
            lightProbePositionOffsetPF.RegisterValueChangeCallback((evt) =>
            {
                if (evt.target == lightProbePositionOffsetPF && lightProbePositionOffsetSP.vector3Value != lightProbePositionOffsetValue)
                {
                    GPUIRenderingSystem.OnLightProbesUpdated();
                    lightProbePositionOffsetValue = lightProbePositionOffsetSP.vector3Value;
                }
            });

            //otherContent.Add(DrawSerializedProperty(serializedObject.FindProperty("depthSortMode"), helpBoxes));

            if (!GPUIRuntimeSettings.Instance.IsBuiltInRP)
            {
                var enablePerObjectMotionVectorsSP = serializedObject.FindProperty("enablePerObjectMotionVectors");
                VisualElement enablePerObjectMotionVectorsVE = DrawSerializedProperty(enablePerObjectMotionVectorsSP, helpBoxes, out var enablePerObjectMotionVectorsPF);
                enablePerObjectMotionVectorsVE.SetEnabled(isEnabled && !Application.isPlaying);
                otherContent.Add(enablePerObjectMotionVectorsVE);
                var perObjectMotionVectorsDisabledWarning = GPUIEditorUtility.CreateGPUIHelpBox("perObjectMotionVectorsDisabledWarning", GPUIEditorSettings.Instance, null, HelpBoxMessageType.Warning);
                otherContent.Add(perObjectMotionVectorsDisabledWarning);
                perObjectMotionVectorsDisabledWarning.SetVisible(enablePerObjectMotionVectorsSP.boolValue && GPUIEditorSettings.Instance.stripObjectMotionVectorVariants);
                enablePerObjectMotionVectorsPF.RegisterValueChangeCallback((evt) =>
                {
                    perObjectMotionVectorsDisabledWarning.SetVisible(enablePerObjectMotionVectorsSP.boolValue && GPUIEditorSettings.Instance.stripObjectMotionVectorVariants);
                });
            }
            #endregion

            profileVE.RegisterCallback<SerializedPropertyChangeEvent>((evt) => OnValueChanged(serializedObject));
        }

        private static void SetShadowLODMapValue(ChangeEvent<int> evt, SerializedProperty element)
        {
            int val = evt.newValue;
            if (val > 7)
                val = 7;
            else if (val < 0)
                val = 0;
            element.floatValue = val;
            ((IntegerField)evt.currentTarget).SetValueWithoutNotify(val);
            element.serializedObject.ApplyModifiedProperties();
            element.serializedObject.Update();
            (element.serializedObject.targetObject as GPUIProfile).SetParameterBufferData();
        }

        private static void OnValueChanged(SerializedObject serializedObject)
        {
            (serializedObject.targetObject as GPUIProfile).SetParameterBufferData();
        }

        private static void CreateNewProfile(GPUIProfile profile)
        {
            GPUIProfile newProfile = GPUIProfile.CreateNewProfile(null, profile);
            Selection.activeObject = newProfile;
        }

        public override string GetTitleText()
        {
            return "GPUI Profile";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#Profile_Settings";
        }
    }
}
