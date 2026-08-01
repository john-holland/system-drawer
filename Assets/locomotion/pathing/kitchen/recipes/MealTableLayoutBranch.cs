using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional SG4D / PlaceBuild topology branch that yields serve waypoints for assigned actor.
/// </summary>
public static class MealTableLayoutBranch
{
    public sealed class LayoutResult
    {
        public List<string> stepIds = new List<string>();
        public Transform serveWaypoint;
        public string assignedActorKey;
    }

    public static LayoutResult Build(PlaceBuildTopologyAsset asset, string assignedActorKey, Transform serveWaypointFallback = null)
    {
        var result = new LayoutResult { assignedActorKey = assignedActorKey };
        if (asset != null)
            result.stepIds = PlaceBuildTopologyBtBuilder.BuildStepIds(asset);
        else
            result.stepIds.Add("Occupy_default_place");
        result.serveWaypoint = serveWaypointFallback;
        return result;
    }
}
