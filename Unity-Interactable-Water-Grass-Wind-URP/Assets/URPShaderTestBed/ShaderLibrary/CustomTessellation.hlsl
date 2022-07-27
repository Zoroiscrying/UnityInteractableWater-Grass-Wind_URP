#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

/////////////////////////////////////////////////////////////////////
//////////////////// tessellation/CustomTessellation.hlsl ////////////////////////
/////////////////////////////////////////////////////////////////////

// Tessellation programs based on this article by Catlike Coding:
// https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
#define UNITY_CAN_COMPILE_TESSELLATION 1
#   define UNITY_domain                 domain
#   define UNITY_partitioning           partitioning
#   define UNITY_outputtopology         outputtopology
#   define UNITY_patchconstantfunc      patchconstantfunc
#   define UNITY_outputcontrolpoints    outputcontrolpoints
#endif

#ifndef PATCH_FUNCTION
	#define PATCH_FUNCTION "patchConstantFunction"
#endif

#ifndef PARTITION_METHOD
	#define PARTITION_METHOD "integer"
#endif

#include "Tessellation.hlsl"

CBUFFER_START(UnityPerMaterial)
//TODO::Change this to various function
float _TessellationUniform;
float _TessellationMinDistance;
float _TessellationMaxDistance;
float _TessellationFactor;
CBUFFER_END

// Constant Function
TessellationFactors patchConstantFunction (InputPatch<Varyings, 3> patch)
{
	TessellationFactors f;
	f.edge[0] = _TessellationUniform;
	f.edge[1] = _TessellationUniform;
	f.edge[2] = _TessellationUniform;
	f.inside = _TessellationUniform;
	return f;
}

// Distance Function
TessellationFactors patchDistanceFunction(InputPatch<Varyings, 3> patch)
{
	return UnityDistanceBasedTess_WS(patch[0].positionWS, patch[1].positionWS, patch[2].positionWS, 50.0, 250.0, 6);
}

TessellationFactors patchDistanceFunction_Variable_WS(InputPatch<Varyings, 3> patch)
{
	return UnityDistanceBasedTess_WS(patch[0].positionWS, patch[1].positionWS, patch[2].positionWS, _TessellationMinDistance, _TessellationMaxDistance, _TessellationFactor);
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
//fractional_odd integer fractional_even
[UNITY_partitioning(PARTITION_METHOD)]
[UNITY_patchconstantfunc(PATCH_FUNCTION)]
Varyings hull (InputPatch<Varyings, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}

[UNITY_domain("tri")]
Varyings domain(TessellationFactors factors, OutputPatch<Varyings, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
	Varyings v;

	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(positionWS)
	MY_DOMAIN_PROGRAM_INTERPOLATE(positionCS)
	MY_DOMAIN_PROGRAM_INTERPOLATE(normalOS)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangentOS)

	return v;
}


#endif