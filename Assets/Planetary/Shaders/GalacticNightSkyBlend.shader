Shader "Planetary/GalacticNightSkyBlend"
{
    Properties
    {
        _SkyTexA ("Sky A", Cube) = "" {}
        _SkyTexB ("Sky B", Cube) = "" {}
        _BlendWeight ("Blend Weight", Range(0,1)) = 1
        _ObserverWorld ("Observer World", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            samplerCUBE _SkyTexA;
            samplerCUBE _SkyTexB;
            float _BlendWeight;
            float3 _ObserverWorld;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 a = texCUBE(_SkyTexA, i.dir);
                fixed4 b = texCUBE(_SkyTexB, i.dir);
                return lerp(b, a, _BlendWeight);
            }
            ENDCG
        }
    }
}
