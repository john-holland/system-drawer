using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Prebaked hierarchical samples along one branch, ready to copy onto a ribbon path.</summary>
[Serializable]
public sealed class BranchPathBake
{
    public Vector3[] positions = Array.Empty<Vector3>();
    public Vector3[] tangents = Array.Empty<Vector3>();
    public string[] hierarchicalPlaneIds = Array.Empty<string>();
    public float length;

    public static float SampleLength(PlantBranchDef branch, int seed)
    {
        if (branch == null) return 0f;
        float min = Mathf.Min(branch.lengthMin, branch.lengthMax);
        float max = Mathf.Max(branch.lengthMin, branch.lengthMax);
        if (Mathf.Abs(max - min) <= 1e-4f) return min;
        var rng = new System.Random(seed);
        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    public static BranchPathBake Bake(PlantBranchDef branch, int seed, int samples = 8)
    {
        var result = new BranchPathBake();
        if (branch == null) return result;
        float target = Mathf.Max(0.01f, SampleLength(branch, seed));
        var shape = branch.curvePoints != null && branch.curvePoints.Count >= 2
            ? branch.curvePoints
            : new List<Vector3> { Vector3.zero, Vector3.forward };
        int count = Mathf.Max(2, samples);
        var raw = new List<Vector3>(count);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (float)(count - 1);
            raw.Add(Evaluate(shape, t));
        }
        float rawLen = PolylineLength(raw);
        float scale = rawLen > 1e-5f ? target / rawLen : 1f;
        Quaternion rot = branch.originRotation;
        result.positions = new Vector3[count];
        result.tangents = new Vector3[count];
        result.hierarchicalPlaneIds = new string[count];
        string label = string.IsNullOrEmpty(branch.branchTypeLabel) ? "branch" : branch.branchTypeLabel;
        Vector3 origin = raw[0];
        for (int i = 0; i < count; i++)
            result.positions[i] = rot * ((raw[i] - origin) * scale);
        for (int i = 0; i < count; i++)
        {
            Vector3 delta = i < count - 1
                ? result.positions[i + 1] - result.positions[i]
                : result.positions[i] - result.positions[i - 1];
            result.tangents[i] = delta.sqrMagnitude > 1e-8f ? delta.normalized : rot * Vector3.forward;
            result.hierarchicalPlaneIds[i] = label + "_" + i;
        }
        result.length = PolylineLength(result.positions);
        return result;
    }

    public void ApplyToRibbon(PlanarSplinePathLocomotion ribbon)
    {
        if (ribbon == null) return;
        if (ribbon.controlPoints == null)
            ribbon.controlPoints = new List<Vector3>();
        else
            ribbon.controlPoints.Clear();
        if (positions == null) return;
        for (int i = 0; i < positions.Length; i++)
            ribbon.controlPoints.Add(positions[i]);
    }

    public Vector3 GrabberPoint(float t01)
    {
        if (positions == null || positions.Length == 0) return Vector3.zero;
        if (positions.Length == 1) return positions[0];
        float target = Mathf.Clamp01(t01) * PolylineLength(positions);
        float walked = 0f;
        for (int i = 1; i < positions.Length; i++)
        {
            float seg = Vector3.Distance(positions[i - 1], positions[i]);
            if (walked + seg >= target || i == positions.Length - 1)
            {
                float u = seg > 1e-5f ? Mathf.Clamp01((target - walked) / seg) : 0f;
                return Vector3.Lerp(positions[i - 1], positions[i], u);
            }
            walked += seg;
        }
        return positions[positions.Length - 1];
    }

    public Vector3 GrabberTangent(float t01)
    {
        if (tangents == null || tangents.Length == 0) return Vector3.forward;
        int i = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(t01) * (tangents.Length - 1)), 0, tangents.Length - 1);
        return tangents[i].sqrMagnitude > 1e-8f ? tangents[i].normalized : Vector3.forward;
    }

    static float PolylineLength(IList<Vector3> pts)
    {
        if (pts == null || pts.Count < 2) return 0f;
        float sum = 0f;
        for (int i = 1; i < pts.Count; i++)
            sum += Vector3.Distance(pts[i - 1], pts[i]);
        return sum;
    }

    static Vector3 Evaluate(IList<Vector3> curve, float t01)
    {
        if (curve.Count == 1) return curve[0];
        if (curve.Count == 2) return Vector3.Lerp(curve[0], curve[1], t01);
        float scaled = Mathf.Clamp01(t01) * (curve.Count - 1);
        int i = Mathf.Min(Mathf.FloorToInt(scaled), curve.Count - 2);
        float u = scaled - i;
        Vector3 p0 = curve[Mathf.Max(0, i - 1)];
        Vector3 p1 = curve[i];
        Vector3 p2 = curve[Mathf.Min(curve.Count - 1, i + 1)];
        Vector3 p3 = curve[Mathf.Min(curve.Count - 1, i + 2)];
        float u2 = u * u;
        float u3 = u2 * u;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * u +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
    }
}
