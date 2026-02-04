using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Planning-time feasibility: can all required tools make contact with the goal?
/// Used by ToolTraversabilityPlanner when requireAllHeldToolsToMakeContact is true.
/// </summary>
public static class ToolContactFeasibility
{
    /// <summary>
    /// Default max distance (meters) from agent to goal for "tool can make contact" when section has no toolReachDistance set.
    /// </summary>
    public const float DefaultMaxContactDistance = 2f;

    /// <summary>
    /// Returns true if the section does not require all tools to make contact, or if all required tools can make contact with the goal.
    /// Uses agent-to-goal distance (or goal target bounds center) vs max contact distance (section.toolReachDistance or default).
    /// </summary>
    public static bool CanAllRequiredToolsMakeContact(
        GoodSection section,
        Vector3 agentPosition,
        Vector3 goalPosition,
        GameObject goalTarget = null)
    {
        if (section == null || !section.requireAllHeldToolsToMakeContact)
            return true;

        List<GameObject> tools = section.GetRequiredToolsList();
        if (tools == null || tools.Count == 0)
            return true;

        Vector3 goalPoint = goalTarget != null ? goalTarget.transform.position : goalPosition;
        float maxDist = section.toolReachDistance > 0f ? section.toolReachDistance : DefaultMaxContactDistance;
        float dist = Vector3.Distance(agentPosition, goalPoint);
        if (dist > maxDist)
            return false;

        if (section.requireAllTargetsToMakeContact && section.targets != null && section.targets.Count > 0)
        {
            foreach (GameObject t in section.targets)
            {
                if (t == null) continue;
                float d = Vector3.Distance(agentPosition, t.transform.position);
                if (d > maxDist)
                    return false;
            }
        }

        return true;
    }
}
