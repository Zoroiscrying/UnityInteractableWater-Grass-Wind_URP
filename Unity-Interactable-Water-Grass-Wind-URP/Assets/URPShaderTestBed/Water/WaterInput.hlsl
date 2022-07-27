#ifndef WATER_INPUT_INCLUDED
#define WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _Color;
float4 _FogColor;
float4 _IntersectionColor;

float _IntersectionThreshold;
float _FogThreshold;
float _FoamThreshold;

TEXTURE2D(_NormalA);    SAMPLER(sampler_NormalA);    float4 _NormalA_ST;
TEXTURE2D(_NormalB);    SAMPLER(sampler_NormalB);    float4 _NormalB_ST;
float _NormalStrength;
float4 _NormalPanningSpeeds;

TEXTURE2D(_FoamTexture);    SAMPLER(sampler_FoamTexture);    float4 _FoamTexture_ST;

float _FoamTextureSpeedX;
float _FoamTextureSpeedY;
float _FoamLinesSpeed;
float _FoamIntensity;

half _Glossiness;
float _FresnelPower;

float4 _CamPosition;
float _OrthographicCamSize;

TEXTURE2D(_WaterWaveResult);    SAMPLER(sampler_WaterWaveResult);    float2 _WaterWaveResult_TexelSize;
float4 _WaterSimulationParams;

half4 _DitherPattern_TexelSize;
CBUFFER_END

SAMPLER(sampler_ScreenTextures_linear_clamp);

#if defined(_REFLECTION_PLANARREFLECTION)
TEXTURE2D(_PlanarReflectionTexture);
#elif defined(_REFLECTION_CUBEMAP)
TEXTURECUBE(_CubemapTexture);
SAMPLER(sampler_CubemapTexture);
#endif

TEXTURE2D(_AbsorptionScatteringRamp); SAMPLER(sampler_AbsorptionScatteringRamp);
TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);
TEXTURE2D(_DitherPattern); SAMPLER(sampler_DitherPattern);
float _MaxDepth;


#endif