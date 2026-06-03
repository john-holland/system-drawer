Shader "Planetary/SdfLod"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 0.4, 0.35, 1)
        _DetailCoeff ("Detail Coeff", Range(0,1)) = 0.5
        _HorizonSdfWeight ("Horizon SDF Weight", Range(0,1)) = 1
        _RevealAmountNadir ("Reveal Nadir", Range(0,1)) = 0
        _HorizonStart ("Horizon Start", Range(0,1)) = 0.35
        _HorizonEnd ("Horizon End", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _DetailCoeff;
            float _HorizonSdfWeight;
            float _RevealAmountNadir;
            float _HorizonStart;
            float _HorizonEnd;
            float3 _PlanetCenter;
            float3 _CameraWorld;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; float3 worldNormal : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            float InterleavedGradientNoise(float2 p)
            {
                return frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 radial = normalize(i.worldPos - _PlanetCenter);
                float3 viewDir = normalize(_CameraWorld - i.worldPos);
                float nadirAlign = saturate(dot(viewDir, -radial));
                float horizonAlign = 1.0 - nadirAlign;
                float pixelHorizon = saturate((horizonAlign - _HorizonStart) / max(0.001, _HorizonEnd - _HorizonStart));
                float sdfIn = pixelHorizon * _HorizonSdfWeight;
                float groundReveal = nadirAlign * _RevealAmountNadir;
                float2 sp = i.pos.xy;
                float n = InterleavedGradientNoise(sp);
                if (n > sdfIn) discard;
                if (n < groundReveal) discard;
                float limb = 0.5 + 0.5 * dot(i.worldNormal, radial);
                return fixed4(_Color.rgb * limb * (0.7 + 0.3 * _DetailCoeff), 1);
            }
            ENDCG
        }
    }
}
