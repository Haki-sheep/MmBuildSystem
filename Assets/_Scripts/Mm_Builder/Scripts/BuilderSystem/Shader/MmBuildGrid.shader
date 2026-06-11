Shader "Mm_Builder/MmBuildGrid"
{
    Properties
    {
        [MainColor] _CellColor("Cell Color", Color) = (0.48, 0.63, 0.48, 1)
        _LineColor("Line Color", Color) = (0.12, 0.12, 0.12, 1)
        _GridSize("Grid Size", Float) = 1
        _LineWidth("Line Width", Float) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "MmBuildGridForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CellColor;
                half4 _LineColor;
                half _GridSize;
                half _LineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float gridSize = max(_GridSize, 1e-4);
                float2 fracXZ = frac(input.positionWS.xz / gridSize);

                float2 edge = min(fracXZ, 1.0 - fracXZ);
                float edgeDist = min(edge.x, edge.y);
                float lineWidthFrac = _LineWidth / gridSize;
                half lineMask = 1.0h - smoothstep(0.0h, (half)lineWidthFrac, (half)edgeDist);

                half3 color = lerp(_CellColor.rgb, _LineColor.rgb, lineMask);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
