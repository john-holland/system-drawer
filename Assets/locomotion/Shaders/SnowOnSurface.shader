Shader "Locomotion/SnowOnSurface"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _SnowColor ("Snow", Color) = (0.93, 0.95, 0.97, 1)
        _PollenColor ("Pollen", Color) = (0.93, 0.86, 0.45, 1)
        _DepthInches ("Depth inches", Float) = 0
        _PollenMix ("Pollen mix", Range(0, 1)) = 0
        _InchToMeter ("Inches to meters", Float) = 0.0254
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _SnowColor;
            float4 _PollenColor;
            float _DepthInches;
            float _PollenMix;
            float _InchToMeter;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float depth = max(_DepthInches, 0) * _InchToMeter;
                v.vertex.xyz += v.normal * depth;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float mix = saturate(_PollenMix);
                float3 snow = _SnowColor.rgb;
                if (mix > 0)
                    snow = lerp(snow, _PollenColor.rgb, mix);
                return fixed4(tex.rgb * snow, 1);
            }
            ENDCG
        }
    }
}
