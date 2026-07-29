using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scalp part path that bisects the gaussian plume — hair density falls off across this spline.
/// </summary>
[Serializable]
public sealed class HairPartSpline
{
    public bool enabled = true;

    [Tooltip("Local-space control points on the scalp (typically forehead → pate → crown).")]
    public List<Vector3> localControlPoints = new List<Vector3>
    {
        new Vector3(0f, 0.02f, 0.09f),
        new Vector3(0f, 0.05f, 0.03f),
        new Vector3(0f, 0.06f, -0.02f)
    };

    [Min(0.001f)] public float partWidthM = 0.01f;
    [Range(0f, 1f)] public float bisectStrength = 1f;
    [Min(4)] public int sampleCount = 32;
    [Tooltip("Ribbon half-width for scene gizmos (meters).")]
    [Min(0.001f)] public float gizmoRibbonHalfWidthM = 0.008f;

    public void EnsureDefaults()
    {
        if (localControlPoints != null && localControlPoints.Count >= 2) return;
        localControlPoints = new List<Vector3>
        {
            new Vector3(0f, 0.02f, 0.09f),
            new Vector3(0f, 0.05f, 0.03f),
            new Vector3(0f, 0.06f, -0.02f)
        };
    }

    /// <summary>Catmull-Rom style sample along the part polyline (t in 0..1).</summary>
    public Vector3 EvaluateLocal(float t01)
    {
        EnsureDefaults();
        int n = localControlPoints.Count;
        if (n == 1) return localControlPoints[0];
        t01 = Mathf.Clamp01(t01);
        float scaled = t01 * (n - 1);
        int i = Mathf.Min(Mathf.FloorToInt(scaled), n - 2);
        float u = scaled - i;
        Vector3 p0 = localControlPoints[Mathf.Max(0, i - 1)];
        Vector3 p1 = localControlPoints[i];
        Vector3 p2 = localControlPoints[i + 1];
        Vector3 p3 = localControlPoints[Mathf.Min(n - 1, i + 2)];
        return CatmullRom(p0, p1, p2, p3, u);
    }

    public Vector3 EvaluateWorld(Transform scalpRoot, float t01)
    {
        Vector3 local = EvaluateLocal(t01);
        return scalpRoot != null ? scalpRoot.TransformPoint(local) : local;
    }

    public Vector3 TangentLocal(float t01)
    {
        const float eps = 0.01f;
        Vector3 a = EvaluateLocal(Mathf.Clamp01(t01 - eps));
        Vector3 b = EvaluateLocal(Mathf.Clamp01(t01 + eps));
        Vector3 d = b - a;
        return d.sqrMagnitude > 1e-10f ? d.normalized : Vector3.forward;
    }

    /// <summary>
    /// Signed distance in scalp XZ (or full 3D) from a local point to the part polyline.
    /// Positive = one side, negative = other (bisect).
    /// </summary>
    public float SignedDistanceLocal(Vector3 localPoint, out float nearestT01)
    {
        EnsureDefaults();
        nearestT01 = 0f;
        float bestSqr = float.MaxValue;
        float bestSigned = 0f;
        int segs = Mathf.Max(4, sampleCount);
        Vector3 prev = EvaluateLocal(0f);
        for (int i = 1; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector3 cur = EvaluateLocal(t);
            Vector3 ab = cur - prev;
            float abLenSq = ab.sqrMagnitude;
            float u = abLenSq > 1e-12f ? Mathf.Clamp01(Vector3.Dot(localPoint - prev, ab) / abLenSq) : 0f;
            Vector3 closest = prev + ab * u;
            Vector3 delta = localPoint - closest;
            // Prefer planar distance on scalp (drop Y for side sign)
            Vector2 d2 = new Vector2(delta.x, delta.z);
            float sqr = d2.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                Vector2 ab2 = new Vector2(ab.x, ab.z);
                float cross = ab2.x * d2.y - ab2.y * d2.x;
                bestSigned = Mathf.Sqrt(sqr) * Mathf.Sign(cross == 0f ? 1f : cross);
                nearestT01 = Mathf.Lerp((i - 1) / (float)segs, t, u);
            }
            prev = cur;
        }
        return bestSigned;
    }

    /// <summary>
    /// Multiplier in [0,1] that carves a valley along the part through a gaussian field.
    /// Optionally thins further by lateral flux away from the part (∇carve).
    /// </summary>
    public float GaussianBisectWeight(Vector3 localPoint, bool useLateralFlux = true)
    {
        if (!enabled) return 1f;
        float signed = SignedDistanceLocal(localPoint, out _);
        float dist = Mathf.Abs(signed);
        float w = partWidthM;
        float carve = Mathf.Exp(-(dist * dist) / (2f * w * w));
        float densityCarve = Mathf.Clamp01(1f - bisectStrength * carve);
        if (!useLateralFlux)
            return densityCarve;
        float lateral = HairGaussianFlux.PartLateralFluxWeight(signed, partWidthM, bisectStrength);
        // Lateral flux redistributes: deepen the valley slightly without zeroing shoulders
        return Mathf.Clamp01(densityCarve * (1f - 0.35f * lateral));
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}

/// <summary>Helpers for part + hairline sampling in bake/gizmos.</summary>
public static class HairPartSampler
{
    public static float ApplyPartToGaussian(
        float height01,
        float azimuth01,
        float length01,
        HairPlumeConfig config)
    {
        if (config?.hairPartSpline == null || !config.hairPartSpline.enabled)
            return height01;

        float ang = Mathf.Repeat(azimuth01, 1f) * Mathf.PI * 2f;
        float r = HairLineSampler.Radius(config, azimuth01) * Mathf.Clamp01(length01);
        // Local point on scalp disk for this lattice bin
        Vector3 local = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
        // Bias toward pate along length so tip bins still feel the part near crown
        Vector3 pate = config.centerPateLocal;
        local = Vector3.Lerp(local, pate, length01 * 0.35f);
        float w = config.hairPartSpline.GaussianBisectWeight(
            local,
            config.usePartLateralFlux);
        return height01 * w;
    }
}
