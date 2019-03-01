Shader "Unlit/Vignette"
{
	Properties
	{
		_Color ("Color 1", Color) = (1,1,1,1)
		_Color2("Color 2", Color) = (0,0,0,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque+100" }
		LOD 100

		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			float4 _Color;
			float4 _Color2;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = v.vertex;// UnityObjectToClipPos(v.vertex);
				o.vertex.z = 0;// o.vertex.w = 0;
				o.color = v.color;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target{
				return lerp(_Color2, _Color, i.color.a);
			}
			ENDCG
		}
	}
}
