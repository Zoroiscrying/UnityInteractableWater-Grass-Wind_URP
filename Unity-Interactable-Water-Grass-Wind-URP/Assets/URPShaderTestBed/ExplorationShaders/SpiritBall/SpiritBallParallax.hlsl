#ifndef SPIRIT_BALL_PARALLAX_INCLUDED

#define SPIRIT_BALL_PARALLAX_INCLUDED

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
    float2 uv           : TEXCOORD0;
    float3 normalWS		: NORMAL;
    float3 positionWS	: TEXCOORD1;
    float4 fogFactorAndVertexLight : TEXCOORD2;
    float3 viewDirectionWS : TEXCOORD3;
    float4 tangentWS    : TEXCOORD6;
    float3 viewDirectionTS : TEXCOORD7;
    #if defined(LIGHTMAP_ON)
    float2 lightmapUV : TEXCOORD4;
    #endif
    
    #if !defined(LIGHTMAP_ON)
    float3 sh : TEXCOORD5;
    #endif
};

//Inputs
CBUFFER_START(UnityPerMaterial)

TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex); float4 _MainTex_ST;
TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);
TEXTURE2D(_HeightMap);  SAMPLER(sampler_HeightMap);

TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);
half4 _EmissionColor;
half _ParallaxStrength;
half _EmissionPercentage;

CBUFFER_END

//Helper Functions

float GetParallaxHeight (float2 uv) {
    return SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).g + Unity_SimpleNoise_float_Fractal(uv + _Time.y * 0.1, 10) * 0.5f;
}

float2 ParallaxOffset (float2 uv, float2 viewDir) {
    float height = GetParallaxHeight(uv);
    height -= 0.5;
    height *= _ParallaxStrength;
    return viewDir * height;
}

float2 ParallaxRaymarching (float2 uv, float2 viewDir) {

    #if !defined(PARALLAX_RAYMARCHING_STEPS)
        #define PARALLAX_RAYMARCHING_STEPS 10
    #endif
    
    float2 uvOffset = 0;
    float stepSize = 1.0 / PARALLAX_RAYMARCHING_STEPS;
    float2 uvDelta = viewDir * (stepSize * _ParallaxStrength);

    // height where the step begins
    float stepHeight = 1;
    // height of the surface (max 1, min 0)
    float surfaceHeight = GetParallaxHeight(uv);

    // keep track of the previous step information
    float2 prevUVOffset = uvOffset;
    float prevStepHeight = stepHeight;
    float prevSurfaceHeight = surfaceHeight;
    
    // If ray-marching steps below the surface, quit
    for (int i = 1; i < PARALLAX_RAYMARCHING_STEPS && stepHeight > surfaceHeight; i++)
    {
        
        //store the data of the last step
        prevUVOffset = uvOffset;
        prevStepHeight = stepHeight;
        prevSurfaceHeight = surfaceHeight;
        
        //step once by change uv offset by uv delta.
        uvOffset -= uvDelta;
        //step down
        stepHeight -= stepSize;
        //update surface height
        surfaceHeight = GetParallaxHeight(uv + uvOffset);
    }

    // Parallax searching for better parallax mapping results
    #if !defined(PARALLAX_RAYMARCHING_SEARCH_STEPS)
        #define PARALLAX_RAYMARCHING_SEARCH_STEPS 0
    #endif
        
    #if (PARALLAX_RAYMARCHING_SEARCH_STEPS > 0)
        for (int i = 0; i < PARALLAX_RAYMARCHING_SEARCH_STEPS; i++)
        {
            uvDelta *= 0.5;
            stepSize *= 0.5;

            if (stepHeight < surfaceHeight) {
                uvOffset += uvDelta;
                stepHeight += stepSize;
            }
            else {
                uvOffset -= uvDelta;
                stepHeight -= stepSize;
            }
            surfaceHeight = GetParallaxHeight(uv + uvOffset);
        }
    #elif defined(PARALLAX_RAYMARCHING_INTERPOLATE)
        // use 
        float prevDifference = prevStepHeight - prevSurfaceHeight;
        float difference = surfaceHeight - stepHeight;
        float t = prevDifference / (prevDifference + difference);
        uvOffset = prevUVOffset - uvDelta * t;
    #endif
    
    return uvOffset;
}

void ApplyParallax (inout Varyings i) {
    i.viewDirectionTS = normalize(i.viewDirectionTS);
    #if !defined(PARALLAX_BIAS)
        #define PARALLAX_BIAS 0.42
    #endif
    
    i.viewDirectionTS.xy /= (i.viewDirectionTS.z + PARALLAX_BIAS);

    #if !defined(PARALLAX_FUNCTION)
        #define PARALLAX_FUNCTION ParallaxOffset
    #endif
    
    float2 uvOffset = PARALLAX_FUNCTION(i.uv.xy, i.viewDirectionTS.xy);
    i.uv.xy += uvOffset;
    //i.uv.zw += uvOffset * (_DetailTex_ST.xy / _MainTex_ST.xy);
}

//Vert and Frag functions
Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    //Use the helper function to calculate variables needed
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - output.positionWS;
    //fog factor and vertex lighting
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    half3 vertexLight = VertexLighting(output.positionWS, output.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    output.uv = input.uv0;
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    //Tangent space view direction
    half3x3 worldToTangent = half3x3(
    output.tangentWS.xyz,
    cross(output.normalWS, output.tangentWS.xyz) * output.tangentWS.w,
    output.normalWS
    );
    output.viewDirectionTS = mul(worldToTangent, output.viewDirectionWS);
    
    //what is this.
    //OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.sh);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    half alpha = 1.0f;
    float3 positionOS = TransformWorldToObject(input.positionWS);
    alpha = step(positionOS.y + 0.5f, _EmissionPercentage);
    half noiseBasedOnXZ = Unity_SimpleNoise_float_Fractal(input.positionWS.xz, 10) * .01f;
    half dissolveEmissionFactor = 1 - step(positionOS.y + 0.5f, _EmissionPercentage - 0.01f + noiseBasedOnXZ);
    clip(alpha - 0.5f);
    
    // Recalculate uv and apply parallax mapping
    input.uv = TRANSFORM_TEX(input.uv, _MainTex);
    ApplyParallax(input);
    half2 uv = input.uv;

    // Albedo Calculation
    float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

    // Normal Calculation
    float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    input.normalWS = TransformTangentToWorld(normal, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));

    // Metallic, AO, Smoothness and Height Data
    float metallic = 0.0h;
    float smoothness = .25h;
    float occlusion = 1.0h;

    // Emission Calculation
    float emission_mask =  1 - (SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv) + Unity_SimpleNoise_float_Fractal(uv + _Time.y * 0.1, 10) * 0.5f);
    emission_mask = pow(emission_mask, 4);
    half4 emission = emission_mask * _EmissionColor + dissolveEmissionFactor * _EmissionColor;
    
    // Unity PBR Function
    InputData inputData = (InputData)0;
    //position WS
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
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
        metallic,
        0.0h,
        smoothness,
        occlusion,
        emission,
        1.0h);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}



#endif