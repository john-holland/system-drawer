using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlanarSplineGranularityMode
{
    Division = 0,
    PerLength = 1
}

[Serializable]
public sealed class PlanarSplinePathPlane
{
    public float tStart;
    public float tEnd;
    public float halfWidth = 0.5f;
    public string hierarchicalPlaneId;
    public Vector3 center;
    public Vector3 normal = Vector3.up;
    public Vector3 tangent = Vector3.forward;
    public Vector3 binormal = Vector3.right;

    public float MidT01 => (tStart + tEnd) * 0.5f;
    public float Length01 => Mathf.Max(0f, tEnd - tStart);
}

[Serializable]
public sealed class PlanarSplineCustomSection
{
    [Range(0f, 1f)] public float startT01;
    [Range(0f, 1f)] public float endT01 = 0.1f;
    public float width = 1f;
    public string hierarchicalPlaneId;
    public Vector3 gizmoLocalPosition;
    public Vector3 gizmoLocalEuler;
    public Vector3 gizmoLocalScale = Vector3.one;
    [NonSerialized] public Transform gizmoTransform;
}

/// <summary>Walkable ribbon of planes along a Catmull-Rom spline — aisles, branches, ledges.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Pathing/Planar Spline Path Locomotion")]
public sealed class PlanarSplinePathLocomotion : MonoBehaviour
{
    public const string RebuildNarrativeAction = "planar_spline_rebuild";

    [Header("Spline")]
    public List<Vector3> controlPoints = new List<Vector3>();
    public float defaultWidth = 1.2f;

    [Header("Granularity")]
    public PlanarSplineGranularityMode granularity = PlanarSplineGranularityMode.Division;
    [Min(1)] public int divisionStopCount = 8;
    [Min(0.1f)] public float perLengthMeters = 2f;

    [Header("Custom sections (override auto on overlap)")]
    public List<PlanarSplineCustomSection> customSections = new List<PlanarSplineCustomSection>();

    [Header("Ledge guard")]
    public bool blockFallUnlessJump;
    [Tooltip("Invisible wall height to jump over (no mantling). Default 0.")]
    public float jumpWallHeight;
    public float wallThickness = 0.08f;

    [Header("Output")]
    public List<PlanarSplinePathPlane> planes = new List<PlanarSplinePathPlane>();

    readonly List<BoxCollider> _ledgeWalls = new List<BoxCollider>();
    float[] _cumulativeLengths;
    float _totalLength;

    void Awake() => Rebuild();

    void OnValidate()
    {
        if (divisionStopCount < 1) divisionStopCount = 1;
        if (perLengthMeters < 0.1f) perLengthMeters = 0.1f;
    }

    public void OnNarrativeSchedulerAction(string actionId)
    {
        if (string.Equals(actionId, RebuildNarrativeAction, StringComparison.OrdinalIgnoreCase))
            Rebuild();
    }

    public float GetTotalLength()
    {
        RebuildLengthTable();
        return _totalLength;
    }

    public void Rebuild()
    {
        RebuildLengthTable();
        planes.Clear();
        if (controlPoints == null || controlPoints.Count < 2 || _totalLength <= 1e-4f)
        {
            SyncLedgeWalls();
            return;
        }

        var auto = BuildAutoPlanes();
        planes.AddRange(MergeCustomOverAuto(auto));
        SyncLedgeWalls();
    }

    List<PlanarSplinePathPlane> BuildAutoPlanes()
    {
        var list = new List<PlanarSplinePathPlane>();
        int count;
        if (granularity == PlanarSplineGranularityMode.Division)
            count = Mathf.Max(1, divisionStopCount);
        else
            count = Mathf.Max(1, Mathf.CeilToInt(_totalLength / perLengthMeters));

        for (int i = 0; i < count; i++)
        {
            float t0 = i / (float)count;
            float t1 = (i + 1) / (float)count;
            list.Add(MakePlane(t0, t1, defaultWidth, "auto_" + i));
        }
        return list;
    }

