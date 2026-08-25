Shader "Roads/SplineLengthBend"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (0.75, 0.72, 0.65, 1)
        _SplineSamples ("Spline Samples", 2D) = "black" {}
        _SampleCount ("Sample Count", Float) = 8
        _MeshLength ("Mesh Length", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _SplineSamples;
        float4 _Color;
        float _SampleCount;
        float _MeshLength;

        struct Input { float2 uv_MainTex; };

        void vert(inout appdata_full v)
        {
            float meshLen = max(0.01, _MeshLength);
            float t = saturate(v.vertex.z / meshLen);
            float u = (_SampleCount > 1) ? t : 0;
            float4 pos = tex2Dlod(_SplineSamples, float4(u, 0.125, 0, 0));
            float4 tan = tex2Dlod(_SplineSamples, float4(u, 0.375, 0, 0));
            float4 bin = tex2Dlod(_SplineSamples, float4(u, 0.625, 0, 0));
            float4 nrm = tex2Dlod(_SplineSamples, float4(u, 0.875, 0, 0));
            float3 world = pos.xyz + bin.xyz * v.vertex.x + nrm.xyz * v.vertex.y;
            v.vertex.xyz = mul(unity_WorldToObject, float4(world, 1)).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Metallic = 0.1;
            o.Smoothness = 0.3;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
