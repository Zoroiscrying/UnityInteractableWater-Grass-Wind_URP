#ifndef GRASS_FRAG_PASS_INCLUDED
#define GRASS_FRAG_PASS_INCLUDED

float4 frag_TipShirnk(GeometryOutput input) : SV_Target {
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(input.positionWS);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif
				
    Light mainLight = GetMainLight(shadowCoord);
				
    half4 sampledRampColor = SAMPLE_TEXTURE2D(_ColorRampTexture, sampler_ColorRampTexture, half2(input.NoiseValue,0));
    float4 grassCol = sampledRampColor * lerp(0.0, 1.0, input.uv.y) * clamp(mainLight.shadowAttenuation, 0.5, 1.0);
    return grassCol;
}

float4 frag_UniformQuad(GeometryOutput input) : SV_Target {
	#if SHADOWS_SCREEN
	float4 clipPos = TransformWorldToHClip(input.positionWS);
	float4 shadowCoord = ComputeScreenPos(clipPos);
	#else
	float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	#endif

	// step from 0 to 3
	float step = floor(saturate(input.NoiseValue) * 4);
	
	float2 uv_random = float2(step * 0.25f + input.uv.x / 4 , input.uv.y);
	//float2 uv_nonRandom = float2(input.uv.x/4 + 0.25f * 3, input.uv.y);

	#if defined(GRASS_UNIFORM_QUAD)
		half alpha = SAMPLE_TEXTURE2D(_GrassBladeTexture, sampler_GrassBladeTexture, uv_random).a;
		clip(alpha - 0.1);
	#endif
	
	Light mainLight = GetMainLight(shadowCoord);
				
	half4 sampledRampColor = SAMPLE_TEXTURE2D(_ColorRampTexture, sampler_ColorRampTexture, half2(input.NoiseValue,0));
	float4 grassCol = sampledRampColor * 1.0 * lerp(0.0, 1.0, input.uv.y) * clamp(mainLight.shadowAttenuation, 0.5, 1.0);
	//grassCol = float(saturate(floor(input.NoiseValue-0.5f)+0.5));
	return grassCol;
}

#endif