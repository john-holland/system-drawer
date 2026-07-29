Shader "Locomotion/HairPlume"
{
    Properties
    {
        _MainTex ("Albedo fallback", 2D) = "white" {}
        _HairRadialTex ("Radial cache (R=h G=pass B=hold A=tip)", 2D) = "black" {}
        _HairDiffuseTex ("Fiber diffuse", 2D) = "white" {}
        _HairSpecTex ("Fiber specular", 2D) = "gray" {}
        _PassthroughATex ("Passthrough shape A", 2D) = "black" {}
        _PassthroughBTex ("Passthrough shape B", 2D) = "black" {}
        _HelmetMaskTex ("Helmet cover mask (R)", 2D) = "black" {}
        _Color ("Color", Color) = (0.35, 0.22, 0.12, 1)
        _ExtrudeGain ("Height extrude gain", Range(0, 1)) = 0.35
        _PlumeTipHold ("Tip hold 0=break 1=hold", Range(0, 1)) = 0.55
        _GravityTipGain ("Gravity tip gain", Range(0, 2)) = 0.35
        _TensionPartGain ("Capsule part gain", Range(0, 2)) = 1
        _ShaderBounceGain ("Shader bounce gain", Range(0, 1)) = 0.35
        _CurlAmount ("Curl amount", Range(0, 1)) = 0
        _CurlFrequency ("Curl frequency", Range(0.5, 8)) = 3
        _CurlTightness ("Curl tightness", Range(0, 1)) = 0.5
        _PassthroughBlendA ("Passthrough A weight", Range(0, 1)) = 1
        _PassthroughBlendB ("Passthrough B weight", Range(0, 1)) = 1
        _HelmetRimUvEdge ("Helmet rim UV edge", Range(0, 1)) = 0.92
        _HelmetActive ("Helmet tuck active", Float) = 0
        _CapsuleCount ("Capsule count", Float) = 0
        _Capsule0 ("Capsule 0", Vector) = (0,0,0,0)
        _Capsule1 ("Capsule 1", Vector) = (0,0,0,0)
        _Capsule2 ("Capsule 2", Vector) = (0,0,0,0)
        _Capsule3 ("Capsule 3", Vector) = (0,0,0,0)
        _Capsule4 ("Capsule 4", Vector) = (0,0,0,0)
        _Capsule5 ("Capsule 5", Vector) = (0,0,0,0)
        _Capsule6 ("Capsule 6", Vector) = (0,0,0,0)
        _Capsule7 ("Capsule 7", Vector) = (0,0,0,0)
        _Capsule8 ("Capsule 8", Vector) = (0,0,0,0)
        _Capsule9 ("Capsule 9", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _HairRadialTex;
            sampler2D _HairDiffuseTex;
            sampler2D _HairSpecTex;
            sampler2D _PassthroughATex;
            sampler2D _PassthroughBTex;
            sampler2D _HelmetMaskTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ExtrudeGain;
            float _PlumeTipHold;
            float _GravityTipGain;
            float _TensionPartGain;
            float _ShaderBounceGain;
            float _CurlAmount;
            float _CurlFrequency;
            float _CurlTightness;
            float _PassthroughBlendA;
            float _PassthroughBlendB;
            float _HelmetRimUvEdge;
            float _HelmetActive;
            float _CapsuleCount;
            float4 _Capsule0, _Capsule1, _Capsule2, _Capsule3, _Capsule4;
            float4 _Capsule5, _Capsule6, _Capsule7, _Capsule8, _Capsule9;

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
                float3 worldPos : TEXCOORD1;
                float3 worldN : TEXCOORD2;
                float height : TEXCOORD3;
                float tip : TEXCOORD4;
            };

            float CapsuleInfluence(float3 worldPos, float4 cap)
            {
                if (cap.w <= 1e-5) return 0;
                float dist = length(worldPos - cap.xyz);
                return saturate(1.0 - dist / max(cap.w * 3.5, 1e-3));
            }

            float CapsuleSoftmax(float3 worldPos)
            {
                float acc = 0;
                if (_CapsuleCount > 0) acc += CapsuleInfluence(worldPos, _Capsule0);
                if (_CapsuleCount > 1) acc += CapsuleInfluence(worldPos, _Capsule1);
                if (_CapsuleCount > 2) acc += CapsuleInfluence(worldPos, _Capsule2);
                if (_CapsuleCount > 3) acc += CapsuleInfluence(worldPos, _Capsule3);
                if (_CapsuleCount > 4) acc += CapsuleInfluence(worldPos, _Capsule4);
                if (_CapsuleCount > 5) acc += CapsuleInfluence(worldPos, _Capsule5);
                if (_CapsuleCount > 6) acc += CapsuleInfluence(worldPos, _Capsule6);
                if (_CapsuleCount > 7) acc += CapsuleInfluence(worldPos, _Capsule7);
                if (_CapsuleCount > 8) acc += CapsuleInfluence(worldPos, _Capsule8);
                if (_CapsuleCount > 9) acc += CapsuleInfluence(worldPos, _Capsule9);
                return saturate(acc);
            }

            void AccCapsule(inout float3 pull, float3 worldPos, float4 cap, float enabled)
            {
                if (enabled < 0.5) return;
                float inf = CapsuleInfluence(worldPos, cap);
                pull += normalize(worldPos - cap.xyz + 1e-5) * inf;
            }

            float3 CapsuleBounceOffset(float3 worldPos, float3 worldN)
            {
                float3 pull = 0;
                AccCapsule(pull, worldPos, _Capsule0, _CapsuleCount > 0);
                AccCapsule(pull, worldPos, _Capsule1, _CapsuleCount > 1);
                AccCapsule(pull, worldPos, _Capsule2, _CapsuleCount > 2);
                AccCapsule(pull, worldPos, _Capsule3, _CapsuleCount > 3);
                AccCapsule(pull, worldPos, _Capsule4, _CapsuleCount > 4);
                AccCapsule(pull, worldPos, _Capsule5, _CapsuleCount > 5);
                AccCapsule(pull, worldPos, _Capsule6, _CapsuleCount > 6);
                AccCapsule(pull, worldPos, _Capsule7, _CapsuleCount > 7);
                AccCapsule(pull, worldPos, _Capsule8, _CapsuleCount > 8);
                AccCapsule(pull, worldPos, _Capsule9, _CapsuleCount > 9);
                float3 bounce = pull + worldN * length(pull) * 0.2;
                return bounce * _ShaderBounceGain;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float2 uv = v.uv;
                float4 radial = tex2Dlod(_HairRadialTex, float4(uv, 0, 0));
                float passA = tex2Dlod(_PassthroughATex, float4(uv, 0, 0)).r * _PassthroughBlendA;
                float passB = tex2Dlod(_PassthroughBTex, float4(uv, 0, 0)).r * _PassthroughBlendB;
                float helmetCover = tex2Dlod(_HelmetMaskTex, float4(uv, 0, 0)).r;

                float tipV = uv.y;
                float breakSpread = radial.r * (1.0 - tipV * 0.85) * (1.0 + radial.a * _GravityTipGain);
                float held = max(radial.r, radial.b) * (1.0 - tipV * 0.15 * (1.0 - _PlumeTipHold));
                float h = lerp(breakSpread, held, _PlumeTipHold);
                h = max(h, max(passA, passB));

                // Helmet: covered sectors zero height; rim may pop via max with interior cache in R
                if (_HelmetActive > 0.5 && helmetCover > 0.5 && uv.y < _HelmetRimUvEdge)
                    h = 0;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldN = UnityObjectToWorldNormal(v.normal);
                float part = CapsuleSoftmax(worldPos) * _TensionPartGain * radial.g;
                h *= saturate(1.0 - part);

                float3 bounce = CapsuleBounceOffset(worldPos, worldN);

                // Helix curl in tangent space; scales with extruded height (zero at scalp)
                float3 up = float3(0, 1, 0);
                float3 tangent = normalize(cross(worldN, up) + 1e-4);
                float3 bitangent = normalize(cross(worldN, tangent));
                float curlAmt = saturate(_CurlAmount);
                float curlFreq = clamp(_CurlFrequency, 0.5, 8.0);
                float curlTight = saturate(_CurlTightness);
                float phase = tipV * curlFreq * 6.2831853 + uv.x * 6.2831853;
                float curlRadius = lerp(0.035, 0.012, curlTight) * curlAmt * h * _ExtrudeGain;
                float3 curlOff = (tangent * sin(phase) + bitangent * cos(phase)) * curlRadius;

                float3 worldOff = worldN * (h * _ExtrudeGain) + bounce + curlOff;
                float3 localOff = mul(unity_WorldToObject, float4(worldOff, 0)).xyz;

                o.pos = UnityObjectToClipPos(v.vertex + float4(localOff, 0));
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.worldPos = worldPos + worldOff;
                o.worldN = worldN;
                o.height = h;
                o.tip = tipV;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_HelmetActive > 0.5)
                {
                    float cover = tex2D(_HelmetMaskTex, i.uv).r;
                    if (cover > 0.5 && i.uv.y < _HelmetRimUvEdge)
                        clip(-1);
                }

                fixed4 diff = tex2D(_HairDiffuseTex, i.uv) * _Color;
                fixed4 fallback = tex2D(_MainTex, i.uv) * _Color;
                fixed3 albedo = lerp(fallback.rgb, diff.rgb, diff.a > 0.01 ? 1 : 0.85);

                // Fiberglass-like anisotropic hint from baked spec (packed lobe intensity)
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 T = normalize(cross(i.worldN, float3(0, 1, 0) + 1e-4));
                float specMap = tex2D(_HairSpecTex, i.uv).r;
                float fiber = pow(saturate(1.0 - abs(dot(T, V))), 4.0) * specMap;
                float tipDark = lerp(1.0, 0.75, i.tip * (1.0 - _PlumeTipHold));

                albedo = albedo * tipDark + fiber * 0.35;
                return fixed4(albedo, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
