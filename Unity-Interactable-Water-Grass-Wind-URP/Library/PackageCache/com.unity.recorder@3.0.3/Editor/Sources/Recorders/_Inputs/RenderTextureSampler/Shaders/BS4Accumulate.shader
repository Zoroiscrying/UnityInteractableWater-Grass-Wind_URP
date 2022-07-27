// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/BeautyShot/Accumulate" {
Properties {
	_MainTex("Diffuse", 2D) = "white" {}
    _OfsX("OfsX", Float) = 0
    _OfsY("OfsY", Float) = 0
    _Width("Width", Float) = 1
    _Height("Height", Float) = 1
    _Scale("Scale", Float) = 1
    _Pass("Pass", int) = 0
}

CGINCLUDE

// FIXME: Had to comment out to make it work on OSX. Needs to be revised.
// #pragma only_renderers d3d11 ps4 opengl

#include "UnityCG.cginc"
#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#pragma multi_compile ___ VERTICAL_FLIP
#pragma multi_compile ___ SRGB_CONVERSION // perform linear -> sRGB
#pragma multi_compile ___ LINEAR_CONVERSION // perform sRGB -> linear

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

struct v2f {
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata_img v)  {
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.texcoord = v.texcoord;
	return o;
}

UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
uniform float4 _MainTex_TexelSize;

uniform sampler2D _PreviousTexture;
float _OfsX;
float _OfsY;
float _Width;
float _Height;
float _Scale;
int _Pass;

float4 frag(v2f i) : SV_Target {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 t = i.texcoord;
    #if defined(VERTICAL_FLIP)
        t.y = 1.0 - t.y;
    #endif
    float4 previous = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_PreviousTexture, t);

	float2 tmp = i.texcoord;
    float4 current = {0,0,0,0};
    if(i.texcoord.x >= _OfsX && i.texcoord.x <= (_OfsX+ _Width) && i.texcoord.y >= _OfsY && i.texcoord.y <= (_OfsY + _Height))
    {
        tmp.x = (tmp.x - _OfsX) / _Scale;
        tmp.y = (tmp.y - _OfsY) / _Scale;
        #if defined(VERTICAL_FLIP)
            tmp.y = 1.0 - tmp.y;
        #endif
        current = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, tmp);

        #if defined(SRGB_CONVERSION)
			current = LinearToSRGB(current);
        #elif defined(LINEAR_CONVERSION)
			current = SRGBToLinear(current);
        #endif

        if (_Pass == 0)
            return current;
        else
            return previous + current;
    }
    else
        return previous;
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
