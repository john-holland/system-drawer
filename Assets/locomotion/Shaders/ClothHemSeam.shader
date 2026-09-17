Shader "Locomotion/ClothHemSeam"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _StitchTex ("Stitch repeat (R=mask G=disp B=spec)", 2D) = "white" {}
        _Color ("Cloth color", Color) = (0.82, 0.78, 0.72, 1)
        _ThreadColor ("Thread color", Color) = (0.15, 0.12, 0.18, 1)
        _Gauge01 ("Cloth gauge", Range(0, 1)) = 0.4
        _Pitch ("Stitch pitch UV", Range(2, 64)) = 16
        _Displace ("Displacement meters", Range(0, 0.02)) = 0.004
        _SpecPower ("Specular power", Range(4, 64)) = 24
        _SpecBoost ("Specular boost", Range(0, 2)) = 0.8
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
            sampler2D _StitchTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ThreadColor;
            float _Gauge01;
            float _Pitch;
            float _Displace;
            float _SpecPower;
            float _SpecBoost;

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
                float3 worldN : TEXCOORD1;
                float3 worldV : TEXCOORD2;
                float stitch : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float2 stitchUv = v.uv * _Pitch;
                float4 stitch = tex2Dlod(_StitchTex, float4(frac(stitchUv), 0, 0));
                float mask = saturate(stitch.r);
                float disp = stitch.g * _Gauge01 * _Displace;
                float3 extruded = v.vertex.xyz + normalize(v.normal) * disp * mask;
                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldN = UnityObjectToWorldNormal(v.normal);
                o.worldV = WorldSpaceViewDir(v.vertex);
                o.stitch = mask;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 stitchUv = i.uv * _Pitch;
                float4 stitch = tex2D(_StitchTex, frac(stitchUv));
                float mask = saturate(max(i.stitch, stitch.r));
                fixed4 cloth = tex2D(_MainTex, i.uv) * _Color;
                fixed3 albedo = lerp(cloth.rgb, _ThreadColor.rgb, mask * 0.85);
                float3 n = normalize(i.worldN);
                float3 v = normalize(i.worldV);
                float spec = pow(saturate(dot(n, v)), _SpecPower) * stitch.b * _SpecBoost * (0.35 + _Gauge01);
                albedo += spec * lerp(0.15, 1.0, mask);
                return fixed4(albedo, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
