Shader "Locomotion/Painting/InkDryingLayer"
{
    Properties
    {
        _Color ("Albedo", Color) = (0.05, 0.06, 0.12, 1)
        _Dry01 ("Dry", Range(0, 1)) = 0
        _Specular ("Specular", Range(0, 1)) = 0.85
        _SeeThrough ("See Through", Range(0, 1)) = 0
        _SeeThroughAlpha ("See Through Alpha", Range(0, 1)) = 0.12
        _Glossiness ("Smoothness", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Dry01;
            float _Specular;
            float _SeeThrough;
            float _SeeThroughAlpha;
            float _Glossiness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 wpos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 vdir = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = saturate(dot(n, vdir));
                float spec = lerp(_Specular, 0.08, saturate(_Dry01));
                float gloss = lerp(_Glossiness, spec, 0.5);
                float rim = pow(1.0 - ndv, 3.0) * spec * (1.0 - _Dry01);
                float3 rgb = _Color.rgb + rim * 0.15;
                float a = lerp(_Color.a, _SeeThroughAlpha, saturate(_SeeThrough));
                a = lerp(a, _Color.a, saturate(_Dry01) * (1.0 - saturate(_SeeThrough)));
                return fixed4(rgb * (0.75 + gloss * 0.25), a);
            }
            ENDCG
        }
    }
    FallBack Off
}
