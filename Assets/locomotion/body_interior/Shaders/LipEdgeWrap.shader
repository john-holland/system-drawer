Shader "Locomotion/LipEdgeWrap"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (0.85, 0.45, 0.5, 1)
        _WrapGain ("Wrap gain", Range(0, 0.2)) = 0.05
        _EdgeMaskPower ("Edge mask power", Range(0.5, 8)) = 2
        _CapsuleCount ("Capsule count", Float) = 0
        _Capsule0 ("Capsule 0 xyz+r", Vector) = (0,0,0,0)
        _Capsule1 ("Capsule 1 xyz+r", Vector) = (0,0,0,0)
        _Capsule2 ("Capsule 2 xyz+r", Vector) = (0,0,0,0)
        _Capsule3 ("Capsule 3 xyz+r", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _WrapGain;
            float _EdgeMaskPower;
            float _CapsuleCount;
            float4 _Capsule0, _Capsule1, _Capsule2, _Capsule3;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float CapsuleAttract(float3 worldPos, float4 cap)
            {
                if (cap.w <= 1e-5) return 0;
                float3 d = worldPos - cap.xyz;
                float dist = length(d);
                float influence = saturate(1.0 - dist / max(cap.w * 4.0, 1e-3));
                return influence;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldN = UnityObjectToWorldNormal(v.normal);
                // Edge mask: vertices near lip rim (high |normal.y| soft) — approx via UV.v
                float edge = pow(saturate(1.0 - abs(v.uv.y - 0.5) * 2.0), _EdgeMaskPower);

                float3 pull = 0;
                if (_CapsuleCount > 0) pull += normalize(worldPos - _Capsule0.xyz + 1e-5) * CapsuleAttract(worldPos, _Capsule0) * -1;
                if (_CapsuleCount > 1) pull += normalize(worldPos - _Capsule1.xyz + 1e-5) * CapsuleAttract(worldPos, _Capsule1) * -1;
                if (_CapsuleCount > 2) pull += normalize(worldPos - _Capsule2.xyz + 1e-5) * CapsuleAttract(worldPos, _Capsule2) * -1;
                if (_CapsuleCount > 3) pull += normalize(worldPos - _Capsule3.xyz + 1e-5) * CapsuleAttract(worldPos, _Capsule3) * -1;

                float3 offset = (pull + worldN * length(pull) * 0.25) * _WrapGain * edge;
                float3 localOff = mul(unity_WorldToObject, float4(offset, 0)).xyz;
                o.pos = UnityObjectToClipPos(v.vertex + float4(localOff, 0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
