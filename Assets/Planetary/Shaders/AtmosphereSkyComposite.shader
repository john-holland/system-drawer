Shader "Planetary/AtmosphereSkyComposite"
{
    Properties
    {
        _DayNightBlend ("Day Night Blend", Range(0,1)) = 0.5
        _RayleighBlue ("Rayleigh Blue", Range(0,1)) = 0.35
        _SunDirection ("Sun Direction", Vector) = (0,1,0,0)
        _SunColor ("Sun Color", Color) = (1,0.95,0.85,1)
        _SunIntensity ("Sun Intensity", Float) = 2
        _NightSkyTex ("Night Sky", Cube) = "" {}
    }
    SubShader
    {
        Tags { "Queue"="Background+1" "RenderType"="Background" }
        Pass
        {
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _DayNightBlend;
            float _RayleighBlue;
            float3 _SunDirection;
            float4 _SunColor;
            float _SunIntensity;
            samplerCUBE _NightSkyTex;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 up = float3(0,1,0);
                float rayleigh = pow(saturate(dot(i.dir, up)), 2) * _RayleighBlue;
                fixed3 dayCol = fixed3(0.4, 0.6, 1.0) * rayleigh + 0.1;
                fixed3 nightCol = texCUBE(_NightSkyTex, i.dir).rgb;
                fixed3 col = lerp(nightCol, dayCol, _DayNightBlend);
                float sunDot = saturate(dot(i.dir, normalize(_SunDirection)));
                float disk = pow(sunDot, 512) * _SunIntensity;
                col += _SunColor.rgb * disk * _DayNightBlend;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
