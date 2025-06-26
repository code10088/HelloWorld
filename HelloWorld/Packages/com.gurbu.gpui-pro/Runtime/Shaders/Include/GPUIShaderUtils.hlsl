// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef GPU_SHADER_UTILS_INCLUDED
#define GPU_SHADER_UTILS_INCLUDED

#ifdef __INTELLISENSE__
static float4x4 unity_ObjectToWorld;
static float4x4 unity_WorldToObject;
static float4x4 UNITY_MATRIX_V;
static float3 _WorldSpaceCameraPos;
static float4 unity_WorldTransformParams;
#endif // __INTELLISENSE__

#ifndef GPUI_PI
#define GPUI_PI            3.14159265359f
#define GPUI_TWO_PI        6.28318530718f
#endif

#ifdef unity_WorldToObject
#undef unity_WorldToObject
#endif
#ifdef unity_ObjectToWorld
#undef unity_ObjectToWorld
#endif

#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/Matrix.hlsl"
#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/GPUIDefines.hlsl"

void GPUIBillboardVertex_float(in float3 vertex_in, in float isSpherical, out float3 vertex_out)
{
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    float4x4 otw = UNITY_MATRIX_M;
    float4x4 wto = UNITY_MATRIX_I_M;
#else
    float4x4 otw = unity_ObjectToWorld;
    float4x4 wto = unity_WorldToObject;
#endif
    // calculate camera vectors
    float3 objectPos = otw._14_24_34;
    float4x4 v = UNITY_MATRIX_V;
    
    float3 up = float3(0, 1, 0);
#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 right = normalize(cross(float3(0, 1, 0), objectPos - _WorldSpaceCameraPos));
#else
    float3 right = normalize(v._m00_m01_m02);
	// adjust rotation matrix if camera is upside down
    right *= sign(normalize(v._m10_m11_m12).y);
    float3 matrixV2 = normalize(v._m10_m11_m12);
    up = lerp(up, float3(matrixV2.x, abs(matrixV2.y), matrixV2.z), isSpherical);
#endif	

	// create camera rotation matrix
    float3 forward = -normalize(v._m20_m21_m22);
    float3x3 rotationMatrix = float3x3(right, up, forward);
    
	// rotate to camera lookAt
    vertex_out = vertex_in * GetScale(otw);
    vertex_out = mul(vertex_out, rotationMatrix);
			
	// account for world position
    vertex_out += objectPos;
			
	// back to object space
    vertex_out = (mul(wto, float4(vertex_out, 1))).xyz;
}

void GPUIBillboardNormalTangent_float(out float3 normal_out, out float3 tangent_out)
{
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    float4x4 otw = UNITY_MATRIX_M;
    float4x4 wto = UNITY_MATRIX_I_M;
#else
    float4x4 otw = unity_ObjectToWorld;
    float4x4 wto = unity_WorldToObject;
#endif
    float3 up = float3(0, 1, 0);
    
    // set vertex normal direction towards camera
    normal_out = _WorldSpaceCameraPos - otw._m03_m13_m23;
    normal_out.y = 0;
    normal_out = normalize(mul((float3x3) wto, normal_out));
    tangent_out = normalize(cross(up, normal_out));
}

void GPUIBillboardNormalTangent(out float3 normal_out, out float4 tangent_out)
{
    float3 tangent;
    GPUIBillboardNormalTangent_float(normal_out, tangent);
    tangent_out = float4(tangent, -1);
}

void GPUIBillboardAtlasUV_float(in float2 atlasUV_in, in float frameCount, out float2 atlasUV_out)
{
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    float4x4 otw = UNITY_MATRIX_M;
    float4x4 wto = UNITY_MATRIX_I_M;
#else
    float4x4 otw = unity_ObjectToWorld;
    float4x4 wto = unity_WorldToObject;
#endif
    float3 billboardCameraDir = normalize(mul((float3x3) wto, _WorldSpaceCameraPos.xyz - otw._m03_m13_m23));
    // get current camera angle in radians
    float angle = atan2(-billboardCameraDir.z, billboardCameraDir.x);
    
    // calculate current frame index and set uvs
    float frameIndex = round((angle - GPUI_PI / 2) / (GPUI_TWO_PI / frameCount));
    atlasUV_out = atlasUV_in * float2(1.0 / frameCount, 1) + float2((1.0 / frameCount) * frameIndex, 0);
}

void GPUIBillboardFragmentNormal_float(in float4 normalTextureSample, in float normalStrength, out float3 normal_out, out float depth_out)
{
    // remap normalTexture back to [-1, 1]
    float3 unpackedNormalTexture = normalTextureSample.xyz * 2.0 - 1.0;
    normal_out = lerp(float3(0, 0, 1), -unpackedNormalTexture, normalStrength);
    depth_out = 1 - normalTextureSample.w;
}

void CalculateHueVariation(float4 hueColor, inout float4 originalColor)
{
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    float4x4 otw = UNITY_MATRIX_M;
#else
    float4x4 otw = unity_ObjectToWorld;
#endif
    float3 worldPosActual = otw._m03_m13_m23;
    float hueVariationAmount = frac(worldPosActual.x + worldPosActual.y + worldPosActual.z);
    hueVariationAmount = saturate(hueVariationAmount * hueColor.a);
    float3 shiftedColor = lerp(originalColor.rgb, hueColor.rgb, hueVariationAmount);
    
    float maxBase = max(originalColor.r, max(originalColor.g, originalColor.b));
    float newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase /= newMaxBase;
    maxBase = maxBase * 0.5f + 0.5f;

    shiftedColor.rgb *= maxBase;
    originalColor.rgb = saturate(shiftedColor);
}

#endif // GPU_SHADER_UTILS_INCLUDED