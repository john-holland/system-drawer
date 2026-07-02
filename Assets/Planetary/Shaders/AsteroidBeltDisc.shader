Shader "Planetary/AsteroidBeltDisc"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.32, 0.3, 0.5)
        _Opacity ("Opacity", Range(0,1)) = 0.85
        _MeanDensity ("Mean Density", Range(0,1)) = 0.35
        _DensityVariance ("Density Variance", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _Opacity;
            float _MeanDensity;
            float _DensityVariance;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float IGradientNoise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float ang = i.uv.x;
                float hash = IGradientNoise(float2(ang * 64, i.uv.y * 8));
                float density = saturate(_MeanDensity + (hash - 0.5) * 2 * _DensityVariance);
                float alpha = _Opacity * (1 - density * 0.85);
                if (IGradientNoise(i.pos.xy) > alpha) discard;
                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
