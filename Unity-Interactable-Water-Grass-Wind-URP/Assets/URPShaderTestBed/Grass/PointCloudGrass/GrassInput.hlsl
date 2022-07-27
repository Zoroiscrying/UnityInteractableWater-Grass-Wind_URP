#ifndef GRASS_INPUT_INCLUDED
#define GRASS_INPUT_INCLUDED

struct Attributes {
    float4 positionOS   : POSITION;
    float3 normalOS		: NORMAL;
    float4 tangentOS		: TANGENT;
    float4 vertexColor  : COLOR;
    float2 texcoord     : TEXCOORD0;
};

struct Varyings {
    float4 positionOS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalOS		: NORMAL;
    float4 tangentOS    : TANGENT;
    float4 color        : COLOR;
};

struct GeometryOutput {
    float4 positionCS	: SV_POSITION;
    float3 positionWS	: TEXCOORD1;
    float2 uv			: TEXCOORD0;
    half   NoiseValue   : TEXCOORD2;
    float  fogFactor    : TEXCOORD3;
    float3 normalWS     : TEXCOORD4;
    float4 diffuseColor : COLOR;
    float3 viewDirectionWS : TEXCOORD5;
};

float3 _LightDirection;

// Variables
CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher

TEXTURE2D(_HeightNoiseTexture);		SAMPLER(sampler_HeightNoiseTexture);
TEXTURE2D(_ColorNoiseTexture);		SAMPLER(sampler_ColorNoiseTexture);   float4 _ColorNoiseTexture_ST;
TEXTURE2D(_ColorRampTexture);		SAMPLER(sampler_ColorRampTexture);

float _ColorNoiseScale;
half3 _BottomColor;
float _BottomFactor;

#if defined(GRASS_UNIFORM_QUAD)
TEXTURE2D(_GrassBladeTexture);		SAMPLER(sampler_GrassBladeTexture);
#endif

float _HeightTextureMult;

float3 _ColliderPosition;

float _BladeCurvature;

float _WindStrengthY;

float _Width;
float _RandomWidth;
float _Height;
float _RandomHeight;

float _collisionOffsetMaxDistance;
float _collisionStrength;

float _grassBladeLODMaxDistance;
CBUFFER_END

#endif