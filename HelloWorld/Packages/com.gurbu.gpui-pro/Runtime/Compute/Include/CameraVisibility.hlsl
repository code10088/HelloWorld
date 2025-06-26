// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef __cameravisibility_hlsl_
#define __cameravisibility_hlsl_

#pragma multi_compile _ GPUI_LOD GPUI_LOD_CROSSFADE GPUI_LOD_CROSSFADE_ANIMATE
#pragma multi_compile _ GPUI_OCCLUSION_CULLING
#pragma multi_compile _ GPUI_SHADOWCASTING GPUI_SHADOWCULLED

#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/GPUIDefines.hlsl"
#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/Matrix.hlsl"
#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/Culling.hlsl"

#ifdef __INTELLISENSE__
#define GPUI_OCCLUSION_CULLING
//#define GPUI_VR_CULLING
#define GPUI_LOD_CROSSFADE
//#define GPUI_SHADOWCASTING
#define GPUI_SHADOWCULLED
#endif 

uniform StructuredBuffer<float4x4> gpuiTransformBuffer;
uniform StructuredBuffer<float> parameterBuffer;
uniform RWStructuredBuffer<float4> gpuiInstanceDataBuffer;
uniform RWStructuredBuffer<GPUIVisibilityData> visibilityBuffer;

uniform float4x4 mvpMatrix;
uniform float4 cameraPositionAndHalfAngle;

#ifdef GPUI_VR_CULLING
uniform float4x4 mvpMatrix2;
#endif

// Buffer parameters
// x=> bufferSize, y => visibilityBufferIndex, z => parameterBufferIndex, w => lodGroupDataBufferIndex
uniform uint4 sizeAndIndexes;
// x=> instanceCount, y => bufferShift, z => unused, w => unused
uniform uint4 sizeAndIndexes2;

// x=> deltaTime, y => occlusionDynamicOffset, z => maximumLODLevel, w => unused
uniform float4 additionalValues;

inline bool IsFrustumOrOcclusionCulled(float4x4 transformData, float dist, float3 p_boundsCenter, float3 p_boundsExtents, float p_minCullingDistance, float p_frustumOffset, uint occlusionOffsetParameterIndex, uint occlusionAccuracyParameterIndex, uint occlusionOffsetSizeMultiplierParameterIndex)
{
    bool isCulled = false;
    if (dist > p_minCullingDistance)
    {
        float4 BoundingBox[8];
        CalculateBoundingBox(mvpMatrix, transformData, p_boundsCenter, p_boundsExtents, BoundingBox);
#ifdef GPUI_VR_CULLING
        float4 BoundingBox2[8];
        CalculateBoundingBox(mvpMatrix2, transformData, p_boundsCenter, p_boundsExtents, BoundingBox2);
#endif
    
        if (p_frustumOffset >= 0)
        {
            isCulled = IsFrustumCulled(BoundingBox, p_frustumOffset);
#ifdef GPUI_VR_CULLING
            if (isCulled)
                isCulled = IsFrustumCulled(BoundingBox2, p_frustumOffset);
#endif
        }
    
#ifdef GPUI_OCCLUSION_CULLING
        float p_occlusionOffset = parameterBuffer[occlusionOffsetParameterIndex];
        if (!isCulled && p_occlusionOffset >= 0)
        {
            float p_occlusionAccuracy = parameterBuffer[occlusionAccuracyParameterIndex];
            float p_occlusionOffsetSizeMultiplier = parameterBuffer[occlusionOffsetSizeMultiplierParameterIndex];
            isCulled = IsOcclusionCulled(BoundingBox, p_occlusionOffset, additionalValues.y, p_occlusionAccuracy, 0, p_occlusionOffsetSizeMultiplier);
#ifdef GPUI_VR_CULLING
            if (isCulled)
                isCulled = IsOcclusionCulled(BoundingBox2, p_occlusionOffset, additionalValues.y, p_occlusionAccuracy, 0.5, p_occlusionOffsetSizeMultiplier);
#endif
        }
#endif    
    }
    return isCulled;
}

