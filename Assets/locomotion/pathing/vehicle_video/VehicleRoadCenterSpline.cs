using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored road-center Catmull-Rom for video steering (does not fork civil RoadNetwork).
/// First control point is start, last is end, interior points are handles.
/// </summary>
[AddComponentMenu("Locomotion/Vehicle/Vehicle Road Center Spline")]
public sealed class VehicleRoadCenterSpline : MonoBehaviour
{
    [Tooltip("World-space control points. Index 0 = start, last = end.")]
    public List<Vector3> controlPoints = new List<Vector3>();

    [Tooltip("Arc-length sample spacing used to rebuild the length table.")]
    public float splineResolution = 0.5f;

    public bool showGizmos = true;
    public float tickSpacing = 2f;

    float[] _cumulativeLengths;
    float _totalLength;

    void Reset()
    {
        Vector3 p = transform.position;
        controlPoints = new List<Vector3>
        {
            p,
            p + transform.forward * 8f,
            p + transform.forward * 16f
        };
        RebuildLengthTable();
    }

    void OnValidate()
    {
        if (splineResolution < 0.05f)
            splineResolution = 0.05f;
        if (controlPoints != null && controlPoints.Count >= 2)
            RebuildLengthTable();
    }

    void Awake() => RebuildLengthTable();

    public int Count => controlPoints != null ? controlPoints.Count : 0;

    public float GetTotalLength()
    {
        if (_cumulativeLengths == null || _totalLength <= 0f)
            RebuildLengthTable();
        return _totalLength;
    }

    public void RebuildLengthTable()
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            _cumulativeLengths = null;
            _totalLength = 0f;
            return;
        }

        int sampleCount = Mathf.Max(8, Mathf.CeilToInt(EstimatePolylineLength() / Mathf.Max(0.1f, splineResolution)));
        _cumulativeLengths = new float[sampleCount];
        _cumulativeLengths[0] = 0f;
        Vector3 prev = EvaluateCatmullRom(0f);
        for (int i = 1; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            Vector3 p = EvaluateCatmullRom(t);
            _cumulativeLengths[i] = _cumulativeLengths[i - 1] + Vector3.Distance(prev, p);
            prev = p;
        }
        _totalLength = _cumulativeLengths[sampleCount - 1];
    }

    float EstimatePolylineLength()
    {
        float len = 0f;
        for (int i = 0; i < controlPoints.Count - 1; i++)
            len += Vector3.Distance(controlPoints[i], controlPoints[i + 1]);
        return Mathf.Max(len, 1f);
    }

    /// <summary>Sample by normalized t in [0,1].</summary>
    public Vector3 Sample(float t) => EvaluateCatmullRom(t);

    public Vector3 EvaluateTangent(float normalizedT)
    {
        const float dt = 0.001f;
        float t0 = Mathf.Clamp01(normalizedT - dt);
        float t1 = Mathf.Clamp01(normalizedT + dt);
        Vector3 a = EvaluateCatmullRom(t0);
        Vector3 b = EvaluateCatmullRom(t1);
        Vector3 tan = (b - a);
        tan.y = 0f;
        return tan.sqrMagnitude > 1e-8f ? tan.normalized : transform.forward;
    }

    public Vector3 EvaluateCatmullRom(float normalizedT)
    {
        if (controlPoints == null || controlPoints.Count == 0)
            return transform.position;
        if (controlPoints.Count == 1)
            return controlPoints[0];

        float t = Mathf.Clamp01(normalizedT);
        int segmentCount = controlPoints.Count - 1;
        float scaled = t * segmentCount;
        int seg = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
        float localT = scaled - seg;

        Vector3 p0 = controlPoints[Mathf.Max(seg - 1, 0)];
        Vector3 p1 = controlPoints[seg];
        Vector3 p2 = controlPoints[Mathf.Min(seg + 1, controlPoints.Count - 1)];
        Vector3 p3 = controlPoints[Mathf.Min(seg + 2, controlPoints.Count - 1)];
        return CatmullRom(p0, p1, p2, p3, localT);
    }

    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    public struct Projection
    {
        public float s;
        public float t01;
        public Vector3 point;
        public Vector3 tangent;
        public float lateral;
    }

    /// <summary>Nearest point on the spline (XZ). Returns arc-length s and tangent.</summary>
    public Projection Project(Vector3 worldPoint)
    {
        RebuildLengthTable();
        float total = GetTotalLength();
        if (controlPoints == null || controlPoints.Count < 2 || total <= 1e-4f)
        {
            Vector3 p = controlPoints != null && controlPoints.Count > 0 ? controlPoints[0] : transform.position;
            return new Projection { s = 0f, t01 = 0f, point = p, tangent = transform.forward };
        }

        int samples = Mathf.Max(16, Mathf.CeilToInt(total / Mathf.Max(0.25f, splineResolution)));
        float bestD = float.MaxValue;
        float bestT = 0f;
        Vector3 bestP = controlPoints[0];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            Vector3 q = EvaluateCatmullRom(t);
            float dx = q.x - worldPoint.x;
            float dz = q.z - worldPoint.z;
            float d = dx * dx + dz * dz;
            if (d < bestD)
            {
                bestD = d;
                bestT = t;
                bestP = q;
            }
        }

        Vector3 tan = EvaluateTangent(bestT);
        Vector3 bin = Vector3.Cross(Vector3.up, tan);
        if (bin.sqrMagnitude < 1e-6f) bin = Vector3.right;
        bin.Normalize();
        float lat = Vector3.Dot(worldPoint - bestP, bin);
        return new Projection
        {
            s = bestT * total,
            t01 = bestT,
            point = bestP,
            tangent = tan,
            lateral = lat
        };
    }

    public Vector3 SampleAtArcLength(float s)
    {
        float total = GetTotalLength();
        if (total <= 1e-6f)
            return Sample(0f);
        return Sample(Mathf.Clamp01(s / total));
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || controlPoints == null || controlPoints.Count < 2)
            return;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.95f);
        Vector3 prev = EvaluateCatmullRom(0f);
        const int n = 32;
        for (int i = 1; i <= n; i++)
        {
            Vector3 p = EvaluateCatmullRom(i / (float)n);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(controlPoints[0], 0.35f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(controlPoints[controlPoints.Count - 1], 0.35f);
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 1f);
        for (int i = 1; i < controlPoints.Count - 1; i++)
            Gizmos.DrawSphere(controlPoints[i], 0.22f);

        float total = GetTotalLength();
        if (tickSpacing > 0.1f && total > tickSpacing)
        {
            Gizmos.color = new Color(0.9f, 0.9f, 0.4f, 0.8f);
            for (float s = tickSpacing; s < total; s += tickSpacing)
            {
                float t = s / total;
                Vector3 p = EvaluateCatmullRom(t);
                Vector3 tan = EvaluateTangent(t);
                Vector3 side = Vector3.Cross(Vector3.up, tan).normalized * 0.4f;
                Gizmos.DrawLine(p - side, p + side);
            }
        }
    }
}
