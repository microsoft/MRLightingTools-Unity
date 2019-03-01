// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit With Shadows" {
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
				#pragma multi_compile_fwdbase
				#pragma fragmentoption ARB_fog_exp2
				#pragma fragmentoption ARB_precision_hint_fastest
				
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				
				struct v2f
				{
					float4 pos : SV_POSITION;
					LIGHTING_COORDS(1,2)
				};

				float4 _Color;

				v2f vert (appdata_tan v)
				{
					v2f o;
					
					o.pos = UnityObjectToClipPos( v.vertex);
					TRANSFER_VERTEX_TO_FRAGMENT(o);
					return o;
				}

				fixed4 frag(v2f i) : COLOR
				{
					//fixed atten = LIGHT_ATTENUATION(i);	// Light attenuation + shadows.
					fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
					clip(0.5-atten);
					return _Color;// tex2D(_MainTex, i.uv) * (lerp(UNITY_LIGHTMODEL_AMBIENT, fixed4(1, 1, 1, 1), atten)) * _Color;
				}
			ENDCG
		}
	}
	FallBack "VertexLit"
}