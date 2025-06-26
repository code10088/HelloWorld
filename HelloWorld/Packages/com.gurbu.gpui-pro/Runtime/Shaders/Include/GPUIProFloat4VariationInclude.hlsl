// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#include "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"

#ifndef GPUIPro_F4Var_INCLUDED
#define GPUIPro_F4Var_INCLUDED

uniform StructuredBuffer<float4> gpuiProFloat4Variation;

void gpuiProF4Var_float(out float4 Float4_Out)
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    Float4_Out = gpuiProFloat4Variation[gpui_InstanceID];
#else
    Float4_Out = float4(1, 1, 1, 1);
#endif
}
#endif // GPUIPro_F4Var_INCLUDED