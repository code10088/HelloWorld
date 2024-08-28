Shader "URP/UI/Light"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Power("Power", Range(0,2)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        LOD 100

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Power;
            CBUFFER_END

            struct Input
            {
                float3 pos : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Output
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                return half4(c.rgb * _Power, c.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
