#ifndef FLAG_INPUT_INCLUDED
#define FLAG_INPUT_INCLUDED

//Inputs
CBUFFER_START(UnityPerMaterial)

TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);     float4 _BaseMap_ST;
TEXTURE2D(_OcclusionMap);
half3 _Tint;
half4 _FlagDispStrength;

CBUFFER_END


#endif