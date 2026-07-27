using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Locomotion/Travel/Combat Planner Service")]
public sealed class CombatPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "combat";
    public CombatMode mode = CombatMode.Melee;
    public GameObject target;
    public ConsiderCombatCards consider;
    public CombatSession session;
    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
        if (session == null && _ctx?.actor != null)
            session = _ctx.actor.GetComponent<CombatSession>();
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
            seg.animationGroupTag.StartsWith("combat.", System.StringComparison.OrdinalIgnoreCase))
            r = Mathf.Clamp01(r + 0.12f);
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
            float risk = EstimateSegmentRisk(seg, ctx);
            var totals = seg.runningTotals;
            totals.risk = risk;
            if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
                seg.animationGroupTag.StartsWith("combat.", System.StringComparison.OrdinalIgnoreCase))
                totals.damage = Mathf.Clamp01(totals.damage + 0.08f);
            seg.runningTotals = totals;
        }
        plan.RecomputePlanTotals();
        return plan;
    }

    public List<CombatCard> FilterFeasible(IList<CombatCard> cards, GameObject actor, GameObject targetGo)
    {
        var list = new List<CombatCard>();
        if (cards == null) return list;
        var rd = actor != null ? actor.GetComponent<RagdollSystem>() : null;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            if (c == null) continue;
            if (!c.MeetsCombatRequirements(actor, targetGo ?? c.primaryTarget, rd)) continue;
            c.physicalPathingTag = $"combat_{c.combatMoveKind.ToString().ToLowerInvariant()}";
            list.Add(c);
        }
        return list;
    }

    public CombatPlannerSolver.SolveResult SolveSession(GameObject actor, GameObject targetGo, IList<CombatCard> pool)
    {
        if (session == null && actor != null)
            session = actor.GetComponent<CombatSession>() ?? actor.AddComponent<CombatSession>();
        return CombatPlannerSolver.Solve(session, pool, actor, targetGo);
    }
}
