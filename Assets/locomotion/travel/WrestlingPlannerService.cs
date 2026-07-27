using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stuntman-style planner for wrestling: enrich opponent/mode/stamina, expand Lift/Throw branches,
/// stamp animation group tags, reject size-gate failures.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Wrestling Planner Service")]
public sealed class WrestlingPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "wrestling";

    public WrestlingMode mode = WrestlingMode.Play;
    public bool professionalStyle = true;
    public GameObject opponent;
    public ConsiderWrestlingCards consider;

    StuntDiscoveryContext _ctx;
    LifeSystemsSheet _staminaSheet;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
        if (_ctx?.actor != null)
        {
            var life = LifeSystemsServices.Instance;
            _staminaSheet = life != null
                ? life.GetOrCreate(_ctx.actor)
                : _ctx.actor.GetComponent<LifeSystemsSheet>();
        }
        if (opponent == null && _ctx?.actor != null && consider != null)
        {
            // Best effort: leave opponent for RescoreCards from Consider pool.
        }
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.animationGroupTag) &&
            seg.animationGroupTag.StartsWith("wrestling.", System.StringComparison.OrdinalIgnoreCase))
        {
            r = Mathf.Clamp01(r + (mode == WrestlingMode.Play ? 0.12f : 0.08f));
            if (mode == WrestlingMode.Subdue)
                r = Mathf.Clamp01(r * 0.85f);
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
                seg.animationGroupTag.StartsWith("wrestling.", System.StringComparison.OrdinalIgnoreCase))
            {
                // Expand lift → throw/drop tags when planner left a lift stub.
                if (seg.animationGroupTag.Contains("lift"))
                {
                    var branch = WrestlingMoveKind.Throw;
                    seg.animationGroupTag = WrestlingAnimationGroup.ForMove(branch, professionalStyle);
                }
            }

            float risk = EstimateSegmentRisk(seg, ctx);
            var totals = seg.runningTotals;
            totals.risk = risk;
            seg.runningTotals = totals;
        }

        plan.RecomputePlanTotals();
        return plan;
    }

    /// <summary>Expand Lift/Throw branch metadata on a card (for BT / card pool).</summary>
    public WrestlingCard ExpandBranches(WrestlingCard card)
    {
        if (card == null) return null;
        if (card.moveKind == WrestlingMoveKind.Lift)
        {
            var next = card.liftBranch;
            card.physicalPathingTag = $"wrestling_{next.ToString().ToLowerInvariant()}";
            // Keep moveKind as Lift for authorship; stamp anim for branch end.
            // Callers that need a full rewrite use RewriteToBranch.
        }
        return card;
    }

    public WrestlingCard RewriteToBranch(WrestlingCard card)
    {
        if (card == null) return null;
        WrestlingMoveKind next = card.moveKind;
        if (card.moveKind == WrestlingMoveKind.Lift)
            next = card.liftBranch;
        else if (card.moveKind == WrestlingMoveKind.Throw)
            next = card.throwBranch;
        else
            return card;

        var rewritten = WrestlingCard.Generate(card.mode, next, card.opponent, card.requiredState, card.professionalStyle, card.sizeGate);
        rewritten.liftBranch = card.liftBranch;
        rewritten.throwBranch = card.throwBranch;
        rewritten.bespokeCounterAnimTag = card.bespokeCounterAnimTag;
        rewritten.counterAngleDeg = card.counterAngleDeg;
        return rewritten;
    }

    public string ResolveCounterAnimTag(WrestlingCard card, Vector3 attackerVelocity)
    {
        if (card == null) return WrestlingAnimationGroup.Counter;
        if (!string.IsNullOrEmpty(card.bespokeCounterAnimTag))
            return card.bespokeCounterAnimTag;
        // Facing from attacker velocity + counterAngleDeg is applied by BT/node; tag stays generic.
        return WrestlingAnimationGroup.ForMove(WrestlingMoveKind.Counter, card.professionalStyle);
    }

    public List<WrestlingCard> FilterFeasible(IList<WrestlingCard> cards, GameObject actor, GameObject opp)
    {
        var list = new List<WrestlingCard>();
        if (cards == null) return list;
        var rd = actor != null ? actor.GetComponent<RagdollSystem>() : null;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            if (c == null) continue;
            if (!c.MeetsWrestlingRequirements(actor, opp ?? c.opponent, rd))
                continue;
            c.physicalPathingTag = $"wrestling_{c.moveKind.ToString().ToLowerInvariant()}";
            // Stamp anim tag into section name for planner visibility.
            c.description = $"{c.AnimationGroupTag}";
            list.Add(ExpandBranches(c));
        }
        return list;
    }

    public float Stamina01()
    {
        if (_staminaSheet == null) return 1f;
        return _staminaSheet.Get01(LifeSystemsChannelCatalog.Adrenaline);
    }
}
