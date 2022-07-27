Shader "Unlit/GeoPointGrass_UniformQuad_Tex" {
	Properties {
		[Header(Shading Control)]
		[NOSCALEOFFSET]_ColorRampTexture("Color Ramp Color Control", 2D) = "white" {}
		_ColorNoiseScale("Color Noise Scale", Float) = 5 
		_ColorNoiseTexture("Color Noise Texture", 2D) = "White" {}
		[NoScaleOffset]_GrassBladeTexture("Grass Blade Texture(Black And White)", 2D) = "White"{}
		_BottomColor("Bottom Color Control", Color) = (0,0,0)
		_BottomFactor("Bottom Amount Control", Range(0.1, 4)) = .5
		
		[Space]
		[Header(Dimention Control)]
		_Width("Width", Float) = 1
		_RandomWidth("Random Width", Float) = 1
		[NoScaleOffset]_HeightNoiseTexture("Height Noise Texture", 2D) = "black" {}
		_HeightTextureMult("Height UV Tilling", Float) = .01
		_Height("Height", Float) = 1
		_RandomHeight("Random Height", Float) = 1
		_BladeCurvature("Blade Curvature", Range(0.01, 1)) = .5

		[Space]
		[Header(Wind Control)]
		_WindStrengthY("Wind Strength", Range(0.01, 1)) = 0.1
		
		[Space]
		[Header(Collision Control)]
		_collisionOffsetMaxDistance("Max Collision Distance", Float) = 2.5
		_collisionStrength("Collision Repulse Strength", Float) = 10
		
		[Space]
		[Header(LOD Control)]
		_grassBladeLODMaxDistance("Grass LOD Max Distance", Float) = 60
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 300

		//AlphaToMask On
		Cull Off

		Pass {
			Name "ForwardLit"
			Tags {"LightMode" = "UniversalForward"}

			HLSLPROGRAM
			
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles
			#pragma target 4.5

			#pragma require geometry

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag_UniformQuad

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile_fog

			// Defines
			#define GRASS_BLADES 3 // blades per vertex
			#define BLADE_SEGMENTS 3
			#define GRASS_UNIFORM_QUAD

			// Includes

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "GrassInput.hlsl"
			#include "../../ShaderLibrary/SimplexNoise.hlsl"
			#include "../../ShaderLibrary/Random.hlsl"
			#include "GrassPasses.hlsl"

			ENDHLSL
		}
	}
}