Shader "ImagineQuest/EmeraldPortal"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct VertexInput { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct FragmentInput { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            FragmentInput vert(VertexInput input)
            {
                FragmentInput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            float4 frag(FragmentInput input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                clip(1.0 - radius);
                float angle = atan2(p.y, p.x);
                float time = _Time.y;
                float spiral = angle * 5.0 + radius * 19.0 - time * 2.2;
                float wisps = pow(saturate(sin(spiral + sin(radius * 28.0 - time) * 0.55)), 9.0);
                float fine = pow(saturate(sin(angle * 11.0 + radius * 44.0 - time * 3.1)), 18.0);
                float rim = pow(saturate(radius), 9.0);
                float center = exp(-radius * 7.0);
                float3 color = lerp(float3(0.005, 0.13, 0.065), float3(0.08, 0.62, 0.16), radius);
                color += float3(0.65, 1.0, 0.12) * (wisps * 0.85 + fine * 0.22 + rim * 0.9);
                color += float3(0.75, 1.0, 0.35) * center;
                return float4(color, 1.0 - smoothstep(0.96, 1.0, radius));
            }
            ENDHLSL
        }
    }
}
