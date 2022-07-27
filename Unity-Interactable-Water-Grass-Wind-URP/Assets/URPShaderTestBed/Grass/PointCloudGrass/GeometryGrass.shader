Shader "Custom/GeometryGrass"
{
	// The properties block of the Unity shader. In this example this block is empty
	// because the output color is predefined in the fragment shader code.
	Properties
	{
		_BottomColor("Bottom Color", Color) = (0,1,0,1)
		_TopColor("Top Color", Color) = (1,1,0,1)
		
		_GrassHeight("Grass Height", Float) = 1
		_GrassWidth("Grass Width", Float) = 0.06
		_RandomHeight("Grass Height Randomness", Float) = 0.25
		_WindSpeed("Wind Speed", Float) = 100
		_WindStrength("Wind Strength", Float) = 0.05
		_Radius("Interactor Radius", Float) = 0.3
		_Strength("Interactor Strength", Float) = 5
		_Rad("Blade Radius", Range(0,1)) = 0.6
		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
		_AmbientStrength("Ambient Strength",  Range(0,1)) = 0.5
		_MinDist("Min Distance", Float) = 40
		_MaxDist("Max Distance", Float) = 60
	}

		// The HLSL code block. Unity SRP uses the HLSL language.
		HLSLINCLUDE
		// This line defines the name of the vertex shader. 
		#pragma vertex vert
		// This line defines the name of the fragment shader. 
		#pragma fragment frag
		#pragma require geometry
		#pragma geometry geom

		#define GrassSegments 5 // segments per blade
		#define GrassBlades 4 // blades per vertex

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

		#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
		#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile_fragment _ _SHADOWS_SOFT
		#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
		#pragma multi_compile _ SHADOWS_SHADOWMASK
		#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
		#pragma multi_compile_fog   
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
		#pragma multi_compile _ LIGHTMAP_ON

		// The Core.hlsl file contains definitions of frequently used HLSL
		// macros and functions, and also contains #include references to other
		// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"	

		// The structure definition defines which variables it contains.
		// This example uses the Attributes structure as an input structure in
		// the vertex shader.
		struct Attributes
		{
			// The positionOS variable contains the vertex positions in object
			// space.
			float4 positionOS   : POSITION;
			float3 normal :NORMAL;
			float2 uv : TEXCOORD0;
			float4 color : COLOR;
			float4 tangent :TANGENT;
		};

		struct v2g
		{
			float4 pos : SV_POSITION;
			float3 norm : NORMAL;
			float2 uv : TEXCOORD0;
			float4 color : COLOR;
			float4 tangent : TANGENT;
		};

		half _GrassHeight;
		half _GrassWidth;
		half _WindSpeed;
		float _WindStrength;
		half _Radius, _Strength;
		float _Rad;

		float _RandomHeight;
		float _BladeForward;
		float _BladeCurve;

		float _MinDist, _MaxDist;

		uniform float3 _PositionMoving;

		v2g vert(Attributes v)
		{
			float3 v0 = v.positionOS.xyz;

			v2g OUT;
			OUT.pos = v.positionOS;
			OUT.norm = v.normal;
			OUT.uv = v.uv;
			OUT.color = v.color;

			OUT.norm = TransformObjectToWorldNormal(v.normal);
			OUT.tangent = v.tangent;
			return OUT;
		}

		struct g2f
		{
			float4 pos : SV_POSITION;
			float3 norm : NORMAL;
			float2 uv : TEXCOORD0;
			float3 diffuseColor : COLOR;
			float3 worldPos : TEXCOORD3;
			float fogFactor : TEXCOORD5;
		};

		float rand(float3 co)
		{
			return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
		}

		// Construct a rotation matrix that rotates around the provided axis, sourced from:
		// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
		float3x3 AngleAxis3x3(float angle, float3 axis)
		{
			float c, s;
			sincos(angle, s, c);

			float t = 1 - c;
			float x = axis.x;
			float y = axis.y;
			float z = axis.z;

			return float3x3(
				t * x * x + c, t * x * y - s * z, t * x * z + s * y,
				t * x * y + s * z, t * y * y + c, t * y * z - s * x,
				t * x * z - s * y, t * y * z + s * x, t * z * z + c
				);
		}


		float4 GetShadowPositionHClip(float3 input, float3 normal)
		{
			float3 positionWS = TransformObjectToWorld(input.xyz);
			float3 normalWS = TransformObjectToWorldNormal(normal);

			float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));

			#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#endif
					return positionCS;
		}

		// per new grass vertex
		g2f GrassVertex(float3 vertexPos, float width, float height, float offset, float curve, float2 uv, float3x3 rotation, float3 faceNormal, float3 color) {
			g2f OUT;
			float3 offsetvertices = vertexPos + mul(rotation, float3(width, height, curve) + float3(0, 0, offset));
			OUT.pos = GetShadowPositionHClip(offsetvertices, faceNormal);
			OUT.norm = faceNormal;
			OUT.diffuseColor = color;
			OUT.uv = uv;
			VertexPositionInputs vertexInput = GetVertexPositionInputs(vertexPos + mul(rotation, float3(width, height, curve)));
			OUT.worldPos = vertexInput.positionWS;
			float fogFactor = ComputeFogFactor(OUT.pos.z);
			OUT.fogFactor = fogFactor;
			return OUT;
		}

		// wind and basic grassblade setup from https://roystan.net/articles/grass-shader.html
		// limit for vertices
		[maxvertexcount(48)]
		void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
		{
			float forward = rand(IN[0].pos.yyz) * _BladeForward;
			// just use an up facing normal, works nicest
			float3 faceNormal = float3(0, 1, 0);
			float3 worldPos = TransformObjectToWorld(IN[0].pos.xyz);
			// camera distance for culling 
			float distanceFromCamera = distance(worldPos, _WorldSpaceCameraPos);
			float distanceFade = 1 - saturate((distanceFromCamera - _MinDist) / _MaxDist);
			// wind
			float3 v0 = IN[0].pos.xyz;
			float3 wind1 = float3(sin(_Time.x * _WindSpeed + v0.x) + sin(_Time.x * _WindSpeed + v0.z * 2) + sin(_Time.x * _WindSpeed * 0.1 + v0.x), 0,
				cos(_Time.x * _WindSpeed + v0.x * 2) + cos(_Time.x * _WindSpeed + v0.z));
			wind1 *= _WindStrength;

			// Interactivity
			float3 dis = distance(_PositionMoving, worldPos); // distance for radius
			float3 radius = 1 - saturate(dis / _Radius); // in world radius based on objects interaction radius
			float3 sphereDisp = worldPos - _PositionMoving; // position comparison
			sphereDisp *= radius; // position multiplied by radius for falloff
								  // increase strength
			sphereDisp = clamp(sphereDisp.xyz * _Strength, -0.8, 0.8);

			// set vertex color
			float3 color = (IN[0].color).rgb;
			// set grass height from tool, uncomment if youre not using the tool!
			_GrassHeight *= IN[0].uv.y;
			_GrassWidth *= IN[0].uv.x;
			_GrassHeight *= clamp(rand(IN[0].pos.xyz), 1 - _RandomHeight, 1 + _RandomHeight);

			// grass blades geometry
			for (int j = 0; j < (GrassBlades * distanceFade); j++)
			{
				// set rotation and radius of the blades
				float3x3 facingRotationMatrix = AngleAxis3x3(rand(IN[0].pos.xyz) * TWO_PI + j, float3(0, 1, -0.1));

				float3x3 transformationMatrix = facingRotationMatrix;

				faceNormal = mul(faceNormal, transformationMatrix);
				float radius = j / (float)GrassBlades;
				float offset = (1 - radius) * _Rad;
				float segmentWidth = _GrassWidth * 0.5;
				for (int i = 0; i < GrassSegments; i++)
				{
					// taper width, increase height;
					float t = i / (float)GrassSegments;
					float segmentHeight = _GrassHeight * t;

					// the first (0) grass segment is thinner
					//segmentWidth = i == 0 ? _GrassWidth * 0.3 : segmentWidth;

					float segmentForward = pow(abs(t), _BladeCurve) * forward;

					// Add below the line declaring float segmentWidth.
					float3x3 transformMatrix = i == 0 ? facingRotationMatrix : transformationMatrix;

					// first grass (0) segment does not get displaced by interactivity
					float3 newPos = i == 0 ? v0 : v0 + ((float3(sphereDisp.x, sphereDisp.y, sphereDisp.z) + wind1) * t);

					// every segment adds 2 new triangles
					triStream.Append(GrassVertex(newPos, segmentWidth, segmentHeight, offset, segmentForward, float2(0, t), transformMatrix, faceNormal, color));
					triStream.Append(GrassVertex(newPos, -segmentWidth, segmentHeight, offset, segmentForward, float2(1, t), transformMatrix, faceNormal, color));
				}
				// Add just below the loop to insert the vertex at the tip of the blade.
				triStream.Append(GrassVertex(v0 + float3(sphereDisp.x * 1.5, sphereDisp.y, sphereDisp.z * 1.5) + wind1, segmentWidth, _GrassHeight, offset, forward, float2(0, 1), transformationMatrix, faceNormal, color));
				triStream.Append(GrassVertex(v0 + float3(sphereDisp.x * 1.5, sphereDisp.y, sphereDisp.z * 1.5) + wind1, -segmentWidth, _GrassHeight, offset, forward, float2(1, 1), transformationMatrix, faceNormal, color));
				// restart the strip to start another grass blade
				triStream.RestartStrip();
			}
		}

		ENDHLSL

		// color pass
		SubShader
		{
			Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

			Cull Off
			Pass
		{

		HLSLPROGRAM

		float4 _TopColor;
		float4 _BottomColor;
		float _AmbientStrength;

		// The fragment shader definition.            
		half4 frag(g2f i) : SV_Target
		{
			float4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
			#if _MAIN_LIGHT_SHADOWS
				Light mainLight = GetMainLight(shadowCoord);
			#else
				Light mainLight = GetMainLight();
			#endif
				float shadow = mainLight.shadowAttenuation;

			// extra point lights support
			float3 extraLights;
			int pixelLightCount = GetAdditionalLightsCount();
			for (int j = 0; j < pixelLightCount; ++j) {
				Light light = GetAdditionalLight(j, i.worldPos);
				float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
				extraLights += attenuatedLightColor;
			}
			float4 baseColor = lerp(_BottomColor, _TopColor, saturate(i.uv.y)) * float4(i.diffuseColor, 1);

			// multiply with lighting color
			float4 litColor = (baseColor * float4(mainLight.color,1));

			litColor += float4(extraLights,1);
			// multiply with vertex color, and shadows
			float4 final = litColor * shadow;
			// add in basecolor when lights turned down
			final += saturate((1 - shadow) * baseColor * 0.2);
			// fog
			float fogFactor = i.fogFactor;

			// Mix the pixel color with fogColor. 
			final.rgb = MixFog(final.rgb, fogFactor);
			// add in ambient color
			final += (unity_AmbientSky * _AmbientStrength);
		   return final;
	   }
	   ENDHLSL
   }
		
	// shadow casting pass with empty fragment
	//Pass{
	//	Name "ShadowCaster"
	//	Tags{ "LightMode" = "ShadowCaster" }
//
	//	ZWrite On
	//	ZTest LEqual
//
	//	HLSLPROGRAM
//
	//	#define SHADERPASS_SHADOWCASTER
//
	//	#pragma shader_feature_local _ DISTANCE_DETAIL
//
	//	half4 frag(g2f input) : SV_TARGET{
	//		return 1;
	//	 }
//
	//	ENDHLSL
	//	}
	}
}
