Shader "ImagineQuest/Quest Item Outline"
{
    Properties
    {
        [HDR] _OutlineColor ("Edge color", Color) = (1.25, 0.85, 0.22, 1)
        _OutlineWidth ("Width in pixels", Range(0.5, 4)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" }
        Pass
        {
            Cull Front ZWrite Off ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _OutlineColor;
            float _OutlineWidth;
            struct input { float4 vertex : POSITION; float3 normal : NORMAL; };
            float4 vert(input v) : SV_POSITION
            {
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float2 edge = mul((float3x3)UNITY_MATRIX_V, worldNormal).xy;
                edge /= max(length(edge), 0.0001);
                clip.xy += edge * (_OutlineWidth * 2 / _ScreenParams.xy) * clip.w;
                return clip;
            }
            float4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
