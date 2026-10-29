Shader "Locomotion/FlowerRetinue"
{
    Properties
    {
        _PollenColor ("Pollen", Color) = (1, 0.85, 0.2, 1)
        _InteriorLight ("Interior light", Color) = (1, 0.72, 0.82, 1)
        _RadialCache ("Radial cache", 2D) = "black" {}
        _WindLut ("Wind bend LUT", 2D) = "gray" {}
        _UseLiveCache ("Use live cache", Range(0, 1)) = 1
        _WindBend ("Wind bend", Range(0, 1)) = 0.25
        _Azimuth ("Azimuth", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _RadialCache;
            sampler2D _WindLut;
            fixed4 _PollenColor;
            fixed4 _InteriorLight;
            float _UseLiveCache;
            float _WindBend;
            float _Azimuth;

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

            v2f vert(appdata v)
            {
                float bend = tex2Dlod(_WindLut, float4(_Azimuth, 0.5, 0, 0)).r;
                v.vertex.xyz += v.normal * bend * _WindBend * _UseLiveCache;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 cache = tex2D(_RadialCache, float2(_Azimuth, 0.5));
                float occ = cache.a;
                fixed3 light = lerp(_InteriorLight.rgb, cache.rgb, _UseLiveCache);
                fixed4 col = _PollenColor;
                col.rgb *= light;
                col.a *= lerp(1.0, 1.0 - occ, _UseLiveCache);
                return col;
            }
            ENDCG
        }
    }
}
