// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER && GPUI_URP
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace GPUInstancerPro
{
    public static class GPUIHiZDepthTextureUtility
    {
        #region Copy Camera Depth to HiZ Depth

        public static void CopyHiZTextureWithComputeShader(ComputeShader cs, Texture source, Texture destination, int offsetX)
        {
            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, source.width);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, source.height);
            cs.SetInt(GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            cs.DispatchXY(kernelIndex, source.width, source.height);
        }

        public static void CopyHiZTextureWithComputeShader(ComputeShader cs, CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, int sourceW, int sourceH, RenderTargetIdentifier destinationIdentifier, int offsetX)
        {
            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceIdentifier, 0, GPUIRuntimeSettings.Instance.IsBuiltInRP ? RenderTextureSubElement.Depth : RenderTextureSubElement.Default);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationIdentifier);

            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

#if UNITY_6000_0_OR_NEWER && GPUI_URP
        public static void CopyHiZTextureWithComputeShader(ComputeShader cs, ComputeCommandBuffer commandBuffer, TextureHandle sourceHandle, int sourceW, int sourceH, TextureHandle destinationHandle, int offsetX)
        {
            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceHandle);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationHandle);

            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }
#endif

        public static void CopyHiZTextureArrayWithComputeShader(ComputeShader cs, Texture source, Texture destination, int offsetX, int textureArrayIndex)
        {
            int kernelIndex = 1;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_textureArray, source);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_textureArrayIndex, textureArrayIndex);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, source.width);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, source.height);
            cs.SetInt(GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);

            cs.DispatchXY(kernelIndex, source.width, source.height);
        }

        public static void CopyHiZTextureArrayWithComputeShader(ComputeShader cs, CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, int sourceW, int sourceH, RenderTargetIdentifier destinationIdentifier, int offsetX, int textureArrayIndex)
        {
            int kernelIndex = 1;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_textureArray, sourceIdentifier);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationIdentifier);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_textureArrayIndex, textureArrayIndex);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

#if UNITY_6000_0_OR_NEWER && GPUI_URP
        public static void CopyHiZTextureArrayWithComputeShader(ComputeShader cs, ComputeCommandBuffer commandBuffer, TextureHandle sourceHandle, int sourceW, int sourceH, TextureHandle destinationHandle, int offsetX, int textureArrayIndex)
        {
            int kernelIndex = 1;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_textureArray, sourceHandle);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, destinationHandle);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_offsetX, offsetX);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_reverseZ, GPUIRuntimeSettings.Instance.ReversedZBuffer ? 1 : 0);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_textureArrayIndex, textureArrayIndex);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(sourceW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(sourceH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }
#endif

        #endregion Copy Camera Depth to HiZ Depth

        #region Generate HiZ Depth Mips

        private static void GetMipSize(int width, int height, int sourceMip, int destinationMip, out int destinationW, out int destinationH, out int sourceW, out int sourceH)
        {
            sourceW = Mathf.Max(width >> sourceMip, 1);
            sourceH = Mathf.Max(height >> sourceMip, 1);
            destinationW = Mathf.Max(width >> destinationMip, 1);
            destinationH = Mathf.Max(height >> destinationMip, 1);
        }

        public static void ReduceTextureWithComputeShader(ComputeShader cs, Texture source, int sourceW, int sourceH, int sourceMip, int destinationMip)
        {
            GetMipSize(sourceW, sourceH, sourceMip, destinationMip, out int destinationW, out int destinationH, out sourceW, out sourceH);

            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, source, destinationMip);

            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);
            cs.SetInt(GPUIConstants.PROP_destinationSizeX, destinationW);
            cs.SetInt(GPUIConstants.PROP_destinationSizeY, destinationH);

            cs.DispatchXY(kernelIndex, destinationW, destinationH);
        }

        public static void ReduceTextureWithComputeShader(ComputeShader cs, CommandBuffer commandBuffer, RenderTargetIdentifier sourceIdentifier, int sourceW, int sourceH, int sourceMip, int destinationMip)
        {
            GetMipSize(sourceW, sourceH, sourceMip, destinationMip, out int destinationW, out int destinationH, out sourceW, out sourceH);

            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceIdentifier, sourceMip);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, sourceIdentifier, destinationMip);

            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeX, destinationW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeY, destinationH);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(destinationW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(destinationH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }

#if UNITY_6000_0_OR_NEWER && GPUI_URP
        public static void ReduceTextureWithComputeShader(ComputeShader cs, ComputeCommandBuffer commandBuffer, TextureHandle sourceIdentifier, int sourceW, int sourceH, int sourceMip, int destinationMip)
        {
            GetMipSize(sourceW, sourceH, sourceMip, destinationMip, out int destinationW, out int destinationH, out sourceW, out sourceH);

            int kernelIndex = 0;

            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_source, sourceIdentifier, sourceMip);
            commandBuffer.SetComputeTextureParam(cs, kernelIndex, GPUIConstants.PROP_destination, sourceIdentifier, destinationMip);

            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeX, sourceW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_sourceSizeY, sourceH);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeX, destinationW);
            commandBuffer.SetComputeIntParam(cs, GPUIConstants.PROP_destinationSizeY, destinationH);
            commandBuffer.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(destinationW / GPUIConstants.CS_THREAD_COUNT_2D), Mathf.CeilToInt(destinationH / GPUIConstants.CS_THREAD_COUNT_2D), 1);
        }
#endif

        #endregion Generate HiZ Depth Mips
    }
}