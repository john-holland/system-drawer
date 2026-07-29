using UnityEngine;

/// <summary>
/// Caches helmet-covered azimuth sectors and max(hair, helmetInterior) height.
/// Gates plume physics and feeds _HelmetMaskTex / rim edge to the shader.
/// </summary>
public sealed class HairHelmetSectionCache
{
    readonly int _azimuthBins;
    readonly int _lengthBins;
    readonly bool[] _coveredAzimuth;
    readonly bool[] _physicsEnabledAzimuth;
    readonly Color[] _maskPixels;
    readonly Color[] _heightLock;
    Texture2D _maskTex;
    bool _active;
    float _rimUvEdge = 0.92f;

    static readonly int HelmetMaskId = Shader.PropertyToID("_HelmetMaskTex");
    static readonly int HelmetActiveId = Shader.PropertyToID("_HelmetActive");
    static readonly int HelmetRimId = Shader.PropertyToID("_HelmetRimUvEdge");

    public bool Active => _active;
    public Texture2D MaskTexture => _maskTex;
    public float RimUvEdge => _rimUvEdge;

    public HairHelmetSectionCache(int azimuthBins, int lengthBins)
    {
        _azimuthBins = Mathf.Max(8, azimuthBins);
        _lengthBins = Mathf.Max(4, lengthBins);
        _coveredAzimuth = new bool[_azimuthBins];
        _physicsEnabledAzimuth = new bool[_azimuthBins];
        for (int i = 0; i < _azimuthBins; i++)
            _physicsEnabledAzimuth[i] = true;
        _maskPixels = new Color[_azimuthBins * _lengthBins];
        _heightLock = new Color[_azimuthBins * _lengthBins];
        _maskTex = new Texture2D(_azimuthBins, _lengthBins, TextureFormat.RGBA32, false, true)
        {
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HairHelmetMask"
        };
    }

    public void SetActive(bool active) => _active = active;

    public void SetRimUvEdge(float edge) => _rimUvEdge = Mathf.Clamp01(edge);

    public void ClearCoverage()
    {
        for (int i = 0; i < _azimuthBins; i++)
        {
            _coveredAzimuth[i] = false;
            _physicsEnabledAzimuth[i] = true;
        }
        for (int i = 0; i < _maskPixels.Length; i++)
            _maskPixels[i] = Color.clear;
        ApplyMask();
        _active = false;
    }

    public void SetAzimuthCovered(int azimuthIndex, bool covered)
    {
        if (azimuthIndex < 0 || azimuthIndex >= _azimuthBins) return;
        _coveredAzimuth[azimuthIndex] = covered;
        _physicsEnabledAzimuth[azimuthIndex] = !covered;
    }

    public void SetAzimuthCovered01(float azimuth01, bool covered)
    {
        int i = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(azimuth01, 1f) * _azimuthBins), 0, _azimuthBins - 1);
        SetAzimuthCovered(i, covered);
    }

    /// <summary>
    /// Sweep a conic shell: cover azimuth bins inside current tuck radius fraction.
    /// </summary>
    public void ApplyConicTuck(float radiusFraction01, float centerAzimuth01 = 0f)
    {
        float half = Mathf.Clamp01(radiusFraction01) * 0.5f;
        for (int u = 0; u < _azimuthBins; u++)
        {
            float a = u / (float)_azimuthBins;
            float d = Mathf.Abs(Mathf.Repeat(a - centerAzimuth01 + 0.5f, 1f) - 0.5f);
            bool covered = d <= half;
            _coveredAzimuth[u] = covered;
            _physicsEnabledAzimuth[u] = !covered;
        }
        RebuildMaskFromCoverage();
        _active = true;
    }

    /// <summary>
    /// Lock height as max(hairHeight, helmetInterior) per bin and refresh mask.
    /// </summary>
    public void CacheMaxHeight(HairRadialTextureCache hairCache, float[] helmetInteriorHeight01)
    {
        if (hairCache == null) return;
        int az = Mathf.Min(_azimuthBins, hairCache.AzimuthBins);
        int len = Mathf.Min(_lengthBins, hairCache.LengthBins);
        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            for (int u = 0; u < az; u++)
            {
                Color h = hairCache.GetPixel(u, v);
                float helmet = 0f;
                if (helmetInteriorHeight01 != null && helmetInteriorHeight01.Length == az * len)
                    helmet = helmetInteriorHeight01[v * az + u];
                else if (helmetInteriorHeight01 != null && helmetInteriorHeight01.Length == az)
                    helmet = helmetInteriorHeight01[u] * (1f - length01);

                float locked = Mathf.Max(h.r, Mathf.Clamp01(helmet));
                _heightLock[v * _azimuthBins + u] = new Color(locked, h.g, h.b, h.a);

                // Covered interior: mask R=1 below rim; pop-out bins stay 0
                bool covered = _coveredAzimuth[u] && length01 < _rimUvEdge;
                _maskPixels[v * _azimuthBins + u] = covered ? Color.white : Color.clear;

                if (covered)
                {
                    // Keep locked height in hair cache for rim pop evaluation
                    h.r = locked;
                    hairCache.SetPixel(u, v, h);
                }
            }
        }
        hairCache.Apply();
        ApplyMask();
        _active = true;
    }

    public void RebuildMaskFromCoverage()
    {
        for (int v = 0; v < _lengthBins; v++)
        {
            float length01 = v / (float)(_lengthBins - 1);
            for (int u = 0; u < _azimuthBins; u++)
            {
                bool covered = _coveredAzimuth[u] && length01 < _rimUvEdge;
                _maskPixels[v * _azimuthBins + u] = covered ? Color.white : Color.clear;
            }
        }
        ApplyMask();
    }

    public bool IsPhysicsEnabledForAzimuth(float azimuth01)
    {
        if (!_active) return true;
        int i = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(azimuth01, 1f) * _azimuthBins), 0, _azimuthBins - 1);
        return _physicsEnabledAzimuth[i];
    }

    public bool IsPhysicsEnabledForLength(float length01)
    {
        // Length alone does not gate; azimuth coverage does. Kept for driver API clarity.
        return true;
    }

    public void BindToMaterial(Material mat)
    {
        if (mat == null) return;
        if (_maskTex != null)
            mat.SetTexture(HelmetMaskId, _maskTex);
        mat.SetFloat(HelmetActiveId, _active ? 1f : 0f);
        mat.SetFloat(HelmetRimId, _rimUvEdge);
    }

    void ApplyMask()
    {
        if (_maskTex == null) return;
        _maskTex.SetPixels(_maskPixels);
        _maskTex.Apply(false, false);
    }

    public void Dispose()
    {
        if (_maskTex != null)
        {
            if (Application.isPlaying)
                Object.Destroy(_maskTex);
            else
                Object.DestroyImmediate(_maskTex);
            _maskTex = null;
        }
    }
}
