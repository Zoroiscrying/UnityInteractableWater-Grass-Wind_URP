////////////////////////////////////////////
//////////////// grass.hlsl ////////////////
////////////////////////////////////////////

// Original by @Cyanilux
// Grass Geometry Shader, Written for Universal RP with help from https://roystan.net/articles/grass-shader.html

// Modified by @niuage to add tessellation. Also removed some code I didnt need in my own game, so feel free to revert to the original
// version from Cyanilux here: https://pastebin.com/Ey01tzLq

// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
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

float3 _LightDirection;

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

// Variables
CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher

TEXTURE2D(_WindNoiseTexture);		SAMPLER(sampler_WindNoiseTexture);

float3 _ColliderPosition;

float _BladeCurvature;

float _WindPosMult;
float _WindTimeMult;
float _WindTexMult;
float _WindStrength;

float4 _Color;
float4 _Color2;
float _Width;
float _RandomWidth;
float _Height;
float _RandomHeight;

float _collisionOffsetMaxDistance;
float _collisionStrength;

float _grassBladeLODMaxDistance;
CBUFFER_END

// Middle functions
float2 GetBladeDimensions(float3 positionWS)
{
	//Calculate the blade width and height, applying random variance
	float width = randNegative1to1(positionWS.zyx, 0) * _RandomWidth + _Width;
	float height = randNegative1to1(positionWS.yxz, 1) * _RandomHeight + _Height;
	return float2(width, height);
}

// Returns the center point of a triangle defined by the three arguments
float3 GetTriangleCenter(float3 a, float3 b, float3 c) {
	return (a + b + c) / 3.0;
}
float2 GetTriangleCenter(float2 a, float2 b, float2 c) {
	return (a + b + c) / 3.0;
}

float3 GetWindAxis(float3 positionWS, float3 normalWS)
{
	//get the world-position-based uv.
	float2 windUV = positionWS.xz * _WindPosMult + _Time.y * _WindTimeMult;
	windUV = windUV * _WindTexMult;
	//sample the wind noise texture 
	float2 windNoise = SAMPLE_TEXTURE2D_LOD(_WindNoiseTexture, sampler_WindNoiseTexture, windUV, 0).xy * 2 - 1;
	// We want the grass to blow by rotating in a direction perpendicular to it's normal
	// cross will find one such vector. Since windNoise is not normalized, it also encodes some strength.
	return cross(normalWS, float3(windNoise.x, 0, windNoise.y));
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
	float t = 1 - smoothstep(0.1f, _collisionOffsetMaxDistance, d);
	return t;
}

void Unity_InverseLerp_float_Clamp01(float A, float B, float T, out float Out)
{
	Out = clamp((T - A)/(B - A), 0.0f, 1.0f);
}

int GetBladeSegment(float3 positionWS)
{
	float d = distance(positionWS, _WorldSpaceCameraPos);
	float t;
	Unity_InverseLerp_float_Clamp01(0.0f, _grassBladeLODMaxDistance, d, t);
	return max(ceil((1.0f - t) * BLADE_SEGMENTS), 1);
}

float3x3 FigureTransformationForHeight(float v, float3x3 twistMatrix, float3x3 tsToWs, float maxBend, float3 windAxis, float offsetAmount, float3 offsetAxis)
{
	// Matrix 2: Bend Matrix
	// the bend amount increases towards the tip
	// higher bladeCurvature values cause the tip to bend more harshly
	float3x3 bendMatrix = AngleAxis3x3(maxBend * pow(v, _BladeCurvature), float3(1,0,0));
	//bendMatrix = AngleAxis3x3(0, float3(1,0,0));
	
	// Matrix 3: Wind Matrix
	//The rotation due to wind is higher closer to the tip
	float3x3 windMatrix = AngleAxis3x3(_WindStrength * v, windAxis);
	//windMatrix = AngleAxis3x3(0, float3(1,0,0));

	// Matrix 4: Offset Matrix
	// * clamp(v, 0.0, 1.0)
	float3x3 offsetMatrix = AngleAxis3x3(min(maxBend * _collisionStrength * offsetAmount, PI/2), offsetAxis);
	
	//rotation is applied from left to right
	//bend, then twist, then convert to world space, then apply wind
	return mul(windMatrix, mul(offsetMatrix ,mul(tsToWs, mul(twistMatrix, bendMatrix))));
}

