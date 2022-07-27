#ifndef GRASS_PASSES_INCLUDED
#define GRASS_PASSES_INCLUDED

#include "../../Wind/GlobalWind.hlsl"

// Helper Functions
float3x3 AngleAxis3x3(float angle, float3 axis) {
	float c, s;
	sincos(angle, s, c);

	float t = 1 - c;
	float x = axis.x;
	float y = axis.y;
	float z = axis.z;

	return float3x3(
		t * x * x + c, t * x * y - s * z, t * x * z + s * y,
		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
		t * x * z - s * y, t * y * z + s * x, t * z * z + c
	);
}

float3x3 AngleAroundZAxis_Vector(float angle)
{
	float c, s;
	sincos(angle, c, s);
	return float3x3(
		c, -s, 0,
		s, c, 0,
		0, 0, 1
	);
}

float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS) {
	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
	positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

	return positionCS;
}

float4 WorldToHClip(float3 positionWS, float3 normalWS) {
#ifdef SHADOW
	return GetShadowPositionHClip(positionWS, normalWS);
#else
	return TransformWorldToHClip(positionWS);
#endif
}

void Unity_InverseLerp_float_Clamp01(float A, float B, float T, out float Out)
{
	Out = clamp((T - A)/(B - A), 0.0f, 1.0f);
}

float Unity_InverseLerp_float_Clamp01(float A, float B, float T)
{
	return clamp((T - A)/(B - A), 0.0f, 1.0f);
}

float2 GetBladeDimensions(Varyings input, float3 positionWS)
{
	//Calculate the blade width and height, applying random variance
	float width = saturate(randNegative1to1(positionWS.zyx, 0) * _RandomWidth + _Width);
	float randomHeightNoise = SAMPLE_TEXTURE2D_LOD(_HeightNoiseTexture, sampler_HeightNoiseTexture, positionWS.xz * _HeightTextureMult, 0).r * 0.4f;
	//float height = randNegative1to1(positionWS.yxz, 1) * _RandomHeight + _Height;
	//randomHeightNoise = pow(randomHeightNoise, 8);
	//randomHeightNoise = (randomHeightNoise - 0.5) * 2;
	float height = max((randomHeightNoise * _RandomHeight + _Height), 0.0);
	//return float2(width, height);
	return float2(width * input.uv.x, height * input.uv.y);
}

float3 GetWindAxis(float3 positionWS, float3 normalWS)
{
	float3 windNoise = GetWindFromWorld(positionWS);
	// We want the grass to blow by rotating in a direction perpendicular to it's normal
	// cross will find one such vector. Since windNoise is not normalized, it also encodes some strength.
	return cross(float3(windNoise.x, 0, windNoise.z), normalWS);
}

float3 GetOffsetAxis(float3 positionWS, float3 normalWS)
{
	float3 dirFromCollisionToGrass = (positionWS - _ColliderPosition);
	dirFromCollisionToGrass.y = 0;
	dirFromCollisionToGrass = normalize(dirFromCollisionToGrass);
	return cross(normalWS, dirFromCollisionToGrass);
}

float GetCollisionOffset(float3 positionWS)
{
	float d = distance(positionWS, _ColliderPosition);
	// min distance, max distance
	float t = 1 - Unity_InverseLerp_float_Clamp01(0.01f, _collisionOffsetMaxDistance, d);
	//float t = 1 - smoothstep(0.1f, _collisionOffsetMaxDistance, d);
	return t;
}

int GetGrassBlade(float3 positionWS)
{
	float d = distance(positionWS, _WorldSpaceCameraPos);
	float t;
	Unity_InverseLerp_float_Clamp01(0.0f, _grassBladeLODMaxDistance, d, t);
	return max(ceil((1.0f - t) * GRASS_BLADES), 1);
}

