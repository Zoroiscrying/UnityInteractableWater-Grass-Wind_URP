#ifndef CUSTOM_TERRAIN_STRUCT
#define CUSTOM_TERRAIN_STRUCT

    //Shadow pass
    #ifdef SHADERPASS_SHADOWCASTER

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float3 normalOS		: NORMAL;
            float2 uv0          : TEXCOORD0;
            float4 tangentOS    : TANGENT;
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float3 normalWS		: NORMAL;
            float3 positionWS	: TEXCOORD1;
        };

    //Forward Pass
    #else

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float3 normalOS		: NORMAL;
            float2 uv0          : TEXCOORD0;
            float2 uv1          : TEXCOORD1;
            float4 tangentOS    : TANGENT;
            float4 vertexColor  : COLOR;
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float2 uv           : TEXCOORD0;
            float3 normalWS		: NORMAL;
            float4 tangentWS    : TEXCOORD6;
            float3 positionWS	: TEXCOORD1;
            float4 vertexColor  : TEXCOORD7;
            float4 fogFactorAndVertexLight : TEXCOORD2;
            float3 viewDirectionWS : TEXCOORD3;
            float3 viewDirectionTS : TEXCOORD8;
            
            #if defined(LIGHTMAP_ON)
            float2 lightmapUV : TEXCOORD4;
            #endif
            
            #if !defined(LIGHTMAP_ON)
            float3 sh : TEXCOORD5;
            #endif
        };

    #endif

#endif