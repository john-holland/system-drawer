using System;
using System.Collections.Generic;
using UnityEngine;

public enum LoveMakingTopologyGoalKind
{
    Duration,
    ContactBones,
    AnimationTag,
    DesireMet,
    ClimaxOptional
}

/// <summary>Topological / time / animation goal for a LoveMakingSession.</summary>
[Serializable]
public sealed class LoveMakingTopologyGoal
{
    public string id = Guid.NewGuid().ToString("N");
    public LoveMakingTopologyGoalKind kind = LoveMakingTopologyGoalKind.Duration;
    public float targetValue = 1f;
    public string animationTag;
    public LoveDesire desire = LoveDesire.Affection;
    public List<string> requiredBones = new List<string>();
    public float progress01;

    public bool IsMet => progress01 >= 0.999f;
}

/// <summary>Active love-making encounter with time budget and topological goals.
/// Aw Cursor, I love it!</summary>
[AddComponentMenu("Locomotion/Love Making/Session")]
public sealed class LoveMakingSession : MonoBehaviour
{
    public List<GameObject> participants = new List<GameObject>();
    public float timeBudgetSeconds = 30f;
    public float elapsedSeconds;
    public List<LoveMakingTopologyGoal> goals = new List<LoveMakingTopologyGoal>();
    public LoveCard activeCard;
    public bool psychApplied;
    public LoveMakingMode mode = LoveMakingMode.Tender;

    public float RemainingSeconds => Mathf.Max(0f, timeBudgetSeconds - elapsedSeconds);
    public bool TimeExpired => elapsedSeconds >= timeBudgetSeconds;
    public int ParticipantCount => participants != null ? participants.Count : 0;

    public float AveragePhysicality01
    {
        get
        {
            if (activeCard != null) return activeCard.physicality01;
            return 0.35f;
        }
    }

    public void Begin(IList<GameObject> people, float budgetSeconds, IList<LoveMakingTopologyGoal> topologyGoals)
    {
        participants.Clear();
        if (people != null)
        {
            for (int i = 0; i < people.Count; i++)
                if (people[i] != null)
                    participants.Add(people[i]);
        }
        timeBudgetSeconds = Mathf.Max(0.5f, budgetSeconds);
        elapsedSeconds = 0f;
        psychApplied = false;
        goals.Clear();
        if (topologyGoals != null)
        {
            for (int i = 0; i < topologyGoals.Count; i++)
                if (topologyGoals[i] != null)
                    goals.Add(topologyGoals[i]);
        }
        if (goals.Count == 0)
        {
            goals.Add(new LoveMakingTopologyGoal
            {
                kind = LoveMakingTopologyGoalKind.Duration,
                targetValue = Mathf.Min(8f, timeBudgetSeconds * 0.4f)
            });
            goals.Add(new LoveMakingTopologyGoal
            {
                kind = LoveMakingTopologyGoalKind.DesireMet,
                desire = LoveDesire.Closeness,
                targetValue = 0.5f
            });
        }
    }

    public void Tick(float dt)
    {
        elapsedSeconds += Mathf.Max(0f, dt);
        for (int i = 0; i < goals.Count; i++)
        {
            var g = goals[i];
            if (g == null || g.IsMet) continue;
            switch (g.kind)
            {
                case LoveMakingTopologyGoalKind.Duration:
                    g.progress01 = Mathf.Clamp01(elapsedSeconds / Mathf.Max(1e-3f, g.targetValue));
                    break;
                case LoveMakingTopologyGoalKind.AnimationTag:
                    if (activeCard != null &&
                        string.Equals(activeCard.LoveAnimationGroupTag, g.animationTag, StringComparison.OrdinalIgnoreCase))
                        g.progress01 = Mathf.MoveTowards(g.progress01, 1f, dt * 0.35f);
                    break;
                case LoveMakingTopologyGoalKind.DesireMet:
                    if (activeCard != null && activeCard.requiredDesires != null &&
                        activeCard.requiredDesires.Contains(g.desire))
                        g.progress01 = Mathf.MoveTowards(g.progress01, 1f, dt * activeCard.desireIntensity01);
                    break;
                case LoveMakingTopologyGoalKind.ContactBones:
                    if (activeCard != null && activeCard.requiredLimbBones != null &&
                        activeCard.requiredLimbBones.Count >= g.requiredBones.Count)
                        g.progress01 = Mathf.MoveTowards(g.progress01, 1f, dt * 0.4f);
                    break;
                case LoveMakingTopologyGoalKind.ClimaxOptional:
                    g.progress01 = Mathf.MoveTowards(g.progress01, activeCard != null && activeCard.physicality01 > 0.7f ? 1f : 0.3f, dt * 0.1f);
                    break;
            }
        }
    }

    public bool AllRequiredGoalsMet()
    {
        for (int i = 0; i < goals.Count; i++)
        {
            var g = goals[i];
            if (g == null) continue;
            if (g.kind == LoveMakingTopologyGoalKind.ClimaxOptional) continue;
            if (!g.IsMet) return false;
        }
        return true;
    }

    public float LoveTint01()
    {
        float phys = AveragePhysicality01;
        float people = Mathf.Clamp01((ParticipantCount - 1) / 3f);
        return Mathf.Clamp01(phys * 0.7f + people * 0.3f);
    }
}
