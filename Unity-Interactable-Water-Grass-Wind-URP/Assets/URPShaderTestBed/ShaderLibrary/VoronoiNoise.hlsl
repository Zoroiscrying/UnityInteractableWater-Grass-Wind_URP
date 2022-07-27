#ifndef VORONOI_NOISE_INCLUDED
#define VORONOI_NOISE_INCLUDED

#include "Random.hlsl"

// --- Helper Functions


// --- Noise Functions

// Smooth Voronoi Noise color and distance, w controls the smoothness
// function from https://www.shadertoy.com/view/ldB3zc
half4 inoise_smooth_half4( in half2 x, float w )
{
    half2 n = floor(x);
    half2 f = frac(x);

    half4 m = half4( 8.0, 0.0, 0.0, 0.0 );
    for( int j=-2; j<=2; j++ )
        for( int i=-2; i<=2; i++ )
        {
            half2 g = float2( float(i),float(j) );
            half2 o = hash2( n + g );
		
            // animate
            o = 0.5 + 0.5*sin( 0 + 6.2831*o );

            // distance to cell		
            float d = length(g - f + o);
		
            // do the smoth min for colors and distances		
            half3 col = 0.5 + 0.5*sin( hash1(dot(n+g,half2(7.0,113.0)))*2.5 + 3.5 + half3(2.0,3.0,0.0));
            float h = smoothstep( 0.0, 1.0, 0.5 + 0.5*(m.x-d)/w );
		
            m.x   = lerp( m.x,     d, h ) - h*(1.0-h)*w/(1.0+3.0*w); // distance
            m.yzw = lerp( m.yzw, col, h ) - h*(1.0-h)*w/(1.0+3.0*w); // color
        }
	
    return m;
}


// 3D voronoi noise by iq: https://www.shadertoy.com/view/ldl3Dl
// returns closest, second closest, and cell id
float3 voronoi_3d_float3( in float3 x )
{
    float3 p = floor( x );
    float3 f = frac( x );

    float id = 0.0;
    float2 res = ( 100.0 );
    for( int k=-1; k<=1; k++ )
        for( int j=-1; j<=1; j++ )
            for( int i=-1; i<=1; i++ )
            {
                float3 b = float3( float(i), float(j), float(k) );
                float3 r = float3( b ) - f + hash3( p + b );
                float d = dot( r, r );

                if( d < res.x )
                {
                    id = dot( p+b, float3(1.0,57.0,113.0 ) );
                    res = float2( d, res.x );			
                }
                else if( d < res.y )
                {
                    res.y = d;
                }
            }

    return float3( sqrt( res ), abs(id) );
}

// 3D Smooth voronoi function based on the functions above
// returns closest, second closest, and cell id
float3 voronoi_3d_smooth_float3( in float3 x, float w)
{
    float3 p = floor( x );
    float3 f = frac( x );

    float id = 0.0;
    float2 res = ( 100.0 );
    float4 m = float4( 8.0, 0.0, 0.0, 0.0 );
    float id_m = 0.0;
    for( int k=-1; k<=1; k++ )
        for( int j=-1; j<=1; j++ )
            for( int i=-1; i<=1; i++ )
            {
                float3 b = float3( float(i), float(j), float(k) );
                float3 r = float3( b ) - f + hash3( p + b );
                float d = dot( r, r );

                if( d < res.x )
                {
                    id = dot( p+b, float3(1.0,57.0,113.0 ) );
                    id = 0.5 + 0.5 * sin( hash1(dot(p+b, float3(1.0,57.0,113.0 ))));
                    res = float2( d, res.x );			
                }
                else if( d < res.y )
                {
                    res.y = d;
                }

                half3 col = 0.5 + 0.5*sin( hash1(dot(p+b,half2(7.0,113.0)))*2.5 + 3.5 + half3(2.0,3.0,0.0));
                float h = smoothstep( 0.0, 1.0, 0.5 + 0.5*(m.x-d)/w );
                m.x   = lerp( m.x,     d, h ) - h*(1.0-h)*w/(1.0+3.0*w); // distance
                m.yzw = lerp( m.yzw, col, h ) - h*(1.0-h)*w/(1.0+3.0*w); // color
                id_m  = lerp( id_m,  id, h ) - h*(1.0-h)*w/(1.0+3.0*w);
            }

    return float3( sqrt( res ), abs(m.y) );
}

#endif