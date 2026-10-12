Shader "ImagineQuest/Aurora Sky"
{
    Properties
    {
        _Zenith ("Upper sky", Color) = (0.018, 0.042, 0.13, 1)
        _Horizon ("Horizon", Color) = (0.32, 0.20, 0.25, 1)
        _Glow ("Sunset glow", Color) = (0.95, 0.49, 0.22, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Zenith, _Horizon, _Glow;
            struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.direction = vertex.xyz;
                return o;
            }
            float hash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float upper = saturate(d.y);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(upper, 0.45));
                float glow = pow(saturate(dot(d, normalize(float3(-0.6, 0.03, 0.8)))), 12);
                sky += _Glow.rgb * glow * exp(-abs(d.y) * 12) * 0.5;
                float3 starGrid = d * 340;
                float star = step(0.998, hash(floor(starGrid))) * pow(saturate(1 - length(frac(starGrid) - 0.5) * 2), 8);
                sky += star * smoothstep(0.08, 0.4, upper) * float3(0.7, 0.85, 1.0);
                float clouds = sin(d.x * 21 + _Time.y * 0.025) * sin(d.z * 17 - _Time.y * 0.02)
                             + sin(d.x * 9 + d.z * 13 + _Time.y * 0.03);
                sky += smoothstep(0.8, 1.6, clouds) * smoothstep(0.03, 0.2, upper) * (1 - upper) * 0.06;
                return float4(sky, 1);
            }
            ENDCG
        }
    }
}
