Shader "URP/UI/ImageColorGradient"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color1("Color1", Color) = (1,1,1,1)
        _Color2("Color2", Color) = (1,1,1,1)
        _Color3("Color3", Color) = (1,1,1,1)
        _Color4("Color4", Color) = (1,1,1,1)
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
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
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
                if (i.uv.x == 0 && i.uv.y == 0) o.color = i.color * _Color1;
                else if(i.uv.x == 1 && i.uv.y == 0) o.color = i.color * _Color2;
                else if(i.uv.x == 0 && i.uv.y == 1) o.color = i.color * _Color3;
                else o.color = i.color * _Color4;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                return c * o.color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
