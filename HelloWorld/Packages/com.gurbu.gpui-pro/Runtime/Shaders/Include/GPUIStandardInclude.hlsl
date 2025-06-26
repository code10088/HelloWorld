#ifndef GPU_INSTANCER_STANDARD_INCLUDED
#define GPU_INSTANCER_STANDARD_INCLUDED

// FORWARD RENDERING:
#if !UNITY_STANDARD_SIMPLE
half4 fragForwardBaseInternalGPUI(VertexOutputForwardBase i)
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP(s);

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    UnityLight mainLight = MainLight();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

    half occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

    half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
    c.rgb += Emission(i.tex.xy);

    UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
    UNITY_APPLY_FOG(_unity_fogCoord, c.rgb);
    return OutputForward(c, s.alpha);
}
#endif

#if UNITY_STANDARD_SIMPLE
    half4 fragBaseGPUI (VertexOutputBaseSimple i) : SV_Target { return fragForwardBaseSimpleInternal(i); }
#else
    half4 fragBaseGPUI (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternalGPUI(i); }
#endif
    
void fragDeferredGPUI(
    VertexOutputDeferred i,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3 // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    ,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
#if (SHADER_TARGET < 30)
    outGBuffer0 = 1;
    outGBuffer1 = 1;
    outGBuffer2 = 0;
    outEmission = 0;
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    outShadowMask = 1;
#endif
    return;
#endif
    
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP(s)

    // no analytic lights in this pass
    UnityLight dummyLight = DummyLight();
    half atten = 1;

    // only GI
    half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
    bool sampleReflectionsInDeferred = false;
#else
    bool sampleReflectionsInDeferred = true;
#endif

    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

    half3 emissiveColor = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

#ifdef _EMISSION
        emissiveColor += Emission (i.tex.xy);
#endif

#ifndef UNITY_HDR_ON
    emissiveColor.rgb = exp2(-emissiveColor.rgb);
#endif

    UnityStandardData data;
    data.diffuseColor = s.diffColor;
    data.occlusion = occlusion;
    data.specularColor = s.specColor;
    data.smoothness = s.smoothness;
    data.normalWorld = s.normalWorld;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Emissive lighting buffer
    outEmission = half4(emissiveColor, 1);

    // Baked direct lighting occlusion if any
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
        outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
#endif
}

#endif