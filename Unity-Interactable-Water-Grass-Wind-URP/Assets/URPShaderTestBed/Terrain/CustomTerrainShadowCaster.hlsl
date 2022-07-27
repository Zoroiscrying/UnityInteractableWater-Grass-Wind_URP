#ifndef CUSTOM_TERRAIN_SHADOWCASTER
#define CUSTOM_TERRAIN_SHADOWCASTER

//Inputs
CBUFFER_START(UnityPerMaterial)



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
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS); 

    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = GetShadowPositionHClip(output.positionWS, normalWS);
    output.normalWS = normalWS;

    return output;
}

float4 frag(Varyings input) : SV_Target
{   
    return 0;
}


#endif