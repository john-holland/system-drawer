Shader "Planetary/AsteroidTeleportFade"
{
    Properties
    {
        _Color ("Color", Color) = (0.5, 0.8, 1, 1)
        _Fade ("Fade", Range(0,1)) = 1
        _Rim ("Rim", Range(0,2)) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _Fade;
            float _Rim;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(dot(normalize(i.n), float3(0,0,1))), 2) * _Rim;
                float alpha = _Fade * (0.3 + rim);
                return fixed4(_Color.rgb + rim, alpha);
            }
            ENDCG
        }
    }
}
