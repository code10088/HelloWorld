// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    [CreateAssetMenu(menuName = "Rendering/GPU Instancer Pro/Detail Material Description", order = 711)]
    public class GPUIDetailMaterialDescription : GPUIMPBDescription
    {
        #region Serialized Properties
        public string mainTextureProperty;
        public string healthyColorProperty;
        public bool isDryColorActive;
        public string dryColorProperty;
        public bool isWavingTintActive;
        public string wavingTintProperty;
        public bool isBillboardActive;
        public string isBillboardProperty;
        public bool isHealthyDryNoiseTextureActive;
        public string healthyDryNoiseTextureProperty;
        #endregion Serialized Properties

        public static readonly int PROP_WindWaveSize = Shader.PropertyToID("_WindWaveSize");
        public static readonly int PROP_AmbientOcclusion = Shader.PropertyToID("_AmbientOcclusion");
        public static readonly int PROP_NoiseSpread = Shader.PropertyToID("_NoiseSpread");
        public static readonly int PROP_WindVector = Shader.PropertyToID("_WindVector");
        public static readonly int PROP_WindWavesOn = Shader.PropertyToID("_WindWavesOn");
        public static readonly int PROP_WindWaveSway = Shader.PropertyToID("_WindWaveSway");
        public static readonly int PROP_WindIdleSway = Shader.PropertyToID("_WindIdleSway");
        public static readonly int PROP_GradientContrastRatioTint = Shader.PropertyToID("_GradientContrastRatioTint");

        private int _mainTexturePropertyID;
        private int _healthyColorPropertyID;
        private int _dryColorPropertyID;
        private int _wavingTintPropertyID;
        private int _isBillboardPropertyID;
        private int _healthyDryNoiseTexturePropertyID;
        private bool _isDefaultShader;

        public void SetPropertyIDs()
        {
            _mainTexturePropertyID = Shader.PropertyToID(mainTextureProperty);
            _healthyColorPropertyID = Shader.PropertyToID(healthyColorProperty);
            if (!string.IsNullOrEmpty(dryColorProperty))
                _dryColorPropertyID = Shader.PropertyToID(dryColorProperty);
            if (!string.IsNullOrEmpty(wavingTintProperty))
                _wavingTintPropertyID = Shader.PropertyToID(wavingTintProperty);
            if (!string.IsNullOrEmpty(isBillboardProperty))
                _isBillboardPropertyID = Shader.PropertyToID(isBillboardProperty);
            if (!string.IsNullOrEmpty(healthyDryNoiseTextureProperty))
                _healthyDryNoiseTexturePropertyID = Shader.PropertyToID(healthyDryNoiseTextureProperty);

            _isDefaultShader = GPUITerrainConstants.IsDefaultDetailShader(shader);
        }

        public override void SetMPBValues(GPUIRenderSourceGroup rsg, GPUIManager manager, int prototypeIndex)
        {
            if (manager is GPUIDetailManager detailManager)
            {
                if (_mainTexturePropertyID == 0 || !_isDefaultShader)
                    SetPropertyIDs();
                GPUIDetailPrototypeData detailPrototypeData = detailManager.GetPrototypeData(prototypeIndex);
                if (detailPrototypeData != null && detailPrototypeData.detailTexture != null)
                {
                    rsg.AddMaterialPropertyOverride(_mainTexturePropertyID, detailPrototypeData.detailTexture, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(_healthyColorPropertyID, detailPrototypeData.healthyColor, -1, -1, true);
                    if (isDryColorActive && !string.IsNullOrEmpty(dryColorProperty))
                        rsg.AddMaterialPropertyOverride(_dryColorPropertyID, detailPrototypeData.dryColor, -1, -1, true);
                    if (isWavingTintActive && !string.IsNullOrEmpty(wavingTintProperty))
                        rsg.AddMaterialPropertyOverride(_wavingTintPropertyID, detailPrototypeData.windWaveTintColor, -1, -1, true);
                    if (isBillboardActive && !string.IsNullOrEmpty(isBillboardProperty))
                        rsg.AddMaterialPropertyOverride(_isBillboardPropertyID, detailPrototypeData.isBillboard ? 1 : 0, -1, -1, true);
                    if (isHealthyDryNoiseTextureActive && !string.IsNullOrEmpty(healthyDryNoiseTextureProperty))
                    {
                        if (detailPrototypeData.isOverrideHealthyDryNoiseTexture && detailPrototypeData.healthyDryNoiseTexture != null)
                            rsg.AddMaterialPropertyOverride(_healthyDryNoiseTexturePropertyID, detailPrototypeData.healthyDryNoiseTexture, -1, -1, true);
                        else if (detailManager.healthyDryNoiseTexture != null)
                            rsg.AddMaterialPropertyOverride(_healthyDryNoiseTexturePropertyID, detailManager.healthyDryNoiseTexture, -1, -1, true);
                    }
                }
                if (_isDefaultShader)
                {
                    rsg.AddMaterialPropertyOverride(PROP_WindWaveSize, detailPrototypeData.windWaveSize, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_AmbientOcclusion, detailPrototypeData.ambientOcclusion, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_NoiseSpread, detailPrototypeData.GetNoiseSpread(), -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_WindVector, detailManager.windVector, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_WindWavesOn, detailPrototypeData.windWavesOn ? 1 : 0, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_WindWaveSway, detailPrototypeData.windWaveSway, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_WindIdleSway, detailPrototypeData.windIdleSway, -1, -1, true);
                    rsg.AddMaterialPropertyOverride(PROP_GradientContrastRatioTint, new Vector4(detailPrototypeData.gradientPower, detailPrototypeData.contrast, detailPrototypeData.healthyDryRatio, detailPrototypeData.windWaveTint), -1, -1, true);
                }
            }
        }

        public void SetDefaultValues()
        {
            shader = GPUITerrainConstants.GetDefaultDetailShader();
            mainTextureProperty = "_MainTex";
            healthyColorProperty = "_HealthyColor";
            isDryColorActive = true;
            dryColorProperty = "_DryColor";
            isWavingTintActive = true;
            wavingTintProperty = "_WindWaveTintColor";
            isBillboardActive = true;
            isBillboardProperty = "_IsBillboard";
            isHealthyDryNoiseTextureActive = true;
            healthyDryNoiseTextureProperty = "_HealthyDryNoiseTexture";
        }
    }
}
