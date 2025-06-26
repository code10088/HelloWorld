// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    [Serializable]
    public class GPUIDetailPrototypeData : GPUIPrototypeData
    {
        #region Serialized Properties

        public GPUIDetailMaterialDescription mpbDescription;

        public Texture2D detailTexture;
        public float noiseSpread = 0.1f;

        #region Shader Properties
        public Color healthyColor = Color.white;
        public Color dryColor = Color.white;

        public bool isBillboard;

        [Range(0f, 1f)]
        public float ambientOcclusion = 0.2f;
        [Range(0f, 1f)]
        public float gradientPower = 0.5f;
        public Color windWaveTintColor = new Color(178f / 255f, 153f / 255f, 128f / 255f);
        public bool isOverrideHealthyDryNoiseTexture;
        public Texture2D healthyDryNoiseTexture;

        // Wind Parameters
        [Range(0f, 1f)]
        public float windIdleSway = 0.6f;
        public bool windWavesOn = true;
        [Range(0f, 1f)]
        public float windWaveSize = 0.8f;
        [Range(0f, 1f)]
        public float windWaveTint = 0.5f;
        [Range(0f, 1f)]
        public float windWaveSway = 0.5f;
        [Range(0f, 4f)]
        public float contrast = 1f;
        [Range(0f, 4f)]
        public float healthyDryRatio = 1f;
        #endregion Shader Properties

        #region Density Reduction Properties
        public bool isUseDensityReduction = true;
        public float densityReduceDistance = 200f;
        [Range(1f, 128f)]
        public float densityReduceMultiplier = 16f;
        [Range(0f, 64f)]
        public float densityReduceMaxScale = 0f;
        [Range(0f, 1f)]
        public float densityReduceHeightScale = 0f;
        #endregion Density Reduction Properties

        #region Advanced Settings
        public int initialBufferSize = 1024;
        [Range(1, 255)]
        public int maxDetailInstanceCountPerUnit = 16;
        [Range(0f, 4f)]
        public float detailBufferSizePercentageDifferenceForReduction = 0.5f;
        [Range(0.05f, 1f)]
        public float detailExtraBufferSizePercentage = 0.2f;
        #endregion Advanced Settings

        #region Detail Adjustments
        [Range(0.0625f, 16.0f)]
        public float densityAdjustment = 1f;
        [Range(-4f, 4f)]
        public float healthyDryScaleAdjustment = 0f;
        public int noiseSeedAdjustment = 0;
        [Range(0f, 4f)]
        public float noiseSpreadAdjustment = 1f;
        #endregion Detail Adjustments

        public GPUIProceduralDetailObject proceduralDensityData;

        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        internal Bounds _bounds;
        [NonSerialized]
        private GPUIDetailPrototypeSubSettings[] _prototypeSubSettings;
        #endregion Runtime Properties

        public void ReadFromDetailPrototypeData(DetailPrototype detailPrototype, int subSettingIndex, GPUIDetailManager detailManager, int prototypeIndex)
        {
            GPUIDetailPrototypeSubSettings subSettings = GetSubSettings(subSettingIndex);

            bool changed = healthyColor != detailPrototype.healthyColor;
            healthyColor = detailPrototype.healthyColor;
            changed |= dryColor != detailPrototype.dryColor;
            dryColor = detailPrototype.dryColor;
            changed |= detailTexture != detailPrototype.prototypeTexture;
            detailTexture = detailPrototype.prototypeTexture;
            changed |= subSettings.minWidth != detailPrototype.minWidth;
            subSettings.minWidth = detailPrototype.minWidth;
            changed |= subSettings.maxWidth != detailPrototype.maxWidth;
            subSettings.maxWidth = detailPrototype.maxWidth;
            changed |= subSettings.minHeight != detailPrototype.minHeight;
            subSettings.minHeight = detailPrototype.minHeight;
            changed |= subSettings.maxHeight != detailPrototype.maxHeight;
            subSettings.maxHeight = detailPrototype.maxHeight;
            changed |= subSettings.noiseSeed != detailPrototype.noiseSeed;
            subSettings.noiseSeed = detailPrototype.noiseSeed;
            subSettings.detailUniqueValue = 0;
            changed |= noiseSpread != detailPrototype.noiseSpread;
            noiseSpread = detailPrototype.noiseSpread;
            changed |= subSettings.alignToGround != detailPrototype.alignToGround;
            subSettings.alignToGround = detailPrototype.alignToGround;
            changed |= isBillboard != (detailPrototype.renderMode == DetailRenderMode.GrassBillboard);
            isBillboard = detailPrototype.renderMode == DetailRenderMode.GrassBillboard;

            if (detailPrototype.prototype == null && mpbDescription == null)
                mpbDescription = GPUITerrainConstants.DefaultDetailMaterialDescription;

            if (initialBufferSize <= 0)
                initialBufferSize = 1024;

            if (!detailManager.IsInitialized || !changed)
                return;
            SetParameterBufferData();
            subSettings.SetParameterBufferData();
            if (detailTexture != null && GPUIRenderingSystem.TryGetRenderSourceGroup(detailManager.GetRenderKey(prototypeIndex), out GPUIRenderSourceGroup rsg))
                SetMPBValues(detailManager, prototypeIndex, rsg);
        }

        public bool IsMatchingPrefabAndTexture(DetailPrototype detailPrototype, GPUIPrototype prototype, bool checkPropertyValues = true)
        {
            if (prototype.prototypeType == GPUIPrototypeType.Prefab
                && detailPrototype.usePrototypeMesh
                && prototype.prefabObject.EqualOrParentOf(detailPrototype.prototype)
                )
                return true;
            if (prototype.prototypeType == GPUIPrototypeType.MeshAndMaterial
                && !detailPrototype.usePrototypeMesh
                && detailPrototype.prototypeTexture == detailTexture
                && (!checkPropertyValues || (detailPrototype.healthyColor.Approximately(healthyColor) && detailPrototype.dryColor.Approximately(dryColor) && detailPrototype.noiseSpread == noiseSpread && (detailPrototype.renderMode == DetailRenderMode.GrassBillboard) == isBillboard))
                )
                return true;
            return false;
        }

        public bool HasSameSettingsWith(DetailPrototype detailPrototype, int subSettingIndex)
        {
            GPUIDetailPrototypeSubSettings subSettings = _prototypeSubSettings[subSettingIndex];
            if (detailPrototype.minWidth == subSettings.minWidth
                && detailPrototype.maxWidth == subSettings.maxWidth
                && detailPrototype.minHeight == subSettings.minHeight
                && detailPrototype.maxHeight == subSettings.maxHeight
                //&& detailPrototype.noiseSeed == subSettings.noiseSeed
                && detailPrototype.alignToGround == subSettings.alignToGround)
                return true;
            return false;
        }

        public void SetMPBValues(GPUIDetailManager detailManager, int prototypeIndex, GPUIRenderSourceGroup rsg)
        {
            if (mpbDescription != null)
                mpbDescription.SetMPBValues(rsg, detailManager, prototypeIndex);
        }

        private void CreateSubSettingAtIndex(int subSettingIndex)
        {
            if (_prototypeSubSettings == null)
            {
                _prototypeSubSettings = new GPUIDetailPrototypeSubSettings[subSettingIndex + 1];
                for (int i = 0; i <= subSettingIndex; i++)
                    _prototypeSubSettings[i] = new GPUIDetailPrototypeSubSettings();
            }
            else if (_prototypeSubSettings.Length <= subSettingIndex)
            {
                int previousSize = _prototypeSubSettings.Length;
                //Debug.Log(GPUIConstants.LOG_PREFIX + "Adding sub setting at index: " + subSettingIndex);
                Array.Resize(ref _prototypeSubSettings, subSettingIndex + 1);
                for (int i = previousSize; i <= subSettingIndex; i++)
                    _prototypeSubSettings[i] = new GPUIDetailPrototypeSubSettings();
            }
        }

        public GPUIDetailPrototypeSubSettings GetSubSettings(int subSettingIndex)
        {
            CreateSubSettingAtIndex(subSettingIndex);
            return _prototypeSubSettings[subSettingIndex];
        }

        public int GetSubSettingCount() => _prototypeSubSettings == null ? 0 : _prototypeSubSettings.Length;

        public float GetNoiseSpread()
        {
            return noiseSpread * noiseSpreadAdjustment;
        }

        #region Parameter Buffer

        public override void SetParameterBufferData()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIDataBuffer<float> parameterBuffer = GPUIRenderingSystem.Instance.ParameterBuffer;

            if (TryGetParameterBufferIndex(out int startIndex))
            {
                parameterBuffer[startIndex + 0] = densityReduceDistance;
                parameterBuffer[startIndex + 1] = densityReduceMultiplier;
                parameterBuffer[startIndex + 2] = densityReduceMaxScale;
                parameterBuffer[startIndex + 3] = densityReduceHeightScale;
                parameterBuffer[startIndex + 4] = densityAdjustment;
                parameterBuffer[startIndex + 5] = isBillboard ? 1f : 0f;
                parameterBuffer[startIndex + 6] = maxDetailInstanceCountPerUnit;
                parameterBuffer[startIndex + 7] = GetNoiseSpread();
                parameterBuffer[startIndex + 8] = healthyDryScaleAdjustment;
            }
            else
            {
                GPUIRenderingSystem.Instance.ParameterBufferIndexes.Add(this, parameterBuffer.Length);
                parameterBuffer.Add(densityReduceDistance, densityReduceMultiplier, densityReduceMaxScale, densityReduceHeightScale, densityAdjustment, isBillboard ? 1f : 0f, maxDetailInstanceCountPerUnit, GetNoiseSpread(), healthyDryScaleAdjustment);
            }

            if (_prototypeSubSettings != null)
            {
                foreach (var subSettings in _prototypeSubSettings)
                {
                    if (subSettings.noiseSeedAdjustment != noiseSeedAdjustment)
                    {
                        subSettings.detailUniqueValue = 0;
                        subSettings.noiseSeedAdjustment = noiseSeedAdjustment;
                    }
                    subSettings.SetParameterBufferData();
                }
            }
        }

        #endregion Parameter Buffer

        public class GPUIDetailPrototypeSubSettings : IGPUIParameterBufferData
        {
            public float minWidth;
            public float maxWidth;
            public float minHeight;
            public float maxHeight;
            public int noiseSeed;
            [Range(0f, 1f)]
            public float alignToGround;

            public float detailUniqueValue;
            public int noiseSeedAdjustment = 0;

            public void SetParameterBufferData()
            {
                if (!GPUIRenderingSystem.IsActive)
                    return;
                 GPUIDataBuffer<float> parameterBuffer = GPUIRenderingSystem.Instance.ParameterBuffer;

                if (TryGetParameterBufferIndex(out int startIndex))
                {
                    parameterBuffer[startIndex + 0] = minWidth;
                    parameterBuffer[startIndex + 1] = maxWidth;
                    parameterBuffer[startIndex + 2] = minHeight;
                    parameterBuffer[startIndex + 3] = maxHeight;
                    parameterBuffer[startIndex + 4] = noiseSeed;
                    parameterBuffer[startIndex + 5] = alignToGround;
                    parameterBuffer[startIndex + 6] = GetUniqueValue();
                }
                else
                {
                    GPUIRenderingSystem.Instance.ParameterBufferIndexes.Add(this, parameterBuffer.Length);
                    parameterBuffer.Add(minWidth, maxWidth, minHeight, maxHeight, noiseSeed, alignToGround, GetUniqueValue());
                }
            }

            public bool TryGetParameterBufferIndex(out int index)
            {
                return GPUIRenderingSystem.Instance.ParameterBufferIndexes.TryGetValue(this, out index);
            }

            public float GetUniqueValue()
            {
                if (detailUniqueValue == 0)
                {
                    UnityEngine.Random.InitState(noiseSeed + noiseSeedAdjustment);
                    detailUniqueValue = (float)Math.Round(UnityEngine.Random.Range(0.001f, 0.999f), 3);
                }
                return detailUniqueValue;
            }
        }
    }

    public abstract class GPUIProceduralDetailObject : ScriptableObject
    {
        [SerializeField]
        public bool isReadTerrainDetails = true;
        public abstract void Execute(GPUITerrain gpuiTerrain, int detailLayer);
    }
}