inline void CameraVisibility(uint3 id)
{
    uint bufferSize = sizeAndIndexes.x;
    uint bufferIndex = id.x + sizeAndIndexes2.y;
    
    if (bufferIndex >= bufferSize || id.x >= sizeAndIndexes2.x)
        return;
    
    // Make visibility calculations
    float4x4 transformData = gpuiTransformBuffer[bufferIndex];
    float3 scale = GetScale(transformData);
    if (scale.x == 0 || scale.y == 0 || scale.z == 0 || transformData._44 != 1) // zero scale or unset matrix
        return;
    
    uint visibilityBufferIndex = sizeAndIndexes.y;
    uint parameterBufferIndex = sizeAndIndexes.z;
    
    float dist = distance(transformData._14_24_34, cameraPositionAndHalfAngle.xyz);
    
    // LOD Calculation
    int lodNo = 0;
    int shadowLODNo = 0;
    int lodGroupDataBufferIndex = sizeAndIndexes.w;
    int p_lodCount = int(parameterBuffer[lodGroupDataBufferIndex]);
    
    float3 p_boundsCenter = float3(parameterBuffer[lodGroupDataBufferIndex + 1], parameterBuffer[lodGroupDataBufferIndex + 2], parameterBuffer[lodGroupDataBufferIndex + 3]);
    float3 p_boundsExtents = float3(parameterBuffer[lodGroupDataBufferIndex + 4], parameterBuffer[lodGroupDataBufferIndex + 5], parameterBuffer[lodGroupDataBufferIndex + 6]);
    bool isLODCulled = false;
    
#if defined(GPUI_LOD) || defined(GPUI_LOD_CROSSFADE) || defined(GPUI_LOD_CROSSFADE_ANIMATE)
    float p_transitionValues[8];
    float p_lodBiasAdjustment = parameterBuffer[parameterBufferIndex + 9];
    float maxViewSize = (parameterBuffer[lodGroupDataBufferIndex + 23] /*p_lodGroupSize*/ * max(max(scale.x, scale.y), scale.z)) / (dist * cameraPositionAndHalfAngle.w * 4.0);
    lodNo = p_lodCount;
    shadowLODNo = p_lodCount;
    int maximumLODLevel = additionalValues.z;
    for (int i = maximumLODLevel; i <= p_lodCount && i < 8; i++)
    {
        float transitionValue = parameterBuffer[lodGroupDataBufferIndex + 7 + i] / p_lodBiasAdjustment;
        p_transitionValues[i] = transitionValue;
        if (lodNo >= p_lodCount && maxViewSize > transitionValue)
            lodNo = i;
    }
    
    isLODCulled = lodNo >= p_lodCount;
#if !defined(GPUI_LOD_CROSSFADE) && !defined(GPUI_LOD_CROSSFADE_ANIMATE)
    if (isLODCulled)
        return;
#endif
    
#if defined(GPUI_SHADOWCASTING) || defined(GPUI_SHADOWCULLED)
    if (!isLODCulled) // LOD culled
        shadowLODNo = int(parameterBuffer[parameterBufferIndex + 11 + lodNo]);
#endif
#endif // GPUI_LOD
    
    bool isCulled = false;
    // Distance culling
    float p_minDistance = parameterBuffer[parameterBufferIndex + 1];
    float p_maxDistance = parameterBuffer[parameterBufferIndex + 2]; // Max distance will be negative when distance culling is disabled
    if (p_maxDistance >= 0 && (dist < p_minDistance || dist > p_maxDistance))
        isCulled = true;
    
#if defined(GPUI_SHADOWCASTING) || defined(GPUI_SHADOWCULLED)
    float p_customShadowDistance = parameterBuffer[parameterBufferIndex + 10];
    bool isShadowCulled = isLODCulled || (p_customShadowDistance > 0 && dist > p_customShadowDistance) || (shadowLODNo >= p_lodCount || parameterBuffer[lodGroupDataBufferIndex + 15 + lodNo] /*isLODShadowCasting*/ < 1.0);
#endif
    
    // Frustum and Occlusion culling
    if (!isCulled)
        isCulled = IsFrustumOrOcclusionCulled(transformData, dist, p_boundsCenter, p_boundsExtents, parameterBuffer[parameterBufferIndex], parameterBuffer[parameterBufferIndex + 3], parameterBufferIndex + 4, parameterBufferIndex + 5, parameterBufferIndex + 24);
#ifdef GPUI_SHADOWCULLED
    if (!isShadowCulled)
        isShadowCulled = IsFrustumOrOcclusionCulled(transformData, dist, p_boundsCenter, p_boundsExtents, parameterBuffer[parameterBufferIndex + 23], parameterBuffer[parameterBufferIndex + 21], parameterBufferIndex + 22, parameterBufferIndex + 5, parameterBufferIndex + 25);
#endif
    
    float4 appendInstance = float4(asfloat(bufferIndex), 1.0, dist, 0); // index, crossFadeValue, cameraDistance, unused
#if defined(GPUI_LOD_CROSSFADE_ANIMATE)
    uint stateIndex = bufferSize * p_lodCount
#if defined(GPUI_SHADOWCASTING) || defined(GPUI_SHADOWCULLED)
    * 2
#endif
    + bufferIndex;
    float4 crossFadeState = gpuiInstanceDataBuffer[stateIndex]; // previousLOD + 1, previousCrossFadeValue, crossFadingLOD, unused
#endif // GPUI_LOD_CROSSFADE_ANIMATE
    
    uint index;
    
    int crossFadingLOD = -1;
    int crossFadingLODShadow = -1;
    float4 crossFadeInstance = float4(0, 0, dist, 0);
    if (!isCulled)
    {
#if defined(GPUI_LOD_CROSSFADE)
        float p_crossFadeWidth = parameterBuffer[lodGroupDataBufferIndex + 24 + lodNo - 1];
        if (lodNo > 0 && p_crossFadeWidth > 0)
        {
            crossFadingLOD = lodNo - 1;
            appendInstance.y = (p_transitionValues[lodNo - 1] - maxViewSize) / (p_transitionValues[lodNo - 1] - p_transitionValues[lodNo]) / p_crossFadeWidth;
            if (appendInstance.y > 1 || appendInstance.y < 0) 
                appendInstance.y = 1.0;
        }
#elif defined(GPUI_LOD_CROSSFADE_ANIMATE)
        float p_crossFadeSpeed = additionalValues.x * parameterBuffer[parameterBufferIndex + 20];
        crossFadingLOD = crossFadeState.x - 1;
        if (p_crossFadeSpeed > 0 && crossFadingLOD >= 0 && crossFadingLOD <= p_lodCount /* We accept also p_lodCount equal to crossfade to LOD culled state. */)
        {
            if (crossFadingLOD != lodNo)
            {
                appendInstance.y = p_crossFadeSpeed;
            }
            else
            {
                crossFadingLOD = crossFadeState.z;
                appendInstance.y = crossFadeState.y + p_crossFadeSpeed;
            }
        }
        if (appendInstance.y <= 0.0 || appendInstance.y >= 1.0)
        {
            appendInstance.y = 1.0;
            crossFadingLOD = -1;
        }
#endif // GPUI_LOD_CROSSFADE
        
#if defined(GPUI_LOD) || defined(GPUI_LOD_CROSSFADE) || defined(GPUI_LOD_CROSSFADE_ANIMATE)
        if (!isLODCulled)
        {
#endif // GPUI_LOD
            InterlockedAdd(visibilityBuffer[visibilityBufferIndex + lodNo].visibleCount, 1, index);
            gpuiInstanceDataBuffer[index + lodNo * bufferSize] = appendInstance;
#if defined(GPUI_LOD) || defined(GPUI_LOD_CROSSFADE) || defined(GPUI_LOD_CROSSFADE_ANIMATE)
        }
#endif // GPUI_LOD
        
#if defined(GPUI_LOD_CROSSFADE) || defined(GPUI_LOD_CROSSFADE_ANIMATE)
        if (appendInstance.y > 0.0 && appendInstance.y < 1.0 && crossFadingLOD >= 0 && crossFadingLOD < p_lodCount)
        {
            crossFadeInstance.x = asfloat(bufferIndex);
            crossFadeInstance.y = -appendInstance.y;
        
            InterlockedAdd(visibilityBuffer[visibilityBufferIndex + crossFadingLOD].visibleCount, 1, index);
            gpuiInstanceDataBuffer[index + crossFadingLOD * bufferSize] = crossFadeInstance;
        
#if defined(GPUI_SHADOWCASTING) || defined(GPUI_SHADOWCULLED)
            crossFadingLODShadow = int(parameterBuffer[parameterBufferIndex + 11 + crossFadingLOD]);
            if (crossFadingLODShadow >= p_lodCount || parameterBuffer[lodGroupDataBufferIndex + 15 + crossFadingLOD] /*isLODShadowCasting*/ < 1.0)
                crossFadingLODShadow = -1;
#endif
        }
#endif // GPUI_LOD_CROSSFADE  || GPUI_LOD_CROSSFADE_ANIMATE
    }
    
#if defined(GPUI_SHADOWCASTING) || defined(GPUI_SHADOWCULLED)
    if (!isShadowCulled)
    {
        // Shadow
        InterlockedAdd(visibilityBuffer[visibilityBufferIndex + p_lodCount + shadowLODNo].visibleCount, 1, index);
        gpuiInstanceDataBuffer[index + (p_lodCount + shadowLODNo) * bufferSize] = appendInstance;
    
#if defined(GPUI_LOD_CROSSFADE) || defined(GPUI_LOD_CROSSFADE_ANIMATE)
        if (appendInstance.y > 0.0 && appendInstance.y < 1.0 && crossFadingLODShadow >= 0)
        {
            InterlockedAdd(visibilityBuffer[visibilityBufferIndex + p_lodCount + crossFadingLODShadow].visibleCount, 1, index);
            gpuiInstanceDataBuffer[index + (p_lodCount + crossFadingLODShadow) * bufferSize] = crossFadeInstance;
        }
#endif // GPUI_LOD_CROSSFADE || GPUI_LOD_CROSSFADE_ANIMATE
    }
#endif // GPUI_SHADOWCASTING || GPUI_SHADOWCULLED
    
#if defined(GPUI_LOD_CROSSFADE_ANIMATE)
    crossFadeState.x = lodNo + 1;
    crossFadeState.y = appendInstance.y;
    crossFadeState.z = crossFadingLOD;
    gpuiInstanceDataBuffer[stateIndex] = crossFadeState;
#endif // GPUI_LOD_CROSSFADE_ANIMATE
}

#endif // __cameravisibility_hlsl_