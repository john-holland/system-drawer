using System;
using UnityEngine;

/// <summary>
/// Cumulative ambulation / stunt metrics for a plan or segment (0–1 normalized unless noted).
/// </summary>
[Serializable]
public struct TravelPlanRunningTotals
{
    [Tooltip("Impulse / SectionLimits effort accumulated along the plan.")]
    [Range(0f, 1f)] public float power;

    [Tooltip("Recoverable elastic / rope tension budget remaining (starts high, turns/jumps consume).")]
    [Range(0f, 1f)] public float spring;

    [Tooltip("Expected contact damage weighted by vulnerable ragdoll markers.")]
    [Range(0f, 1f)] public float damage;

    [Tooltip("Fall / crash / crowd composite risk (complement of safety01).")]
    [Range(0f, 1f)] public float risk;

    [Tooltip("Leftover yaw energy after turn cost; consumed before jump spring.")]
    [Range(0f, 1f)] public float radialTurningPotential;

    public float Safety01 => Mathf.Clamp01(1f - risk);

    public static TravelPlanRunningTotals Neutral => new TravelPlanRunningTotals
    {
        power = 0f,
        spring = 1f,
        damage = 0f,
        risk = 0f,
        radialTurningPotential = 1f
    };

    public TravelPlanRunningTotals Add(in TravelPlanRunningTotals delta)
    {
        return new TravelPlanRunningTotals
        {
            power = Mathf.Clamp01(power + delta.power),
            spring = Mathf.Clamp01(spring * delta.spring), // multiplicative remaining budget
            damage = Mathf.Clamp01(damage + delta.damage),
            risk = Mathf.Clamp01(Mathf.Max(risk, delta.risk) * 0.5f + (risk + delta.risk) * 0.25f),
            radialTurningPotential = Mathf.Clamp01(radialTurningPotential * delta.radialTurningPotential)
        };
    }

    public static TravelPlanRunningTotals FromTurnCost(float yawDegrees)
    {
        float turn01 = Mathf.Clamp01(Mathf.Abs(yawDegrees) / 180f);
        return new TravelPlanRunningTotals
        {
            power = turn01 * 0.15f,
            spring = 1f - turn01 * 0.25f,
            damage = 0f,
            risk = turn01 * 0.05f,
            radialTurningPotential = 1f - turn01
        };
    }

    public static TravelPlanRunningTotals FromJump(float entrySpeed01, float crowd01, float damage01)
    {
        float speed = Mathf.Clamp01(entrySpeed01);
        return new TravelPlanRunningTotals
        {
            power = 0.2f + speed * 0.4f,
            spring = 1f - speed * 0.35f,
            damage = Mathf.Clamp01(damage01),
            risk = Mathf.Clamp01(0.15f + speed * 0.35f + crowd01 * 0.4f + damage01 * 0.3f),
            radialTurningPotential = 1f - speed * 0.2f
        };
    }
}
