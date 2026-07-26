using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Membership curves for NSM fuzzy hedges (mirrors Python nsm_logical_form.apply_curve).</summary>
public static class NsmFuzzyHedgeCurves
{
    [Serializable]
    public struct Curve
    {
        public string kind;
        public float k;
        public float x0;
        public float yMin;
        public float yMax;
        public float p;
        public float yScale;
        public bool clamp;
        public List<Vector2> points;

        public static Curve Logistic(float k, float x0, float yMin, float yMax) => new Curve
        {
            kind = "logistic",
            k = k,
            x0 = x0,
            yMin = yMin,
            yMax = yMax,
            yScale = 1f,
            clamp = true
        };

        public static Curve Power(float p, float yScale = 1f) => new Curve
        {
            kind = "power",
            p = p,
            yScale = yScale,
            clamp = true
        };
    }

    static readonly Dictionary<string, Curve> Defaults =
        new Dictionary<string, Curve>(StringComparer.OrdinalIgnoreCase)
        {
            { "somewhat", Curve.Logistic(8f, 0.45f, 0.15f, 0.7f) },
            { "mostly", Curve.Logistic(10f, 0.6f, 0.55f, 0.95f) },
            { "basically", Curve.Logistic(9f, 0.55f, 0.55f, 0.92f) },
            { "maybe", Curve.Logistic(6f, 0.5f, 0.2f, 0.65f) },
            { "very", Curve.Power(2f) },
            { "more", Curve.Power(0.7f) },
            { "less", Curve.Power(1.5f, 0.7f) },
            { "little", Curve.Logistic(7f, 0.35f, 0.05f, 0.45f) },
            { "much", Curve.Logistic(7f, 0.55f, 0.5f, 0.95f) },
            { "just-like", Curve.Logistic(12f, 0.5f, 0.7f, 0.98f) },
            { "just like", Curve.Logistic(12f, 0.5f, 0.7f, 0.98f) },
        };

    public static bool TryGetDefault(string hedgeId, out Curve curve) =>
        Defaults.TryGetValue(hedgeId ?? "", out curve);

    public static float Evaluate(string hedgeId, float x, Curve? overrideCurve = null)
    {
        Curve c;
        if (overrideCurve.HasValue)
            c = overrideCurve.Value;
        else if (!TryGetDefault(hedgeId, out c))
            return Mathf.Clamp01(x);
        return Apply(c, x);
    }

    public static float Apply(Curve curve, float x)
    {
        x = Mathf.Clamp01(x);
        float y;
        string kind = string.IsNullOrEmpty(curve.kind) ? "logistic" : curve.kind;
        if (string.Equals(kind, "power", StringComparison.OrdinalIgnoreCase))
        {
            float p = curve.p <= 0f ? 1f : curve.p;
            float scale = curve.yScale <= 0f ? 1f : curve.yScale;
            y = Mathf.Pow(x, p) * scale;
        }
        else if (string.Equals(kind, "piecewise", StringComparison.OrdinalIgnoreCase) &&
                 curve.points != null && curve.points.Count > 0)
        {
            var pts = new List<Vector2>(curve.points);
            pts.Sort((a, b) => a.x.CompareTo(b.x));
            if (x <= pts[0].x) y = pts[0].y;
            else if (x >= pts[pts.Count - 1].x) y = pts[pts.Count - 1].y;
            else
            {
                y = pts[pts.Count - 1].y;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    if (x >= pts[i].x && x <= pts[i + 1].x)
                    {
                        float t = Mathf.Approximately(pts[i + 1].x, pts[i].x)
                            ? 0f
                            : (x - pts[i].x) / (pts[i + 1].x - pts[i].x);
                        y = Mathf.Lerp(pts[i].y, pts[i + 1].y, t);
                        break;
                    }
                }
            }
        }
        else
        {
            float k = curve.k <= 0f ? 8f : curve.k;
            float sig = 1f / (1f + Mathf.Exp(-k * (x - curve.x0)));
            y = curve.yMin + sig * (curve.yMax - curve.yMin);
        }

        return curve.clamp ? Mathf.Clamp01(y) : y;
    }
}
