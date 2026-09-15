using System.Collections.Generic;
using UnityEngine;

/// <summary>Standard curves for a bicycle-style roller link: raccoon-mask plates, pins, hollow sheaths, shear clip.</summary>
public static class GarageChainLinkCurves
{
    public static List<Vector3> RaccoonMaskPath(GarageChainLinkCurve curve, int segmentsPerArc = 12)
    {
        curve ??= new GarageChainLinkCurve();
        int arc = Mathf.Max(4, segmentsPerArc);
        var path = new List<Vector3>(arc * 2 + 16);
        Vector3 left = curve.LeftPin;
        Vector3 right = curve.RightPin;
        float r = curve.PlateOuterR;

        AppendArc(path, left, r, 90f, 270f, arc);
        AppendWaist(path, curve, -1f, arc);
        AppendArc(path, right, r, -90f, 90f, arc);
        AppendWaist(path, curve, 1f, arc);

        if (path.Count > 0)
            path.Add(path[0]);
        return path;
    }

    public static List<Vector3> PinAxis(GarageChainLinkCurve curve, bool left)
    {
        curve ??= new GarageChainLinkCurve();
        Vector3 c = left ? curve.LeftPin : curve.RightPin;
        float h = curve.PinLength * 0.5f;
        return new List<Vector3> { c + Vector3.back * h, c + Vector3.forward * h };
    }

    public static List<Vector3> RollerAxis(GarageChainLinkCurve curve, bool left)
    {
        curve ??= new GarageChainLinkCurve();
        Vector3 c = left ? curve.LeftPin : curve.RightPin;
        float h = curve.RollerLength * 0.5f;
        return new List<Vector3> { c + Vector3.back * h, c + Vector3.forward * h };
    }

    public static List<Vector3> ShearClipPath(GarageChainLinkCurve curve, int segments = 10)
    {
        curve ??= new GarageChainLinkCurve();
        int n = Mathf.Max(4, segments);
        var path = new List<Vector3>(n + 8);
        Vector3 c = curve.LeftPin;
        float r = curve.PinRadius + curve.ClipThickness * 2f;
        float span = curve.ClipSpan;
        AppendArc(path, c, r, 210f, 150f, n);
        path.Add(c + new Vector3(span * 0.35f, r * 0.25f, 0f));
        path.Add(c + new Vector3(span * 0.15f, 0f, 0f));
        path.Add(c + new Vector3(span * 0.35f, -r * 0.25f, 0f));
        AppendArc(path, c, r, -150f, -210f, n);
        return path;
    }

    public static List<Vector3> ShearBladePath(GarageChainLinkCurve curve, bool upper)
    {
        curve ??= new GarageChainLinkCurve();
        Vector3 c = curve.LeftPin;
        float s = curve.ClipSpan;
        float y = upper ? 1f : -1f;
        return new List<Vector3>
        {
            c + new Vector3(-s * 0.55f, y * s * 0.4f, 0f),
            c + new Vector3(s * 0.15f, -y * s * 0.08f, 0f),
            c + new Vector3(s * 0.4f, -y * s * 0.18f, 0f)
        };
    }

    public static bool IsClosed(IList<Vector3> path, float eps = 0.0005f)
    {
        if (path == null || path.Count < 3)
            return false;
        return (path[0] - path[path.Count - 1]).sqrMagnitude <= eps * eps;
    }

    public static bool IsLeftRightSymmetric(IList<Vector3> path, float eps = 0.002f)
    {
        if (path == null || path.Count < 4)
            return false;
        float e2 = eps * eps;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i];
            Vector3 mirror = new Vector3(-p.x, p.y, p.z);
            bool hit = false;
            for (int j = 0; j < path.Count; j++)
            {
                if ((path[j] - mirror).sqrMagnitude <= e2)
                {
                    hit = true;
                    break;
                }
            }
            if (!hit)
                return false;
        }
        return true;
    }

    static void AppendArc(List<Vector3> path, Vector3 center, float radius, float fromDeg, float toDeg, int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float deg = Mathf.Lerp(fromDeg, toDeg, t);
            float rad = deg * Mathf.Deg2Rad;
            path.Add(center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius);
        }
    }

    static void AppendWaist(List<Vector3> path, GarageChainLinkCurve curve, float ySign, int samples)
    {
        float half = curve.Pitch * 0.5f;
        float x0 = ySign > 0f ? half : -half;
        float x1 = ySign > 0f ? -half : half;
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            float x = Mathf.Lerp(x0, x1, t);
            path.Add(new Vector3(x, ySign * curve.WaistY(x), 0f));
        }
    }
}
