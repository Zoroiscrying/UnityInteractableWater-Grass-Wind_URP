/////////////////////////////////////////////////
//////////////// GeoGrass.shader ////////////////
/////////////////////////////////////////////////
// @Cyanilux
// Grass Geometry Shader, Written for Universal RP with help from https://roystan.net/articles/grass-shader.html

Shader "Unlit/GeoGrass" {
	Properties {
		[Header(Shading Control)]
		[NOSCALEOFFSET]_ColorRampTexture("Color Ramp Color Control", 2D) = "white" {}
		_ColorNoiseTexture("Color Noise Texture", 2D) = "White" {}
		
		[Space]
		[Header(Dimention Control)]
		_Width("Width", Float) = 1
		_RandomWidth("Random Width", Float) = 1
		[NoScaleOffset]_HeightNoiseTexture("Height Noise Texture", 2D) = "black" {}
		_HeightTextureMult("Height UV Tilling", Float) = .01
		_Height("Height", Float) = 1
		_RandomHeight("Random Height", Float) = 1
		_BladeCurvature("Blade Curvature", Range(0.01, 1)) = .5
		
		[Header(Tessellation Control)]
		_TessellationMinDistance("Tessellation Min Distance", Float) = 1
		_TessellationMaxDistance("Tessellation Max Distance", Float) = 1
		_TessellationFactor("Tessellation Factor", Range(1, 64)) = 1
		
		[Space]
		[Header(Wind Control)]
		_WindPosMult("World Position UV Multiplier", Float) = 1
		_WindTimeMult("UV Scrolling Speed", Float) = 1
		_WindTexMult("UV Tilling", Float) = 1
		[NoScaleOffset]_WindNoiseTexture("Wind Noise Texture", 2D) = "black" {}
		_WindStrength("Wind Strength", Float) = 0.1
		
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

		Cull Off

		Pass {
			Name "ForwardLit"
			Tags {"LightMode" = "UniversalForward"}

			HLSLPROGRAM
			
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles
			#pragma require tessellation tessHW
			#pragma target 4.5

			#pragma require geometry

			#pragma vertex vert
			#pragma geometry geom_TipShrink
			#pragma fragment frag_TipShirnk
			
			#pragma hull hull
			#pragma domain domain

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT

			// Defines

			#define BLADE_SEGMENTS 4
			#define PATCH_FUNCTION "patchDistanceFunction_Variable_WS"

			// Includes

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "grass_struct.hlsl"
			#include "grassInput_TipShrink.hlsl"
			#include "../ShaderLibrary/SimplexNoise.hlsl"
			#include "../ShaderLibrary/CustomTessellation.hlsl"
			#include "../ShaderLibrary/Random.hlsl"
			#include "grassVertGeomPasses.hlsl"
			#include "grassFragPasses.hlsl"
			
			ENDHLSL
		}
	}
}