int GetBladeSegment(float3 positionWS)
{
	float d = distance(positionWS, _WorldSpaceCameraPos);
	float t;
	Unity_InverseLerp_float_Clamp01(0.0f, _grassBladeLODMaxDistance, d, t);
	return max(ceil((1.0f - t) * BLADE_SEGMENTS), 1);
}

float3x3 FigureTransformationForHeight(float v, float3x3 twistMatrix, float3x3 tsToWs, float maxBend, float maxWindBend, float3 windAxis, float offsetAmount, float3 offsetAxis)
{
	// Matrix 2: Bend Matrix
	// the bend amount increases towards the tip
	// higher bladeCurvature values cause the tip to bend more harshly
	float3x3 bendMatrix = AngleAxis3x3(maxBend * pow(abs(v), _BladeCurvature), float3(1,0,0));
	//bendMatrix = AngleAxis3x3(0, float3(1,0,0));
	
	// Matrix 3: Wind Matrix
	//The rotation due to wind is higher closer to the tip
	float3x3 windMatrix = AngleAxis3x3(maxWindBend * v, windAxis);
	//windMatrix = AngleAxis3x3(0, float3(1,0,0));

	// Matrix 4: Offset Matrix
	// * clamp(v, 0.0, 1.0)
	float3x3 offsetMatrix = AngleAxis3x3(min(PI * 0.5f * _collisionStrength * offsetAmount * v, PI * 0.5f), offsetAxis);
	//float3x3 offsetMatrix = AngleAxis3x3(PI/5 * offsetAmount, offsetAxis);
	
	//rotation is applied from left to right
	//bend, then twist, then convert to world space, then apply wind
	return mul(windMatrix, mul(offsetMatrix ,mul(tsToWs, mul(twistMatrix, bendMatrix))));
}

GeometryOutput BuildGrassVertex(
	float3 anchorWS, float2 uv, float width, float height,
	float3x3 transformMatrix,
	float3 faceNormalTS,
	half fogFactor,
	half4 vertexColor) {
	GeometryOutput OUT = ZERO_INITIALIZE(GeometryOutput, OUT);

	float3 offsetTS = float3((uv.x - 0.5f) * width, 0, uv.y * height);
	float3 offsetWS = mul(transformMatrix, offsetTS);
	
	float3 positionWS = anchorWS + offsetWS;

	float3 faceNormalWS_T = mul(faceNormalTS, transformMatrix);
	
	OUT.diffuseColor = vertexColor;
	OUT.uv = uv;
	OUT.positionWS = positionWS;
	
	float2 worldPosUV = anchorWS.xz;
	worldPosUV = TRANSFORM_TEX(worldPosUV, _ColorNoiseTexture);
	//half noiseColorControlValue = SAMPLE_TEXTURE2D(_ColorNoiseTexture, sampler_ColorNoiseTexture, worldPosUV).g + 0.2;
	half scale = _ColorNoiseScale;
	//World-space noise value
	OUT.NoiseValue = Unity_SimpleNoise_float_Fractal(worldPosUV, scale) + 0.1;

	OUT.normalWS = faceNormalWS_T;
	//OUT.normalWS = normalWS;
	
	//Clip space position
	//OUT.positionCS = WorldToHClip(positionWS, OUT.normalWS);
	OUT.positionCS = TransformWorldToHClip(positionWS);
	//Fog factor
	OUT.fogFactor = fogFactor;
	//View Direction
	OUT.viewDirectionWS = _WorldSpaceCameraPos.xyz - OUT.positionWS;

	//Double-Sided Normal Fixation
	//float viewNormalDot = dot(OUT.viewDirectionWS, OUT.normalWS);
	//int signOfViewNormal = sign(viewNormalDot);
	//OUT.normalWS *= signOfViewNormal;
	
	return OUT;
}

