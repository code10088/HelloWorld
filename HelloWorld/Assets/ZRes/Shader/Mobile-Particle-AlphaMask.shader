// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Alpha Blended Particle shader. Differences from regular Alpha Blended Particle one:
// - no Tint color
// - no Smooth particle support
// - no AlphaTest
// - no ColorMask

Shader "Mobile/Particles/Alpha Blended Mask" {
Properties {
    _MainTex ("Particle Texture", 2D) = "white" {}
    _MinX("MinX", Float) = 0
    _MinY("MinY", Float) = 0
    _MaxX("MaxX", Float) = 0
    _MaxY("MaxY", Float) = 0
}

SubShader {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 worldPos : TEXCOORD1;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float _MinX;
        float _MinY;
        float _MaxX;
        float _MaxY;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            fixed4 col = tex2D(_MainTex, i.uv);
            float posX = i.worldPos.x;
            float posY = i.worldPos.y;
            fixed result = step(_MinX, posX) * step(posX, _MaxX) * step(_MinY, posY) * step(posY, _MaxY);
            return result * col;
        }
        ENDCG
    }
}
}
