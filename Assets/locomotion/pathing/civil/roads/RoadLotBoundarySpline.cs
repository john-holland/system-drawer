using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoadLotWallSection
{
    [Range(0f, 1f)] public float startT01;
    [Range(0f, 1f)] public float endT01 = 1f;
    public float height = 2f;
    public bool isGap;
    public string gateOpenCloseTopologyId;
    public Material wallMaterial;

    public float Length01
    {
        get
        {
            float a = Mathf.Repeat(startT01, 1f);
            float b = endT01;
            // Full-loop section authored as 0..1.
            if (a <= 1e-6f && b >= 1f - 1e-6f)
                return 1f;
            float len = b - a;
            if (len <= 0f) len += 1f;
            return Mathf.Clamp01(len);
        }
    }
}

/// <summary>Closed exterior spline + optional segmented walls (section lengths must sum to 1).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Road Lot Boundary Spline")]
public sealed class RoadLotBoundarySpline : MonoBehaviour
{
    public List<Vector3> controlPoints = new List<Vector3>();
    public List<RoadLotWallSection> wallSections = new List<RoadLotWallSection>();
    public float wallValidateEpsilon = 0.001f;
    public bool wallsEnabled = true;

    public void EnsureClosedLoopDefault()
    {
        if (controlPoints.Count < 3)
        {
            controlPoints.Clear();
            controlPoints.Add(new Vector3(-20f, 0f, -20f));
            controlPoints.Add(new Vector3(20f, 0f, -20f));
            controlPoints.Add(new Vector3(20f, 0f, 20f));
            controlPoints.Add(new Vector3(-20f, 0f, 20f));
        }
        if (wallSections.Count == 0)
            wallSections.Add(new RoadLotWallSection { startT01 = 0f, endT01 = 1f, height = 2f });
    }

    public Vector3 CentroidLocal()
    {
        if (controlPoints.Count == 0) return Vector3.zero;
        Vector3 s = Vector3.zero;
        for (int i = 0; i < controlPoints.Count; i++)
            s += controlPoints[i];
        return s / controlPoints.Count;
    }

    public Vector3 SampleLocal(float t01)
    {
        if (controlPoints.Count == 0) return Vector3.zero;
        if (controlPoints.Count == 1) return controlPoints[0];
        t01 = Mathf.Repeat(t01, 1f);
        int n = controlPoints.Count;
        float ft = t01 * n;
        int i0 = Mathf.FloorToInt(ft) % n;
        int i1 = (i0 + 1) % n;
        float u = ft - Mathf.Floor(ft);
        return Vector3.Lerp(controlPoints[i0], controlPoints[i1], u);
    }

    public Vector3 SampleWorld(float t01) => transform.TransformPoint(SampleLocal(t01));

    /// <summary>Sum of section Length01 must equal 1. Throws if invalid.</summary>
    public void ValidateWallSections()
    {
        if (!wallsEnabled || wallSections == null || wallSections.Count == 0)
            return;
        float sum = 0f;
        for (int i = 0; i < wallSections.Count; i++)
        {
            if (wallSections[i] == null)
                throw new InvalidOperationException("RoadLot wall section " + i + " is null.");
            sum += wallSections[i].Length01;
        }
        if (Mathf.Abs(sum - 1f) > wallValidateEpsilon)
            throw new InvalidOperationException(
                "RoadLot wall section Length01 values must sum to 1 (got " + sum + "). Adjust splits before bake.");
    }

    public bool TryValidateWallSections(out string error)
    {
        try
        {
            ValidateWallSections();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Split the section covering t01 into two; rebalances so sum stays 1.</summary>
    public void SplitAt(float t01)
    {
        EnsureClosedLoopDefault();
        t01 = Mathf.Clamp01(t01);
        ValidateWallSections();
        for (int i = 0; i < wallSections.Count; i++)
        {
            var s = wallSections[i];
            bool wraps = s.endT01 < s.startT01 - 1e-5f;
            bool inside = wraps
                ? (t01 >= s.startT01 || t01 <= s.endT01)
                : (t01 > s.startT01 + 1e-4f && t01 < s.endT01 - 1e-4f);
            if (!inside) continue;
            var a = new RoadLotWallSection
            {
                startT01 = s.startT01,
                endT01 = t01,
                height = s.height,
                isGap = s.isGap,
                gateOpenCloseTopologyId = s.gateOpenCloseTopologyId,
                wallMaterial = s.wallMaterial
            };
            var b = new RoadLotWallSection
            {
                startT01 = t01,
                endT01 = s.endT01,
                height = s.height,
                isGap = s.isGap,
                gateOpenCloseTopologyId = s.gateOpenCloseTopologyId,
                wallMaterial = s.wallMaterial
            };
            wallSections[i] = a;
            wallSections.Insert(i + 1, b);
            ValidateWallSections();
            return;
        }
        throw new InvalidOperationException("Split t01=" + t01 + " did not land inside any wall section.");
    }
}
