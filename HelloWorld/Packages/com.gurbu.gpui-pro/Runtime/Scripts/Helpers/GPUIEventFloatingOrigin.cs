// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#Floating_Origin_Event")]
    public class GPUIEventFloatingOrigin : MonoBehaviour
    {
        [SerializeField]
        public Transform floatingOrigin;
        [SerializeField]
        public GPUIManager manager;
        [NonSerialized]
        private Matrix4x4 _previousMatrix;

        private void OnEnable()
        {
            if (manager == null || floatingOrigin == null)
                return;
            _previousMatrix = floatingOrigin.localToWorldMatrix;

            GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPreCull -= HandleFloatingOrigin;
            GPUIRenderingSystem.Instance.OnPreCull += HandleFloatingOrigin;
        }

        private void OnDisable()
        {
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.OnPreCull -= HandleFloatingOrigin;
        }

        public void HandleFloatingOrigin(GPUICameraData cameraData)
        {
            Matrix4x4 newMatrix = floatingOrigin.localToWorldMatrix;
            if (newMatrix != _previousMatrix)
            {
                Matrix4x4 matrixOffset = newMatrix * _previousMatrix.inverse;
                GPUITransformBufferUtility.ApplyMatrixOffsetToTransforms(manager, matrixOffset);
                _previousMatrix = newMatrix;
            }
        }
    }
}