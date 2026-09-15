Shader "Locomotion/ConveyorScroll"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (0.35, 0.28, 0.18, 1)
        _ScrollV ("Scroll V (rope wind)", Float) = 0
        _Skew ("Planar skew", Range(-0.5, 0.5)) = 0
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
            fixed4 _Color;
            float _ScrollV;
            float _Skew;

            struct appdata
            {
                float4 vertex : POSITION;
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
                float4 skewed = v.vertex;
                skewed.x += v.vertex.z * _Skew;
                o.pos = UnityObjectToClipPos(skewed);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv.y = frac(uv.y + _ScrollV);
                o.uv = uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
