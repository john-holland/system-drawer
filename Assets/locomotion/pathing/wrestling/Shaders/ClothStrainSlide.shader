Shader "Locomotion/ClothStrainSlide"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _ClothStretchTex ("Stretch cache (R=strain G=slipU B=slipV A=contact)", 2D) = "black" {}
        _SlideMaskTex ("Slide mask (R)", 2D) = "white" {}
        _ElasticMaskTex ("Elastic mask (R)", 2D) = "white" {}
        _StretchGain ("Stretch extrude gain", Range(0, 0.2)) = 0.04
        _SlideGain ("UV slide gain", Range(0, 1)) = 1
        _Color ("Color", Color) = (0.85, 0.85, 0.9, 1)
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
            sampler2D _ClothStretchTex;
            sampler2D _SlideMaskTex;
            sampler2D _ElasticMaskTex;
            float4 _MainTex_ST;
            float _StretchGain;
            float _SlideGain;
            fixed4 _Color;

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

            v2f vert(appdata v)
            {
                v2f o;
                float4 stretch = tex2Dlod(_ClothStretchTex, float4(v.uv.x, v.uv.y, 0, 0));
                float slideMask = tex2Dlod(_SlideMaskTex, float4(v.uv.x, v.uv.y, 0, 0)).r;
                float elasticMask = tex2Dlod(_ElasticMaskTex, float4(v.uv.x, v.uv.y, 0, 0)).r;

                // Decode slip from 0..1 centered at 0.5
                float2 slip = (stretch.gb - 0.5) * 2.0 * _SlideGain * slideMask;
                float2 uvAlbedo = v.uv + slip;

                float extrude = stretch.r * _StretchGain * elasticMask * stretch.a;
                float3 extruded = v.vertex.xyz + normalize(v.normal) * extrude;

                o.pos = UnityObjectToClipPos(float4(extruded, 1));
                o.uv = v.uv;
                o.uvAlbedo = TRANSFORM_TEX(uvAlbedo, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uvAlbedo) * _Color;
                float4 stretch = tex2D(_ClothStretchTex, i.uv);
                albedo.rgb *= lerp(1.0, 1.15, stretch.r * stretch.a);
                return albedo;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
