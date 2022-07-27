#ifndef CUSTOM_TERRAIN
#define CUSTOM_TERRAIN

//Inputs
CBUFFER_START(UnityPerMaterial)

half4 _WaterColor;
half _WaterEdge;
TEXTURE2D(_WaterRoughness);    SAMPLER(sampler_WaterRoughness);
half _ParallaxStrength;

TEXTURE2D(_Albedo01);    SAMPLER(sampler_Albedo01);
TEXTURE2D(_Normal01);    SAMPLER(sampler_Normal01);
TEXTURE2D(_MRHAO01);    SAMPLER(sampler_MRHAO01);
half _TextureScale01; half _Falloff01;

TEXTURE2D(_Albedo02);    
TEXTURE2D(_Normal02);    
TEXTURE2D(_Normal02Detail);    SAMPLER(sampler_Normal02Detail);
TEXTURE2D(_MRHAO02);    
half _TextureScale02; half _Falloff02;

TEXTURE2D(_Albedo03);    
TEXTURE2D(_Normal03);    
TEXTURE2D(_MRHAO03);    
half _TextureScale03; half _Falloff03;

TEXTURE2D(_Albedo04);    
TEXTURE2D(_Normal04);    
TEXTURE2D(_MRHAO04);    
half _TextureScale04; half _Falloff04;

CBUFFER_END

//Custom Functions
// Reoriented Normal Mapping
// http://blog.selfshadow.com/publications/blending-in-detail/
// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
half3 blend_rnm(half3 n1, half3 n2)
{
    n1.z += 1;
    n2.xy = -n2.xy;

    return n1 * dot(n1, n2) / n1.z - n2;
}


//Vert and Frag functions

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    //Use the helper function to calculate variables needed
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
 
    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);    //input.normalOS
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - output.positionWS;
    //fog factor and vertex lighting
    half fogFactor = ComputeFogFactor(output.positionCS.z);
    half3 vertexLight = 0.0;
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    output.uv = input.uv0;
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    output.vertexColor = input.vertexColor;

    float3x3 worldToTangent = float3x3(output.tangentWS.xyz, cross(output.normalWS, output.tangentWS.xyz) * output.tangentWS.w, output.normalWS);
    output.viewDirectionTS = TransformWorldToTangent(output.viewDirectionWS, worldToTangent);
    // do not know if this line should be added or not
    output.viewDirectionTS.xy /= output.viewDirectionTS.z;
    //what is this.
    OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.sh);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    half4 vertexColor = input.vertexColor;
    // Albedo calculation
    float2 uv_xz = input.positionWS.xz;
    float4 albedo_01 = SAMPLE_TEXTURE2D(_Albedo01, sampler_Albedo01, uv_xz * _TextureScale01);
    float4 albedo_02 = SAMPLE_TEXTURE2D(_Albedo02, sampler_Albedo01, uv_xz * _TextureScale02);
    float4 albedo_03 = SAMPLE_TEXTURE2D(_Albedo03, sampler_Albedo01, uv_xz * _TextureScale03);
    float4 albedo_04 = SAMPLE_TEXTURE2D(_Albedo04, sampler_Albedo01, uv_xz * _TextureScale04);

    // r && g && b == 0 --> albedo_01
    // r == 1 --> albedo_02
    // g == 1 --> albedo_03
    // b == 1 --> albedo_04
    // If albedo_01's alpha is close to 0, blend the color to albedo_02, same for other blend factors.
    half blend01 = smoothstep(vertexColor.r, vertexColor.r-_Falloff01, 1-albedo_01.a);
    half blend02 = smoothstep(vertexColor.g, vertexColor.g-_Falloff02, 1-albedo_02.a);
    half blend03 = smoothstep(vertexColor.b, vertexColor.b-_Falloff03, 1-albedo_04.a);

    float2 uv_xz_parallax = uv_xz + input.viewDirectionTS.xy * _ParallaxStrength * blend01;
    albedo_02 = SAMPLE_TEXTURE2D(_Albedo02, sampler_Albedo01, uv_xz_parallax * _TextureScale02);
    
    half4 AlbedoFinal = lerp(albedo_01, albedo_02, blend01);
    AlbedoFinal = lerp(AlbedoFinal, albedo_03, blend02);
    AlbedoFinal = lerp(AlbedoFinal, albedo_04, blend03);

    //Normal Calculation
    // -- tangent space normals
    half3 normal_01 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal01, sampler_Normal01, uv_xz * _TextureScale01));
    half3 normal_02 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal02, sampler_Normal01, uv_xz_parallax * _TextureScale02));
    half3 normal_03 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal03, sampler_Normal01, uv_xz * _TextureScale03));
    half3 normal_04 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal04, sampler_Normal01, uv_xz * _TextureScale04));

    half3 normal_02_detail = UnpackNormal(SAMPLE_TEXTURE2D(_Normal02Detail, sampler_Normal02Detail, uv_xz * _TextureScale04));

    half3 tangentNormal = lerp(normal_01, normal_02, blend01);
    tangentNormal = lerp(tangentNormal, normal_03, blend02);
    tangentNormal = lerp(tangentNormal, normal_04, blend03);
    //-- transformation matrix
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    float3 NormalFinal = TransformTangentToWorld(tangentNormal, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
    NormalFinal = normalize(NormalFinal);
    
    //Roughness Calculation
    half roughness_01 = SAMPLE_TEXTURE2D(_MRHAO01, sampler_MRHAO01, uv_xz * _TextureScale01).a;
    half roughness_02 = SAMPLE_TEXTURE2D(_MRHAO02, sampler_MRHAO01, uv_xz_parallax * _TextureScale02).a;
    half roughness_03 = SAMPLE_TEXTURE2D(_MRHAO03, sampler_MRHAO01, uv_xz * _TextureScale03).a;
    half roughness_04 = SAMPLE_TEXTURE2D(_MRHAO04, sampler_MRHAO01, uv_xz * _TextureScale04).a;
    half RoughnessFinal = lerp(roughness_01, roughness_02, blend01);
    RoughnessFinal = lerp(RoughnessFinal, roughness_03, blend02);
    RoughnessFinal = lerp(RoughnessFinal, roughness_04, blend03);
    
    //Metallic Calculation


    //AO Calculation
    

    //Water Calculation
    // For water, higher albedo_01's alpha value will cause more water if vertexColor.a is a high value.
    float waterBlend = smoothstep(vertexColor.a + _WaterEdge, vertexColor.a, 1-albedo_01.a);
    half3 waterNormal = TransformTangentToWorld(float3(0,0,1), half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
        
    AlbedoFinal = lerp(AlbedoFinal * _WaterColor, AlbedoFinal, waterBlend);
    NormalFinal = lerp(waterNormal, NormalFinal, waterBlend);
    RoughnessFinal = lerp( SAMPLE_TEXTURE2D(_WaterRoughness, sampler_WaterRoughness, uv_xz_parallax * 0.3).a * 0.95, (float)RoughnessFinal, waterBlend);
    
    // Unity PBR Function
    InputData inputData = (InputData)0;
    //position WS
    inputData.positionWS = input.positionWS;
    inputData.normalWS = NormalFinal;
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
        AlbedoFinal.rgb,
        0.0h,
        0.0h,
        RoughnessFinal,
        1.0h,
        0.0h,
        1.0h);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    //color.rgb = blend03;
    //color.rgb = 1 - albedo_03.a;
    //color.rgb = waterBlend;
    return color;
}


#endif