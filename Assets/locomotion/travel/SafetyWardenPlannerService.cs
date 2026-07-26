using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gates plans to the risk/safety band; prefers walk/open over window crash; hard crowd risk inflation.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Safety Warden Planner Service")]
public sealed class SafetyWardenPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "safety_warden";

    [Range(0f, 1f)] public float hardCrowdRiskWeight = 0.55f;
    [Range(0f, 1f)] public float crashThroughPenalty = 0.35f;

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
        if (_ctx?.apertures == null) return;
        for (int i = 0; i < _ctx.apertures.Length; i++)
        {
            if (_ctx.apertures[i] != null)
                ApertureCrowdSampler.Refresh(_ctx.apertures[i], _ctx.crowdSampleRadius);
        }
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (seg.mode == TravelLegMode.Acrobatics)
            r = Mathf.Clamp01(r + 0.1f);
        if (!string.IsNullOrEmpty(seg.apertureId) && ctx?.apertures != null)
        {
            for (int i = 0; i < ctx.apertures.Length; i++)
            {
                var a = ctx.apertures[i];
                if (a == null || !string.Equals(a.apertureId, seg.apertureId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                float crowd = ApertureCrowdSampler.GetOccupancy01(a);
                r = Mathf.Clamp01(r + crowd * hardCrowdRiskWeight);
                if (a.passMode == PathingAperturePassMode.CrashThrough)
                    r = Mathf.Clamp01(r + crashThroughPenalty);
                if (a.tags != null)
                {
                    for (int t = 0; t < a.tags.Count; t++)
                    {
                        if (string.Equals(a.tags[t], "window", System.StringComparison.OrdinalIgnoreCase))
                            r = Mathf.Clamp01(r + 0.4f);
                    }
                }
                break;
            }
        }
        return r;
    }

    public GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        if (plan?.segments == null) return plan;
        var band = TravelRiskBand.Resolve(in hints);
        var ctx = _ctx ?? StuntDiscoveryContext.FromScene(Vector3.zero, Vector3.zero, null);
        EnrichDiscovery(ctx);

        if (plan.rejectedForks == null)
            plan.rejectedForks = new List<MultiModalSegment>();

        var kept = new List<MultiModalSegment>();
        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            float risk = EstimateSegmentRisk(seg, ctx);
            var totals = seg.runningTotals;
            totals.risk = risk;
            seg.runningTotals = totals;

            if (band.Contains(risk))
            {
                kept.Add(seg);
                continue;
            }

            plan.rejectedForks.Add(seg.CloneShallowRefs());
            // Rewrite crash/acrobatics out-of-band to walk approach when possible
            if (seg.mode == TravelLegMode.Acrobatics || seg.mode == TravelLegMode.ToolBridge)
            {
                Vector3 from = seg.waypoints != null && seg.waypoints.Count > 0 ? seg.waypoints[0] : ctx.startWorld;
                Vector3 to = seg.waypoints != null && seg.waypoints.Count > 1
                    ? seg.waypoints[seg.waypoints.Count - 1]
                    : seg.segmentEnd;
                var walk = MultiModalSegment.FromWalk(new List<Vector3> { from, to });
                walk.runningTotals = new TravelPlanRunningTotals
                {
                    power = 0.1f,
                    spring = 0.95f,
                    damage = 0f,
                    risk = band.ClampRisk(0.05f),
                    radialTurningPotential = 0.9f
                };
                walk.animationGroupTag = ParkourAnimationGroup.LopingStrides;
                if (band.Contains(walk.runningTotals.risk))
                    kept.Add(walk);
            }
        }

        plan.segments = kept;
        // If plan risk still below minRisk, leave as-is (Stuntman should have biased); clamp totals into band for reporting
        plan.RecomputePlanTotals();
        if (!band.Contains(plan.planTotals.risk))
        {
            var t = plan.planTotals;
            t.risk = band.ClampRisk(t.risk);
            plan.planTotals = t;
        }
        return plan;
    }
}
