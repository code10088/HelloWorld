// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef _gpui_terrain_heightmap_hlsl
#define _gpui_terrain_heightmap_hlsl

#pragma multi_compile _ GPUI_TWO_CHANNEL_HEIGHTMAP

#if GPUI_TWO_CHANNEL_HEIGHTMAP
uniform Texture2D<float2> heightmapTexture;
#else
uniform Texture2D<float> heightmapTexture;
#endif
uniform uint heightmapTextureSize;

float SampleHeightmapTexture(uint2 index)
{
#if GPUI_TWO_CHANNEL_HEIGHTMAP
    float2 height = heightmapTexture[index];
    return (height.r + height.g * 256.0f) / 257.0f;
#else
    return heightmapTexture[index];
#endif
}

#endif