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

    [Header("Bake")]
    public MeshFilter wallMeshFilter;
    public MeshCollider wallMeshCollider;
    public float wallThickness = 0.15f;
    public int bakeSamplesPerSection = 16;
    public Mesh bakedWallMesh;

    /// <summary>Bake non-gap wall sections into a mesh + collider. Gaps = no wall / gate topology id.</summary>
    public Mesh BakeWallMesh()
    {
        EnsureClosedLoopDefault();
        ValidateWallSections();
        if (!wallsEnabled)
        {
            ClearBakedWall();
            return null;
        }

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        float half = Mathf.Max(0.02f, wallThickness * 0.5f);

        for (int s = 0; s < wallSections.Count; s++)
        {
            var sec = wallSections[s];
            if (sec == null || sec.isGap) continue;
            int samples = Mathf.Max(2, bakeSamplesPerSection);
            float len = sec.Length01;
            for (int i = 0; i < samples; i++)
            {
                float u0 = i / (float)samples;
                float u1 = (i + 1) / (float)samples;
                float t0 = SampleSectionT(sec, u0);
                float t1 = SampleSectionT(sec, u1);
                Vector3 a = SampleLocal(t0);
                Vector3 b = SampleLocal(t1);
                Vector3 tangent = (b - a);
                if (tangent.sqrMagnitude < 1e-8f) continue;
                tangent.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
                float h = Mathf.Max(0.1f, sec.height);

                Vector3 bl = a - right * half;
                Vector3 br = a + right * half;
                Vector3 tl = bl + Vector3.up * h;
                Vector3 tr = br + Vector3.up * h;
                Vector3 bl2 = b - right * half;
                Vector3 br2 = b + right * half;
                Vector3 tl2 = bl2 + Vector3.up * h;
                Vector3 tr2 = br2 + Vector3.up * h;

                int baseIdx = verts.Count;
                verts.Add(bl); verts.Add(br); verts.Add(tr); verts.Add(tl);
                verts.Add(bl2); verts.Add(br2); verts.Add(tr2); verts.Add(tl2);
                // outer face
                tris.Add(baseIdx); tris.Add(baseIdx + 3); tris.Add(baseIdx + 7);
                tris.Add(baseIdx); tris.Add(baseIdx + 7); tris.Add(baseIdx + 4);
                // inner face
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 5); tris.Add(baseIdx + 6);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 6); tris.Add(baseIdx + 2);
                // top
                tris.Add(baseIdx + 3); tris.Add(baseIdx + 2); tris.Add(baseIdx + 6);
                tris.Add(baseIdx + 3); tris.Add(baseIdx + 6); tris.Add(baseIdx + 7);
                for (int u = 0; u < 8; u++)
                    uvs.Add(new Vector2(u0 + (u >= 4 ? len / samples : 0f), u % 2 == 0 ? 0f : 1f));
            }
        }

        if (bakedWallMesh == null)
            bakedWallMesh = new Mesh { name = "RoadLotWallBake" };
        else
            bakedWallMesh.Clear();
        bakedWallMesh.SetVertices(verts);
        bakedWallMesh.SetTriangles(tris, 0);
        bakedWallMesh.SetUVs(0, uvs);
        bakedWallMesh.RecalculateNormals();
        bakedWallMesh.RecalculateBounds();

        if (wallMeshFilter == null)
            wallMeshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        wallMeshFilter.sharedMesh = bakedWallMesh;
        if (wallMeshCollider == null)
            wallMeshCollider = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        wallMeshCollider.sharedMesh = null;
        wallMeshCollider.sharedMesh = bakedWallMesh;
        return bakedWallMesh;
    }

    public void ClearBakedWall()
    {
        if (wallMeshFilter != null) wallMeshFilter.sharedMesh = null;
        if (wallMeshCollider != null) wallMeshCollider.sharedMesh = null;
        if (bakedWallMesh != null)
            bakedWallMesh.Clear();
    }

    static float SampleSectionT(RoadLotWallSection sec, float u01)
    {
        float a = Mathf.Repeat(sec.startT01, 1f);
        float b = sec.endT01;
        if (a <= 1e-6f && b >= 1f - 1e-6f)
            return Mathf.Lerp(0f, 1f, u01);
        float len = b - a;
        if (len <= 0f) len += 1f;
        return Mathf.Repeat(a + len * Mathf.Clamp01(u01), 1f);
    }
}
