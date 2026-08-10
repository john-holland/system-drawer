Shader "Locomotion/LotGrassMeshMorph"
{
    Properties
    {
        _MainTex ("Texture A", 2D) = "white" {}
        _MainTexB ("Texture B", 2D) = "white" {}
        _Color ("Color", Color) = (0.3, 0.7, 0.25, 1)
        _Blend ("Blend A→B", Range(0,1)) = 0
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
            sampler2D _MainTex;
            sampler2D _MainTexB;
            float4 _MainTex_ST;
            float4 _Color;
            float _Blend;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 a = tex2D(_MainTex, i.uv);
                fixed4 b = tex2D(_MainTexB, i.uv);
                return lerp(a, b, _Blend) * _Color;
            }
            ENDCG
        }
    }
}