    List<PlanarSplinePathPlane> MergeCustomOverAuto(List<PlanarSplinePathPlane> auto)
    {
        if (customSections == null || customSections.Count == 0)
            return auto;

        var result = new List<PlanarSplinePathPlane>();
        for (int i = 0; i < auto.Count; i++)
        {
            var a = auto[i];
            bool covered = false;
            for (int c = 0; c < customSections.Count; c++)
            {
                var cs = customSections[c];
                if (cs == null) continue;
                float s = Mathf.Min(cs.startT01, cs.endT01);
                float e = Mathf.Max(cs.startT01, cs.endT01);
                if (a.MidT01 >= s && a.MidT01 <= e)
                {
                    covered = true;
                    break;
                }
            }
            if (!covered)
                result.Add(a);
        }

        for (int c = 0; c < customSections.Count; c++)
        {
            var cs = customSections[c];
            if (cs == null) continue;
            float s = Mathf.Clamp01(Mathf.Min(cs.startT01, cs.endT01));
            float e = Mathf.Clamp01(Mathf.Max(cs.startT01, cs.endT01));
            if (e - s < 1e-4f) e = Mathf.Min(1f, s + 0.01f);
            float width = cs.width > 1e-3f ? cs.width : defaultWidth;
            if (cs.gizmoLocalScale.x > 1e-3f)
                width = Mathf.Abs(cs.gizmoLocalScale.x) * defaultWidth;
            var plane = MakePlane(s, e, width, string.IsNullOrEmpty(cs.hierarchicalPlaneId)
                ? "custom_" + c
                : cs.hierarchicalPlaneId);
            if (cs.gizmoLocalEuler.sqrMagnitude > 1e-6f)
            {
                Quaternion q = Quaternion.Euler(cs.gizmoLocalEuler);
                plane.normal = q * plane.normal;
                plane.tangent = q * plane.tangent;
                plane.binormal = Vector3.Cross(plane.normal, plane.tangent).normalized;
            }
            if (cs.gizmoLocalPosition.sqrMagnitude > 1e-8f)
                plane.center += transform.TransformVector(cs.gizmoLocalPosition);
            result.Add(plane);
        }

        result.Sort((a, b) => a.tStart.CompareTo(b.tStart));
        return result;
    }

    PlanarSplinePathPlane MakePlane(float t0, float t1, float width, string id)
    {
        float mid = (t0 + t1) * 0.5f;
        Vector3 pos = Evaluate(mid);
        Vector3 tan = EvaluateTangent(mid);
        Vector3 bin = Vector3.Cross(Vector3.up, tan).normalized;
        if (bin.sqrMagnitude < 1e-6f) bin = Vector3.right;
        Vector3 nrm = Vector3.Cross(tan, bin).normalized;
        return new PlanarSplinePathPlane
        {
            tStart = t0,
            tEnd = t1,
            halfWidth = Mathf.Max(0.05f, width * 0.5f),
            hierarchicalPlaneId = id,
            center = pos,
            normal = nrm,
            tangent = tan,
            binormal = bin
        };
    }

    public bool TryProject(Vector3 worldPoint, out Vector3 onPlane, out PlanarSplinePathPlane plane)
    {
        onPlane = worldPoint;
        plane = null;
        if (planes == null || planes.Count == 0) return false;
        float best = float.MaxValue;
        for (int i = 0; i < planes.Count; i++)
        {
            var p = planes[i];
            if (p == null) continue;
            Vector3 local = worldPoint - p.center;
            float along = Vector3.Dot(local, p.tangent);
            float side = Vector3.Dot(local, p.binormal);
            float halfLen = Mathf.Max(0.05f, p.Length01 * GetTotalLength() * 0.5f);
            along = Mathf.Clamp(along, -halfLen, halfLen);
            side = Mathf.Clamp(side, -p.halfWidth, p.halfWidth);
            Vector3 candidate = p.center + p.tangent * along + p.binormal * side;
            float d = (candidate - worldPoint).sqrMagnitude;
            if (d < best)
            {
                best = d;
                onPlane = candidate;
                plane = p;
            }
        }
        return plane != null;
    }

    public Vector3 ClampToPath(Vector3 worldPoint) =>
        TryProject(worldPoint, out var on, out _) ? on : worldPoint;

    void SyncLedgeWalls()
    {
        for (int i = 0; i < _ledgeWalls.Count; i++)
            if (_ledgeWalls[i] != null)
                DestroyImmediateSafe(_ledgeWalls[i].gameObject);
        _ledgeWalls.Clear();

        if (!blockFallUnlessJump || jumpWallHeight <= 1e-4f || planes == null)
            return;

        for (int i = 0; i < planes.Count; i++)
        {
            var p = planes[i];
            if (p == null) continue;
            float len = Mathf.Max(0.2f, p.Length01 * GetTotalLength());
            SpawnWall(p, +1, len);
            SpawnWall(p, -1, len);
        }
    }

