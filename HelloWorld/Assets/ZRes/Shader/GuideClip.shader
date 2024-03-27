Shader "URP/GuideClip"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Center("Center", vector) = (0,0,0,0)
        _Width("Width", Range(0,1000)) = 100
        _Height("Height", Range(0,1000)) = 100
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        LOD 100

        Pass
        {
            //Tags { "LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local_fragment __ CIRCLE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float2 _Center;
            float _Width;
            float _Height;

            struct Input
            {
                float3 pos : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4  pos : SV_POSITION;
                float2  uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.worldPos = i.pos;
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
#if CIRCLE
                c.a *= lerp(0, 1, (distance(o.worldPos.xy, _Center) - _Width) / _Height);
#else
                float2 dis = o.worldPos.xy - _Center;
                c.a *= step(step(abs(dis.x), _Width), step(_Height, abs(dis.y)));
#endif
                c.rgb *= _Color.rgb;
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
