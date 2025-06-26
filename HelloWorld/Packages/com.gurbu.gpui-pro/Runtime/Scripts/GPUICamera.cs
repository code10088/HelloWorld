// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEngine;

namespace GPUInstancerPro
{
    /// <summary>
    /// This component is automatically attached to cameras that are used for visibility calculations for GPUI.
    /// It can also be attached manually to determine which cameras should use GPUI when there are multiple cameras.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Camera")]
    public class GPUICamera : MonoBehaviour
    {
        [SerializeField]
        private bool _enableOcclusionCulling = true;
        [SerializeField]
        [Range(0f, 10.0f)]
        private float _dynamicOcclusionOffsetIntensity = 1f;

        [NonSerialized]
        internal Camera _cameraRef;
        [NonSerialized]
        internal GPUICameraData _cameraData;
        [NonSerialized]
        private bool _isInitialized;

        #region MonoBehaviour

        private void OnEnable()
        {
            if (!_isInitialized)
                Initialize();
        }

        private void OnDisable()
        {
            Dispose();
        }

        #endregion MonoBehaviour

        #region Initialize/Dispose

        public void Initialize()
        {
            if (_cameraRef == null)
                _cameraRef = GetComponent<Camera>();
            bool hasCameraData = _cameraData != null;
            if (_isInitialized && hasCameraData)
                return;
            if (hasCameraData)
                Dispose();
            _cameraData = new(_cameraRef);
            GPUIRenderingSystem.AddCameraData(_cameraData);
            if (_enableOcclusionCulling)
                _cameraData.autoInitializeOcclusionCulling = true;
            _isInitialized = true;
            SetDynamicOcclusionOffsetIntensity(_dynamicOcclusionOffsetIntensity);
            if (!enabled)
                enabled = true; // GPUICamera will always be enabled when Initialized.
        }

        public void Dispose()
        {
            _isInitialized = false;
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.CameraDataProvider.RemoveCamera(_cameraRef);

            if (_cameraData != null)
                _cameraData.Dispose();
            _cameraData = null;
        }

        #endregion Initialization

        public GPUICameraData GetCameraData()
        {
            return _cameraData;
        }

        public Camera GetCamera()
        {
            if (_cameraRef == null)
                _cameraRef = GetComponent<Camera>();
            return _cameraRef;
        }

        public void SetOcclusionCullingEnabled(bool enabled)
        {
            _enableOcclusionCulling = enabled;
            if (_cameraData == null)
                return;

            if (enabled)
                _cameraData.autoInitializeOcclusionCulling = true;
            else
            {
                _cameraData.autoInitializeOcclusionCulling = false;
                _cameraData.DisableOcclusionCulling();
            }
        }

        public void SetDynamicOcclusionOffsetIntensity(float intensity)
        {
            _dynamicOcclusionOffsetIntensity = intensity;
            if (_cameraData == null) return;
            _cameraData.SetDynamicOcclusionOffsetIntensity(intensity);
        }
    }
}