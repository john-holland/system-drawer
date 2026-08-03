using System;
using UnityEngine;

/// <summary>Serialized health channels for a BuildingRagdoll.</summary>
[Serializable]
public sealed class BuildingHealthState
{
    [Range(0f, 1f)] public float integrity01 = 1f;
    [Range(0f, 1f)] public float occupancyLoad01;
    [Range(0f, 1f)] public float exteriorPressure01;
    [Range(0f, 1f)] public float commodityHunger01;
    [Range(0f, 1f)] public float memoryAggregate01;

    public void ApplyImpulseDamage(float damage01)
    {
        integrity01 = Mathf.Clamp01(integrity01 - Mathf.Max(0f, damage01));
        memoryAggregate01 = Mathf.Clamp01(memoryAggregate01 + damage01 * 0.5f);
    }

    public void TickDecay(float dt, float repairBias01 = 0f)
    {
        // Slow natural settling of exterior pressure; integrity does not self-heal without CivicCard.
        exteriorPressure01 = Mathf.MoveTowards(exteriorPressure01, 0f, dt * 0.01f);
        if (repairBias01 > 0f)
            integrity01 = Mathf.MoveTowards(integrity01, 1f, dt * repairBias01 * 0.05f);
    }
}
