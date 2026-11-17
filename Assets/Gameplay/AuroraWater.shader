Shader "ImagineQuest/Aurora Water"
{
    Properties
    {
        _Color ("Ocean", Color) = (0.025, 0.13, 0.23, 1)
        _ReflectionTint ("Reflection Tint", Vector) = (0.26, 0.27, 0.36, 1)
        _HorizonTint ("Horizon Tint", Vector) = (0.19, 0.18, 0.26, 1)
        _HazeDensity ("Haze Density", Float) = 0.006
    }
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
            float4 _ReflectionTint;
            float4 _HorizonTint;
            float _HazeDensity;
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            float WavePhase(float2 position, float2 direction, float frequency)
            {
                // Deep-water waves travel faster at longer wavelengths.
                return dot(position, direction) * frequency - _Time.y * sqrt(9.81 * frequency);
            }

            float2 WaveSlope(float2 position, float2 direction, float frequency, float steepness, float footprint)
            {
                // Fade waves before they become smaller than a screen pixel.
                float visibility = 1 - smoothstep(0.45, 2.4, footprint * frequency);
                return direction * (cos(WavePhase(position, direction, frequency)) * steepness * visibility);
            }

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, vertex).xyz;
                // The ocean plane is coarse. Keep geometric motion at long wavelengths.
                world.y += sin(WavePhase(world.xz, float2(0.8, 0.6), 0.028)) * 0.12;
                o.world = world;
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 position = i.world.xz;
                float footprint = max(length(ddx(position)), length(ddy(position)));
                float2 slope = WaveSlope(position, float2(0.8, 0.6), 0.47, 0.075, footprint)
                             + WaveSlope(position, float2(-0.6, 0.8), 0.81, 0.05, footprint)
                             + WaveSlope(position, float2(0.28, 0.96), 1.69, 0.042, footprint)
                             + WaveSlope(position, float2(0.96, -0.28), 3.7, 0.029, footprint)
                             + WaveSlope(position, float2(0.6, 0.8), 7.9, 0.021, footprint)
                             + WaveSlope(position, float2(-0.8, 0.6), 15.4, 0.012, footprint);
                float3 normal = normalize(float3(-slope.x, 1, -slope.y));
                float3 toCamera = _WorldSpaceCameraPos - i.world;
                float3 view = normalize(toCamera);

                // Water reflects roughly 2% head-on and approaches a mirror at grazing angles.
                float fresnel = 0.02 + 0.98 * pow(1 - saturate(dot(view, normal)), 5);
                float3 color = lerp(_Color.rgb, _ReflectionTint.rgb, fresnel);

                // Use the same position as the scene's visible moon to anchor the reflection.
                float3 moonDirection = normalize(float3(-18, 22, 37) - i.world);
                float3 halfVector = normalize(moonDirection + view);
                float alignment = saturate(dot(normal, halfVector));
                float broadReflection = pow(alignment, 48) * 0.09;
                float glint = pow(alignment, 420) * 0.25
                           * (1 - smoothstep(0.12, 0.65, footprint));
                color += (broadReflection + glint) * float3(0.54, 0.69, 0.86);

                float haze = 1 - exp(-length(toCamera) * _HazeDensity);
                return float4(lerp(color, _HorizonTint.rgb, haze), 1);
            }
            ENDCG
        }
    }
}
