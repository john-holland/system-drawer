Shader "Continuuuum/Dimensions/CrossFadeMesh"
{
    Properties
    {
        _MainTexA ("Texture A", 2D) = "white" {}
        _MainTexB ("Texture B", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _DimBlend ("Dim Blend", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
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
            sampler2D _MainTexA;
            sampler2D _MainTexB;
            float4 _MainTexA_ST;
            float4 _ColorA;
            float4 _ColorB;
            float _DimBlend;
            float _Dissolve;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTexA);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                if (_Dissolve > 0.999) discard;
                fixed4 a = tex2D(_MainTexA, i.uv) * _ColorA;
                fixed4 b = tex2D(_MainTexB, i.uv) * _ColorB;
                return lerp(a, b, saturate(_DimBlend));
            }
            ENDCG
        }
    }
}
