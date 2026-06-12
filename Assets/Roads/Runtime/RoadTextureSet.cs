using UnityEngine;

namespace Roads
{
    /// <summary>Five texture maps for road surface rendering.</summary>
    [CreateAssetMenu(fileName = "RoadTextureSet", menuName = "Roads/Road Texture Set")]
    public class RoadTextureSet : ScriptableObject
    {
        [Header("Surface Maps")]
        public Texture2D road;
        public Texture2D centerLine;
        public Texture2D sideLine;
        public Texture2D dirt;
        public Texture2D underside;

        [Header("Tiling")]
        public Vector2 roadTile = new Vector2(1f, 1f);
        public Vector2 dirtTile = new Vector2(2f, 2f);

        [Header("Blend")]
        [Range(0f, 1f)] public float centerLineWidth = 0.08f;
        [Range(0f, 1f)] public float sideLineWidth = 0.06f;
        [Range(0f, 1f)] public float dirtShoulderStart = 0.35f;
        [Range(0f, 1f)] public float terrainSpillBlend = 0.25f;
        public Texture2D macroVariation;

        public void ApplyToMaterial(Material mat)
        {
            if (mat == null)
                return;
            if (road != null) mat.SetTexture("_RoadTex", road);
            if (centerLine != null) mat.SetTexture("_CenterLineTex", centerLine);
            if (sideLine != null) mat.SetTexture("_SideLineTex", sideLine);
            if (dirt != null) mat.SetTexture("_DirtTex", dirt);
            if (underside != null) mat.SetTexture("_UndersideTex", underside);
            if (macroVariation != null) mat.SetTexture("_MacroVariationTex", macroVariation);
            mat.SetVector("_RoadTile", roadTile);
            mat.SetVector("_DirtTile", dirtTile);
            mat.SetFloat("_CenterLineWidth", centerLineWidth);
            mat.SetFloat("_SideLineWidth", sideLineWidth);
            mat.SetFloat("_DirtShoulderStart", dirtShoulderStart);
            mat.SetFloat("_TerrainSpillBlend", terrainSpillBlend);
        }
    }
}
