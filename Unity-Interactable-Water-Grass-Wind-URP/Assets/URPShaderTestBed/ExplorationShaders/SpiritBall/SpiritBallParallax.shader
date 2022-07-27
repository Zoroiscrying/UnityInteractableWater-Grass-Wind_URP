Shader "Custom/SpiritBallParallax"
{
    Properties
    {
        [Header(Base Lighting Params)]
        _MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset]_NormalMap ("Normal Map", 2D) = "Bump" {}
        
        [Header(Parallax Mapping)]
        [NoScaleOffset]_HeightMap ("Height Map", 2D) = "black" {}
        _ParallaxStrength ("Parallax Strength", Float) = .1
        
        [Header(Emission)]
        [NoScaleOffset]_EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionPercentage ("Emission Percentage", Range(0, 1)) = 1
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
            
            CULL OFF
            
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

            // Defines
            #define PARALLAX_BIAS 0
            //#define PARALLAX_OFFSET_LIMITING
            #define PARALLAX_RAYMARCHING_STEPS 10
            #define PARALLAX_RAYMARCHING_INTERPOLATE
	        #define PARALLAX_RAYMARCHING_SEARCH_STEPS 3
	        #define PARALLAX_FUNCTION ParallaxRaymarching

            // includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/URPShaderTestBed/ShaderLibrary/SimplexNoise.hlsl"
            #include "SpiritBallParallax.hlsl"
            
            ENDHLSL
        }
    }
}