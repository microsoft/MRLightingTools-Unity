Shader "Mixed Reality Toolkit/Shadow AR Transparent" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque"}

		Pass {
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fwdbase nolightmap
				
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			
			struct appdata {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float3 vertex : POSITION;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				SHADOW_COORDS(0)
			};

			float4 _Color;

			v2f vert (appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o)
				return o;
			}

			fixed4 frag(v2f i) : COLOR {
				fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
				clip((0.5-atten));
				return _Color;
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}