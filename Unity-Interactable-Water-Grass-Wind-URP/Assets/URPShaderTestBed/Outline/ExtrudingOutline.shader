Shader "Custom/ExtrudingOutline"
{
    Properties
    {
        _OutlineThickness("Outline Thickness", float) = 0.1
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
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
            Name "Outline"
            CULL FRONT
            
            HLSLPROGRAM

            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            // register functions
            #pragma vertex vert
            #pragma fragment frag

            #include "ExtrudingOutline.hlsl"
            ENDHLSL
        }
    }
}
