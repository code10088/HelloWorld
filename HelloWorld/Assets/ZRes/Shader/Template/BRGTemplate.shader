Shader "URP/BRGTemplate"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask("Color Mask", Float) = 15
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Diffuse("Diffuse", Color) = (1,1,1,1)
        _Specular("Specular", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(8.0,256)) = 20
    }

    SubShader
    {
        Tags 
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        ColorMask [_ColorMask]
        LOD 100

        Pass
        {
            Name "Default"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                half4 _Diffuse;
                half4 _Specular;
                half _Gloss;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _Color UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color)
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
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

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
                
                Light light = GetMainLight(o.shadowCoord);
                half3 worldNormal = normalize(o.normal);
                half3 worldLightDir = normalize(light.direction);
                half3 halfLambert = dot(worldNormal, worldLightDir) * 0.5 + 0.5;
                half3 diffuse = _Diffuse.rgb * light.color.rgb * halfLambert;

                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
                half3 halfDir = normalize(worldLightDir + viewDir);
                half3 specular = _Specular.rgb * light.color.rgb * pow(saturate(dot(worldNormal, halfDir)), _Gloss);

                half shadow = light.shadowAttenuation;
                return half4(color.rgb * (diffuse + specular) * _Color.rgb * shadow, color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            float3 _LightDirection;
            float3 _LightPosition;

            struct Input
            {
                float3 pos : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4 pos : SV_POSITION;
            };

            Output ShadowVert(Input i)
            {
                UNITY_SETUP_INSTANCE_ID(i);

                Output o = (Output)0;
                float3 worldPos = TransformObjectToWorld(i.pos);
                float3 worldNormal = TransformObjectToWorldNormal(i.normal);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - worldPos);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                worldPos = ApplyShadowBias(worldPos, worldNormal, lightDirectionWS);
                o.pos = TransformWorldToHClip(worldPos);
                o.pos = ApplyShadowClamping(o.pos);
                return o;
            }
            half4 ShadowFrag(Output o) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            struct Input
            {
                float3 pos : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4 pos : SV_POSITION;
            };

            Output DepthVert(Input i)
            {
                UNITY_SETUP_INSTANCE_ID(i);

                Output o = (Output)0;
                o.pos = TransformObjectToHClip(i.pos);
                return o;
            }
            half DepthFrag(Output o) : SV_Target
            {
                return o.pos.z;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
