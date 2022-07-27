Shader "Hidden/BeautyShot/Normalize" {
	Properties { _MainTex ("Texture", any) = "" {} }

	SubShader {
		Pass {
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
            #pragma multi_compile ___ VERTICAL_FLIP
            #pragma multi_compile ___ SRGB_CONVERSION // perform linear -> sRGB
            #pragma multi_compile ___ LINEAR_CONVERSION // perform sRGB -> linear

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			uniform float4 _MainTex_ST;

			float _NormalizationFactor;

            struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				return o;
			}

			// This is a port of the HLSL code in StdLib.hlsl
			float3 PositivePow(float3 base, float3 power)
			{
				return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
			}

			// This is a port of the HLSL code in Color.hlsl. Also see https://entropymine.com/imageworsener/srgbformula
			float3 LinearToSRGB(float3 c)
			{
				float3 sRGBLo = c * 12.92;
				float3 sRGBHi = (PositivePow(c, float3(1.0/2.4, 1.0/2.4, 1.0/2.4)) * 1.055) - 0.055;
				float3 sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
				return sRGB;
			}

			float4 LinearToSRGB(float4 c)
			{
				return float4(LinearToSRGB(c.rgb), c.a);
			}

            // See https://entropymine.com/imageworsener/srgbformula/
            float3 SRGBToLinear(float3 c)
			{
				float3 LinearLo = c / 12.92;
				float3 LinearHi = PositivePow((c + 0.055)/1.055, float3(2.4, 2.4, 2.4));
				float3 Linear   = (c <= 0.04045) ? LinearLo : LinearHi;
				return Linear;
			}

			float4 SRGBToLinear(float4 c)
			{
				return float4(SRGBToLinear(c.rgb), c.a);
			}

			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 t = i.texcoord;
                #if defined(VERTICAL_FLIP)
                    t.y = 1.0 - t.y;
                #endif
                fixed4 color = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, t) * _NormalizationFactor;

                #if defined(SRGB_CONVERSION)
				    return LinearToSRGB(color);
                #elif defined(LINEAR_CONVERSION)
				    return SRGBToLinear(color);
                #else
                    return color;
                #endif
			}
			ENDCG

		}
	}
	Fallback Off
}
