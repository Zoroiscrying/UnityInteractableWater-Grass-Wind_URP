#ifndef CUSTOM_TILLING_INCLUDED
#define CUSTOM_TILLING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//#define USEHASH

float4 hash4( float2 p ) { return frac(sin(float4( 1.0+dot(p,float2(37.0,17.0)), 
                                              2.0+dot(p,float2(11.0,47.0)),
                                              3.0+dot(p,float2(41.0,29.0)),
                                              4.0+dot(p,float2(23.0,31.0))))*103.0); }

float4 textureNoTile( TEXTURE2D(texture), SAMPLER(sampler), float2 uv )
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
    
    float2 ddx = ddx( uv );
    float2 ddy = ddy( uv );

    // transform per-tile uvs
    ofa.zw = sign(ofa.zw-0.5);
    ofb.zw = sign(ofb.zw-0.5);
    ofc.zw = sign(ofc.zw-0.5);
    ofd.zw = sign(ofd.zw-0.5);
    
    // uv's, and derivarives (for correct mipmapping)
    float2 uva = uv*ofa.zw + ofa.xy; float2 ddxa = ddx*ofa.zw; float2 ddya = ddy*ofa.zw;
    float2 uvb = uv*ofb.zw + ofb.xy; float2 ddxb = ddx*ofb.zw; float2 ddyb = ddy*ofb.zw;
    float2 uvc = uv*ofc.zw + ofc.xy; float2 ddxc = ddx*ofc.zw; float2 ddyc = ddy*ofc.zw;
    float2 uvd = uv*ofd.zw + ofd.xy; float2 ddxd = ddx*ofd.zw; float2 ddyd = ddy*ofd.zw;
        
    // fetch and blend
    float2 b = smoothstep(0.25,0.75,fuv);
    
    return lerp( lerp( SAMPLE_TEXTURE2D_GRAD( texture, sampler, uva, ddxa, ddya ), 
                     SAMPLE_TEXTURE2D_GRAD( texture, sampler, uvb, ddxb, ddyb ), b.x ), 
                lerp( SAMPLE_TEXTURE2D_GRAD( texture, sampler, uvc, ddxc, ddyc ),
                     SAMPLE_TEXTURE2D_GRAD( texture, sampler, uvd, ddxd, ddyd ), b.x), b.y );
}

#endif