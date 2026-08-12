Shader "Continuuuum/Dimensions/CrossFadeFire"
{
    Properties
    {
        _SteamColor ("Steam A", Color) = (0.7, 0.7, 0.75, 0.4)
        _PlasmaColor ("Plasma A", Color) = (1, 0.4, 0.1, 0.9)
        _SteamColorB ("Steam B", Color) = (0.65, 0.7, 0.8, 0.4)
        _PlasmaColorB ("Plasma B", Color) = (1, 0.25, 0.05, 0.9)
        _DimBlend ("Dim Blend", Range(0,1)) = 0
        _CurlStrength ("Curl", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _SteamColor;
            float4 _PlasmaColor;
            float4 _SteamColorB;
            float4 _PlasmaColorB;
            float _DimBlend;
            float _CurlStrength;
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
                float n = frac(sin(dot(i.uv, float2(12.9898, 78.233))) * 43758.5453);
                float heat = saturate(1 - i.uv.y + n * 0.1 * _CurlStrength);
                fixed4 steam = lerp(_SteamColor, _SteamColorB, saturate(_DimBlend));
                fixed4 plasma = lerp(_PlasmaColor, _PlasmaColorB, saturate(_DimBlend));
                return lerp(steam, plasma, heat);
            }
            ENDCG
        }
    }
}
