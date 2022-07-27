#ifndef FLAG_SAMPLE_WIND_INCLUDED
#define FLAG_SAMPLE_WIND_INCLUDED

#include "../../Wind/GlobalWind.hlsl"

void FlagSampleWind_float(in float3 samplePosWS, out float3 WindDirection)
{
    WindDirection = GetWindFromWorld(samplePosWS);
}

#endif