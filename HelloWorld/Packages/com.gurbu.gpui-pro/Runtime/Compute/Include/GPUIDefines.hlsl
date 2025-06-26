// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef _gpui_defines_hlsl
#define _gpui_defines_hlsl

struct GPUICommandArgs
{
    uint indexCountPerInstance;
    uint instanceCount;
    uint startIndex;
    uint baseVertexIndex;
    uint startInstance;
};

struct GPUIVisibilityData
{
    uint visibleCount;
    uint commandStartIndex;
    uint commandCount;
    uint additional; // 0 for instances,  1 for shadows
};

struct GPUICounterData
{
    uint count;
    uint dummy1; // We need padding because GPU loads data in 16 bytes and we use counters with InterlockedAdd
    uint dummy2;
    uint dummy3;
};

struct GPUITransformData
{
    float3 position; // Translation (12 bytes)
    float4 rotation; // Quaternion for rotation (16 bytes)
    float3 scale; // Non-uniform scale (12 bytes)
};

struct GPUISphericalHarmonicsL2
{
    float shr0;
    float shr1;
    float shr2;
    float shr3;
    float shr4;
    float shr5;
    float shr6;
    float shr7;
    float shr8;
    float shg0;
    float shg1;
    float shg2;
    float shg3;
    float shg4;
    float shg5;
    float shg6;
    float shg7;
    float shg8;
    float shb0;
    float shb1;
    float shb2;
    float shb3;
    float shb4;
    float shb5;
    float shb6;
    float shb7;
    float shb8;
};

static const float3 vector3Up = float3(0, 1, 0);
static const float3 vector3One = float3(1, 1, 1);
static const float4 vector4One = float4(1, 1, 1, 1);
static const float4x4 identityMatrix = float4x4(1, 0, 0, 0,
                                                0, 1, 0, 0,
                                                0, 0, 1, 0,
                                                0, 0, 0, 1);
static const float4x4 zeroMatrix = float4x4(0, 0, 0, 0,
                                            0, 0, 0, 0,
                                            0, 0, 0, 0,
                                            0, 0, 0, 0);

static const uint2 uint2Zero = uint2(0, 0);
static const uint2 uint2One = uint2(1, 1);

static const float GPUIPI = 3.14159265;
static const float GPUITwoPI = 6.28318531;
static const float GPUIFloatMax = 3.402823466e+38;

static const GPUITransformData identityGPUITransform =
{
    float3(0, 0, 0), // position
    float4(0, 0, 0, 1), // rotation (identity quaternion)
    float3(1, 1, 1) // scale
};

#endif