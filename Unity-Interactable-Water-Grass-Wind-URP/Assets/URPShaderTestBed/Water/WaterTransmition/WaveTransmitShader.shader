Shader "Custom/WaveTransmitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

			TEXTURE2D_X(_MainTex);
			float4 _MainTex_TexelSize;
            
			TEXTURE2D_X (_PrevWaveMarkTex);
			float4 _WaveTransmitParams;
			float _WaveAtten;

			static const float2 WAVE_DIR[4] = { float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1) };

            half4 frag (Varyings i) : SV_Target
            {
				/*波传递公式
				 (4 - 8 * c^2 * t^2 / d^2) / (u * t + 2) + (u * t - 2) / (u * t + 2) * z(x,y,z, t - dt) + (2 * c^2 * t^2 / d ^2) / (u * t + 2)
				 * (z(x + dx,y,t) + z(x - dx, y, t) + z(x,y + dy, t) + z(x, y - dy, t);*/

            	//
				float dx = _WaveTransmitParams.w;

				float avgWaveHeight = 0;
				for (int s = 0; s < 4; s++)
				{
					avgWaveHeight += DecodeHeight(SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + WAVE_DIR[s] * dx));
				}

				//(2 * c^2 * t^2 / d ^2) / (u * t + 2)*(z(x + dx, y, t) + z(x - dx, y, t) + z(x, y + dy, t) + z(x, y - dy, t);
				float agWave = _WaveTransmitParams.z * avgWaveHeight;
				
				// (4 - 8 * c^2 * t^2 / d^2) / (u * t + 2)
				float curWave = _WaveTransmitParams.x *  DecodeHeight(SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv));
				// (u * t - 2) / (u * t + 2) * z(x,y,z, t - dt) 上一次波浪值 t - dt
				float prevWave = _WaveTransmitParams.y * DecodeHeight(SAMPLE_TEXTURE2D_X(_PrevWaveMarkTex, sampler_LinearClamp, i.uv));

				//波衰减
				float waveValue = (curWave + prevWave + agWave) * _WaveAtten;

                return EncodeHeight(waveValue);
            }
            ENDHLSL
        }
    }
}
