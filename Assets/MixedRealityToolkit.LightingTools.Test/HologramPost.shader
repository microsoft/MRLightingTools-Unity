Shader "Hidden/HologramPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_HologramTex("Hologram Tex", 2D) = "black" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			sampler2D _HologramTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col  = tex2D(_MainTex, i.uv);
				fixed4 col2 = tex2D(_HologramTex, i.uv);
				float bright = 1-dot(col2, float3(.2126, .7152, .0722));

				return col * .50 + col2;
				//return lerp(col * .75 + col2, col2, 1-(bright*bright));//lerp(col*0.75, col2, 1-bright*bright);
            }
            ENDCG
        }
    }
}
