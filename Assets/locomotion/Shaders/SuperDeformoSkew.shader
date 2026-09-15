Shader "Locomotion/SuperDeformoSkew"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (0.55, 0.38, 0.18, 1)
        _Skew ("Planar skew", Range(-0.5, 0.5)) = 0.08
        _UvLoopMin ("UV loop min", Range(0, 1)) = 0
        _UvLoopMax ("UV loop max", Range(0, 1)) = 1
        _EndCapMin ("End cap min", Range(0, 1)) = 0
        _EndCapMax ("End cap max", Range(0, 1)) = 0.08
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
            float _Skew;
            float _UvLoopMin;
            float _UvLoopMax;
            float _EndCapMin;
            float _EndCapMax;

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
                skewed.x += v.vertex.y * _Skew;
                o.pos = UnityObjectToClipPos(skewed);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                float span = max(1e-4, _UvLoopMax - _UvLoopMin);
                uv.x = _UvLoopMin + frac((uv.x - _UvLoopMin) / span) * span;
                o.uv = uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                if (i.uv.y < _EndCapMax && i.uv.y > _EndCapMin)
                    c.rgb *= 0.75;
                return c;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
