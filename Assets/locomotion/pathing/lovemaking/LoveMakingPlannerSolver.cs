using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-local solver: pick/sequence LoveCards that satisfy time + physical + animation topology goals.
/// </summary>
public static class LoveMakingPlannerSolver
{
    public sealed class SolveResult
    {
        public List<LoveCard> sequence = new List<LoveCard>();
        public float score;
        public bool feasible;
    }

    public static SolveResult Solve(
        LoveMakingSession session,
        IList<LoveCard> available,
        GameObject actor,
        GameObject partner,
        RagdollState state = null)
    {
        var result = new SolveResult();
        if (session == null || available == null) return result;

        var scored = new List<(LoveCard card, float score)>();
        var rd = actor != null ? actor.GetComponent<RagdollSystem>() : null;
        for (int i = 0; i < available.Count; i++)
        {
            var c = available[i];
            if (c == null) continue;
            if (!c.MeetsLoveRequirements(actor, partner ?? c.opponent, rd))
                continue;
            float s = ScoreCard(c, session);
            scored.Add((c, s));
        }
        scored.Sort((a, b) => b.score.CompareTo(a.score));

        float budget = session.RemainingSeconds;
        for (int i = 0; i < scored.Count && budget > 0.5f; i++)
        {
            var c = scored[i].card;
            result.sequence.Add(c);
            result.score += scored[i].score;
            budget -= EstimateCardSeconds(c);
            if (CoversGoals(result.sequence, session))
                break;
        }

        result.feasible = result.sequence.Count > 0;
        return result;
    }

    public static float ScoreCard(LoveCard card, LoveMakingSession session)
    {
        if (card == null) return 0f;
        float phys = card.physicality01;
        float people = session != null ? Mathf.Max(1, session.ParticipantCount) : 2;
        float desire = card.desireIntensity01;
        float goalBoost = 0f;
        if (session?.goals != null)
        {
            for (int i = 0; i < session.goals.Count; i++)
            {
                var g = session.goals[i];
                if (g == null || g.IsMet) continue;
                if (g.kind == LoveMakingTopologyGoalKind.AnimationTag &&
                    string.Equals(card.LoveAnimationGroupTag, g.animationTag, System.StringComparison.OrdinalIgnoreCase))
                    goalBoost += 0.35f;
                if (g.kind == LoveMakingTopologyGoalKind.DesireMet &&
                    card.requiredDesires != null && card.requiredDesires.Contains(g.desire))
                    goalBoost += 0.25f;
            }
        }
        return phys * people * 0.15f + desire + goalBoost;
    }

    static float EstimateCardSeconds(LoveCard card) =>
        card == null ? 2f : Mathf.Lerp(1.5f, 5f, card.physicality01);

    static bool CoversGoals(List<LoveCard> seq, LoveMakingSession session)
    {
        if (session?.goals == null || session.goals.Count == 0) return seq.Count > 0;
        var desires = new HashSet<LoveDesire>();
        var tags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < seq.Count; i++)
        {
            var c = seq[i];
            if (c == null) continue;
            tags.Add(c.LoveAnimationGroupTag);
            if (c.requiredDesires != null)
                for (int d = 0; d < c.requiredDesires.Count; d++)
                    desires.Add(c.requiredDesires[d]);
        }
        for (int i = 0; i < session.goals.Count; i++)
        {
            var g = session.goals[i];
            if (g == null || g.kind == LoveMakingTopologyGoalKind.ClimaxOptional ||
                g.kind == LoveMakingTopologyGoalKind.Duration ||
                g.kind == LoveMakingTopologyGoalKind.ContactBones)
                continue;
            if (g.kind == LoveMakingTopologyGoalKind.AnimationTag && !tags.Contains(g.animationTag ?? ""))
                return false;
            if (g.kind == LoveMakingTopologyGoalKind.DesireMet && !desires.Contains(g.desire))
                return false;
        }
        return true;
    }
}
