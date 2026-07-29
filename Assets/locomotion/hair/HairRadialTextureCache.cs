using UnityEngine;

/// <summary>
/// Complete radial texture cache for hair (azimuth × length).
/// R=plume height, G=passthrough occlusion, B=curve hold mask, A=tip break energy.
/// </summary>
public sealed class HairRadialTextureCache
{
    readonly int _azimuthBins;
    readonly int _lengthBins;
    Texture2D _texture;
    Color[] _pixels;
    bool _dirty;

    public Texture2D Texture => _texture;
    public int AzimuthBins => _azimuthBins;
    public int LengthBins => _lengthBins;

    public HairRadialTextureCache(int azimuthBins = 64, int lengthBins = 32)
    {
        _azimuthBins = Mathf.Max(8, azimuthBins);
        _lengthBins = Mathf.Max(4, lengthBins);
        _texture = new Texture2D(_azimuthBins, _lengthBins, TextureFormat.RGBA32, false, true)
        {
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HairRadialTextureCache"
        };
        _pixels = new Color[_azimuthBins * _lengthBins];
    }

    public void Clear(Color fill)
    {
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = fill;
        _dirty = true;
    }

    public void Clear() => Clear(Color.clear);

    public Color GetPixel(int azimuth, int length)
    {
        azimuth = ((azimuth % _azimuthBins) + _azimuthBins) % _azimuthBins;
        length = Mathf.Clamp(length, 0, _lengthBins - 1);
        return _pixels[length * _azimuthBins + azimuth];
    }

    public void SetPixel(int azimuth, int length, Color c)
    {
        azimuth = ((azimuth % _azimuthBins) + _azimuthBins) % _azimuthBins;
        length = Mathf.Clamp(length, 0, _lengthBins - 1);
        _pixels[length * _azimuthBins + azimuth] = c;
        _dirty = true;
    }

    public void MaxChannel(int azimuth, int length, int channel, float value)
    {
        Color c = GetPixel(azimuth, length);
        float v = Mathf.Clamp01(value);
        switch (channel)
        {
            case 0: c.r = Mathf.Max(c.r, v); break;
            case 1: c.g = Mathf.Max(c.g, v); break;
            case 2: c.b = Mathf.Max(c.b, v); break;
            default: c.a = Mathf.Max(c.a, v); break;
        }
        SetPixel(azimuth, length, c);
    }

    public void LerpChannel(int azimuth, int length, int channel, float value, float t)
    {
        Color c = GetPixel(azimuth, length);
        float v = Mathf.Clamp01(value);
        t = Mathf.Clamp01(t);
        switch (channel)
        {
            case 0: c.r = Mathf.Lerp(c.r, v, t); break;
            case 1: c.g = Mathf.Lerp(c.g, v, t); break;
            case 2: c.b = Mathf.Lerp(c.b, v, t); break;
            default: c.a = Mathf.Lerp(c.a, v, t); break;
        }
        SetPixel(azimuth, length, c);
    }

    /// <summary>Soft circular write in radial UV space (azimuth wraps).</summary>
    public void WriteSoftBlob(float azimuth01, float length01, Color sample, float radiusUv = 0.06f)
    {
        float cx = Mathf.Repeat(azimuth01, 1f) * _azimuthBins;
        float cy = Mathf.Clamp01(length01) * (_lengthBins - 1);
        int rPix = Mathf.Max(1, Mathf.RoundToInt(radiusUv * Mathf.Max(_azimuthBins, _lengthBins)));
        for (int dy = -rPix; dy <= rPix; dy++)
        {
            int y = Mathf.Clamp(Mathf.RoundToInt(cy) + dy, 0, _lengthBins - 1);
            for (int dx = -rPix; dx <= rPix; dx++)
            {
                int x = Mathf.RoundToInt(cx) + dx;
                x = ((x % _azimuthBins) + _azimuthBins) % _azimuthBins;
                float nx = dx / (float)rPix;
                float ny = dy / (float)rPix;
                float w = 1f - Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
                if (w <= 0f) continue;
                Color c = _pixels[y * _azimuthBins + x];
                c.r = Mathf.Max(c.r, sample.r * w);
                c.g = Mathf.Lerp(c.g, sample.g, w);
                c.b = Mathf.Max(c.b, sample.b * w);
                c.a = Mathf.Max(c.a, sample.a * w);
                _pixels[y * _azimuthBins + x] = c;
                _dirty = true;
            }
        }
    }

    public void CopyFrom(Color[] source)
    {
        if (source == null || source.Length != _pixels.Length) return;
        System.Array.Copy(source, _pixels, _pixels.Length);
        _dirty = true;
    }

    public Color[] ClonePixels()
    {
        var copy = new Color[_pixels.Length];
        System.Array.Copy(_pixels, copy, _pixels.Length);
        return copy;
    }

    public void Apply()
    {
        if (_texture == null || !_dirty) return;
        _texture.SetPixels(_pixels);
        _texture.Apply(false, false);
        _dirty = false;
    }

    public void BindToMaterial(Material mat, string propertyName = "_HairRadialTex")
    {
        if (mat != null && _texture != null)
            mat.SetTexture(propertyName, _texture);
    }

    public void Dispose()
    {
        if (_texture != null)
        {
            if (Application.isPlaying)
                Object.Destroy(_texture);
            else
                Object.DestroyImmediate(_texture);
            _texture = null;
        }
        _pixels = null;
    }
}
