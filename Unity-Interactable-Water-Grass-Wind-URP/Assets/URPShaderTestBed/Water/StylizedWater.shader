Shader "Custom/StylizedWater"
{
    Properties
    {
        [Header(Colors)]
        _Color ("Water Body Color", Color) = (1,1,1,1)
        _FogColor("Fog Color", Color) = (1,1,1,1)

        [Header(Normal maps)]
        [Normal]_NormalA("Normal A", 2D) = "bump" {} 
        [Normal]_NormalB("Normal B", 2D) = "bump" {}
        _NormalStrength("Normal strength", float) = 1
        _NormalPanningSpeeds("Normal panning speeds", Vector) = (0,0,0,0)

        [Header(Foam)]
        _FoamThreshold("Foam threshold", float) = 0
        _FoamTexture("Foam texture", 2D) = "white" {} 
        _FoamTextureSpeedX("Foam texture speed X", float) = 0
        _FoamTextureSpeedY("Foam texture speed Y", float) = 0
        _FoamLinesSpeed("Foam lines speed", float) = 0
        _FoamIntensity("Foam intensity", float) = 1

        [Header(Other)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _FresnelPower("Fresnel power", float) = 1
    }
    
    SubShader
    {
        Tags 
        {             
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward Lit"
            Tags 
            { 
                "LightMode" = "UniversalForward"
            }
            LOD 200
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            CULL BACK
            ZTest LEqual
            ZWrite Off
            
            HLSLPROGRAM
            //register functions
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

            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #define _ALPHAPREMULTIPLY_ON 1
            #define _REFLECTION_PLANARREFLECTION 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "StylizedWater.hlsl"

            ENDHLSL
        }
    }
}