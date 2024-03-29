#if !defined (STYLIZED_WATER_INCLUDED)
#define STYLIZED_WATER_INCLUDED

#include "WaterInput.hlsl"
#include "WaterLighting.hlsl"
#include "WaterTransmition/WaveUtils.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    //float3 normalOS		: NORMAL;
    //float4 tangentOS    : TANGENT;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS	: TEXCOORD2;
    float4 positionSS   : TEXCOORD3;
    float3 normalWS		: NORMAL;
    float3 viewDirectionWS : TEXCOORD4;
    //float4 tangentWS    : TEXCOORD5;
    float4 fogFactorAndVertexLight : TEXCOORD1;
    float2 uv           : TEXCOORD9;

    float4 additionalData : TEXCOORD10;
    
    #if defined(LIGHTMAP_ON)
    float2 lightmapUV : TEXCOORD6;
    #endif
    
    #if !defined(LIGHTMAP_ON)
    float3 sh : TEXCOORD7;
    #endif

    float2 uv_WaterWave : TEXCOORD8;
};

//Helper functions
float DecodeFloatRGBA( float4 enc )
{
    float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
    return dot( enc, kDecodeDot );
}

float3 CreateNormalFromTransmit(float2 uv)
{
    float3 outNormal = 0;
    //Calculate Normal from Grayscale
    float3 graynorm = float3(0, 0, 1);
    float heightSampleCenter = DecodeHeight(SAMPLE_TEXTURE2D_LOD(_WaterWaveResult, sampler_WaterWaveResult, float2(uv), 0).r);
    float heightSampleRight = DecodeHeight(SAMPLE_TEXTURE2D_LOD(_WaterWaveResult, sampler_WaterWaveResult,float2(uv + float2(_WaterWaveResult_TexelSize.x, 0)),0).r);
    float heightSampleUp = DecodeHeight(SAMPLE_TEXTURE2D_LOD(_WaterWaveResult, sampler_WaterWaveResult,float2(uv + float2(0, _WaterWaveResult_TexelSize.y)),0).r);
    float sampleDeltaRight = heightSampleRight - heightSampleCenter;
    float sampleDeltaUp = heightSampleUp - heightSampleCenter;
    graynorm = cross(
    float3(1, 0, sampleDeltaRight * 1.0),
    float3(0, 1, sampleDeltaUp * 1.0));
	 
    outNormal = normalize(graynorm);
    return outNormal;
}

half4 AdditionalData(float3 positionWS, float height)
{
    half4 data = half4(0.0, 0.0, 0.0, 0.0);
    float3 viewPos = TransformWorldToView(positionWS);
    data.x = length(viewPos / viewPos.z);// distance to surface
    data.y = length(GetCameraPositionWS().xyz - positionWS); // local position in camera space
    data.z = height;
    return data;
}

half2 DistortionUVs(half depth, float3 normalWS)
{
    half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;

    return viewNormal.xz * saturate((depth) * 0.0025);
}

half3 Scattering(half depth)
{
    return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.75h)).rgb;
}

half3 Absorption(half depth)
{
    return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.0h)).rgb;
}

float2 AdjustedDepth(half2 uvs, half4 additionalData)
{
    float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_linear_clamp, uvs);
    float d = LinearEyeDepth(rawD, _ZBufferParams);

    // TODO: Changing the usage of UNITY_REVERSED_Z this way to fix testing, but I'm not sure the original code is correct anyway.
    // In OpenGL, rawD should already have be remmapped before converting depth to linear eye depth.
    #if UNITY_REVERSED_Z
    float offset = 0;
    #else
    float offset = 1;
    #endif
	
    return float2(d * additionalData.x - additionalData.y, (rawD * -_ProjectionParams.x) + offset);
}

float3 WaterDepth(float3 posWS, half4 additionalData, half2 screenUVs)// x = seafloor depth, y = water depth
{
    float3 outDepth = 0;
    outDepth.xz = AdjustedDepth(screenUVs, additionalData);
    //float wd = WaterTextureDepth(posWS);
    float wd = 0.0f;
    outDepth.y = wd + posWS.y;
    return outDepth;
}

