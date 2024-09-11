Shader "URP/UI/ImageCircle"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Circle("_Circle", float) = 0.5
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
            float _Circle;
            CBUFFER_END

            struct Input
            {
                float3 pos : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct Output
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                o.pos = TransformObjectToHClip(i.pos);
                o.color = i.color;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                float dis = distance(o.uv, half2(0.5, 0.5));
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                return c * o.color * step(dis, _Circle);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
