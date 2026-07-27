using UnityEngine;

/// <summary>
/// Sibling of RopeRadialStrainCache: RGBA map in body/cloth UV space.
/// R=strain, G=slipU, B=slipV, A=contact weight.
/// </summary>
public sealed class ClothUvStretchCache
{
    readonly int _width;
    readonly int _height;
    Texture2D _texture;
    Color[] _pixels;

    public Texture2D Texture => _texture;
    public int Width => _width;
    public int Height => _height;

    public ClothUvStretchCache(int width = 64, int height = 64)
    {
        _width = Mathf.Max(4, width);
        _height = Mathf.Max(4, height);
        _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "ClothUvStretchCache"
        };
        _pixels = new Color[_width * _height];
    }

    public void Clear()
    {
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = Color.clear;
        Apply();
    }

    /// <summary>Write a soft blob around UV for the current layer sample.</summary>
    public void WriteSample(Vector2 uv01, float strain, Vector2 slip, float contact, float radiusUv = 0.08f)
    {
        float cx = Mathf.Clamp01(uv01.x) * (_width - 1);
        float cy = Mathf.Clamp01(uv01.y) * (_height - 1);
        int rPix = Mathf.Max(1, Mathf.RoundToInt(radiusUv * Mathf.Max(_width, _height)));
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx) - rPix);
        int x1 = Mathf.Min(_width - 1, Mathf.CeilToInt(cx) + rPix);
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy) - rPix);
        int y1 = Mathf.Min(_height - 1, Mathf.CeilToInt(cy) + rPix);

        float strainC = Mathf.Clamp01(strain);
        float slipU = Mathf.Clamp01(slip.x * 0.5f + 0.5f);
        float slipV = Mathf.Clamp01(slip.y * 0.5f + 0.5f);
        float contactC = Mathf.Clamp01(contact);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rPix;
                float dy = (y - cy) / rPix;
                float w = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                if (w <= 0f) continue;
                int idx = y * _width + x;
                Color c = _pixels[idx];
                c.r = Mathf.Max(c.r, strainC * w);
                c.g = Mathf.Lerp(c.g, slipU, w * contactC);
                c.b = Mathf.Lerp(c.b, slipV, w * contactC);
                c.a = Mathf.Max(c.a, contactC * w);
                _pixels[idx] = c;
            }
        }
    }

    public void Apply()
    {
        if (_texture == null) return;
        _texture.SetPixels(_pixels);
        _texture.Apply(false, false);
    }

    public void BindToMaterial(Material mat, string propertyName = "_ClothStretchTex")
    {
        if (mat != null && _texture != null)
            mat.SetTexture(propertyName, _texture);
    }

    public void Dispose()
    {
        if (_texture != null)
        {
            Object.Destroy(_texture);
            _texture = null;
        }
    }
}
