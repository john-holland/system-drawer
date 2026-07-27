using UnityEngine;

/// <summary>
/// Soft-gates love-making intensity (consent / comfort analog of RefereeWarden).
/// </summary>
[AddComponentMenu("Locomotion/Travel/Consent Warden Planner Service")]
public sealed class ConsentWardenPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "consent_warden";

    [Range(0f, 1f)] public float maxPhysicality01 = 0.95f;
    public bool requireConsentFlag = true;

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx) => _ctx = ctx ?? _ctx;

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
            seg.animationGroupTag.StartsWith("lovemaking.", System.StringComparison.OrdinalIgnoreCase) &&
            seg.animationGroupTag.Contains("caress"))
            r = Mathf.Clamp01(r + 0.04f);
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

    public LoveCard SoftGate(LoveCard card)
    {
        if (card == null) return null;
        if (card.physicality01 > maxPhysicality01)
            card.physicality01 = maxPhysicality01;
        if (requireConsentFlag)
            card.requiresConsent = true;
        return card;
    }
}
