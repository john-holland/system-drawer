Shader "Continuuuum/DrinkWaterStream"
{
    Properties
    {
        _StreamForce ("Stream Force", Vector) = (0, -1, 0, 0)
        _Color ("Color", Color) = (0.4, 0.7, 1, 0.85)
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
            float4 _StreamForce;
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float mag = length(_StreamForce.xyz);
                return fixed4(_Color.rgb, _Color.a * saturate(mag * 4));
            }
            ENDCG
        }
    }
    FallBack Off
}
