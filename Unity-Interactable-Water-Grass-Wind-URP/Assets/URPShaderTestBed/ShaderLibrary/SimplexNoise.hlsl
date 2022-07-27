#ifndef SIMPLEX_NOISE_INCLUDED
#define SIMPLEX_NOISE_INCLUDED

#include "Random.hlsl"

// --- Helper Functions
const float3x3 m = float3x3( 0.00,  0.80,  0.60,
                            -0.80,  0.36, -0.48,
                            -0.60, -0.48,  0.64 );

/* const matrices for 3d rotation */
const float3x3 rot1 = float3x3(-0.37, 0.36, 0.85,-0.14,-0.93, 0.34,0.92, 0.01,0.4);
const float3x3 rot2 = float3x3(-0.55,-0.39, 0.74, 0.33,-0.91,-0.24,0.77, 0.12,0.63);
const float3x3 rot3 = float3x3(-0.71, 0.52,-0.47,-0.08,-0.72,-0.68,-0.7,-0.45,0.56);

inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
}

inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}

void randomize_3d_position(inout float3 p)
{
    p.x = p.x * 0.00 + p.y * 0.80 + p.z * 0.60;
    p.y = p.x * -0.80 + p.y * 0.36 + p.z * -0.48;
    p.z = p.x * -0.60 + p.y * -0.48 + p.z * 0.64;
    //p = mul(m, p);
}

// --- Noise Functions
// Simplex noise from unity, providing scale control
inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = Unity_SimpleNoise_RandomValue_float(c0);
    float r1 = Unity_SimpleNoise_RandomValue_float(c1);
    float r2 = Unity_SimpleNoise_RandomValue_float(c2);
    float r3 = Unity_SimpleNoise_RandomValue_float(c3);

    float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
    float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
    float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
    return t;
}

// Simple Noise from unity, multiple noise combined
float Unity_SimpleNoise_float_Fractal(float2 UV, float Scale)
{
    float t = 0.0;

    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3-0));
    t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    return t;
}

// Simplex noise from https://github.com/keijiro/NoiseShader/blob/master/Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl
float3 mod289(float3 x)
{
    return x - floor(x / 289.0) * 289.0;
}

float4 mod289(float4 x)
{
    return x - floor(x / 289.0) * 289.0;
}

float4 permute(float4 x)
{
    return mod289((x * 34.0 + 1.0) * x);
}

float4 taylorInvSqrt(float4 r)
{
    return 1.79284291400159 - r * 0.85373472095314;
}

float simplex_noise_3d_float(float3 v)
{
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);

    // First corner
    float3 i  = floor(v + dot(v, C.yyy));
    float3 x0 = v   - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    // x1 = x0 - i1  + 1.0 * C.xxx;
    // x2 = x0 - i2  + 2.0 * C.xxx;
    // x3 = x0 - 1.0 + 3.0 * C.xxx;
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - 0.5;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float4 p =
      permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0))
                            + i.y + float4(0.0, i1.y, i2.y, 1.0))
                            + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)

    float4 x_ = floor(j / 7.0);
    float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)

    float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
    float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 g0 = float3(a0.xy, h.x);
    float3 g1 = float3(a0.zw, h.y);
    float3 g2 = float3(a1.xy, h.z);
    float3 g3 = float3(a1.zw, h.w);

    // Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
    g0 *= norm.x;
    g1 *= norm.y;
    g2 *= norm.z;
    g3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    m = m * m;

    float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
    return 42.0 * dot(m, px);
}

float simplex_noise_3d_fractal(float3 q)
{
    float s = 1.0;
    
    float f = 0.0;

    float3 coord = float3(q * s);
    float v0 = simplex_noise_3d_float(coord);
    f += 0.5333333 * v0;

    s *= 2.0;

    coord = float3(q * s);
    v0 = simplex_noise_3d_float(coord);
    f += 0.2666667 * v0;

    s *= 2.0;

    coord = float3(q * s);
    v0 = simplex_noise_3d_float(coord);
    f += 0.1333333 * v0;

    s *= 2.0;

    coord = float3(q * s);
    v0 = simplex_noise_3d_float(coord);
    f += 0.0666667 * v0;
    
    return f;
}


// Simple noise 3d from iq: https://www.shadertoy.com/view/4sfGzS
float snoise_3d_float( float3 x )
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f*f*(3.0-2.0*f);
	
    return lerp(lerp(lerp( hash3(i+float3(0,0,0)), 
                        hash3(i+float3(1,0,0)),f.x),
                   lerp( hash3(i+float3(0,1,0)), 
                        hash3(i+float3(1,1,0)),f.x),f.y),
               lerp(lerp( hash3(i+float3(0,0,1)), 
                        hash3(i+float3(1,0,1)),f.x),
                   lerp( hash3(i+float3(0,1,1)), 
                        hash3(i+float3(1,1,1)),f.x),f.y),f.z);
}

float snoise_3d_float_fractal( float3 q)
{
    //return   0.5333333*snoise_3d_float(mul(rot1, q))
    //        +0.2666667*snoise_3d_float(2.0*mul(rot2, q))
    //        +0.1333333*snoise_3d_float(4.0*mul(rot3, q))
    //        +0.0666667*snoise_3d_float(8.0*q);
    
    float f = 0.0;
    f  = 0.5000*snoise_3d_float(q);

    //q = mul(m, q)*2.01;
    randomize_3d_position(q);
    q *= 2.01;
    f += 0.2500*snoise_3d_float(q);

    //q = mul(m, q)*2.02;
    randomize_3d_position(q);
    q *= 2.02;
    f += 0.1250*snoise_3d_float(q);

    //q = mul(m, q)*2.03;
    randomize_3d_position(q);
    q *= 2.03;
    f += 0.0625*snoise_3d_float(q);
    
    return f;
}

#endif