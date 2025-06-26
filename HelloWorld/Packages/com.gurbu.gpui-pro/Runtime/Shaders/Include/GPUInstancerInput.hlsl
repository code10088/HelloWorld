// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef GPU_INSTANCER_PRO_INPUT_INCLUDED
#define GPU_INSTANCER_PRO_INPUT_INCLUDED

#include "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUIShaderSettings.hlsl"

#ifdef __INTELLISENSE__
#define UNITY_SUPPORT_INSTANCING
#define PROCEDURAL_INSTANCING_ON
#define UNITY_PROCEDURAL_INSTANCING_ENABLED
#undef GPUI_NO_BUFFER
#define GPUI_ENABLE_OBJECT_MOTION_VECTOR
#define GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES
#define LOD_FADE_CROSSFADE
//#define LIGHTMAP_ON
//#define DYNAMICLIGHTMAP_ON
static uint unity_InstanceID;
static float4x4 unity_ObjectToWorld;
static float4x4 unity_WorldToObject;
static float4x4 unity_MatrixPreviousM;
static float4x4 unity_MatrixPreviousMI;
static float4 unity_LODFade;
static float4 unity_LightmapST;
static float4 unity_DynamicLightmapST;
static float4 unity_SHAr;
static float4 unity_SHAg;
static float4 unity_SHAb;
static float4 unity_SHBr;
static float4 unity_SHBg;
static float4 unity_SHBb;
static float4 unity_SHC;
static float4 unity_ProbesOcclusion;
#endif // __INTELLISENSE__

uniform uint gpui_InstanceID;

#if (defined(UNITY_SUPPORT_INSTANCING) && defined(PROCEDURAL_INSTANCING_ON)) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
#define GPUI_PRO_ACTIVE

uniform float4x4 gpuiTransformOffset;
uniform uint instanceDataBufferShift;
uniform float maxTextureSize;
uniform float instanceDataBufferSize;
uniform float transformBufferSize;

#undef UNITY_DEFINE_INSTANCED_PROP
#define UNITY_DEFINE_INSTANCED_PROP(type, var) type var;

#if defined(GPUI_OBJECT_MOTION_VECTOR_ON) && defined(UNITY_PREV_MATRIX_M) && !defined(BUILTIN_TARGET_API) && !defined(GPUI_NO_BUFFER)
    #define GPUI_ENABLE_OBJECT_MOTION_VECTOR
#endif

#if defined(GPUI_PER_INSTANCE_LIGHTPROBES_ON) && !defined(GPUI_NO_BUFFER)
    #define GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES
    #ifdef unity_SHAr
        #undef unity_SHAr
    #endif
    #ifdef unity_SHAg
        #undef unity_SHAg
    #endif
    #ifdef unity_SHAb
        #undef unity_SHAb
    #endif
    #ifdef unity_SHBr
        #undef unity_SHBr
    #endif
    #ifdef unity_SHBg
        #undef unity_SHBg
    #endif
    #ifdef unity_SHBb
        #undef unity_SHBb
    #endif
    #ifdef unity_SHC
        #undef unity_SHC
    #endif
    #ifdef unity_ProbesOcclusion
        #undef unity_ProbesOcclusion
    #endif
#endif

#ifdef GPUI_NO_BUFFER
    uniform sampler2D_float gpuiTransformBufferTexture;
    uniform sampler2D_float gpuiInstanceDataBufferTexture;
#else // GPUI_NO_BUFFER
    uniform StructuredBuffer<float4x4> gpuiTransformBuffer;
    uniform StructuredBuffer<float4> gpuiInstanceDataBuffer;
#endif // GPUI_NO_BUFFER
#ifdef GPUI_ENABLE_OBJECT_MOTION_VECTOR
    uniform uint hasPreviousFrameTransformBuffer;
    uniform StructuredBuffer<float4x4> gpuiPreviousFrameTransformBuffer;
#endif // GPUI_ENABLE_OBJECT_MOTION_VECTOR
#ifdef GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES
    uniform uint hasPerInstanceLightProbes;
    uniform StructuredBuffer<float4> gpuiPerInstanceLightProbesBuffer;
#endif // GPUI_ENABLE_PER_INSTANCE_LIGHTPROBES

#endif //UNITY_PROCEDURAL_INSTANCING_ENABLED
#endif // GPU_INSTANCER_PRO_INPUT_INCLUDED