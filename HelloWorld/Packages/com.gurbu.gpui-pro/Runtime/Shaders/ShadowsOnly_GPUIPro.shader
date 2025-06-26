Shader "Hidden/GPUInstancerPro/ShadowsOnly" {

	SubShader {
		Tags {
		"Queue" = "Transparent"
		}
		// Pass to render object as a shadow caster
		Pass {
			Name "ShadowCaster"
			Tags { 
				"LightMode" = "ShadowCaster" 
			}

			CGPROGRAM
		    #pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma instancing_options procedural:setupGPUI

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i);
			}

			ENDCG
		}

	}
}
