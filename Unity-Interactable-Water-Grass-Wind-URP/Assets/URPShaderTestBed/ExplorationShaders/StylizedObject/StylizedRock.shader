Shader "Custom/StylizedRock"
{
    Properties
    {
        _RockAlbedo("Rock Albedo", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry+0"
        }

        Pass
        {
            Name "Forward Light Pass"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM

            #pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles
			#pragma target 3.0

            //register functions
            #pragma vertex vert
			#pragma fragment frag
            
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
            #include "Assets/URPShaderTestBed/ShaderLibrary/SimplexNoise.hlsl"
            #include "Assets/URPShaderTestBed/ShaderLibrary/VoronoiNoise.hlsl"
            //#include "Assets/URPShaderTestBed/ShaderLibrary/Tilling.hlsl"
            #include "StylizedRock.hlsl"
            
            ENDHLSL
            
        }
    }
}