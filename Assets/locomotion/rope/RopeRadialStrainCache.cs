using UnityEngine;

/// <summary>
/// CPU radial cache (arc x radial slice) uploaded as Texture2D for rope strain/twist shader.
/// R=strain, G=twist, B=tensionNorm, A=woundFlag
/// </summary>
public class RopeRadialStrainCache
{
    readonly RopeConfig _config;
    readonly RopeArcLengthState _arc;
    readonly RopeTensileModel _tensile;
    Texture2D _texture;
    Color[] _pixels;
    int _arcBins;
    int _radialSlices;

    public Texture2D Texture => _texture;

    public RopeRadialStrainCache(RopeConfig config, RopeArcLengthState arc, RopeTensileModel tensile)
    {
        _config = config;
        _arc = arc;
        _tensile = tensile;
        _arcBins = config.ArcBinCount;
        _radialSlices = Mathf.Max(4, config.radialSlices);
        _texture = new Texture2D(_arcBins, _radialSlices, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        _pixels = new Color[_arcBins * _radialSlices];
    }

    public void WriteFromSimulation()
    {
        float totalBreak = Mathf.Max(1f, _tensile.TotalBreakTensionN);
        float woundNorm = _arc.WoundLengthM / Mathf.Max(0.01f, _arc.TotalLength);

        for (int u = 0; u < _arcBins; u++)
        {
            float arcM = u * _config.arcBinSizeM;
            int logical = Mathf.Clamp(Mathf.FloorToInt(arcM / _arc.SegmentLength()), 0, _arc.SegmentCount - 1);
            RopeSegmentTensionSample sample = _tensile.GetSample(logical);
            float strain = Mathf.Clamp01(sample.strain);
            float twistNorm = Mathf.Repeat(sample.twistRad / (Mathf.PI * 2f), 1f);
            float tensionNorm = Mathf.Clamp01(sample.tensionN / totalBreak);
            float woundFlag = arcM <= _arc.WoundLengthM ? 1f : 0f;

            for (int v = 0; v < _radialSlices; v++)
            {
                float radialPhase = v / (float)_radialSlices;
                float oval = 1f - 0.15f * strain * Mathf.Sin(radialPhase * Mathf.PI * 2f);
                _pixels[v * _arcBins + u] = new Color(strain * oval, twistNorm, tensionNorm, woundFlag);
            }
        }

        _texture.SetPixels(_pixels);
        _texture.Apply(false, false);
    }

    public void BindToMaterial(Material mat, string propertyName = "_RopeStrainTex")
    {
        if (mat != null && _texture != null)
            mat.SetTexture(propertyName, _texture);
    }
}
