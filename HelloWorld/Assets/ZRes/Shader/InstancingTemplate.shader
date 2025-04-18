Shader "URP/InstancingTemplate"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
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
            //Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            CBUFFER_END
            //不同Instancing不用属性
            //UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            //UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
            //UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            //UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            struct Input
            {
                float3 pos : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);
                //不同Instancing不用属性
                //UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(o);
                //不同Instancing不用属性
                //float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color)

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                return c * _Color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
