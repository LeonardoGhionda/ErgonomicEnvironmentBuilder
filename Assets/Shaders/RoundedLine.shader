Shader "Custom/RoundedLine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Gloss ("Gloss", Range(0,1)) = 0.4
        _Intensity ("Light Intensity", Range(0,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float4 _Color;
            float _Gloss;
            float _Intensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); //from object space to clip space
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // remap uv x value from [0,1]to [-1,1]
                float nx = (i.uv.x * 2.0 - 1.0);

                // fake hemisphere normal with falloff
                // if x=0 max value if abs(x) = 1 min value
                float nDotL = sqrt(1 - saturate(nx * nx));

                float brightness = lerp(0.2, 1.0, nDotL) * _Intensity;

                float3 col = _Color.rgb * brightness;

                return float4(col, _Color.a);
            }
            ENDCG
        }
    }
}
