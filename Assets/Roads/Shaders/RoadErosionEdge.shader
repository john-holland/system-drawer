Shader "Roads/RoadErosionEdge"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "gray" {}
        _ErodeAmount ("Erode Amount", Range(0,1)) = 0.5
        _FlowDir ("Flow Direction", Vector) = (1,0,0,0)
        _EdgeSharpness ("Edge Sharpness", Range(0.1,10)) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        float _ErodeAmount;
        float4 _FlowDir;
        float _EdgeSharpness;

        struct Input { float2 uv_MainTex; float erode; };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 flow = normalize(_FlowDir.xyz);
            float edge = saturate(abs(dot(normalize(v.normal), flow)));
            float noise = frac(sin(dot(v.vertex.xz, float2(12.9898, 78.233))) * 43758.5453);
            v.vertex.xyz -= v.normal * _ErodeAmount * (1.0 - edge) * noise * 0.15;
            o.uv_MainTex = v.texcoord.xy;
            o.erode = (1.0 - edge) * _ErodeAmount;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            c.rgb *= 1.0 - IN.erode * 0.35;
            o.Albedo = c.rgb;
            o.Smoothness = 0.2;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
