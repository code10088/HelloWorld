// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    [CreateAssetMenu(menuName = "Rendering/GPU Instancer Pro/Profile", order = 511)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#Profile_Settings")]
    public class GPUIProfile : ScriptableObject, IGPUIParameterBufferData
    {
        #region Serialized Properties

        // Switches
        public bool isShadowCasting = true;
        public bool isDistanceCulling = true;
        public bool isFrustumCulling = true;
        public bool isOcclusionCulling = true;
        public bool isShadowFrustumCulling = false;
        public bool isShadowOcclusionCulling = false;
        public bool isShadowDistanceCulling = true;
        public bool isLODCrossFade = false;
        public bool isAnimateCrossFade = true;

        // Variables
        [Range(0f, 100f)]
        public float minCullingDistance = 0;
        [Range(0f, 100f)]
        public float minShadowCullingDistance = 20f;
        public Vector2 minMaxDistance = new(0, 500);
        [Range(0f, 10f)]
        public float frustumOffset = 0.01f;
        [Range(0f, 0.01f)]
        public float occlusionOffset = 0.0001f;
        [Range(0f, 5f)]
        public float occlusionOffsetSizeMultiplier = 0f;
        [Range(0f, 100f)]
        public float shadowFrustumOffset = 10f;
        [Range(0f, 0.01f)]
        public float shadowOcclusionOffset = 0.001f;
        [Range(0f, 5f)]
        public float shadowOcclusionOffsetSizeMultiplier = 0.5f;
        [Range(1, 3)]
        public int occlusionAccuracy = 1;
        public Vector3 boundsOffset;
        [Range(0.01f, 10f)]
        public float lodBiasAdjustment = 1;
        [Range(0f, 1000f)]
        public float customShadowDistance = 0;
        public float[] shadowLODMap = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        [Range(0.1f, 20f)]
        public float lodCrossFadeAnimateSpeed = 4f;
        [Range(0, 7)]
        public int maximumLODLevel = 0;

        // Other
        public bool enablePerObjectMotionVectors;
        public GPUILightProbeSetting lightProbeSetting = GPUILightProbeSetting.Single;
        public Vector3 lightProbePositionOffset;
        //public DepthSortMode depthSortMode;

        [SerializeField]
        [HideInInspector]
        public bool isDefaultProfile;

        #endregion Serialized Properties

        #region Default Profile

        private static GPUIProfile _defaultProfile;
        public static GPUIProfile DefaultProfile
        {
            get
            {
                if (_defaultProfile == null)
                {
#if UNITY_EDITOR
                    _defaultProfile = AssetDatabase.LoadAssetAtPath<GPUIProfile>(GPUIConstants.GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_PROFILES + GPUIConstants.FILE_DEFAULT_PROFILE + ".asset");
                    if (_defaultProfile == null)
                    {
#endif
                        _defaultProfile = ScriptableObject.CreateInstance<GPUIProfile>();
                        _defaultProfile.isDefaultProfile = true;
#if UNITY_EDITOR
                    }
#endif
                }
                return _defaultProfile;
            }
        }
        public static GPUIProfile defaultGPUSkinningProfile;

        #endregion Default Profile

        public static GPUIProfile CreateNewProfile(string name, GPUIProfile copyFrom = null)
        {
            GPUIProfile profile;
            if (copyFrom == null)
                profile = ScriptableObject.CreateInstance<GPUIProfile>();
            else
                profile = ScriptableObject.Instantiate(copyFrom);
            profile.name = string.IsNullOrEmpty(name) ? "New GPUI Profile" : name + " Profile";
            profile.isDefaultProfile = false;

#if UNITY_EDITOR
            profile.SaveAsAsset(GPUIConstants.GetProfilesPath(), profile.name + ".asset", true);
#endif
            return profile;
        }

        public void CopyValuesFrom(GPUIProfile copyFrom)
        {
            isShadowCasting = copyFrom.isShadowCasting;
            isDistanceCulling = copyFrom.isDistanceCulling;
            isFrustumCulling = copyFrom.isFrustumCulling;
            isOcclusionCulling = copyFrom.isOcclusionCulling;
            isShadowFrustumCulling = copyFrom.isShadowFrustumCulling;
            isShadowOcclusionCulling = copyFrom.isShadowOcclusionCulling;
            isShadowDistanceCulling = copyFrom.isShadowDistanceCulling;
            isLODCrossFade = copyFrom.isLODCrossFade;
            isAnimateCrossFade = copyFrom.isAnimateCrossFade;
            minCullingDistance = copyFrom.minCullingDistance;
            minShadowCullingDistance = copyFrom.minShadowCullingDistance;
            minMaxDistance = copyFrom.minMaxDistance;
            frustumOffset = copyFrom.frustumOffset;
            shadowFrustumOffset = copyFrom.shadowFrustumOffset;
            shadowOcclusionOffset = copyFrom.shadowOcclusionOffset;
            occlusionAccuracy = copyFrom.occlusionAccuracy;
            boundsOffset = copyFrom.boundsOffset;
            lodBiasAdjustment = copyFrom.lodBiasAdjustment;
            customShadowDistance = copyFrom.customShadowDistance;
            shadowLODMap = copyFrom.shadowLODMap;
            lodCrossFadeAnimateSpeed = copyFrom.lodCrossFadeAnimateSpeed;
            maximumLODLevel = copyFrom.maximumLODLevel;
            enablePerObjectMotionVectors = copyFrom.enablePerObjectMotionVectors;
            lightProbeSetting = copyFrom.lightProbeSetting;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #region Parameter Buffer

        public void SetParameterBufferData()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIDataBuffer<float> parameterBuffer = GPUIRenderingSystem.Instance.ParameterBuffer;

            if (TryGetParameterBufferIndex(out int startIndex))
            {
                parameterBuffer[startIndex + 0] = minCullingDistance;
                parameterBuffer[startIndex + 1] = minMaxDistance.x;
                parameterBuffer[startIndex + 2] = GetMaxDistance();
                parameterBuffer[startIndex + 3] = GetFrustumOffset();
                parameterBuffer[startIndex + 4] = GetOcclusionOffset();
                parameterBuffer[startIndex + 5] = occlusionAccuracy;
                parameterBuffer[startIndex + 6] = boundsOffset.x;
                parameterBuffer[startIndex + 7] = boundsOffset.y;
                parameterBuffer[startIndex + 8] = boundsOffset.z;
                parameterBuffer[startIndex + 9] = GetLODBiasAdjustment();
                parameterBuffer[startIndex + 10] = GetShadowDistance();
                for (int i = 0; i < 8; i++)
                {
                    parameterBuffer[startIndex + 11 + i] = shadowLODMap[i];
                }
                //parameterBuffer[startIndex + 19] = lodCrossFadeTransitionWidth;
                parameterBuffer[startIndex + 20] = lodCrossFadeAnimateSpeed;
                parameterBuffer[startIndex + 21] = GetShadowFrustumOffset();
                parameterBuffer[startIndex + 22] = GetShadowOcclusionOffset();
                parameterBuffer[startIndex + 23] = minShadowCullingDistance;
                parameterBuffer[startIndex + 24] = occlusionOffsetSizeMultiplier;
                parameterBuffer[startIndex + 25] = shadowOcclusionOffsetSizeMultiplier;
            }
            else
            {
                GPUIRenderingSystem.Instance.ParameterBufferIndexes.Add(this, parameterBuffer.Length);

                parameterBuffer.Add(minCullingDistance, minMaxDistance.x, GetMaxDistance(), GetFrustumOffset(), GetOcclusionOffset(), occlusionAccuracy, boundsOffset.x, boundsOffset.y, boundsOffset.z, GetLODBiasAdjustment(), GetShadowDistance());
                parameterBuffer.Add(shadowLODMap);
                parameterBuffer.Add(0/*unused*/, lodCrossFadeAnimateSpeed, GetShadowFrustumOffset(), GetShadowOcclusionOffset(), minShadowCullingDistance, occlusionOffsetSizeMultiplier, shadowOcclusionOffsetSizeMultiplier);
            }
        }

        public bool TryGetParameterBufferIndex(out int index)
        {
            return GPUIRenderingSystem.Instance.ParameterBufferIndexes.TryGetValue(this, out index);
        }

        #endregion Parameter Buffer

        #region Getters/Setters

        public float GetMaxDistance()
        {
            if (isDistanceCulling)
                return minMaxDistance.y;
            return -1f;
        }

        public float GetLODBiasAdjustment()
        {
            return lodBiasAdjustment > 0f ? lodBiasAdjustment : 1f;
        }

        public float GetShadowDistance()
        {
            if (!isShadowDistanceCulling)
                return 0;
            if (customShadowDistance > 0f)
                return customShadowDistance;
            return GPUIRuntimeSettings.Instance.GetDefaultShadowDistance();
        }

        public float GetFrustumOffset()
        {
            if (!isFrustumCulling)
                return -1f;
            return frustumOffset;
        }

        public float GetOcclusionOffset()
        {
            if (!isOcclusionCulling)
                return -1f;
            return occlusionOffset;
        }

        public float GetShadowFrustumOffset()
        {
            if (!isShadowFrustumCulling)
                return -1f;
            return shadowFrustumOffset;
        }

        public float GetShadowOcclusionOffset()
        {
            if (!isShadowOcclusionCulling)
                return -1f;
            return shadowOcclusionOffset;
        }

        public void SetShadowFrustumCulling(bool value)
        {
            isShadowFrustumCulling = value;
            SetParameterBufferData();
        }

        public void SetShadowFrustumOffset(float value)
        {
            shadowFrustumOffset = value;
            SetParameterBufferData();
        }

        public void SetShadowOcclusionCulling(bool value)
        {
            isShadowOcclusionCulling = value;
            SetParameterBufferData();
        }

        public void SetShadowOcclusionOffset(float value)
        {
            shadowOcclusionOffset = value;
            SetParameterBufferData();
        }

        public void SetShadowMinCullingDistance(float value)
        {
            minShadowCullingDistance = value;
            SetParameterBufferData();
        }

        public void SetLightProbeSetting(int value)
        {
            if (value < 0 || value > 2)
                return;
            lightProbeSetting = (GPUILightProbeSetting)value;
        }

        public bool HasLODLevelShadows(int lodLevel)
        {
            for (int i = 0; i < 8; i++)
            {
                if (shadowLODMap[i] == lodLevel)
                    return true;
            }
            return false;
        }

        #endregion Getters/Setters

        public enum DepthSortMode
        {
            None = 0,
            FrontToBack = 1,
            BackToFront = 2
        }
    }

    public enum GPUILightProbeSetting
    {
        Off = 0,
        Single = 1,
        PerInstance = 2
    }
}