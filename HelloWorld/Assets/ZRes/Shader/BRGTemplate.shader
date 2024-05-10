Shader "URP/BRGTemplate"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _Diffuse("Diffuse", Color) = (1,1,1,1)
        _Specular("Specular", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(8.0,256)) = 20
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _BaseColor;
            half4 _Diffuse;
            half4 _Specular;
            half _Gloss;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
            #endif

            struct Input
            {
                float4 pos : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.normal = TransformObjectToWorldNormal(i.normal);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.worldPos = TransformObjectToWorld(i.pos.xyz);
                o.shadowCoord = TransformWorldToShadowCoord(o.worldPos);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(o);
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                
                Light light = GetMainLight();
                half3 worldNormal = normalize(o.normal);
                half3 worldLightDir = normalize(light.direction);
                half3 halfLambert = dot(worldNormal, worldLightDir) * 0.5 + 0.5;
                half3 diffuse = _Diffuse.rgb * light.color.rgb * halfLambert;

                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
                half3 halfDir = normalize(worldLightDir + viewDir);
                half3 specular = _Specular.rgb * light.color.rgb * pow(saturate(dot(worldNormal, halfDir)), _Gloss);

                half shadow = MainLightRealtimeShadow(o.shadowCoord);
                return half4(color.rgb * (diffuse + specular) * _BaseColor.rgb * shadow, color.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
