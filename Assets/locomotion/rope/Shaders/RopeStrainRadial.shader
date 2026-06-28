Shader "Locomotion/RopeStrainRadial"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _RopeStrainTex ("Strain radial cache (R=strain G=twist B=tension A=wound)", 2D) = "black" {}
        _StrainExtrude ("Strain extrude", Range(0, 0.2)) = 0.05
        _TwistDegrees ("Twist scale", Range(0, 360)) = 45
        _Color ("Color", Color) = (0.45, 0.32, 0.18, 1)
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
            sampler2D _RopeStrainTex;
            float4 _MainTex_ST;
            float _StrainExtrude;
            float _TwistDegrees;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 strainSample = tex2Dlod(_RopeStrainTex, float4(v.uv.x, v.uv.y, 0, 0));
                float strain = strainSample.r;
                float twistNorm = strainSample.g;

                float twistRad = twistNorm * _TwistDegrees * 0.01745329251;
                float s = sin(twistRad);
                float c = cos(twistRad);
                float3 n = v.normal;
                float3 t = v.tangent.xyz;
                float3 b = cross(n, t);
                float3 twistedNormal = normalize(n * c + b * s);

                float3 extruded = v.vertex.xyz + twistedNormal * strain * _StrainExtrude;
                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(twistedNormal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                float4 strainSample = tex2D(_RopeStrainTex, float2(i.uv.x, i.uv.y));
                albedo.rgb *= lerp(1.0, 1.25, strainSample.r);
                albedo.rgb = lerp(albedo.rgb, fixed3(0.2, 0.2, 0.2), strainSample.a * 0.35);
                return albedo;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
