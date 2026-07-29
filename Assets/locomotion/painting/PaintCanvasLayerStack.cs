using System;
using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>
/// One paint layer: SDF Max expression + material + dry field.
/// </summary>
[Serializable]
public sealed class PaintLayerExpression
{
    public string name = "Layer";
    public SdfMaxCompositionAsset composition;
    public Material material;
    [Range(0f, 1f)] public float dry01;
    public Color albedo = Color.white;
    [Range(0f, 1f)] public float specular = 0.35f;
    [Range(0f, 1f)] public float roughness = 0.55f;
}

/// <summary>
/// Ordered SDF Max paint layers on a canvas with optional destructive smudge.
/// </summary>
[CreateAssetMenu(fileName = "PaintCanvasLayerStack", menuName = "Locomotion/Painting/Canvas Layer Stack")]
public sealed class PaintCanvasLayerStack : ScriptableObject
{
    public List<PaintLayerExpression> layers = new List<PaintLayerExpression>();
    public bool enableDestructiveSmudge;
    [Range(0f, 1f)] public float smudgeDryLock = 0.85f;
    public bool smudgeDryLayers;
    [Min(0.001f)] public float smudgeRadius = 0.02f;
    [Range(0f, 2f)] public float smudgeStrength = 1f;

    public PaintLayerExpression TopWetLayer()
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            if (layers[i] != null && layers[i].dry01 < smudgeDryLock)
                return layers[i];
        }
        return layers.Count > 0 ? layers[layers.Count - 1] : null;
    }

    public void EnsureBaseLayer()
    {
        if (layers == null)
            layers = new List<PaintLayerExpression>();
        if (layers.Count > 0) return;
        var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        comp.name = "PaintBaseLayer";
        comp.nodes = new List<SdfMaxNode>
        {
            new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Plane,
                localPosition = Vector3.zero
            }
        };
        comp.rootNodeIndex = 0;
        layers.Add(new PaintLayerExpression
        {
            name = "Base",
            composition = comp,
            dry01 = 0f,
            albedo = Color.white
        });
    }
}

/// <summary>Planar viscosity / dry cache on canvas UV.</summary>
public sealed class PaintPlanarViscosityCache
{
    readonly int _w;
    readonly int _h;
    readonly Color[] _pixels;
    Texture2D _tex;

    public Texture2D Texture => _tex;
    public int Width => _w;
    public int Height => _h;

    public PaintPlanarViscosityCache(int w = 128, int h = 128)
    {
        _w = Mathf.Max(8, w);
        _h = Mathf.Max(8, h);
        _pixels = new Color[_w * _h];
        _tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "PaintPlanarViscosity"
        };
    }

    public Color Get(int x, int y) => _pixels[y * _w + x];

    public void Set(int x, int y, Color c)
    {
        x = Mathf.Clamp(x, 0, _w - 1);
        y = Mathf.Clamp(y, 0, _h - 1);
        _pixels[y * _w + x] = c;
    }

    public void SampleUv(Vector2 uv, out Color c)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.x) * (_w - 1)), 0, _w - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(uv.y) * (_h - 1)), 0, _h - 1);
        c = _pixels[y * _w + x];
    }

    public void Stamp(Vector2 uv, Color sample, float radiusUv)
    {
        float cx = Mathf.Clamp01(uv.x) * (_w - 1);
        float cy = Mathf.Clamp01(uv.y) * (_h - 1);
        int r = Mathf.Max(1, Mathf.RoundToInt(radiusUv * Mathf.Max(_w, _h)));
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(cx) + dx, 0, _w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(cy) + dy, 0, _h - 1);
            float w = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / r);
            if (w <= 0f) continue;
            Color c = _pixels[y * _w + x];
            c.r = Mathf.Max(c.r, sample.r * w); // wet
            c.g = Mathf.Lerp(c.g, sample.g, w); // dry
            c.b = Mathf.Max(c.b, sample.b * w); // mass
            c.a = Mathf.Max(c.a, sample.a * w); // spec
            _pixels[y * _w + x] = c;
        }
    }

    public void Apply()
    {
        _tex.SetPixels(_pixels);
        _tex.Apply(false, false);
    }

    public void Dispose()
    {
        if (_tex == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(_tex);
        else UnityEngine.Object.DestroyImmediate(_tex);
        _tex = null;
    }
}
