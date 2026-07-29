using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>
/// Intersects brush tip curves with canvas plane and appends SDF Max stroke segments + dry gradient.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Stroke Stamper")]
public sealed class PaintStrokeStamper : MonoBehaviour
{
    public PaintCanvas canvas;
    Vector3 _lastTip;
    bool _hasLast;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<PaintCanvas>();
    }

    public void StampFromBrush(PaintBrushRuntime brush)
    {
        if (canvas == null || brush == null || brush.definition == null || brush.tip == null)
            return;
        if (canvas.layerStack == null)
            return;
        canvas.layerStack.EnsureBaseLayer();

        Vector3 tip = brush.tip.position;
        if (!canvas.WorldToCanvasUv(tip, out Vector2 uv))
            return;

        float load = brush.Load01;
        if (load <= 1e-4f) return;

        // Footprint from end × conical × hairline sample ring
        float radiusUv = brush.definition.ferruleRadiusM * (0.5f + load) * (1f + canvas.streakiness);
        float dryAlong = 0f;
        if (_hasLast)
        {
            Vector3 delta = tip - _lastTip;
            dryAlong = Mathf.Clamp01(delta.magnitude * 2f);
        }
        _lastTip = tip;
        _hasLast = true;

        Color sample = brush.loadedColor;
        sample.r = load; // wet
        sample.g = dryAlong * (1f - canvas.streakiness); // dry gradient along stroke
        sample.b = load;
        sample.a = brush.definition != null ? 0.4f + brush.Load01 * 0.4f : 0.5f;

        canvas.Viscosity?.Stamp(uv, sample, radiusUv);
        canvas.Viscosity?.Apply();

        AppendSdfSegment(brush, tip, uv, load, dryAlong);

        var hydro = canvas.Hydro;
        if (hydro != null)
        {
            hydro.surfaceTension = canvas.surfaceTension;
            hydro.SeedFromStamp(tip, brush.loadedColor, load, wet01: load, count: 10);
            if (brush.pileSource != null)
                hydro.pileSource = brush.pileSource;
        }

        canvas.BindMaterials();
    }

    void AppendSdfSegment(PaintBrushRuntime brush, Vector3 worldTip, Vector2 uv, float load, float dryAlong)
    {
        var layer = canvas.layerStack.TopWetLayer();
        if (layer == null) return;
        if (layer.composition == null)
        {
            layer.composition = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            layer.composition.nodes = new List<SdfMaxNode>();
            layer.composition.rootNodeIndex = -1;
        }

        var nodes = layer.composition.nodes;
        Vector3 local = canvas.transform.InverseTransformPoint(worldTip);
        float r = brush.definition.ferruleRadiusM * brush.definition.ConicalFlare(1f) * load;

        int leaf = nodes.Count;
        nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            radius = Mathf.Max(0.002f, r),
            sphereRadius = Mathf.Max(0.002f, r),
            localPosition = local,
            weight = Mathf.Clamp01(1f - dryAlong),
            tMin = 0f,
            tMax = 1f
        });

        int prev = layer.composition.ResolveRootIndex();
        if (prev < 0)
        {
            layer.composition.rootNodeIndex = leaf;
        }
        else
        {
            nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Max,
                childIndexA = prev,
                childIndexB = leaf,
                weight = 1f - dryAlong * 0.5f
            });
            layer.composition.rootNodeIndex = nodes.Count - 1;
        }

        layer.albedo = Color.Lerp(layer.albedo, brush.loadedColor, load * 0.35f);
        layer.dry01 = Mathf.Clamp01(layer.dry01 + dryAlong * 0.02f);
        // Specular finalized by PaintCanvasHydroSolver; seed a wet-film baseline
        layer.specular = Mathf.Clamp01(0.2f + (1f - layer.dry01) * 0.5f);
    }
}
