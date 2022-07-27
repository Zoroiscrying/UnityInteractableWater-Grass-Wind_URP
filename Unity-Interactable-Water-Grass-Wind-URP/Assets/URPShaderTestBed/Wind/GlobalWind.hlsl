#ifndef GLOBAL_WIND_INCLUDED
#define GLOBAL_WIND_INCLUDED

TEXTURE2D(_WindNoiseTexture);       SAMPLER(sampler_WindNoiseTexture);
float _WindPosMult;
float _WindTimeMult;
float _WindTexMult;
float3 _WindStrength;

float3 GetWindFromWorld(float3 positionWS)
{
    //get the world-position-based uv.
    float2 windUV = positionWS.xz * _WindPosMult + _Time.y * _WindTimeMult;
    windUV = windUV * _WindTexMult;
    //sample the wind noise texture 
    float3 windNoise = SAMPLE_TEXTURE2D_LOD(_WindNoiseTexture, sampler_WindNoiseTexture, windUV, 0).xyz * 2 - 1;
    // We want the grass to blow by rotating in a direction perpendicular to it's normal
    // cross will find one such vector. Since windNoise is not normalized, it also encodes some strength.
    return windNoise * _WindStrength;
}

float3 GetWindFromWorld(float3 positionWS, float3 strengthMultiplier)
{
    return GetWindFromWorld(positionWS) * strengthMultiplier;
}

#endif