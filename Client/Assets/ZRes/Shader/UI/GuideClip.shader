Shader "URP/UI/GuideClip"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask("Color Mask", Float) = 15
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Center("Center", vector) = (0,0,0,0)
        _Width("Width", Range(0,1000)) = 100
        _Height("Height", Range(0,1000)) = 100
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        ColorMask [_ColorMask]
        LOD 100

        Pass
        {
            Name "Default"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local_fragment __ CIRCLE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _Center;
                float _Width;
                float _Height;
            CBUFFER_END

            struct Input
            {
                float3 pos : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4  pos : SV_POSITION;
                float2  uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);

                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.worldPos = i.pos;
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
#if CIRCLE
                c.a *= smoothstep(0, 1, (distance(o.worldPos.xy, _Center.xy) - _Width) / _Height);
#else
                float2 dis = o.worldPos.xy - _Center.xy;
                c.a *= step(step(abs(dis.x), _Width), step(_Height, abs(dis.y)));
#endif
                return c * _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
