using System.Collections.Generic;
using UnityEngine;

/// <summary>Session-local solver: sequence CombatCards for time / damage / ward topology.</summary>
public static class CombatPlannerSolver
{
    public sealed class SolveResult
    {
        public List<CombatCard> sequence = new List<CombatCard>();
        public float score;
        public bool feasible;
    }

    public static SolveResult Solve(
        CombatSession session,
        IList<CombatCard> available,
        GameObject actor,
        GameObject target)
    {
        var result = new SolveResult();
        if (session == null || available == null) return result;
        var scored = new List<(CombatCard card, float score)>();
        var rd = actor != null ? actor.GetComponent<RagdollSystem>() : null;
        for (int i = 0; i < available.Count; i++)
        {
            var c = available[i];
            if (c == null) continue;
            if (!c.MeetsCombatRequirements(actor, target ?? c.primaryTarget, rd))
                continue;
            scored.Add((c, ScoreCard(c, session)));
        }
        scored.Sort((a, b) => b.score.CompareTo(a.score));
        float budget = session.RemainingSeconds;
        for (int i = 0; i < scored.Count && budget > 0.5f; i++)
        {
            result.sequence.Add(scored[i].card);
            result.score += scored[i].score;
            budget -= EstimateSeconds(scored[i].card);
            if (CoversGoals(result.sequence, session)) break;
        }
        result.feasible = result.sequence.Count > 0;
        return result;
    }

    public static float ScoreCard(CombatCard card, CombatSession session)
    {
        if (card == null) return 0f;
        float s = card.impact != null ? card.impact.damage01 : 0.2f;
        s *= 1f + (session != null ? session.participants.Count * 0.05f : 0f);
        if (card.combatMoveKind == CombatMoveKind.Block || card.combatMoveKind == CombatMoveKind.Parry)
            s += 0.15f;
        if (card.instrumentProxy != null && card.instrumentProxy.useProxyInstrument)
            s += 0.1f;
        return s;
    }

    static float EstimateSeconds(CombatCard c)
    {
        if (c == null) return 1f;
        return c.combatMoveKind == CombatMoveKind.Fire ? 0.8f : 1.5f;
    }

    static bool CoversGoals(List<CombatCard> seq, CombatSession session)
    {
        if (session?.goals == null) return seq != null && seq.Count > 0;
        var tags = new HashSet<string>();
        bool hasWard = false;
        float dmg = 0f;
        for (int i = 0; i < seq.Count; i++)
        {
            var c = seq[i];
            if (c == null) continue;
            tags.Add(c.CombatAnimationGroupTag ?? "");
            if (c.defendWards != null && c.defendWards.Count > 0) hasWard = true;
            if (c.impact != null) dmg += c.impact.damage01;
        }
        for (int i = 0; i < session.goals.Count; i++)
        {
            var g = session.goals[i];
            if (g == null || g.kind == CombatTopologyGoalKind.Duration || g.kind == CombatTopologyGoalKind.ContactBones)
                continue;
            if (g.kind == CombatTopologyGoalKind.AnimationTag && !tags.Contains(g.animationTag ?? ""))
                return false;
            if (g.kind == CombatTopologyGoalKind.WardActive && !hasWard) return false;
            if (g.kind == CombatTopologyGoalKind.DamageDealt && dmg < g.targetValue) return false;
        }
        return true;
    }
}