// Vertex, Geometry & Fragment Shaders
Varyings vert(Attributes input) {
	Varyings output = (Varyings)0;
	output.positionOS = input.positionOS;
	output.normalOS = input.normalOS;
	output.tangentOS = input.tangentOS;
	output.color = input.vertexColor;
	output.uv = input.texcoord;
	return output;
}

// wind and basic grassblade setup from https://roystan.net/articles/grass-shader.html
// limit for vertices
[maxvertexcount(GRASS_BLADES * (BLADE_SEGMENTS * 2 + 2))]
void geom(point Varyings IN[1], inout TriangleStream<GeometryOutput> triStream)
{
	//Tangent Space Calculation
	float3 tangentOS = float3(1,0,0);
	float3 tangentWS = TransformObjectToWorldDir(tangentOS);
	float3 normalWS = TransformObjectToWorldNormal(IN[0].normalOS);
	//float3 faceNormalOS = cross(IN[0].normalOS, tangentWS); //float3(0,1,0);
	float3 faceNormalTS = float3(0,1,0);
	
	float3 bitangentWS = cross(normalWS, tangentWS);
	float3x3 tangentToWorld = transpose(float3x3(tangentWS, bitangentWS, normalWS));

	//World-Space Position
	float3 positionWS = TransformObjectToWorld(IN[0].positionOS.xyz);

	//_Height *= clamp(rand(IN[0].positionOS.xyz), 1 - _RandomHeight, 1 + _RandomHeight);
	float2 grassWidthAndHeight = GetBladeDimensions(IN[0], positionWS);

	// camera distance for culling 
	int blades_LOD = GetGrassBlade(positionWS);
	int segment_LOD = GetBladeSegment(positionWS);

	// Generate Grass Blades
	for (int j = 0; j < blades_LOD; j++)
	{
		float2 randomOffsetXZ = hash3(float3(positionWS.x, positionWS.z, j)).xy * 2;
		float3 positionOffsetOS = j == 0 ? 0 : float3(randomOffsetXZ.x, 0, randomOffsetXZ.y);
		
		positionWS = TransformObjectToWorld(IN[0].positionOS.xyz + positionOffsetOS);
		
		//Fog factor
		half4 positionCS = TransformWorldToHClip(positionWS);
		half fogFactor = ComputeFogFactor(positionCS.z);
		
		// Build Matrices
		// Matrix 1: Twist matrix, the matrix to rotate the grass blade
		float angle = rand(positionWS, 2) * PI;
		float3x3 twistMatrix = AngleAxis3x3(angle, float3(0,0,1));
		//float3x3 twistMatrix_T = AngleAroundZAxis_Vector(angle);
		//twistMatrix = AngleAxis3x3(0, float3(0,0,1));

		float maxBend = rand(positionWS, 3) * PI * 0.5f * 0.2f;
	
		// Matrix 2: Wind matrix axis
		float3 windAxis = GetWindAxis(positionWS, normalWS);
		float maxWindBend = PI * 0.5f * _WindStrengthY;

		// Matrix 3: Collision matrix axis
		float3 offsetAxis = GetOffsetAxis(positionWS, normalWS);
		float offsetByCollision = GetCollisionOffset(positionWS);
		
		//float3 faceNormalWS;
		//faceNormalWS = mul(faceNormalTS, twistMatrix);
		//faceNormalWS = TransformTangentToWorld(faceNormalWS, tangentToWorld);

		// Generate Blade Segments
		for (int i = 0; i < segment_LOD; i++)
		{
			// taper width, increase height;
			float v = i / (float)segment_LOD;

			float3x3 transformMatrix = FigureTransformationForHeight(v, twistMatrix, tangentToWorld, maxBend, maxWindBend, windAxis, offsetByCollision, offsetAxis);
			
			// every segment adds 2 new triangles
			triStream.Append(BuildGrassVertex(positionWS, float2(0, v), grassWidthAndHeight.x, grassWidthAndHeight.y, transformMatrix, faceNormalTS, fogFactor, IN[0].color));
			triStream.Append(BuildGrassVertex(positionWS, float2(1, v), grassWidthAndHeight.x, grassWidthAndHeight.y, transformMatrix, faceNormalTS, fogFactor, IN[0].color));
		}
		
		float3x3 transformMatrix = FigureTransformationForHeight(1, twistMatrix, tangentToWorld, maxBend, maxWindBend, windAxis, offsetByCollision, offsetAxis);
		// Add just below the loop to insert the vertex at the tip of the blade.
		triStream.Append(BuildGrassVertex(positionWS, float2(0, 1), grassWidthAndHeight.x, grassWidthAndHeight.y, transformMatrix, faceNormalTS, fogFactor, IN[0].color));
		triStream.Append(BuildGrassVertex(positionWS, float2(1, 1), grassWidthAndHeight.x, grassWidthAndHeight.y, transformMatrix, faceNormalTS, fogFactor, IN[0].color));

		// restart the strip to start another grass blade
		triStream.RestartStrip();
	}
	
}

