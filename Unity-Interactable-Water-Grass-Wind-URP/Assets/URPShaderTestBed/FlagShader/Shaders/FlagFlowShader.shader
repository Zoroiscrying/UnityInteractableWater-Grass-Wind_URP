Shader "Custom/FlagFlowShader"
{
    Properties
    {
        _Tint("Albedo Tint", Color) = (1, 1, 1)
        _BaseMap ("Texture", 2D) = "white" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _FlagDispStrength("Flag Displacement Strength", vector) = (0.5,1,0,0)
    }
    SubShader
    {
        Tags 
        {             
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+0"
        }
        
        CULL OFF

        Pass
        {
            Name "Universal Forward"
            Tags 
            { 
                "LightMode" = "UniversalForward"
            }
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM
            
            // register functions
            #pragma vertex vert
            #pragma fragment frag

            // Pragmas
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles
            #pragma target 3.0
            
            // Keywords
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FlagFlow.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags 
            { 
                "LightMode" = "ShadowCaster"
            }
            
            // Render State
            Blend One Zero, One Zero
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM
            // Register Functions
            #pragma vertex vert
            #pragma fragment frag
            
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // Defines
            #define SHADERPASS_SHADOWCASTER

            // includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FlagFlowShadowCaster.hlsl"
            
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FlagInput.hlsl"
            #include "../../Wind/GlobalWind.hlsl"

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
                float2 uv           : TEXCOORD0;
            };

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

            Varyings vert(Attributes input)
            {

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // vertex displacement
                float3 windNoise = GetWindFromWorld(positionWS * 2);
                float3 wind_modified_value_float3 = float3(windNoise.x, windNoise.y, windNoise.z) * _FlagDispStrength.xyz;
                // modified normal calculation
                float3 displacedPositionOS = input.positionOS + wind_modified_value_float3;
                float3 bitangent = cross(input.normalOS, input.tangentOS);
                float3 posPlusTangent = displacedPositionOS + input.tangentOS * 0.01;
                float3 posPlusBitangent = displacedPositionOS + bitangent * 0.01;
                float3 modifiedTangent = posPlusTangent - displacedPositionOS;
                float3 modifiedBitangent = posPlusBitangent - displacedPositionOS;
                // - Get modified normal
                float3 modifiedNormal = normalize(cross(modifiedTangent, modifiedBitangent));

                float3 normalWS = TransformObjectToWorldNormal(modifiedNormal); 

                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPositionOS.xyz);
                
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv0;

                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 albedo_uv = TRANSFORM_TEX(input.uv, _BaseMap);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, albedo_uv).a;
                //clip(alpha - 0.5);
                return 0;
            }
            
            ENDHLSL
        }
    }
}
