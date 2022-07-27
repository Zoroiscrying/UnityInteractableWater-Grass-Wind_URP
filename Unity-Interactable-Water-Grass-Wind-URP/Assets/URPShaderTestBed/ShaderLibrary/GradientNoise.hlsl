#if !defined(GRADIENT_NOISE_INCLUDED)
#define GRADIENT_NOISE_INCLUDED

#include "Random.hlsl"

// --- Gradient Noise
// 2D Gradient Noise from https://www.shadertoy.com/view/XdXGW8 by iq
float gnoise_2D_float( in float2 p )
{
    float2 i = floor( p );
    float2 f = frac( p );
	
    float2 u = f*f*(3.0-2.0*f);

    return lerp( lerp( dot( hash2( i + float2(0.0,0.0) ), f - float2(0.0,0.0) ), 
                     dot( hash2( i + float2(1.0,0.0) ), f - float2(1.0,0.0) ), u.x),
                lerp( dot( hash2( i + float2(0.0,1.0) ), f - float2(0.0,1.0) ), 
                     dot( hash2( i + float2(1.0,1.0) ), f - float2(1.0,1.0) ), u.x), u.y);
}

#endif