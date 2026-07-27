using System;
using System.Collections.Generic;
using UnityEngine;

public enum CombatTopologyGoalKind
{
    Duration,
    ContactBones,
    AnimationTag,
    DamageDealt,
    WardActive
}

[Serializable]
public sealed class CombatTopologyGoal
{
    public string id = Guid.NewGuid().ToString("N");
    public CombatTopologyGoalKind kind = CombatTopologyGoalKind.Duration;
    public float targetValue = 1f;
    public string animationTag;
    public List<string> requiredBones = new List<string>();
    public float progress01;
    public bool IsMet => progress01 >= 0.999f;
}

[AddComponentMenu("Locomotion/Combat/Session")]
public sealed class CombatSession : MonoBehaviour
{
    public List<GameObject> participants = new List<GameObject>();
    public float timeBudgetSeconds = 20f;
    public float elapsedSeconds;
    public List<CombatTopologyGoal> goals = new List<CombatTopologyGoal>();
    public CombatCard activeCard;
    public CombatMode mode = CombatMode.Melee;
    public float damageDealt01;

    public float RemainingSeconds => Mathf.Max(0f, timeBudgetSeconds - elapsedSeconds);

    public void Begin(IList<GameObject> people, float budgetSeconds, IList<CombatTopologyGoal> topologyGoals)
    {
        participants.Clear();
        if (people != null)
            for (int i = 0; i < people.Count; i++)
                if (people[i] != null) participants.Add(people[i]);
        timeBudgetSeconds = Mathf.Max(0.5f, budgetSeconds);
        elapsedSeconds = 0f;
        damageDealt01 = 0f;
        goals.Clear();
        if (topologyGoals != null)
            for (int i = 0; i < topologyGoals.Count; i++)
                if (topologyGoals[i] != null) goals.Add(topologyGoals[i]);
        if (goals.Count == 0)
        {
            goals.Add(new CombatTopologyGoal { kind = CombatTopologyGoalKind.Duration, targetValue = Mathf.Min(6f, timeBudgetSeconds * 0.4f) });
            goals.Add(new CombatTopologyGoal { kind = CombatTopologyGoalKind.DamageDealt, targetValue = 0.25f });
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
                case CombatTopologyGoalKind.Duration:
                    g.progress01 = Mathf.Clamp01(elapsedSeconds / Mathf.Max(1e-3f, g.targetValue));
                    break;
                case CombatTopologyGoalKind.AnimationTag:
                    if (activeCard != null &&
                        string.Equals(activeCard.CombatAnimationGroupTag, g.animationTag, StringComparison.OrdinalIgnoreCase))
                        g.progress01 = Mathf.MoveTowards(g.progress01, 1f, dt * 0.4f);
                    break;
                case CombatTopologyGoalKind.DamageDealt:
                    g.progress01 = Mathf.Clamp01(damageDealt01 / Mathf.Max(1e-3f, g.targetValue));
                    break;
                case CombatTopologyGoalKind.WardActive:
                    g.progress01 = activeCard != null && activeCard.defendWards != null && activeCard.defendWards.Count > 0
                        ? Mathf.MoveTowards(g.progress01, 1f, dt)
                        : g.progress01;
                    break;
                case CombatTopologyGoalKind.ContactBones:
                    g.progress01 = Mathf.MoveTowards(g.progress01, 1f, dt * 0.35f);
                    break;
            }
        }
    }

    public bool AllRequiredGoalsMet()
    {
        for (int i = 0; i < goals.Count; i++)
            if (goals[i] != null && !goals[i].IsMet) return false;
        return true;
    }
}