half3 Refraction(half2 distortion, half depth, real depthMulti)
{
    half3 output = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion, depth * 0.25).rgb;
    output *= Absorption((depth) * depthMulti);
    output = lerp(output, Absorption(depth), saturate(depth * depthMulti));
    return output;
}

//Vert and Frag functions

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    //calculate relative position uv
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float uv_X = (positionWS.x - _WaterSimulationParams.x)/_WaterSimulationParams.z + 0.5f;
    float uv_Z = (positionWS.z - _WaterSimulationParams.y)/_WaterSimulationParams.w + 0.5f;
    float2 uv_waterResult = float2(uv_X, uv_Z);

    //sample the transmit texture
    float4 waveTransmit = SAMPLE_TEXTURE2D_X_LOD(_WaterWaveResult, sampler_WaterWaveResult, uv_waterResult, 0);
    float waveHeight = DecodeHeight(waveTransmit);
    //waveHeight = DecodeHeight(waveTransmit);
    input.positionOS.y += waveHeight * 0.5;

    //Use the helper function to calculate variables needed
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = float3(0, 1, 0);
    //output.normalWS = TransformObjectToWorldNormal(input.normalOS);    //input.normalOS
    output.positionSS = ComputeScreenPos(vertexInput.positionCS);
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - output.positionWS;
    //output.tangentWS = float4(TransformObjectToWorldDir(float3(1,0,0)), input.tangentOS.w);
    //fog factor and vertex lighting
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    half3 vertexLight = VertexLighting(output.positionWS, output.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    output.uv_WaterWave = uv_waterResult;
    output.uv = input.uv0;
    output.additionalData.z = waveHeight;
    //output.additionalData;
    //what is this.
    OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.sh);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    half3 screenUV = input.positionSS.xyz / input.positionSS.w; // Screen UVs
    input.viewDirectionWS = SafeNormalize(input.viewDirectionWS);

    // Depth
    float3 viewPos = TransformWorldToView(input.positionWS);
    input.additionalData.x = length(viewPos / viewPos.z);// distance to surface
    input.additionalData.y = length(GetCameraPositionWS().xyz - input.positionWS); // local position in camera space
    float3 depth = WaterDepth(input.positionWS, input.additionalData, screenUV.xy);
    half depthMulti = 1 / _MaxDepth;

    //higher intersection Diff => lower intersection threshold / higher distance
    float foamDiff = saturate(depth.x / _FoamThreshold);
    half fogBlend = saturate(smoothstep(_MaxDepth - 5, _MaxDepth + 5, depth.x));

    // Albedo calculation
    half4 albedo = _Color;

    // Foam texture sampling
    float foamTex = SAMPLE_TEXTURE2D(_FoamTexture, sampler_FoamTexture, input.positionWS.xz * _FoamTexture_ST.xy + _Time.y * float2(_FoamTextureSpeedX, _FoamTextureSpeedY));
    float foam = step(foamDiff - (saturate(sin((foamDiff - _Time.y * _FoamLinesSpeed) * 8 * PI)) * (1.0 - foamDiff)), foamTex);

    // Detail Waves
    float4 normalA = SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, input.positionWS.xz * _NormalA_ST.xy + _Time.y * _NormalPanningSpeeds.xy);
    normalA.rgb = UnpackNormalScale(normalA, _NormalStrength);
    float4 normalB = SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, input.positionWS.xz * _NormalB_ST.xy + _Time.y * _NormalPanningSpeeds.zw);
    normalB.rgb = UnpackNormalScale(normalB, _NormalStrength);
    float4 normal_WaterWave = float4(CreateNormalFromTransmit(input.uv_WaterWave), 1);
    float4 normal = normalA + normalB + normal_WaterWave;
    //-- transformation matrix
    //float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    //float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    //input.normalWS = TransformTangentToWorld(normal, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
    input.normalWS += half3(normal.x, 0, normal.y);
    input.normalWS = normalize(input.normalWS);

    // Distortion
    half2 distortion = DistortionUVs(depth.x, input.normalWS);
    distortion = screenUV.xy + distortion;// * clamp(depth.x, 0, 5);
    float d = depth.x;
    depth.xz = AdjustedDepth(distortion, input.additionalData);
    distortion = depth.x < 0 ? screenUV.xy : distortion;
    depth.x = depth.x < 0 ? d : depth.x;

    // Fresnel
    float fresnel = pow(1.0 - saturate(dot(input.normalWS, normalize(input.viewDirectionWS))), _FresnelPower);

    // Lighting
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
    half shadow = SoftShadows(screenUV, input.positionWS, input.viewDirectionWS.xyz, depth.x);
    half3 GI = SampleSH(input.normalWS);

    // SSS
    half3 directLighting = dot(mainLight.direction, half3(0,1,0)) * mainLight.color;
    directLighting += saturate(pow(dot(input.viewDirectionWS, -mainLight.direction) * input.additionalData.z, 4)) * 5 * mainLight.color;
    half3 sss = directLighting * shadow + GI;
    //sss = 0.0f;

    BRDFData brdfData;
    half alpha = lerp(lerp(albedo.a * fresnel, 1.0, foam), _FogColor.a, fogBlend);
    alpha = 1.0;
    InitializeBRDFData(albedo, 0, 0, _Glossiness, alpha, brdfData);
    half3 spec = DirectBDRF(brdfData, input.normalWS, mainLight.direction, input.viewDirectionWS) * shadow * mainLight.color;
    #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();
        for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
        {
            Light light = GetAdditionalLight(lightIndex, input.positionWS);
            spec += LightingPhysicallyBased(brdfData, light, input.normalWS, input.viewDirectionWS.xyz);
            sss += light.distanceAttenuation * light.color;
        }
    #endif

    sss *= Scattering(depth.x * depthMulti);
    
    //sss = 0;
    
    // Reflections
    half3 reflection = SampleReflections(input.normalWS, input.viewDirectionWS.xyz, screenUV.xy, 0.0);

    // Refraction
    //half3 refraction = lerp(Refraction(distortion, depth.x, depthMulti), _FogColor, fogBlend);
    half3 refraction = Refraction(distortion, depth.x, depthMulti);
    //refraction = Refraction(screenUV.xy, depth.x, depthMulti);
    //refraction = 0;
    // Do compositing
    half3 comp = lerp(refraction, reflection, fresnel) + sss + spec + foam * _FoamIntensity; //lerp(refraction, color + reflection + foam, 1-saturate(1-depth.x * 25));

    // Fog
    float fogFactor = input.fogFactorAndVertexLight.x;
    comp = MixFog(comp, fogFactor);
    //comp = spec;
    //comp = normal_WaterWave;
    //comp = refraction;
    //comp = foam * _FoamIntensity;
    //comp = refraction;
    //comp = refraction;
    return float4(comp, 1);
    
    ////test code
    //InputData inputData = (InputData)0;
    ////position WS
    //inputData.positionWS = input.positionWS;
    //inputData.normalWS = input.normalWS;
    ////shadow coord
    //inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    ////viewDirection WS
    //inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);
    ////fog coordinate
    //inputData.fogCoord = input.fogFactorAndVertexLight.x;
    ////vertex lighting
    //inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    ////GI
    //inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
    //half4 color = 1.0f;
    //color = UniversalFragmentPBR(
    //    inputData,
    //    albedo,
    //    0.0h,
    //    0.0h,
    //    _Glossiness,
    //    0.0h,
    //    foam * _FoamIntensity,
    //    lerp(lerp(albedo.a * fresnel, 1.0, foam), _FogColor.a, fogDiff));
    ////lerp(lerp(albedo.a * fresnel, 1.0, foam), _FogColor.a, fogDiff)
    //color.rgb = MixFog(color.rgb, inputData.fogCoord);
    //return color;
}

#endif