    void SpawnWall(PlanarSplinePathPlane p, int sideSign, float length)
    {
        var go = new GameObject("LedgeWall_" + p.hierarchicalPlaneId + "_" + (sideSign > 0 ? "R" : "L"));
        go.transform.SetParent(transform, false);
        Vector3 pos = p.center + p.binormal * (p.halfWidth * sideSign);
        go.transform.position = pos + p.normal * (jumpWallHeight * 0.5f);
        go.transform.rotation = Quaternion.LookRotation(p.tangent, p.normal);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(wallThickness, jumpWallHeight, length);
        box.isTrigger = false;
        _ledgeWalls.Add(box);
    }

    static void DestroyImmediateSafe(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(go);
        else UnityEngine.Object.DestroyImmediate(go);
    }

    void RebuildLengthTable()
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            _cumulativeLengths = null;
            _totalLength = 0f;
            return;
        }
        int sampleCount = Mathf.Max(8, controlPoints.Count * 8);
        _cumulativeLengths = new float[sampleCount];
        _cumulativeLengths[0] = 0f;
        Vector3 prev = EvaluateCatmull(0f);
        for (int i = 1; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            Vector3 p = EvaluateCatmull(t);
            _cumulativeLengths[i] = _cumulativeLengths[i - 1] + Vector3.Distance(prev, p);
            prev = p;
        }
        _totalLength = _cumulativeLengths[sampleCount - 1];
    }

    public Vector3 Evaluate(float normalizedT) => EvaluateCatmull(Mathf.Clamp01(normalizedT));

    public Vector3 EvaluateTangent(float normalizedT)
    {
        const float dt = 0.001f;
        Vector3 a = EvaluateCatmull(Mathf.Clamp01(normalizedT - dt));
        Vector3 b = EvaluateCatmull(Mathf.Clamp01(normalizedT + dt));
        Vector3 t = (b - a).normalized;
        return t.sqrMagnitude > 1e-8f ? t : transform.forward;
    }

    Vector3 EvaluateCatmull(float normalizedT)
    {
        if (controlPoints == null || controlPoints.Count == 0)
            return transform.position;
        if (controlPoints.Count == 1)
            return transform.TransformPoint(controlPoints[0]);

        float t = Mathf.Clamp01(normalizedT);
        int segmentCount = controlPoints.Count - 1;
        float scaled = t * segmentCount;
        int seg = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
        float localT = scaled - seg;
        Vector3 p0 = controlPoints[Mathf.Max(seg - 1, 0)];
        Vector3 p1 = controlPoints[seg];
        Vector3 p2 = controlPoints[Mathf.Min(seg + 1, controlPoints.Count - 1)];
        Vector3 p3 = controlPoints[Mathf.Min(seg + 2, controlPoints.Count - 1)];
        return transform.TransformPoint(CatmullRom(p0, p1, p2, p3, localT));
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

#if UNITY_EDITOR
    public void ApplyGizmoSave(int sectionIndex)
    {
        if (customSections == null || sectionIndex < 0 || sectionIndex >= customSections.Count) return;
        var cs = customSections[sectionIndex];
        if (cs?.gizmoTransform == null) return;
        cs.gizmoLocalPosition = cs.gizmoTransform.localPosition;
        cs.gizmoLocalEuler = cs.gizmoTransform.localEulerAngles;
        cs.gizmoLocalScale = cs.gizmoTransform.localScale;
        Rebuild();
    }

    public void ApplyGizmoRevert(int sectionIndex, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        if (customSections == null || sectionIndex < 0 || sectionIndex >= customSections.Count) return;
        var cs = customSections[sectionIndex];
        if (cs == null) return;
        cs.gizmoLocalPosition = pos;
        cs.gizmoLocalEuler = euler;
        cs.gizmoLocalScale = scale;
        if (cs.gizmoTransform != null)
        {
            cs.gizmoTransform.localPosition = pos;
            cs.gizmoTransform.localEulerAngles = euler;
            cs.gizmoTransform.localScale = scale;
        }
        Rebuild();
    }
#endif
}

/// <summary>Optional clamp for TravelAgent / ambulation onto a planar spline path.</summary>
public interface IPlanarPathConstraint
{
    Vector3 ClampToPath(Vector3 worldPoint);
    bool TryProject(Vector3 worldPoint, out Vector3 onPlane, out PlanarSplinePathPlane plane);
}
