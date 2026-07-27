using UnityEngine;

/// <summary>Soft-gates weapon safety lock force (5 lbf / car-door-spring gag).</summary>
[AddComponentMenu("Locomotion/Travel/Safety Lock Warden Planner Service")]
public sealed class SafetyLockWardenPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "safety_lock_warden";
    [Tooltip("~22.24 N = 5 lbf")]
    public float requiredForceN = 22.24f;
    public string hardwareNote = "the safety lock requires five pounds of pressure, for safety, also, all we had was car door spring";

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx) => _ctx = ctx ?? _ctx;

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
            seg.animationGroupTag.IndexOf("fire", System.StringComparison.OrdinalIgnoreCase) >= 0)
            r = Mathf.Clamp01(r + 0.05f);
        return r;
    }

    public GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        if (plan?.segments == null) return plan;
        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            float risk = EstimateSegmentRisk(seg, _ctx);
            var totals = seg.runningTotals;
            totals.risk = risk;
            seg.runningTotals = totals;
        }
        plan.RecomputePlanTotals();
        return plan;
    }

    public bool GateFire(CombatCard card)
    {
        if (card?.instrumentProxy == null || !card.instrumentProxy.useProxyInstrument)
            return true;
        if (card.instrumentProxy.safetyLockForceN < 1e-3f)
            card.instrumentProxy.safetyLockForceN = requiredForceN;
        if (string.IsNullOrEmpty(card.instrumentProxy.hardwareFlavorNote))
            card.instrumentProxy.hardwareFlavorNote = hardwareNote;
        return card.instrumentProxy.SafetyLockSatisfied;
    }
}
