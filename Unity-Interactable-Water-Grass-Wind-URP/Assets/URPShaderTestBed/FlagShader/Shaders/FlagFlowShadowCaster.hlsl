#ifndef FLAG_FLOW_SHADOWCASTER
#define FLAG_FLOW_SHADOWCASTER

#include "../../Wind/GlobalWind.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS		: NORMAL;
    float2 uv0          : TEXCOORD0;
    float4 tangentOS    : TANGENT;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 normalWS		: NORMAL;
    float3 positionWS	: TEXCOORD1;
    float2 uv           : TEXCOORD0;
};

//Inputs
CBUFFER_START(UnityPerMaterial)

TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);     float4 _BaseMap_ST;
TEXTURE2D(_OcclusionMap);
half3 _Tint;
half4 _FlagDispStrength;

CBUFFER_END

//Custom Functions

float3 _LightDirection;
float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS) {
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

//Vert and Frag functions

Varyings vert(Attributes input)
{

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    
    // vertex displacement
    float3 windNoise = GetWindFromWorld(positionWS * 2);
    float3 wind_modified_value_float3 = float3(windNoise.x, windNoise.y, windNoise.z) * _FlagDispStrength.xyz;
    // modified normal calculation
    float3 displacedPositionOS = input.positionOS + wind_modified_value_float3;
    
    float3 bitangent = cross(input.normalOS, input.tangentOS);
    float3 posPlusTangent = displacedPositionOS + input.tangentOS * 0.01;
    float3 posPlusBitangent = displacedPositionOS + bitangent * 0.01;
    float3 modifiedTangent = posPlusTangent - displacedPositionOS;
    float3 modifiedBitangent = posPlusBitangent - displacedPositionOS;
    // - Get modified normal
    float3 modifiedNormal = normalize(cross(modifiedTangent, modifiedBitangent));

    float3 normalWS = TransformObjectToWorldNormal(modifiedNormal); 

    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output.positionWS = TransformObjectToWorld(displacedPositionOS.xyz);
    output.positionCS = GetShadowPositionHClip(output.positionWS, normalWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv0;

    return output;
}

float4 frag(Varyings input) : SV_Target
{
    float2 albedo_uv = TRANSFORM_TEX(input.uv, _BaseMap);
    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, albedo_uv).a;
    clip(alpha - 0.5);
    return 0;
}


#endif