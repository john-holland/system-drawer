Shader "Locomotion/PlantWindBend"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _WindCache ("Wind cache (R=delta G=advected B=axis A=limit)", 2D) = "black" {}
        _BendAxis ("Bend axis", Vector) = (1, 0, 0, 0)
        _OmitBend ("Omit bend", Range(0, 1)) = 0
        _PlantCapCount ("Capsule count", Float) = 0
        _Color ("Color", Color) = (0.35, 0.55, 0.28, 1)
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
            sampler2D _WindCache;
            float4 _MainTex_ST;
            float4 _BendAxis;
            fixed4 _Color;
            float _OmitBend;
            float _PlantCapCount;
            float4 _PlantCapA[8];
            float4 _PlantCapB[8];

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
                float2 uvAlbedo : TEXCOORD1;
            };

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
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 parted = _OmitBend > 0.5 ? world : PartCapsules(world);
                float3 capsuleOffset = mul((float3x3)unity_WorldToObject, parted - world);

                float4 bin = tex2Dlod(_WindCache, float4(v.uv.x, v.uv.y, 0, 0));
                float delta = _OmitBend > 0.5 ? 0.0 : min(bin.r, bin.a);
                float3 axis = normalize(_BendAxis.xyz);
                float3 extruded = v.vertex.xyz + capsuleOffset + axis * delta * bin.g;

                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = v.uv;
                o.uvAlbedo = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uvAlbedo) * _Color;
                float4 bin = tex2D(_WindCache, i.uv);
                albedo.rgb *= lerp(1.0, 1.12, saturate(bin.r));
                return albedo;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
