using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Roads
{
    /// <summary>
    /// 4D road spline with Bounds4 gateway transitions (Back/Pause/Forward) for width/grade/banking.
    /// </summary>
    [AddComponentMenu("Roads/Road Spline 4D")]
    public class RoadSpline4D : RoadSplineBase
    {
        [Header("4D References")]
        public SpatialGenerator4D spatialGenerator4D;
        public SpatialGenerator4DOrchestrator orchestrator;
        public float narrativeTime;

        [Header("Gateway Blending")]
        [Range(0f, 1f)] public float gatewayBlendStrength = 1f;
        public int generatorSeed = 12345;

        readonly List<Bounds4GatewayBlend> _activeBlends = new List<Bounds4GatewayBlend>();

        struct Bounds4GatewayBlend
        {
            public Bounds4 volume;
            public float entryT;
            public float exitT;
            public float widthScale;
            public float gradeOffset;
            public float bankingOffset;
            public string leafBack;
            public string leafPause;
            public string leafForward;
        }

        SpatialGenerator4D ResolveGenerator()
        {
            if (spatialGenerator4D != null)
                return spatialGenerator4D;
            if (orchestrator != null && orchestrator.spatialGenerators != null)
            {
                foreach (var g in orchestrator.spatialGenerators)
                {
                    if (g is SpatialGenerator4D gen)
                        return gen;
                }
            }
            return FindAnyObjectByType<SpatialGenerator4D>();
        }

        public void RebuildGatewayBlends()
        {
            _activeBlends.Clear();
            var gen = ResolveGenerator();
            if (gen == null)
                return;

            var entries = gen.GetPlacedEntriesWithGatewayTermini();
            foreach (var entry in entries)
            {
                var vol = entry.volume;
                if (!SplineIntersectsBounds4(vol))
                    continue;

                float widthScale = 1f;
                float gradeOffset = 0f;
                float bankingOffset = 0f;
                if (!string.IsNullOrEmpty(entry.gateway.forward?.causalityLeafId))
                    widthScale = 1.1f;
                if (!string.IsNullOrEmpty(entry.gateway.back?.causalityLeafId))
                    gradeOffset = -2f;
                if (!string.IsNullOrEmpty(entry.gateway.pause?.causalityLeafId))
                    bankingOffset = 0f;

                _activeBlends.Add(new Bounds4GatewayBlend
                {
                    volume = vol,
                    entryT = DistanceToNormalizedT(ProjectDistanceAtTime(vol.tMin)),
                    exitT = DistanceToNormalizedT(ProjectDistanceAtTime(vol.tMax)),
                    widthScale = widthScale,
                    gradeOffset = gradeOffset,
                    bankingOffset = bankingOffset,
                    leafBack = entry.gateway.back?.causalityLeafId,
                    leafPause = entry.gateway.pause?.causalityLeafId,
                    leafForward = entry.gateway.forward?.causalityLeafId
                });
            }
        }

        bool SplineIntersectsBounds4(Bounds4 vol)
        {
            int steps = 16;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = EvaluateCatmullRom(t);
                if (vol.Contains(p, narrativeTime))
                    return true;
            }
            return false;
        }

        float ProjectDistanceAtTime(float t)
        {
            float norm = Mathf.Clamp01((t - narrativeTime + 1f) * 0.5f);
            return norm * GetTotalLength();
        }

        public override float GetWidthAtNormalizedT(float t)
        {
            float w = base.GetWidthAtNormalizedT(t);
            ApplyGatewayBlend(t, ref w);
            return w;
        }

        public float GetGradeAtNormalizedTWithGateway(float t)
        {
            float grade = GetGradeAtNormalizedT(t);
            ComputeGatewayOffsets(t, out float gradeOff, out _);
            return grade + gradeOff;
        }

        public float GetBankingAtNormalizedTWithGateway(float t)
        {
            float bank = GetBankingAtNormalizedT(t);
            ComputeGatewayOffsets(t, out _, out float bankOff);
            return bank + bankOff;
        }

        void ApplyGatewayBlend(float t, ref float width)
        {
            if (_activeBlends.Count == 0)
                RebuildGatewayBlends();

            foreach (var blend in _activeBlends)
            {
                if (t < blend.entryT || t > blend.exitT)
                    continue;
                float w = GatewayWeight(t, blend.entryT, blend.exitT);
                width *= Mathf.Lerp(1f, blend.widthScale, w);
            }
        }

        void ComputeGatewayOffsets(float t, out float gradeOff, out float bankOff)
        {
            gradeOff = 0f;
            bankOff = 0f;
            if (_activeBlends.Count == 0)
                RebuildGatewayBlends();

            foreach (var blend in _activeBlends)
            {
                if (t < blend.entryT || t > blend.exitT)
                    continue;
                float w = GatewayWeight(t, blend.entryT, blend.exitT);
                gradeOff += blend.gradeOffset * w;
                bankOff += blend.bankingOffset * w;
            }
        }

        float GatewayWeight(float t, float entryT, float exitT)
        {
            float mid = (entryT + exitT) * 0.5f;
            float w = t < mid
                ? Mathf.InverseLerp(entryT, mid, t)
                : Mathf.InverseLerp(exitT, mid, t);
            return Mathf.Clamp01(w) * gatewayBlendStrength;
        }

        public RoadSpline4DSnapshot ExportSnapshot()
        {
            RebuildGatewayBlends();
            var snap = new RoadSpline4DSnapshot
            {
                seed = generatorSeed,
                narrativeTime = narrativeTime,
                controlPoints = new List<Vector3>(controlPoints),
                defaultWidth = defaultWidth,
                gradeSlope = gradeSlope,
                widthCurve = widthCurve != null ? new AnimationCurve(widthCurve.keys) : AnimationCurve.Constant(0f, 1f, 1f),
                gradeCurve = gradeCurve != null ? new AnimationCurve(gradeCurve.keys) : AnimationCurve.Constant(0f, 1f, 0f),
                bankingCurve = bankingCurve != null ? new AnimationCurve(bankingCurve.keys) : AnimationCurve.Constant(0f, 1f, 0f),
                roadSegmentId = System.Guid.NewGuid().ToString("N").Substring(0, 8)
            };

            foreach (var blend in _activeBlends)
            {
                snap.gatewayLeafBack.Add(blend.leafBack);
                snap.gatewayLeafPause.Add(blend.leafPause);
                snap.gatewayLeafForward.Add(blend.leafForward);
            }
            return snap;
        }

        public void BakeTo3D(RoadSpline3D target)
        {
            if (target == null)
                return;
            target.ApplySnapshot(ExportSnapshot());
            target.RebuildBakedSamples(splineResolution);
        }

        public void OnNarrativeTimeJump(float newTime)
        {
            narrativeTime = newTime;
            RebuildGatewayBlends();
        }
    }
}
