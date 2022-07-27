#if !defined(EXTRUDINGOUTLINE_INCLUDED)
#define EXTRUDINGOUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The vertex function input
struct Attributes {
    float4 positionOS   : POSITION; // Vertex position in object space
    float3 normalOS     : NORMAL; // Vertex normal vector in object space
};

// Vertex function output and geometry function input
struct VertexOutput {
    float4 positionCS   : SV_POSITION; // Position in clip space
};

float _OutlineThickness;
float4 _OutlineColor;

// Vertex functions
VertexOutput vert(Attributes input) {
    // Initialize the output struct
    VertexOutput output = (VertexOutput)0;
    float3 normalOS = input.normalOS;
    float3 extrudedPosition = input.positionOS.xyz + normalOS * _OutlineThickness;
    // Calculate position in world space
    VertexPositionInputs vertexInput = GetVertexPositionInputs(extrudedPosition.xyz);
    output.positionCS = vertexInput.positionCS;
    return output;
}

// Fragment functions
half4 frag(VertexOutput input) : SV_Target {
    return _OutlineColor;
}

#endif