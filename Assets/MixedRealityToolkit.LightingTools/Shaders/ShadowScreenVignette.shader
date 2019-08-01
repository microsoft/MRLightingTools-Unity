Shader "Hidden/Shadow Screen Vignette"
{
	Properties
	{
		_Brightness ("Brightness", float) = .1
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
			#pragma multi_compile_instancing
			
			#include "UnityCG.cginc"

			struct appdata {
				UNITY_VERTEX_INPUT_INSTANCE_ID

				float4 vertex : POSITION;
			};

			struct v2f {
				UNITY_VERTEX_OUTPUT_STEREO

				float4 vertex : SV_POSITION;
				float2 screen : TEXCOORD0;
			};

			float _Brightness;

			v2f vert (appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.screen = v.vertex.xy;
				o.vertex = v.vertex;
				o.vertex.zw = float2(0,1);

				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target{
				// Exponentially ramp the screen coordinates to create a rounded square shape
				// Add another to make it more square, or remove one to make it more round!
				i.screen *= i.screen;

				// Get distance squared from center of screen
				float dist = dot(i.screen,i.screen);

				// Blend colors on the invers of the distance!
				fixed bright = (1 - dist) * _Brightness;
				return fixed4(bright, bright, bright, 1);
			}
			ENDCG
		}
	}
}
