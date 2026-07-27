using UnityEngine;

/// <summary>
/// Soft-gates high-damage Play spots (Safety Warden analog). Subdue allows control holds.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Referee Warden Planner Service")]
public sealed class RefereeWardenPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "referee_warden";

    public WrestlingMode mode = WrestlingMode.Play;
    [Range(0f, 1f)] public float maxPlayDamage01 = 0.75f;
    [Range(0f, 1f)] public float softGateWeight = 0.35f;

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        return seg.runningTotals.risk;
    }

    public GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        if (plan?.segments == null) return plan;
        if (mode == WrestlingMode.Subdue)
        {
            // Allow control holds — lightly reduce throw damage totals.
            for (int i = 0; i < plan.segments.Count; i++)
            {
                var seg = plan.segments[i];
                if (seg == null) continue;
                if (seg.animationGroupTag != null &&
                    (seg.animationGroupTag.Contains("throw") || seg.animationGroupTag.Contains("drop_on")))
                {
                    var totals = seg.runningTotals;
                    totals.damage = Mathf.Min(totals.damage, maxPlayDamage01 * 0.5f);
                    seg.runningTotals = totals;
                }
            }
            plan.RecomputePlanTotals();
            return plan;
        }

        if (mode != WrestlingMode.Play)
            return plan;

        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            var totals = seg.runningTotals;
            if (totals.damage > maxPlayDamage01)
            {
                totals.damage = Mathf.Lerp(totals.damage, maxPlayDamage01, softGateWeight);
                totals.risk = Mathf.Clamp01(totals.risk + softGateWeight * 0.1f);
                seg.runningTotals = totals;
            }
        }

        plan.RecomputePlanTotals();
        return plan;
    }
}
