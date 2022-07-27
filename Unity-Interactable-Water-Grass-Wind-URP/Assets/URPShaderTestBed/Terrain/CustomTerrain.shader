Shader "Custom/CustomTerrain"
{
    Properties
    {
        [Header(Water(Alpha))]
        _WaterColor ("Water Color", Color) = (1,1,1,1)
        _WaterEdge("Water Edge Hardness", Range(0, 1)) = 0.2
        [NoScaleOffset]_WaterRoughness ("Water Roughness", 2D) = "white" {}
        _ParallaxStrength("Parallax Strength", Range(0, 0.1)) = 0.05
        //01
        [Header(Textures_01(Black))]
        [NoScaleOffset]_Albedo01 ("Albedo 01", 2D) = "white" {}
        [NoScaleOffset]_Normal01("Normal 01", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO01 ("Metal/Rough/Height/AO 01", 2D) = "white" {}
        _TextureScale01("Texture Scale 01", Float) = 1.0
        _Falloff01("Blend Falloff 01", Range(0, 1)) = 0.2
        //02
        [Header(Textures_02(Red))]
        [NoScaleOffset]_Albedo02 ("Albedo 02", 2D) = "white" {}
        [NoScaleOffset]_Normal02("Normal 02", 2D) = "bump" {}
        [NoScaleOffset]_Normal02Detail("Normal 02 Detail", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO02 ("Metal/Rough/Height/AO 02", 2D) = "white" {}
        _TextureScale02("Texture Scale 02", Float) = 1.0
        _Falloff02("Blend Falloff 02", Range(0, 1)) = 0.2
        //03
        [Header(Textures_03(Green))]
        [NoScaleOffset]_Albedo03 ("Albedo 03", 2D) = "white" {}
        [NoScaleOffset]_Normal03("Normal 03", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO03 ("Metal/Rough/Height/AO 03", 2D) = "white" {}
        _TextureScale03("Texture Scale 03", Float) = 1.0
        _Falloff03("Blend Falloff 03", Range(0, 1)) = 0.2
        //04
        [Header(Textures_04(Blue))]
        [NoScaleOffset]_Albedo04 ("Albedo 04", 2D) = "white" {}
        [NoScaleOffset]_Normal04("Normal 04", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO04 ("Metal/Rough/Height/AO 04", 2D) = "white" {}
        _TextureScale04("Texture Scale 04", Float) = 1.0
        _Falloff04("Blend Falloff 04", Range(0, 1)) = 0.2
        
    }
    SubShader
    {
        Tags 
        {             
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+0"
        }

        Pass
        {
            Name "Universal Forward"
            Tags 
            { 
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
            
            // register functions
            #pragma vertex vert
            #pragma fragment frag
            //#pragma hull hull
			//#pragma domain domain

            // Pragmas
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles
            //#pragma require tessellation tessHW
            #pragma target 5.0
            
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
            //#define PARTITION_METHOD "fractional_odd"
            //#define PATCH_FUNCTION "patchDistanceFunction_Variable_WS"
            
            // includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "CustomTerrainStruct.hlsl"
            //#include "CustomTessellation_Terrain.hlsl"
            #include "CustomTerrain.hlsl"
            
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
            Cull Back
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM
            // Register Functions
            #pragma vertex vert
            #pragma fragment frag
            
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // Defines
            #define SHADERPASS_SHADOWCASTER

            // includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "CustomTerrainStruct.hlsl"
            #include "CustomTerrainShadowCaster.hlsl"
            
            
            ENDHLSL
        }
    }
    
}