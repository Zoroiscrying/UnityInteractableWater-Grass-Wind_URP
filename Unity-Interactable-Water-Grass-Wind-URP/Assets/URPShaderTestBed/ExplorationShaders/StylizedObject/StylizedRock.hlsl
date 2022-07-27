#ifndef STYLIZED_ROCK_INCLUDED
#define STYLIZED_ROCK_INCLUDED

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
    #if defined(LIGHTMAP_ON)
    float2 lightmapUV : TEXCOORD4;
    #endif
    
    #if !defined(LIGHTMAP_ON)
    float3 sh : TEXCOORD5;
    #endif
};

//Inputs
CBUFFER_START(UnityPerMaterial)

TEXTURE2D(_RockAlbedo);  SAMPLER(sampler_RockAlbedo);

CBUFFER_END

float4 hash4( float2 p ) { return frac(sin(float4( 1.0+dot(p,float2(37.0,17.0)), 
                                              2.0+dot(p,float2(11.0,47.0)),
                                              3.0+dot(p,float2(41.0,29.0)),
                                              4.0+dot(p,float2(23.0,31.0))))*103.0); }

float4 textureNoTile( float2 uv )
{
    float2 iuv = floor( uv );
    float2 fuv = frac( uv );

    //#ifdef USEHASH    
    // generate per-tile transform (needs GL_NEAREST_MIPMAP_LINEARto work right)
    //float4 ofa = texture( iChannel1, (iuv + float2(0.5,0.5))/256.0 );
    //float4 ofb = texture( iChannel1, (iuv + float2(1.5,0.5))/256.0 );
    //float4 ofc = texture( iChannel1, (iuv + float2(0.5,1.5))/256.0 );
    //float4 ofd = texture( iChannel1, (iuv + float2(1.5,1.5))/256.0 );
    //#else
    // generate per-tile transform
    float4 ofa = hash4( iuv + float2(0.0,0.0) );
    float4 ofb = hash4( iuv + float2(1.0,0.0) );
    float4 ofc = hash4( iuv + float2(0.0,1.0) );
    float4 ofd = hash4( iuv + float2(1.0,1.0) );
    //#endif
    
    float2 m_ddx = ddx( uv );
    float2 m_ddy = ddy( uv );

    // transform per-tile uvs
    ofa.zw = sign(ofa.zw-0.5);
    ofb.zw = sign(ofb.zw-0.5);
    ofc.zw = sign(ofc.zw-0.5);
    ofd.zw = sign(ofd.zw-0.5);
    
    // uv's, and derivarives (for correct mipmapping)
    float2 uva = uv*ofa.zw + ofa.xy; float2 ddxa = m_ddx*ofa.zw; float2 ddya = m_ddy*ofa.zw;
    float2 uvb = uv*ofb.zw + ofb.xy; float2 ddxb = m_ddx*ofb.zw; float2 ddyb = m_ddy*ofb.zw;
    float2 uvc = uv*ofc.zw + ofc.xy; float2 ddxc = m_ddx*ofc.zw; float2 ddyc = m_ddy*ofc.zw;
    float2 uvd = uv*ofd.zw + ofd.xy; float2 ddxd = m_ddx*ofd.zw; float2 ddyd = m_ddy*ofd.zw;
        
    // fetch and blend
    float2 b = smoothstep(0.25,0.75,fuv);
    
    return lerp( lerp( SAMPLE_TEXTURE2D_GRAD( _RockAlbedo, sampler_RockAlbedo, uva, ddxa, ddya ), 
                     SAMPLE_TEXTURE2D_GRAD( _RockAlbedo, sampler_RockAlbedo, uvb, ddxb, ddyb ), b.x ), 
                lerp( SAMPLE_TEXTURE2D_GRAD( _RockAlbedo, sampler_RockAlbedo, uvc, ddxc, ddyc ),
                     SAMPLE_TEXTURE2D_GRAD( _RockAlbedo, sampler_RockAlbedo, uvd, ddxd, ddyd ), b.x), b.y );
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
    //what is this.
    OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS, output.sh);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    half2 uv = input.uv;

    // Smooth Voronoi Color Calculation
    half voronoi_smoothness = 0.0h;
    voronoi_smoothness = saturate(simplex_noise_3d_fractal(input.positionWS * 15.0h) * 0.5 + 0.5);
    //voronoi_smoothness = Unity_SimpleNoise_float_Fractal(input.uv, 50.0);
    float positionWSOffset = 1.0h;
    positionWSOffset = lerp(simplex_noise_3d_fractal(input.positionWS * 5.0h) * 0.3, 1.0h, 0.0h);
    float3 voronoi_smooth = voronoi_3d_smooth_float3(input.positionWS * 2.0h + positionWSOffset, voronoi_smoothness * 0.25);
    float3 voronoi_smooth_detail = voronoi_3d_smooth_float3(input.positionWS * 26.0h + positionWSOffset, voronoi_smoothness * 0.25);

    float3 rock_albedo_untiled = textureNoTile(uv * 10.0f);
    
    // Up Vector Zone Calculation
    float up_degree = dot(input.normalWS, float3(0, 1, 0));
    up_degree = smoothstep(0.95, 1.00, up_degree + voronoi_smooth);
    
    // Base Albedo Combination -- detail and base
    float combine_voronoi_base = (voronoi_smooth * 0.75 + lerp(voronoi_smooth, voronoi_smooth_detail, 0.5) * 0.25).z;

    // Final Combination
    float3 albedo = lerp(float3(75, 90, 84)/255 * 2, float3(34, 44, 41)/255 * 2, combine_voronoi_base);
    albedo = lerp(albedo, float3(0.4, 0.8, 0.4) * saturate(voronoi_smooth.z + 0.5), up_degree);

    //albedo = rock_albedo_untiled;
    
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
        //voronoi_smoothness,
        0.0h,
        0.0h,
        .25h * voronoi_smooth,
        1.0h,
        0.0h,
        1.0h);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}


#endif