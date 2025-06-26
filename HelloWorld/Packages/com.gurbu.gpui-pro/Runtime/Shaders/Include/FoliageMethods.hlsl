// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef GPUI_FOLIAGE_METHODS_INCLUDED
#define GPUI_FOLIAGE_METHODS_INCLUDED

#include "Packages/com.gurbu.gpui-pro/Runtime/Compute/Include/Matrix.hlsl"
#include "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUIShaderUtils.hlsl"

//void BillboardFoliageVertex(float3 vertex_in, out float3 vertex_out)
//{
//    float3 scaledVertex = vertex_in.xyz * GetScale(unity_ObjectToWorld);
//    float3 objectPos = unity_ObjectToWorld._14_24_34;
    
//#ifdef BILLBOARD_FACE_CAMERA_POS
//	float3 xRotation =  normalize(cross(float3(0, 1, 0), (objectPos - _WorldSpaceCameraPos)));
//	float3 yRotation = float3(0,1,0);
//#else
//    float3 xRotation = normalize(UNITY_MATRIX_V._11_12_13);
//    float3 matrixV2 = normalize(UNITY_MATRIX_V._21_22_23);
//    float3 yRotation = ((matrixV2.y > 0.0) ? matrixV2 : float3(matrixV2.x, (matrixV2.y * -1.0), matrixV2.z));
//#endif
    
//    float3 matrixV3 = normalize(UNITY_MATRIX_V._31_32_33);
//    float3 rotatedVertex = mul(scaledVertex, float3x3(xRotation, yRotation, matrixV3 * -1.0));
    
//    float4 billboardVertexWorld = float4((rotatedVertex + objectPos), 1);
//    vertex_out = mul(unity_WorldToObject, billboardVertexWorld).xyz;
//}

void HealthyDryTextureUV_float(float3 worldPos, float noiseSpread, out float2 uv)
{
    uv = worldPos.xz * 0.05 * noiseSpread;
}

void WindWaveNormalUV_float(float3 worldPos, float2 windVector, float windWaveSize, out float2 uv)
{
    float timeMultiplier = _Time.y * (length(windVector) * 0.01);
    uv = timeMultiplier * windVector + ((1.0 - windWaveSize * 0.9) * 0.003 * worldPos.xz);
}

void BillboardAndWind_float(float3 vertex_in, float3 windWaveSample, float isBillboard, float isWindWavesOn, float windIdleSway, float windWaveSway, float2 windVector, out float3 vertex_out)
{
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    float4x4 wto = UNITY_MATRIX_I_M;
#else
    float4x4 wto = unity_WorldToObject;
#endif
    vertex_out = vertex_in;
    if (isBillboard > 0)
        GPUIBillboardVertex_float(vertex_in, 1, vertex_out);
    
    // Wind Start
    windWaveSample = float3(windWaveSample.x, 0, windWaveSample.y);
    float3 windIdleSwayCalculated = lerp(float3(0, 0, 0), (windWaveSample * windIdleSway), saturate(vertex_in.y));
    float2 windWaveSampleSaturated = saturate(windWaveSample.z) * windVector;
    float3 windWaveSwayCalculated = lerp(float3(0, 0, 0), (windWaveSway * 20.0 * float3(windWaveSampleSaturated.x, 0, windWaveSampleSaturated.y)), saturate(vertex_in.y));
    
    float3 windVertexOffset = mul(wto, float4(lerp(windIdleSwayCalculated, windIdleSwayCalculated - windWaveSwayCalculated, saturate(windWaveSample.z) * isWindWavesOn), 0.0)).xyz;
    // Wind End
    
    vertex_out += windVertexOffset;
}

void FoliageAlbedo_float(float4 mainTexSample, float4 healthyDryNoiseSample, float3 windWaveSample, float4 _HealthyColor, float4 _DryColor, float4 _WindWaveTintColor, float4 _GradientContrastRatioTint, float vertexLocalY, float _WindWavesOn, out float3 albedo)
{
    float4 healthyDryColor = lerp(_DryColor, _HealthyColor, pow(saturate(healthyDryNoiseSample.r), _GradientContrastRatioTint.z));
    float4 windWaveTint = lerp(healthyDryColor, _WindWaveTintColor, saturate(saturate(windWaveSample.r * 4.0) * _GradientContrastRatioTint.w * _WindWavesOn));
    windWaveTint.rgb = saturate(lerp(float3(0.5, 0.5, 0.5), windWaveTint.rgb, _GradientContrastRatioTint.y));
    float gradient = (1.0 - _GradientContrastRatioTint.x) + saturate(vertexLocalY) * _GradientContrastRatioTint.x;
    albedo = windWaveTint.rgb * mainTexSample.rgb * gradient;
}

void FoliageVertex(float3 worldPos, float3 vertex_in, float isBillboard, float isWindWavesOn, float windWaveSize, float windIdleSway, float windWaveSway, float2 windVector, sampler2D _WindWaveNormalTexture, out float3 vertex_out)
{
    float2 windWaveUV;
    WindWaveNormalUV_float(worldPos, windVector, windWaveSize, windWaveUV);
    float3 windWaveSample = UnpackNormal(tex2Dlod(_WindWaveNormalTexture, float4(windWaveUV, 0, 0)));
    
    BillboardAndWind_float(vertex_in, windWaveSample, isBillboard, isWindWavesOn, windIdleSway, windWaveSway, windVector, vertex_out);
}

void FoliageFragment(float4 mainTexSample, float vertexLocalY, float3 worldPos, float _NoiseSpread, sampler2D _HealthyDryNoiseTexture, float4 _HealthyColor, float4 _DryColor, float2 _WindVector, float _WindWaveSize, float4 _WindWaveTintColor, sampler2D _WindWaveNormalTexture, float _WindWavesOn, float4 _GradientContrastRatioTint, out float3 albedo)
{
    float2 healthyDryUV;
    HealthyDryTextureUV_float(worldPos, _NoiseSpread, healthyDryUV);
    float4 healthyDryNoiseSample = tex2D(_HealthyDryNoiseTexture, healthyDryUV);   
    
    float2 windWaveUV;
    WindWaveNormalUV_float(worldPos, _WindVector, _WindWaveSize, windWaveUV);
    float3 windWaveSample = UnpackNormal(tex2D(_WindWaveNormalTexture, windWaveUV));
    
    FoliageAlbedo_float(mainTexSample, healthyDryNoiseSample, windWaveSample, _HealthyColor, _DryColor, _WindWaveTintColor, _GradientContrastRatioTint, vertexLocalY, _WindWavesOn, albedo);
}

#endif