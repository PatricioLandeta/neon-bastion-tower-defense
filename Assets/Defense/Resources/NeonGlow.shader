Shader "NeonBastion/Glow"
{
    Properties { _BaseColor ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float shade : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normal = TransformObjectToWorldNormal(input.normalOS, false);
                normal *= rsqrt(max(dot(normal, normal), .00001));
                output.shade = .65 + .35 * abs(dot(normal, normalize(float3(-.4,.7,-1))));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return half4(_BaseColor.rgb * input.shade, 1); }
            ENDHLSL
        }
    }
}
