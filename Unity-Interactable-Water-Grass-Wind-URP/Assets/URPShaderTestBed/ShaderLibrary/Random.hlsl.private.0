#if !defined(RANDOM_INCLUDED)
#define RANDOM_INCLUDED

// Returns a pseudorandom number. By Ronja Böhringer
float rand(float4 value) {
    float4 smallValue = sin(value);
    float random = dot(smallValue, float4(12.9898, 78.233, 37.719, 09.151));
    random = frac(sin(random) * 143758.5453);
    return random;
}

float rand(float3 seed) {
    return frac(sin(dot(seed.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

float rand(float3 pos, float offset) {
    return rand(float4(pos, offset));
}

float rand(float2 pos){
    return frac(sin(dot(pos.xy ,float2(12.9898,78.233))) * 43758.5453);
}

float randNegative1to1(float3 pos, float offset) {
    return rand(pos, offset) * 2 - 1;
}

float hash1( float n ) { return frac(sin(n)*43758.5453); }

float2 hash2( half2  p ) { p = half2( dot(p,half2(127.1,311.7)), dot(p,half2(269.5,183.3)) ); return frac(sin(p)*43758.5453); }

float3 hash3( float3 x )
{
    x = float3( dot(x,float3(127.1,311.7, 74.7)),
              dot(x,float3(269.5,183.3,246.1)),
              dot(x,float3(113.5,271.9,124.6)));

    return frac(sin(x)*43758.5453123);
}

/* discontinuous pseudorandom uniformly distributed in [-0.5, +0.5]^3 */
float3 random3(float3 c) {
    float j = 4096.0*sin(dot(c,float3(17.0, 59.4, 15.0)));
    float3 r;
    r.z = frac(512.0*j);
    j *= .125;
    r.x = frac(512.0*j);
    j *= .125;
    r.y = frac(512.0*j);
    return r-0.5;
}

#endif