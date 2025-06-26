// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef _gpui_terrain_defines_hlsl
#define _gpui_terrain_defines_hlsl

struct GPUITreeInstance
{
    float3 position;
    float3 scale;
    float rotation;
    float4 color;
    uint prototypeIndex;
};

struct TerrainTreeInstance
{
    float3 position;
    float widthScale;
    float heightScale;
    float rotation;
    float color;
    float lightmapColor;
    int prototypeIndex;
    float dummy;
};

#endif