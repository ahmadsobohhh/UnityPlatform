Shader "ImagineQuest/ClientPortal"
{
    Properties { _MainTex ("Client video: color left, alpha right", 2D) = "black" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(input.uv.x * .5, input.uv.y)).rgb;
                // The source's alpha is packed beside its color; dark leaves remain opaque.
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(.5 + input.uv.x * .5, input.uv.y)).r;
                #ifndef UNITY_COLORSPACE_GAMMA
                mask = LinearToSRGB(mask);
                #endif
                half alpha = smoothstep(.015, .985, mask);
                clip(alpha - .004);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
