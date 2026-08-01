Shader "URP/InstancingTemplate"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask("Color Mask", Float) = 15
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
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
            Tags { "LightMode" = "SRPDefaultUnlit" }
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
            CBUFFER_END

            //如果实例只有位置、旋转、缩放不同，不需要定义InstancedProperty
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

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
                //InstancedProperty
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(o);
                //InstancedProperty
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                return c * color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
