// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef _gpui_platformDefines_hlsl
#define _gpui_platformDefines_hlsl

#pragma multi_compile _ GPUI_THREAD_SIZE_256 GPUI_THREAD_SIZE_512

#if GPUI_THREAD_SIZE_512
    #define GPUI_THREADS 512
    #define GPUI_THREADS_2D 16
    #define GPUI_THREADS_3D 8
#elif GPUI_THREAD_SIZE_256
    #define GPUI_THREADS 256
    #define GPUI_THREADS_2D 16
    #define GPUI_THREADS_3D 4
#else
    #define GPUI_THREADS 64
    #define GPUI_THREADS_2D 8
    #define GPUI_THREADS_3D 4
#endif

#define GPUI_API_HAS_GUARANTEED_R16_SUPPORT !(SHADER_API_VULKAN || SHADER_API_GLES || SHADER_API_GLES3)

#endif