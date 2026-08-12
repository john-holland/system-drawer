Shader "Continuuuum/Dimensions/CrossFadeSdfMax"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.8, 0.8, 0.85, 1)
        _ColorB ("Color B", Color) = (0.6, 0.7, 0.9, 1)
        _DimBlend ("Dim Blend", Range(0,1)) = 0
        _RevealAmountNadir ("Reveal", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _ColorA;
            float4 _ColorB;
            float _DimBlend;
            float _RevealAmountNadir;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                if (_RevealAmountNadir < 0.001) discard;
                float ndl = saturate(dot(normalize(i.normal), float3(0.3, 0.8, 0.4)));
                fixed4 c = lerp(_ColorA, _ColorB, saturate(_DimBlend));
                return c * (0.35 + 0.65 * ndl) * _RevealAmountNadir;
            }
            ENDCG
        }
    }
}
