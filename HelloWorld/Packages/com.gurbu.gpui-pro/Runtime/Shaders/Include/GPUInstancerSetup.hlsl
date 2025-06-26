// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef GPU_INSTANCER_PRO_INCLUDED
#define GPU_INSTANCER_PRO_INCLUDED

#ifndef GPUI_PRO_PRAGMAS_DEFINED
    #define GPUI_PRO_PRAGMAS_DEFINED
    #pragma shader_feature_local _ GPUI_OBJECT_MOTION_VECTOR_ON
    #pragma shader_feature_local _ GPUI_PER_INSTANCE_LIGHTPROBES_ON
#endif

#include "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerInput.hlsl"
#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/Matrix.hlsl"

#ifdef GPUI_PRO_ACTIVE
#ifdef unity_ObjectToWorld
    #undef unity_ObjectToWorld
#endif
#ifdef unity_WorldToObject
    #undef unity_WorldToObject
#endif
#ifdef unity_MatrixPreviousM
    #undef unity_MatrixPreviousM
#endif
#ifdef unity_MatrixPreviousMI
    #undef unity_MatrixPreviousMI
#endif

void SetVisibilityData(uint unityInstanceID)
{
    uint shiftedInstanceID = unityInstanceID + instanceDataBufferShift;
    
#ifdef GPUI_NO_BUFFER    
    float indexX = ((shiftedInstanceID % maxTextureSize) + 0.5) / min(instanceDataBufferSize, maxTextureSize);
    float rowCount = ceil(instanceDataBufferSize / maxTextureSize);
    float rowIndex = floor(shiftedInstanceID / maxTextureSize) + 0.5;

    float4 instanceData = tex2Dlod(gpuiInstanceDataBufferTexture, float4(indexX, rowIndex / rowCount, 0.0, 0.0));

    gpui_InstanceID = asuint(instanceData.x);
    
    indexX = ((gpui_InstanceID % maxTextureSize) + 0.5) / min(transformBufferSize, maxTextureSize);
    rowCount = ceil(transformBufferSize / maxTextureSize) * 4.0;
    rowIndex = floor(gpui_InstanceID / maxTextureSize) * 4.0 + 0.5;
    
    float4x4 transformData = float4x4(
        tex2Dlod(gpuiTransformBufferTexture, float4(indexX, (0.0 + rowIndex) / rowCount, 0.0, 0.0)),
        tex2Dlod(gpuiTransformBufferTexture, float4(indexX, (1.0 + rowIndex) / rowCount, 0.0, 0.0)),
        tex2Dlod(gpuiTransformBufferTexture, float4(indexX, (2.0 + rowIndex) / rowCount, 0.0, 0.0)),
        tex2Dlod(gpuiTransformBufferTexture, float4(indexX, (3.0 + rowIndex) / rowCount, 0.0, 0.0))
    );

    //transformData = float4x4(1,0,0,gpui_InstanceID,  0,1,0, unityInstanceID, 0,0,1,0,  0,0,0,1);
#else // GPUI_NO_BUFFER
    float4 instanceData = gpuiInstanceDataBuffer[shiftedInstanceID];
    gpui_InstanceID = asuint(instanceData.x);
    float4x4 transformData = gpuiTransformBuffer[gpui_InstanceID];
#endif // GPUI_NO_BUFFER
    
    unity_ObjectToWorld = mul(transformData, gpuiTransformOffset);

//#ifdef LOD_FADE_CROSSFADE // Some shaders manually undefine LOD_FADE_CROSSFADE for custom implementations while still using unity_LODFade.
    unity_LODFade.x = instanceData.y;
    unity_LODFade.y = round(instanceData.y * 16.0);
//#endif

//#ifdef LIGHTMAP_ON
//    unity_LightmapST = float4(0.1699218, 0.1699218, 0.7851563, 0);
//#endif
//#ifdef DYNAMICLIGHTMAP_ON
//    unity_DynamicLightmapST = float4(0.25, 0.25, 0, 0.6875);
//#endif
}

#endif // GPUI_PRO_ACTIVE

void setupGPUI()
{
#ifdef GPUI_PRO_ACTIVE
    SetVisibilityData(unity_InstanceID);
    unity_WorldToObject = GetInverseTransformMatrix(unity_ObjectToWorld);
#ifdef GPUI_ENABLE_OBJECT_MOTION_VECTOR
    if (hasPreviousFrameTransformBuffer == 1)
    {
        unity_MatrixPreviousM = gpuiPreviousFrameTransformBuffer[gpui_InstanceID];
        unity_MatrixPreviousM = mul(unity_MatrixPreviousM, gpuiTransformOffset);
        unity_MatrixPreviousMI = GetInverseTransformMatrix(unity_MatrixPreviousM);
    }
    else
    {
        unity_MatrixPreviousM = unity_ObjectToWorld;
        unity_MatrixPreviousMI = unity_WorldToObject;
    }
#elif defined(UNITY_PREV_MATRIX_M) && !defined(BUILTIN_TARGET_API)
    unity_MatrixPreviousM = unity_ObjectToWorld;
    unity_MatrixPreviousMI = unity_WorldToObject;
#endif // GPUI_ENABLE_OBJECT_MOTION_VECTOR
#ifdef GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES
    if (hasPerInstanceLightProbes == 1)
    {
        uint shIndex = gpui_InstanceID * 8;
        unity_SHAr = gpuiPerInstanceLightProbesBuffer[shIndex];
        unity_SHAg = gpuiPerInstanceLightProbesBuffer[shIndex + 1];
        unity_SHAb = gpuiPerInstanceLightProbesBuffer[shIndex + 2];
        unity_SHBr = gpuiPerInstanceLightProbesBuffer[shIndex + 3];
        unity_SHBg = gpuiPerInstanceLightProbesBuffer[shIndex + 4];
        unity_SHBb = gpuiPerInstanceLightProbesBuffer[shIndex + 5];
        unity_SHC = gpuiPerInstanceLightProbesBuffer[shIndex + 6];
        unity_ProbesOcclusion = gpuiPerInstanceLightProbesBuffer[shIndex + 7];
    }
#endif // GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES
#endif // GPUI_PRO_ACTIVE
}

// Dummy methods for Shader Graph
void gpuiDummy_float(float3 inPos, out float3 outPos)
{
    outPos = inPos;
}

void gpuiDummy_half(half3 inPos, out half3 outPos)
{
    outPos = inPos;
}

#endif // GPU_INSTANCER_PRO_INCLUDED