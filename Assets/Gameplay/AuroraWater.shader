Shader "ImagineQuest/Aurora Water"
{
    Properties { _Color ("Ocean", Color) = (0.025, 0.13, 0.23, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct v2f { float4 vertex : SV_POSITION; float3 world : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, vertex).xyz;
                world.y += sin(world.x * 0.45 + _Time.y * 0.55) * 0.055
                         + sin(world.z * 0.32 - _Time.y * 0.42) * 0.04;
                o.world = world;
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float rippleX = cos(i.world.x * 1.15 + _Time.y * 0.65) * 0.12;
                float rippleZ = cos(i.world.z * 1.4 - _Time.y * 0.55) * 0.1;
                float3 normal = normalize(float3(rippleX, 1, rippleZ));
                float fresnel = pow(1 - saturate(dot(view, normal)), 4);
                float3 color = lerp(_Color.rgb, float3(0.26, 0.27, 0.36), fresnel);
                float shine = pow(saturate(dot(reflect(-normalize(float3(-0.4, 0.8, 0.6)), normal), view)), 90);
                float wave = sin(i.world.x * 2.3 + _Time.y) * sin(i.world.z * 2.0 - _Time.y * 0.7);
                color += shine * float3(0.45, 0.63, 0.85) + smoothstep(0.86, 1, wave) * 0.025;
                float haze = 1 - exp(-length(_WorldSpaceCameraPos - i.world) * 0.006);
                color = lerp(color, float3(0.19, 0.18, 0.26), haze);
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
