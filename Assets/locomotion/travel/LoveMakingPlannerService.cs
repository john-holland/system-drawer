using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Travel risk planner for love-making: enrich partner/mode, stamp lovemaking.* tags, filter consent.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Love Making Planner Service")]
public sealed class LoveMakingPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "lovemaking";

    public LoveMakingMode mode = LoveMakingMode.Tender;
    public bool intimateStyle = true;
    public GameObject partner;
    public ConsiderLoveMakingCards consider;
    public LoveMakingSession session;

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
        if (session == null && _ctx?.actor != null)
            session = _ctx.actor.GetComponent<LoveMakingSession>();
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
            seg.animationGroupTag.StartsWith("lovemaking.", System.StringComparison.OrdinalIgnoreCase))
        {
            // Intimate segments are low travel risk but mark social exposure slightly.
            r = Mathf.Clamp01(r * 0.5f + 0.05f);
        }
        return r;
    }

    public GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        if (plan?.segments == null) return plan;
        var ctx = _ctx ?? StuntDiscoveryContext.FromScene(Vector3.zero, Vector3.zero, null);
        EnrichDiscovery(ctx);

        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
                seg.animationGroupTag.StartsWith("lovemaking.", System.StringComparison.OrdinalIgnoreCase))
            {
                float risk = EstimateSegmentRisk(seg, ctx);
                var totals = seg.runningTotals;
                totals.risk = risk;
                seg.runningTotals = totals;
            }
        }

        plan.RecomputePlanTotals();
        return plan;
    }

    public List<LoveCard> FilterFeasible(IList<LoveCard> cards, GameObject actor, GameObject partnerGo)
    {
        var list = new List<LoveCard>();
        if (cards == null) return list;
        var rd = actor != null ? actor.GetComponent<RagdollSystem>() : null;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            if (c == null) continue;
            if (!c.MeetsLoveRequirements(actor, partnerGo ?? c.opponent, rd))
                continue;
            c.physicalPathingTag = $"lovemaking_{c.loveMoveKind.ToString().ToLowerInvariant()}";
            c.description = c.LoveAnimationGroupTag;
            list.Add(c);
        }
        return list;
    }

    public LoveMakingPlannerSolver.SolveResult SolveSession(GameObject actor, GameObject partnerGo, IList<LoveCard> pool)
    {
        if (session == null && actor != null)
            session = actor.GetComponent<LoveMakingSession>() ?? actor.AddComponent<LoveMakingSession>();
        return LoveMakingPlannerSolver.Solve(session, pool, actor, partnerGo);
    }
}
