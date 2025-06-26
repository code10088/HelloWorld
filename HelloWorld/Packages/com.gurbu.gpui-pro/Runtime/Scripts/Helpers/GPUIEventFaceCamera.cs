// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#Face_Camera_Event")]
    public class GPUIEventFaceCamera : MonoBehaviour
    {
        public GPUIManager gpuiManager;
        public int prototypeIndex;
        public bool isFaceCameraPos;

        private void OnEnable()
        {
            GPUIRenderingSystem.InitializeRenderingSystem();
            GPUIRenderingSystem.Instance.OnPreCull -= TransformFaceCameraPos;
            GPUIRenderingSystem.Instance.OnPreCull -= TransformFaceCameraView;
            GPUIRenderingSystem.Instance.OnPreCull += isFaceCameraPos ? TransformFaceCameraPos : TransformFaceCameraView;
        }

        private void OnDisable()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;
            GPUIRenderingSystem.Instance.OnPreCull -= TransformFaceCameraPos;
            GPUIRenderingSystem.Instance.OnPreCull -= TransformFaceCameraView;
        }

        public void TransformFaceCameraView(GPUICameraData cameraData)
        {
            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            if (cameraData.TryGetShaderBuffer(gpuiManager, prototypeIndex, out GPUIShaderBuffer shaderBuffer))
            {
                cs.SetBuffer(6, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_bufferSize, shaderBuffer.BufferSize);
                cs.SetMatrix(GPUIConstants.PROP_matrix44, cameraData.ActiveCamera.cameraToWorldMatrix);
                cs.DispatchX(6, shaderBuffer.BufferSize);
            }
        }

        public void TransformFaceCameraPos(GPUICameraData cameraData)
        {
            ComputeShader cs = GPUIConstants.CS_TransformModifications;
            if (cameraData.TryGetShaderBuffer(gpuiManager, prototypeIndex, out GPUIShaderBuffer shaderBuffer))
            {
                cs.SetBuffer(7, GPUIConstants.PROP_gpuiTransformBuffer, shaderBuffer.Buffer);
                cs.SetInt(GPUIConstants.PROP_bufferSize, shaderBuffer.BufferSize);
                cs.SetVector(GPUIConstants.PROP_position, cameraData.ActiveCamera.transform.position);
                cs.DispatchX(7, shaderBuffer.BufferSize);
            }
        }
    }
}