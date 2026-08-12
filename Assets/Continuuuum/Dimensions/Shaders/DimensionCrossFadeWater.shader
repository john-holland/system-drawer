Shader "Continuuuum/Dimensions/CrossFadeWater"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.2, 0.45, 0.8, 0.7)
        _ColorB ("Color B", Color) = (0.15, 0.55, 0.75, 0.7)
        _DimBlend ("Dim Blend", Range(0,1)) = 0
        _StreamForce ("Stream Force", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _ColorA;
            float4 _ColorB;
            float _DimBlend;
            float _StreamForce;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float wave = 0.05 * sin(i.uv.x * 20 + _Time.y * (1 + _StreamForce));
                fixed4 c = lerp(_ColorA, _ColorB, saturate(_DimBlend));
                c.a *= saturate(0.85 + wave);
                return c;
            }
            ENDCG
        }
    }
}
