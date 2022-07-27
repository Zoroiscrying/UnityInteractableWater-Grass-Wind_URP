Shader "Unlit/WaterRipples"
{
    Properties
    {
        [Normal]_MainTex ("Texture", 2D) = "bump" {}
        _MaskRadiusOuter("Mask radius outer", Range(0,.5)) = 0
        _MaskRadiusInner("Mask radius inner", Range(0,.5)) = 0
        _MaskSoftnessOuter("Mask softness outer", Range(0,1)) = 0
        _MaskSoftnessInner("Mask softness inner", Range(0,1)) = 0
    }
    SubShader
    {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "PreviewType"="Plane"
            }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);    float4 _MainTex_ST;
            TEXTURE2D(_Mask);    SAMPLER(sampler_Mask);   
            float _MaskSoftnessOuter;
            float _MaskRadiusOuter;
            float _MaskRadiusInner;
            float _MaskSoftnessInner;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float circleSDF = distance(i.uv, float2(0.5,0.5));
                float outerMask = 1.0 - smoothstep(_MaskRadiusOuter, saturate(_MaskRadiusOuter + _MaskSoftnessOuter), circleSDF);
                float innerMask = smoothstep(_MaskRadiusInner, saturate(_MaskRadiusInner + _MaskSoftnessInner), circleSDF);
                float mask = outerMask * innerMask;
                half4 col = half4(UnpackNormal(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex ,i.uv)), mask) * i.color;
                return col;
            }
            ENDHLSL
        }
    }
}
