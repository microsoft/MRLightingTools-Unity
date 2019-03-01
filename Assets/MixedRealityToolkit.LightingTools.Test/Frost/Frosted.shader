// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Frosted" {
	Properties {
		_Color    ("Color",     Color) = (1,1,1,1)
		_MainTex  ("Texture",   2D) = "white" {}
		_FrostMap ("FrostMap",  2D) = "white" {}
		_FrostWarp("FrostWarp", 2D) = "normal" {}
		_Frost    ("Frost",     Range(0,1)) = 1

		//_Smoothness("Smoothness", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct appdata {
				float4 vertex  : POSITION;
				float2 uv      : TEXCOORD0;
				half3  normal  : NORMAL;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD2;
				half3 normal  : NORMAL;
			};

			sampler2D _FrostMap;
			sampler2D _FrostWarp;
			sampler2D _MainTex;
			float4    _MainTex_ST;
			float     _Smoothness;
			float     _Frost;
			float4    _Color;

			float4 _CubePos;
			float3 _CubeMin;
			float3 _CubeMax;
			float4 _CubeRot;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex   = UnityObjectToClipPos(v.vertex);
				o.uv       = TRANSFORM_TEX(v.uv, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.normal   = UnityObjectToWorldNormal(v.normal);
				
				return o;
			}
			
			float3 BoxProjection(float3 direction, float3 position) {
				float3 boxPos = _CubePos.xyz;
				float2 boxRot = _CubeRot;
				float3 boxMin = _CubeMin;
				float3 boxMax = _CubeMax;

				position = position - boxPos;
				position.xz = float2(
					position.x * boxRot.x - position.z * boxRot.y,
					position.x * boxRot.y + position.z * boxRot.x);
				direction.xz = float2(
					direction.x * boxRot.x - direction.z * boxRot.y,
					direction.x * boxRot.y + direction.z * boxRot.x);
				//position = clamp(position, boxMin*.8, boxMax*.8); // don't let the position go outside the box, this leads to ugly!
				position = position + boxPos;

				float3 factors = ((direction > 0 ? boxMax : boxMin) - position) / direction;
				float  scalar  = min(min(factors.x, factors.y), factors.z);
				direction = direction * scalar + (position - boxPos);

				direction.xz = float2(
					direction.x *  boxRot.x + direction.z * boxRot.y,
					direction.x * -boxRot.y + direction.z * boxRot.x);
				return direction;
			}

			fixed4 frag (v2f i) : SV_Target {
				
				fixed4 col      = tex2D(_MainTex,  i.uv);
				fixed4 frostMap = tex2D(_FrostMap, i.uv);
				float3 warp     = UnpackNormal(tex2D(_FrostWarp, i.uv));
				
				float3 normal      = normalize(i.normal);
				float3 frostNormal = normalize((i.worldPos - _WorldSpaceCameraPos));
				//return fixed4((frostNormal.xyz+1)/2, 1);

				frostNormal = BoxProjection(frostNormal, i.worldPos);
				float3 projNormal = BoxProjection(normal, i.worldPos);

				half3  worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				half3  worldRefl    = reflect  (-worldViewDir, normal);
				float3 halfVector   = normalize(_WorldSpaceLightPos0.xyz + worldViewDir);

				float specularHigh = pow(saturate(dot(halfVector, normal)), (1-_Frost*frostMap.r) * 40);
				half3 specColor    = _LightColor0;// *lerp(half3(1, 1, 1), albedo, 1 - metal);
				half3 specular     = specColor * specularHigh;

				half3 ambient = ShadeSH9(half4(projNormal, 1));// DecodeHDR(ambientSample, unity_SpecCube0_HDR);
				half  nl = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
				ambient = float3(1, 1, 1);

				half4 frost = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, frostNormal,_Frost*8*frostMap.r);
				frost = half4(DecodeHDR(frost, unity_SpecCube0_HDR), 1);
				return float4((ambient + nl * _LightColor0)*frost.rgb*col.rgb*_Color + specular, 1);
			}
			ENDCG
		}
	}
}