//Fragment Pass
float4 frag_UniformQuad(GeometryOutput input) : SV_Target {
	input.viewDirectionWS = SafeNormalize(input.viewDirectionWS);
	
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
	float shadow = clamp(mainLight.shadowAttenuation, 0.25, 1.0);
	half4 sampledRampColor = SAMPLE_TEXTURE2D(_ColorRampTexture, sampler_ColorRampTexture, half2(input.NoiseValue,0));

	half3 GI = SampleSH(input.normalWS) * 1;
	
	// SSS
	half3 directLighting = dot(mainLight.direction, input.normalWS) * mainLight.color;
	directLighting +=
		saturate(pow(dot(input.viewDirectionWS, -mainLight.direction), 8) * saturate(dot(input.normalWS, mainLight.direction)))
		* mainLight.color * 0.5f;
	half3 sss = directLighting * shadow + GI;
	//sss = directLighting;
	
	// Light Calculation
	BRDFData brdfData;
	alpha = 1.0;
	float lerpFactor = SafePositivePow_float(input.uv.y, .3f);
	float3 albedo = lerp(_BottomColor, sampledRampColor.xyz * input.diffuseColor.xyz, lerpFactor);
	//albedo = pow(input.uv.y, _BottomFactor);
	//float3 albedo = sampledRampColor.xyz * 1.0 * pow(input.uv.y, .5) * input.diffuseColor.xyz;
	InitializeBRDFData(albedo, 0, 0, 0.1f, alpha, brdfData);
	
	//float3 litColor =
	//	max(dot(input.normalWS, mainLight.direction), 0.75) *clamp(mainLight.shadowAttenuation, 0.25, 1.0)
	//	* sampledRampColor.rgb * 3.0 *input.uv.y *mainLight.color;
	
	//Direct PBR
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

	sss *= albedo;
	
	float3 grassCol = spec + sss;

	// Recalculate Fog Factor
	//half clipSpaceZ = TransformWorldToHClip(input.positionWS).z;
	//half fogF = ComputeFogFactor(clipSpaceZ);
	
	//grassCol = input.fogFactor;
	//grassCol = input.normalWS;
	//grassCol = GI;
	//grassCol = float(saturate(floor(input.NoiseValue-0.5f)+0.5));
	//return fogF;
	//return clipSpaceZ;
	//return 1;
	return half4(MixFog(grassCol, input.fogFactor), 1.0h);
}

//float3 grassCol = sampledRampColor * 3.0 * input.uv.y * clamp(mainLight.shadowAttenuation, 0.25, 1.0) * mainLight.color;

//return half4(
//	max(
//		dot(input.normalWS, mainLight.direction), 0.75) *
//		clamp(mainLight.shadowAttenuation, 0.25, 1.0) *
//		sampledRampColor.rgb * 3.0 *
//		input.uv.y *
//		mainLight.color, 1.0);

#endif