void SetupOutputVertex(inout TriangleStream<GeometryOutput> triStream,
	GeometryOutput output, float3 normalWS,
	float3 anchorWS, float2 dimensions, float3x3 tsToWs, float2 uv)
{
	float3 offsetTS = float3((uv.x - 0.5f) * dimensions.x, 0, uv.y * dimensions.y);//we want 0.5 to be on the center
	float3 offsetWS = mul(tsToWs, offsetTS);

	float3 positionWS = anchorWS + offsetWS;
	
	output.positionWS = positionWS;
	output.positionCS = WorldToHClip(output.positionWS, normalWS);
	output.uv = uv;
	triStream.Append(output);
}


// Vertex, Geometry & Fragment Shaders

Varyings vert(Attributes input) {
	Varyings output = (Varyings)0;

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	// Seems like GetVertexPositionInputs doesn't work with SRP Batcher inside geom function?
	// Had to move it here, in order to obtain positionWS and pass it through the Varyings output.

	output.positionCS = vertexInput.positionCS; //vertexInput.positionCS; //
	output.positionWS = vertexInput.positionWS;
	output.normalOS = input.normalOS;
	output.tangentOS = input.tangentOS;
	return output;
}

[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
void geom(uint primitiveID : SV_PrimitiveID, triangle Varyings input[3], inout TriangleStream<GeometryOutput> triStream) {
	GeometryOutput output = (GeometryOutput)0;

	// Construct World -> Tangent Matrix (for aligning grass with mesh normals)
	float3 tangentWS = normalize(input[1].positionWS - input[0].positionWS);
	float3 normalWS = normalize(cross(tangentWS, input[2].positionWS - input[0].positionWS));
	float3 bitangentWS = normalize(cross(tangentWS, normalWS));
	//construct the TsToWs Matrix
	float3x3 tangentToWorld = transpose(float3x3(tangentWS, bitangentWS, normalWS));
	//calculate the center of the triangle
	float3 positionWS = GetTriangleCenter(input[0].positionWS,input[1].positionWS,input[2].positionWS);
	//get the grass width and height using the positonWS
	float2 grassWidthAndHeight = GetBladeDimensions(positionWS);
	
	// Matrix 1: Twist matrix, the matrix to rotate the grass blade
	float3x3 twistMatrix = AngleAxis3x3(rand(positionWS, 2) * TWO_PI, float3(0, 0, 1));
	// This bend angle decides how much the tip bends towards 90 degrees
	float maxBend = rand(positionWS, 3) * PI * 0.5f * 0.2f;

	float3 windAxis = GetWindAxis(positionWS, normalWS);
	float3 offsetAxis = GetOffsetAxis(positionWS, normalWS);

	float offsetByCollision = GetCollisionOffset(positionWS);

	int bladeSegments_LOD = GetBladeSegment(positionWS);
	
	for (int i = 0; i < bladeSegments_LOD; i++)
	{
		// The v rises as we increase in segments
		float v = i / (float)bladeSegments_LOD;
		// The u of the first vertex. It converges on 0.5 as the segment increases.
		// 0/1 -> 0.5/0.5
		float u = 0.5 - (1 - v) * 0.5f;

		// Transform matrix to figure out the world space vertex
		float3x3 transform = FigureTransformationForHeight(v, twistMatrix, tangentToWorld, maxBend, windAxis, offsetByCollision, offsetAxis);

		// Append the first vertex
		SetupOutputVertex(triStream, output, normalWS, positionWS, grassWidthAndHeight, transform, float2(u, v));
		
		// Append the Second vertex
		SetupOutputVertex(triStream, output, normalWS,positionWS, grassWidthAndHeight, transform, float2(1-u, v));
	}

	// Final vertex at top of blade
	float3x3 tipTransform = FigureTransformationForHeight(1, twistMatrix, tangentToWorld, maxBend, windAxis, offsetByCollision, offsetAxis);
	SetupOutputVertex(triStream, output, normalWS, positionWS, grassWidthAndHeight, tipTransform, float2(0.5, 1));

	triStream.RestartStrip();
}