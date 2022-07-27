#ifndef FLAG_FLOW
#define FLAG_FLOW

#include "../../Wind/GlobalWind.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS		: NORMAL;
    float2 uv0          : TEXCOORD0;
    float4 tangentOS    : TANGENT;
    float4 color        : COLOR;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalWS		: NORMAL;
    float3 positionWS	: TEXCOORD1;
    float4 fogFactorAndVertexLight : TEXCOORD2;
    float3 viewDirectionWS : TEXCOORD3;
    #if defined(LIGHTMAP_ON)
    float2 lightmapUV : TEXCOORD4;
    #endif
    
    #if !defined(LIGHTMAP_ON)
    float3 sh : TEXCOORD5;
    #endif

    float debug : TEXCOORD6;
};

//Inputs
CBUFFER_START(UnityPerMaterial)

TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);     float4 _BaseMap_ST;
TEXTURE2D(_OcclusionMap);
half3 _Tint;
half4 _FlagDispStrength;

CBUFFER_END

//Vert and Frag functions

Varyings vert(Attributes input)
{
    float dispStrength = input.color.a;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

    // vertex displacement
    float3 windNoise = GetWindFromWorld(positionWS * 2);
    float3 wind_modified_value_float3 = float3(windNoise.x, windNoise.y, windNoise.z) * _FlagDispStrength.xyz * dispStrength;

    // modified normal calculation
    float3 displacedPositionOS = input.positionOS + TransformWorldToObject(wind_modified_value_float3);

    float3 bitangent = cross(input.normalOS, input.tangentOS);
    float3 posPlusTangent = displacedPositionOS + input.tangentOS * 0.01;
    float3 posPlusBitangent = displacedPositionOS + bitangent * 0.01;

    float3 modifiedTangent = posPlusTangent - displacedPositionOS;
    float3 modifiedBitangent = posPlusBitangent - displacedPositionOS;
    
    // - Get modified normal
    float3 modifiedNormal = normalize(cross(modifiedTangent, modifiedBitangent));
    
    Varyings output = (Varyings)0;
    //Use the helper function to calculate variables needed
    VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPositionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - output.positionWS;
    float viewNormalDot = dot(output.viewDirectionWS, output.normalWS);
    
    output.normalWS = TransformObjectToWorldNormal(modifiedNormal);
    
    int signOfViewNormal = sign(viewNormalDot);
    output.normalWS *= signOfViewNormal;
    
    //fog factor and vertex lighting
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    half3 vertexLight = VertexLighting(output.positionWS, output.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    output.uv = input.uv0;
    //what is this.
    //OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.sh);
    
    output.debug = input.color.r;
    return output;
}

float4 frag(Varyings input) : SV_Target
{   
    // Albedo calculation
    float2 albedo_uv = TRANSFORM_TEX(input.uv, _BaseMap);
    float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, albedo_uv).xyz * _Tint;
    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, albedo_uv).a;
    half ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, albedo_uv);

    //clip(alpha - 0.5);
    
    // Unity PBR Function
    InputData inputData = (InputData)0;
    //position WS
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
    //inputData.normalWS = normal;
    //inputData.normalWS = normal;
    //shadow coord
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    //viewDirection WS
    inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);
    //fog coordinate
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    //vertex lighting
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    //GI
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
    half4 color = 1.0f;
    color = UniversalFragmentPBR(
        inputData,
        albedo,
        0.05h,
        0.0h,
        0.0h,
        1.0h,
        albedo,
        1.0h);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.rgb = input.debug;
    return color;
}


#endif