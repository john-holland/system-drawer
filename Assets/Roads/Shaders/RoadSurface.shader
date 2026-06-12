Shader "Roads/RoadSurface"
{
    Properties
    {
        _RoadTex ("Road", 2D) = "gray" {}
        _CenterLineTex ("Center Line", 2D) = "white" {}
        _SideLineTex ("Side Line", 2D) = "white" {}
        _DirtTex ("Dirt", 2D) = "brown" {}
        _UndersideTex ("Underside", 2D) = "gray" {}
        _MacroVariationTex ("Macro Variation", 2D) = "white" {}
        _RoadTile ("Road Tile", Vector) = (1,1,0,0)
        _DirtTile ("Dirt Tile", Vector) = (2,2,0,0)
        _CenterLineWidth ("Center Line Width", Range(0,0.5)) = 0.08
        _SideLineWidth ("Side Line Width", Range(0,0.5)) = 0.06
        _DirtShoulderStart ("Dirt Shoulder Start", Range(0,1)) = 0.35
        _TerrainSpillBlend ("Terrain Spill Blend", Range(0,1)) = 0.25
        _IsUnderside ("Is Underside", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _RoadTex, _CenterLineTex, _SideLineTex, _DirtTex, _UndersideTex, _MacroVariationTex;
        float4 _RoadTile, _DirtTile;
        float _CenterLineWidth, _SideLineWidth, _DirtShoulderStart, _TerrainSpillBlend, _IsUnderside;

        struct Input
        {
            float2 uv_RoadTex;
            float lateral;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_RoadTex = v.texcoord.xy;
            o.lateral = abs(v.texcoord.y - 0.5) * 2.0;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 roadUv = IN.uv_RoadTex * _RoadTile.xy;
            float2 dirtUv = IN.uv_RoadTex * _DirtTile.xy;
            float macro = tex2D(_MacroVariationTex, IN.uv_RoadTex * 0.05).r;

            fixed4 col;
            if (_IsUnderside > 0.5)
            {
                col = tex2D(_UndersideTex, roadUv);
            }
            else
            {
                col = tex2D(_RoadTex, roadUv);
                float centerMask = 1.0 - smoothstep(0.0, _CenterLineWidth, abs(IN.uv_RoadTex.y - 0.5));
                float sideMask = smoothstep(1.0 - _SideLineWidth, 1.0, IN.lateral);
                float dirtMask = smoothstep(_DirtShoulderStart, 1.0, IN.lateral);

                fixed4 center = tex2D(_CenterLineTex, roadUv);
                fixed4 side = tex2D(_SideLineTex, roadUv);
                fixed4 dirt = tex2D(_DirtTex, dirtUv);

                col = lerp(col, center, centerMask);
                col = lerp(col, side, sideMask);
                col = lerp(col, dirt, dirtMask * (1.0 - _TerrainSpillBlend));
            }

            col.rgb *= lerp(0.85, 1.15, macro);
            o.Albedo = col.rgb;
            o.Smoothness = 0.35;
            o.Metallic = 0.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
