///////////////////////////////////////////////////
//////////////// grass_struct.hlsl ////////////////
///////////////////////////////////////////////////
#ifndef GRASS_STRUCT_INCLUDED
#define GRASS_STRUCT_INCLUDED

struct Attributes {
    float4 positionOS   : POSITION;
    float3 normalOS		: NORMAL;
    float4 tangentOS		: TANGENT;
    float2 texcoord     : TEXCOORD0;
};

struct Varyings {
    float4 positionCS   : SV_POSITION;
    float3 positionWS	: TEXCOORD1;
    float3 normalOS		: NORMAL;
    float4 tangentOS    : TANGENT;
};

struct GeometryOutput {
    float4 positionCS	: SV_POSITION;
    float3 positionWS	: TEXCOORD1;
    float2 uv			: TEXCOORD0;
    half   NoiseValue   : TEXCOORD2;
};

#endif