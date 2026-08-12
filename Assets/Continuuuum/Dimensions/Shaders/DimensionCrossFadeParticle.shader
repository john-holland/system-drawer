Shader "Continuuuum/Dimensions/CrossFadeParticle"
{
    Properties
    {
        _MainTexA ("Texture A", 2D) = "white" {}
        _MainTexB ("Texture B", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _DimBlend ("Dim Blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
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
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTexA);
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 a = tex2D(_MainTexA, i.uv) * _ColorA;
                fixed4 b = tex2D(_MainTexB, i.uv) * _ColorB;
                fixed4 c = lerp(a, b, saturate(_DimBlend)) * i.color;
                return c;
            }
            ENDCG
        }
    }
}
