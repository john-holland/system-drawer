using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    /// <summary>
    /// Catmull-Rom spline road path with width, grade, and banking curves.
    /// Extends the RiverSpline pattern with smooth interpolation and Frenet frames.
    /// </summary>
    public class RoadSplineBase : MonoBehaviour
    {
        [Header("Control Points")]
        public List<Vector3> controlPoints = new List<Vector3>();

        [Header("Segmentation")]
        public bool useNestedSegments;
        public List<Transform> segmentTransforms = new List<Transform>();

        [Header("Dimensions")]
        public float defaultWidth = 6f;
        public float gradeSlope = 0f;

        [Header("Curves (normalized arc-length t in [0,1])")]
        public AnimationCurve widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve gradeCurve = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve bankingCurve = AnimationCurve.Constant(0f, 1f, 0f);

        [Header("Sampling")]
        public float splineResolution = 0.5f;

        [Header("Gizmos")]
        public bool showGizmos = true;
        public float arrowSpacing = 5f;

        float[] _cumulativeLengths;
        float _totalLength;

        void Awake()
        {
            if (useNestedSegments)
                CollectSegmentTransforms();
            if (controlPoints.Count == 0)
                GenerateControlPointsFromSegments();
            RebuildLengthTable();
        }

        void OnValidate()
        {
            if (splineResolution < 0.05f)
                splineResolution = 0.05f;
        }

        public void CollectSegmentTransforms()
        {
            segmentTransforms.Clear();
            foreach (Transform child in transform)
                segmentTransforms.Add(child);
        }

        public void GenerateControlPointsFromSegments()
        {
            if (segmentTransforms.Count == 0)
                return;
            controlPoints.Clear();
            foreach (var segment in segmentTransforms)
            {
                if (segment != null)
                    controlPoints.Add(segment.position);
            }
        }

        public void RebuildLengthTable()
        {
            if (controlPoints.Count < 2)
            {
                _cumulativeLengths = null;
                _totalLength = 0f;
                return;
            }

            int sampleCount = Mathf.Max(8, Mathf.CeilToInt(EstimateCurveLength() / Mathf.Max(0.1f, splineResolution)));
            _cumulativeLengths = new float[sampleCount];
            _cumulativeLengths[0] = 0f;
            Vector3 prev = EvaluateCatmullRom(0f);
            for (int i = 1; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                Vector3 p = EvaluateCatmullRom(t);
                _cumulativeLengths[i] = _cumulativeLengths[i - 1] + Vector3.Distance(prev, p);
                prev = p;
            }
            _totalLength = _cumulativeLengths[sampleCount - 1];
        }

        float EstimateCurveLength()
        {
            float len = 0f;
            for (int i = 0; i < controlPoints.Count - 1; i++)
                len += Vector3.Distance(controlPoints[i], controlPoints[i + 1]);
            return len;
        }

        public float GetTotalLength()
        {
            if (_cumulativeLengths == null || _totalLength <= 0f)
                RebuildLengthTable();
            return _totalLength;
        }

        public float DistanceToNormalizedT(float distance)
        {
            float total = GetTotalLength();
            if (total <= 1e-6f)
                return 0f;
            return Mathf.Clamp01(distance / total);
        }

        public Vector3 EvaluateCatmullRom(float normalizedT)
        {
            if (controlPoints.Count == 0)
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

        public Vector3 EvaluateTangent(float normalizedT)
        {
            const float dt = 0.001f;
            float t0 = Mathf.Clamp01(normalizedT - dt);
            float t1 = Mathf.Clamp01(normalizedT + dt);
            Vector3 a = EvaluateCatmullRom(t0);
            Vector3 b = EvaluateCatmullRom(t1);
            Vector3 tan = (b - a).normalized;
            return tan.sqrMagnitude > 1e-8f ? tan : transform.forward;
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

        public virtual float GetWidthAtNormalizedT(float t)
        {
            return defaultWidth * widthCurve.Evaluate(Mathf.Clamp01(t));
        }

        public float GetGradeAtNormalizedT(float t)
        {
            return gradeCurve.Evaluate(Mathf.Clamp01(t)) + gradeSlope;
        }

        public float GetBankingAtNormalizedT(float t)
        {
            return bankingCurve.Evaluate(Mathf.Clamp01(t));
        }

        public RoadSplineSample GetSampleAtDistance(float distance)
        {
            float total = GetTotalLength();
            float normT = DistanceToNormalizedT(distance);
            Vector3 pos = EvaluateCatmullRom(normT);
            Vector3 tangent = EvaluateTangent(normT);
            ComputeFrenetFrame(tangent, normT, out Vector3 normal, out Vector3 binormal);

            float banking = GetBankingAtNormalizedT(normT);
            Quaternion bankRot = Quaternion.AngleAxis(banking, tangent);
            normal = bankRot * normal;
            binormal = bankRot * binormal;

            float grade = GetGradeAtNormalizedT(normT);
            pos += normal * (Mathf.Tan(grade * Mathf.Deg2Rad) * defaultWidth * 0.25f);

            return new RoadSplineSample
            {
                distance = distance,
                normalizedT = normT,
                position = pos,
                tangent = tangent,
                normal = normal,
                binormal = binormal,
                width = GetWidthAtNormalizedT(normT),
                gradeDegrees = grade,
                bankingDegrees = banking,
                heightOffset = 0f
            };
        }

        public RoadSplineSample[] BuildSamples(float spacingMeters)
        {
            float total = GetTotalLength();
            if (total <= 1e-4f || controlPoints.Count < 2)
                return System.Array.Empty<RoadSplineSample>();

            spacingMeters = Mathf.Max(0.1f, spacingMeters);
            int count = Mathf.Max(2, Mathf.CeilToInt(total / spacingMeters) + 1);
            var samples = new RoadSplineSample[count];
            for (int i = 0; i < count; i++)
            {
                float d = i == count - 1 ? total : i * spacingMeters;
                samples[i] = GetSampleAtDistance(d);
            }
            return samples;
        }

        void ComputeFrenetFrame(Vector3 tangent, float normT, out Vector3 normal, out Vector3 binormal)
        {
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangent, up)) > 0.95f)
                up = Vector3.forward;
            binormal = Vector3.Cross(up, tangent).normalized;
            if (binormal.sqrMagnitude < 1e-6f)
                binormal = Vector3.right;
            normal = Vector3.Cross(tangent, binormal).normalized;

            // Refine normal from curvature
            const float dt = 0.005f;
            Vector3 t0 = EvaluateTangent(Mathf.Clamp01(normT - dt));
            Vector3 t1 = EvaluateTangent(Mathf.Clamp01(normT + dt));
            Vector3 curvature = (t1 - t0).normalized;
            if (curvature.sqrMagnitude > 1e-6f)
            {
                normal = Vector3.Cross(curvature, tangent).normalized;
                if (normal.sqrMagnitude > 1e-6f)
                    binormal = Vector3.Cross(tangent, normal).normalized;
            }
        }

        public Vector3 GetPositionAtDistance(float distance) => GetSampleAtDistance(distance).position;
        public Vector3 GetTangentAtDistance(float distance) => GetSampleAtDistance(distance).tangent;
        public float GetWidthAtDistance(float distance) => GetSampleAtDistance(distance).width;

        void OnDrawGizmos()
        {
            if (!showGizmos || controlPoints.Count < 2)
                return;

            Gizmos.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);
            int steps = 32;
            Vector3 prev = EvaluateCatmullRom(0f);
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = EvaluateCatmullRom(t);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }

            float total = GetTotalLength();
            if (total <= 0f)
                return;

            float dist = 0f;
            while (dist < total)
            {
                var sample = GetSampleAtDistance(dist);
                Vector3 perp = sample.binormal;
                Gizmos.color = new Color(0.9f, 0.85f, 0.2f, 0.7f);
                Gizmos.DrawLine(
                    sample.position - perp * sample.width * 0.5f,
                    sample.position + perp * sample.width * 0.5f);
                dist += arrowSpacing;
            }
        }
    }
}
