// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Volund/BS4SuperShader" {
Properties {
	_MainTex("Diffuse", 2D) = "white" {}
}

CGINCLUDE

// FIXME: Had to comment out to make it work on OSX. Needs to be revised.
// #pragma only_renderers d3d11 ps4 opengl

#include "UnityCG.cginc"
#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#pragma multi_compile ___ VERTICAL_FLIP
#pragma multi_compile ___ SRGB_CONVERSION // perform linear -> sRGB
#pragma multi_compile ___ LINEAR_CONVERSION // perform sRGB -> linear

struct v2f {
	float4 pos	: SV_Position;
	float2 uv	: TEXCOORD0;
};

v2f vert(appdata_img v)  {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord;
	return o;
}

uniform sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;

uniform float4 _Target_TexelSize;

uniform float _KernelCosPower;
uniform float _KernelScale;
uniform float _NormalizationFactor;

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

float4 frag(v2f i) : SV_Target {
	const int width = ceil(_MainTex_TexelSize.z / _Target_TexelSize.z / 2.f);
	const float ratio = 1.f / (1.41f * width);

	float weight = 0.f;
	float4 color = float4(0.f, 0.f, 0.f, 0.f);


	for(int y = -width; y <= width; ++y) {
		for(int x = -width; x <= width; ++x) {
			float2 off = float2(x * _MainTex_TexelSize.x, y * _MainTex_TexelSize.y);
			float2 uv = i.uv + off;
#if defined(VERTICAL_FLIP)
		    uv.y = 1.0f - uv.y;
#endif

			float4 s = tex2D(_MainTex, uv).rgba;

			float c = clamp(sqrt(x*x + y*y) * ratio * (1.f/_KernelScale), -1.57f, 1.57f);
			float w = pow(cos(c), _KernelCosPower);
			color.rgba += s.rgba * w;
			weight += w;
		}
	}

    fixed4 colorResult = _NormalizationFactor * color.rgba / weight;

#if defined(SRGB_CONVERSION)
	return LinearToSRGB(colorResult);
#elif defined(LINEAR_CONVERSION)
    return SRGBToLinear(colorResult);
#else
    return colorResult;
#endif
}

ENDCG

SubShader {
	Cull Off ZTest Always ZWrite Off

	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		ENDCG
	}
}}
