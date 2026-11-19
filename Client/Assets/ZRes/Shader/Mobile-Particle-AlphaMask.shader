Shader "URP/Particles/Alpha Blended Mask"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _MinX("MinX", Float) = 0
        _MinY("MinY", Float) = 0
        _MaxX("MaxX", Float) = 0
        _MaxY("MaxY", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            //Tags { "LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            //#pragma multi_compile_fragment _ DEBUG_DISPLAY

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _MinX;
            float _MinY;
            float _MaxX;
            float _MaxY;

            struct Input
            {
                float3 pos : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Output
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Output vert(Input i)
            {
                Output o = (Output)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(i.pos);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.worldPos = TransformObjectToWorld(i.pos).xy;
                return o;
            }
            half4 frag(Output o) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                half posX = o.worldPos.x;
                half posY = o.worldPos.y;
                half result = step(_MinX, posX) * step(posX, _MaxX) * step(_MinY, posY) * step(posY, _MaxY);
                return result * c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
