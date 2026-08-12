using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// KeepAlive path for TravelAgent across Continuuuum dimension switches:
/// restore velocity via DimensionalLemmaVelocityBridge / Rigidbody; skip RebuildCachedPlan when goal unchanged.
/// </summary>
public static class DimensionalTravelAgentKeepAlive
{
    static readonly Dictionary<int, Vector3> LastGoals = new Dictionary<int, Vector3>();

    public static void CaptureGoals()
    {
        LastGoals.Clear();
        foreach (var agent in TravelAgentRegistry.All)
        {
            if (agent == null)
                continue;
            LastGoals[agent.GetInstanceID()] = agent.previewGoalWorld;
        }
    }

    public static void ApplyAfterDimSwitch()
    {
        foreach (var agent in TravelAgentRegistry.All)
        {
            if (agent == null || !agent.isActiveAndEnabled)
                continue;

            var binding = agent.GetComponent<DimensionalLemmaBinding>()
                          ?? agent.GetComponentInParent<DimensionalLemmaBinding>();
            if (binding != null && binding.ResolvedPolicy == DimensionalActorPolicy.ReplaceActor)
                continue;

            // Velocity already restored by DimensionalLemmaVelocityBridge when present.
            var bridge = agent.GetComponent<DimensionalLemmaVelocityBridge>()
                         ?? agent.GetComponentInChildren<DimensionalLemmaVelocityBridge>();
            if (bridge == null && agent.TryGetComponent<Rigidbody>(out var rb))
            {
                // no-op: keep current velocity
                _ = rb;
            }

            int id = agent.GetInstanceID();
            bool goalChanged = true;
            if (LastGoals.TryGetValue(id, out var prevGoal))
                goalChanged = (prevGoal - agent.previewGoalWorld).sqrMagnitude > 0.01f;

            if (!goalChanged)
            {
                // KeepAlive: do not RebuildCachedPlan
                continue;
            }

            agent.RebuildCachedPlan();
        }
    }
}
