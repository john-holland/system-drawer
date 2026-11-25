Shader "Locomotion/SubterraneanIce"
{
    Properties
    {
        _Color ("Lens", Color) = (0.55, 0.72, 0.78, 1)
        _Angular ("Angular deflection", Float) = 1
        _SphFlow ("SPH flow factor", Range(0, 1)) = 0
        _Sd ("Signed distance", Float) = 0.2
        _C0 ("c0", Float) = 0.02
        _C1 ("c1", Float) = 0
        _C2 ("c2", Float) = 0.04
        _C3 ("c3", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Angular;
            float _SphFlow;
            float _Sd;
            float _C0;
            float _C1;
            float _C2;
            float _C3;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float az = atan2(i.normal.z, i.normal.x);
                float angular = _Angular * (0.5 + 0.5 * cos(az));
                float sd = _Sd;
                float kappa = _C0 + _C1 * sd + _C2 * sd * sd + _C3 * sd * sd * sd;
                float rendered = angular + _SphFlow * kappa;
                return fixed4(_Color.rgb * saturate(rendered), 0.65);
            }
            ENDCG
        }
    }
}
