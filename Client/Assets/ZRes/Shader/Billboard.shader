Shader "URP/Billboard"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask("Color Mask", Float) = 15
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
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
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float3 objectScale = float3(length(float3(objectToWorld._m00, objectToWorld._m10, objectToWorld._m20)), length(float3(objectToWorld._m01, objectToWorld._m11, objectToWorld._m21)), length(float3(objectToWorld._m02, objectToWorld._m12, objectToWorld._m22)));
                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 offsetWS = mul((float3x3)UNITY_MATRIX_I_V, input.positionOS * objectScale);
                output.positionHCS = TransformWorldToHClip(centerWS + offsetWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return color * _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
