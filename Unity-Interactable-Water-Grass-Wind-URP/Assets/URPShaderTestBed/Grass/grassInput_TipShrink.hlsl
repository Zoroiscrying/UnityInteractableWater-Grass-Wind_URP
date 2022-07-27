#ifndef GRASS_INPUT_INCLUDED
#define GRASS_INPUT_INCLUDED

float3 _LightDirection;

// Variables
CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher

TEXTURE2D(_WindNoiseTexture);		SAMPLER(sampler_WindNoiseTexture);
TEXTURE2D(_HeightNoiseTexture);		SAMPLER(sampler_HeightNoiseTexture);
TEXTURE2D(_ColorNoiseTexture);		SAMPLER(sampler_ColorNoiseTexture);   float4 _ColorNoiseTexture_ST;
TEXTURE2D(_ColorRampTexture);		SAMPLER(sampler_ColorRampTexture);

float _HeightTextureMult;

float3 _ColliderPosition;

float _BladeCurvature;

float _WindPosMult;
float _WindTimeMult;
float _WindTexMult;
float _WindStrength;

float _Width;
float _RandomWidth;
float _Height;
float _RandomHeight;

float _collisionOffsetMaxDistance;
float _collisionStrength;

float _grassBladeLODMaxDistance;
CBUFFER_END

#endif