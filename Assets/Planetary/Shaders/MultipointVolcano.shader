Shader "Planetary/MultipointVolcano"
{
    Properties
    {
        _SteamColor ("Steam", Color) = (0.8, 0.85, 0.9, 0.6)
        _PlasmaColor ("Plasma", Color) = (1, 0.4, 0.1, 1)
        _CurlStrength ("Curl", Range(0, 2)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _SteamColor;
            float4 _PlasmaColor;
            float _CurlStrength;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = i.uv.y;
                float curl = sin(i.uv.x * 20 + _Time.y) * _CurlStrength;
                float4 c = lerp(_SteamColor, _PlasmaColor, saturate(t + curl * 0.1));
                c.a *= saturate(1 - abs(i.uv.x - 0.5) * 2);
                return c;
            }
            ENDCG
        }
    }
}
