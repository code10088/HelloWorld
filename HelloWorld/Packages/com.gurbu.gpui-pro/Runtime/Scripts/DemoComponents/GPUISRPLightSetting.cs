// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    [RequireComponent(typeof(Light))]
    [DefaultExecutionOrder(-1000)]
    [ExecuteInEditMode]
    public class GPUISRPLightSetting : MonoBehaviour
    {
        [Header("URP")]
        public float uRPIntensity = 1f;
        [Header("HDRP")]
        public float hDRPIntensity = 100000f;
        [Range(0, 3)]
        public int hDRPShadowResolutionLevel = 2;
        public LightProbeGroup lightProbeGroup;
        public Vector3 probeVolumeSize = new Vector3(50, 50, 50);
        public UnityEvent onAPVEnabled;
        public UnityEvent onAPVDisabled;

        private void OnEnable()
        {
            switch (GPUIRuntimeSettings.Instance.RenderPipeline)
            {
#if GPUI_HDRP
                case GPUIRenderPipeline.HDRP:
                    var hdLight = gameObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                    if (hdLight != null)
                    {
#if UNITY_6000_0_OR_NEWER
                        Light lightHDRP = GetComponent<Light>();
                        lightHDRP.intensity = hDRPIntensity;
#else
                        hdLight.intensity = hDRPIntensity;
#endif
                        hdLight.SetShadowResolutionOverride(false);
                        hdLight.SetShadowResolutionLevel(hDRPShadowResolutionLevel);
                    }
                    HandleAPV();
                    break;
#endif
#if GPUI_URP
                case GPUIRenderPipeline.URP:
                    Light lightURP = GetComponent<Light>();
                    lightURP.intensity = uRPIntensity;
                    HandleAPV();
                    break;
#endif
                default:
                    HandleAPV();
                    break;
            }
        }

        private void HandleAPV()
        {
            if (lightProbeGroup != null)
            {
                if (GPUIRuntimeSettings.IsAdaptiveProbeVolumesEnabled())
                    onAPVEnabled?.Invoke();
                else
                    onAPVDisabled?.Invoke();
            }
#if (GPUI_URP || GPUI_HDRP) && UNITY_6000_0_OR_NEWER && UNITY_EDITOR
            if (!Application.isPlaying && lightProbeGroup != null)
            {
                if (GPUIRuntimeSettings.IsAdaptiveProbeVolumesEnabled())
                {
                    var probeVolume = FindAnyObjectByType<ProbeVolume>();
                    if (probeVolume == null || probeVolume.size == new Vector3(10, 10, 10))
                    {
                        if (probeVolume == null)
                            probeVolume = lightProbeGroup.gameObject.AddComponent<ProbeVolume>();
                        else
                            probeVolume.transform.position = lightProbeGroup.transform.position;
                        probeVolume.size = probeVolumeSize;
                        probeVolume.overridesSubdivLevels = true;
                        probeVolume.highestSubdivLevelOverride = 2;
                        probeVolume.lowestSubdivLevelOverride = 1;

                        LightingSettings lightingSettings = Lightmapping.lightingSettings;
                        if (lightingSettings != null)
                            lightingSettings.realtimeGI = false;
                    }
                }
            }
#endif
        }
    }
}
