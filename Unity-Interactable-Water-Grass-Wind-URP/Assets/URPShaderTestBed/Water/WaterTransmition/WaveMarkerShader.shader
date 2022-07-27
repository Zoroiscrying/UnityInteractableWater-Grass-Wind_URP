Shader "Custom/WaveMarkerShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
		ZTest Always ZWrite Off Cull Off
    	
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
			#include "WaveUtils.hlsl"

            float2 _DeltaTargetPositionUV;
            float4 _WaterSimulationParams; //zw position xz length
			float4 _WaveMarkParams;
			TEXTURE2D_X(_MainTex);
			float4 _MainTex_TexelSize;

            half4 frag (Varyings i) : SV_Target
            {
            	// update to world space distance
				float dx = (i.uv.x - _WaveMarkParams.x) * _WaterSimulationParams.z;
				float dy = (i.uv.y - _WaveMarkParams.y) * _WaterSimulationParams.w;

				float disSqr = dx * dx + dy * dy;

            	//if (waveHeight > disSqr)
            	//which means that the hit point is within the water height range.
				int hasCol = step(0, _WaveMarkParams.z - disSqr);

				float waveValue = DecodeHeight(SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - _DeltaTargetPositionUV));

				if (hasCol == 1) {
					//pass the water height value and encode height into the texture.
					waveValue = _WaveMarkParams.w;
				}
				
                return EncodeHeight(waveValue);
            }
            ENDHLSL
        }
	}
}
