Shader "Locomotion/GrassWindBend"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _WindCache ("Radial cache", 2D) = "black" {}
        _MacroWind ("Macro chord (R gust G flutter BA direction)", 2D) = "gray" {}
        _BendAxis ("Bend axis", Vector) = (1, 0, 0, 0)
        _WindDirection ("Wind direction", Vector) = (1, 0, 0, 0)
        _OmitBend ("Omit bend", Range(0, 1)) = 0
        _PlantCapCount ("Capsule count", Float) = 0
        _CutAlong ("Cut along blade", Range(0, 1)) = 1
        _LayerHeight ("Layer height", Range(0, 1)) = 1
        _MacroScale ("Macro scale", Vector) = (0.05, 0.05, 0, 0)
        _MicroScale ("Micro scale", Vector) = (0.2, 0.2, 0, 0)
        _MacroVelocity ("Macro velocity", Float) = 0.35
        _MicroVelocity ("Micro velocity", Float) = 0.8
        _BlockMotion ("Block procedural motion", Range(0, 1)) = 0
        _BlockAlong0 ("Block start", Range(0, 1)) = 0
        _BlockAlong1 ("Block end", Range(0, 1)) = 1
        _Color ("Color", Color) = (0.32, 0.55, 0.24, 1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _WindCache;
            sampler2D _MacroWind;
            float4 _MainTex_ST;
            float4 _BendAxis;
            float4 _WindDirection;
            fixed4 _Color;
            float _OmitBend;
            float _PlantCapCount;
            float _CutAlong;
            float _LayerHeight;
            float4 _MacroScale;
            float4 _MicroScale;
            float _MacroVelocity;
            float _MicroVelocity;
            float _BlockMotion;
            float _BlockAlong0;
            float _BlockAlong1;
            float4 _PlantCapA[8];
            float4 _PlantCapB[8];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvAlbedo : TEXCOORD1;
                float strength : TEXCOORD2;
            };

            float2 Hash2(float2 p)
            {
                float x = sin(dot(p, float2(127.1, 311.7))) * 43758.5453;
                float y = sin(dot(p, float2(269.5, 183.3))) * 43758.5453;
                return -1.0 + 2.0 * frac(float2(x, y));
            }

            float AnalyticalNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = dot(Hash2(i), f);
                float b = dot(Hash2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(Hash2(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(Hash2(i + float2(1, 1)), f - float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float3 PartCapsules(float3 p)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (i >= (int)_PlantCapCount)
                        continue;
                    float3 a = _PlantCapA[i].xyz;
                    float3 b = _PlantCapB[i].xyz;
                    float reach = max(0.0, _PlantCapA[i].w + _PlantCapB[i].w);
                    float3 ab = b - a;
                    float denom = max(dot(ab, ab), 1e-6);
                    float t = saturate(dot(p - a, ab) / denom);
                    float3 closest = a + ab * t;
                    float3 away = p - closest;
                    float dist = length(away);
                    if (dist < reach)
                    {
                        float3 dir = dist > 1e-4 ? away / dist : float3(0, 1, 0);
                        p += dir * (reach - dist);
                    }
                }
                return p;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float along = v.uv.y;
                float blocked = (_BlockMotion > 0.5 && along >= min(_BlockAlong0, _BlockAlong1) && along <= max(_BlockAlong0, _BlockAlong1)) ? 1.0 : 0.0;
                float omit = _OmitBend > 0.5 || blocked ? 1.0 : 0.0;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 parted = omit > 0.5 ? world : PartCapsules(world);
                float3 capsuleOffset = mul((float3x3)unity_WorldToObject, parted - world);

                float2 p = world.xz;
                float2 dir = _WindDirection.xy;
                float2 uvMacro = p * _MacroScale.xy + dir * _Time.y * _MacroVelocity;
                float2 uvMicro = p * _MicroScale.xy + dir * _Time.y * _MicroVelocity;
                float4 chord = tex2Dlod(_MacroWind, float4(uvMacro, 0, 0));
                float gust = chord.r;
                float flutter = chord.g;
                if (gust + flutter < 1e-4)
                {
                    gust = AnalyticalNoise(uvMacro) * 0.5 + 0.5;
                    flutter = AnalyticalNoise(uvMicro) * 0.5 + 0.5;
                }
                float strength = gust + flutter * 0.25;
                float sharpened = pow(max(strength, 0.0), 2.0);
                float height = _LayerHeight * along;
                float2 warp = dir * strength * sharpened * (height * height);

                float4 bin = tex2Dlod(_WindCache, float4(v.uv.x, along, 0, 0));
                float delta = omit > 0.5 ? 0.0 : min(bin.r, bin.a);
                float3 axis = normalize(_BendAxis.xyz);
                float3 extruded = v.vertex.xyz + capsuleOffset + axis * delta * bin.g;
                extruded.xz += omit > 0.5 ? float2(0.0, 0.0) : warp;

                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = v.uv;
                o.uvAlbedo = TRANSFORM_TEX(v.uv, _MainTex);
                o.strength = strength;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(_CutAlong - i.uv.y);
                fixed4 albedo = tex2D(_MainTex, i.uvAlbedo) * _Color;
                albedo.rgb *= lerp(0.75, 1.0, i.uv.y);
                albedo.rgb *= lerp(1.0, 1.08, saturate(i.strength));
                return albedo;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
