using System.Collections.Generic;
using UnityEngine;

/// <summary>Idempotent insert of landing-queue + optional refuel before land/park on aircraft routes.</summary>
public static class AircraftTravelRouteMerger
{
    public const string LandingQueueName = "AircraftLandingQueue";
    public const string RefuelName = "AircraftRefuel";

    public static void MergeIntoLeg(TravelLegSequenceNode legNode, AirplaneVehicleRagdoll plane, MultiModalSegment seg)
    {
        if (legNode == null || plane == null || seg == null) return;
        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();

        bool landish = seg.mode == TravelLegMode.Land
                       || seg.mode == TravelLegMode.Park
                       || seg.mode == TravelLegMode.Fly;
        if (!landish) return;

        if (plane.insertLandingQueue)
            EnsureLandingQueue(legNode, plane);
        if (plane.insertRefuelBeforePark || NeedsRefuel(plane, seg))
            EnsureRefuel(legNode, plane);
    }

    static bool NeedsRefuel(AirplaneVehicleRagdoll plane, MultiModalSegment seg)
    {
        if (plane.fuel01 <= plane.refuelFuelThreshold01) return true;
        string tag = seg != null ? seg.animationGroupTag : null;
        return !string.IsNullOrEmpty(tag)
               && tag.IndexOf("refuel", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void EnsureLandingQueue(TravelLegSequenceNode legNode, AirplaneVehicleRagdoll plane)
    {
        for (int i = 0; i < legNode.children.Count; i++)
        {
            if (legNode.children[i] is AircraftLandingQueueNode existing)
            {
                existing.airplane = plane;
                return;
            }
        }
        var go = new GameObject(LandingQueueName);
        go.transform.SetParent(legNode.transform, false);
        var node = go.AddComponent<AircraftLandingQueueNode>();
        node.airplane = plane;
        // Insert before last terminal/landing children when possible.
        int idx = Mathf.Max(0, legNode.children.Count - 1);
        legNode.children.Insert(idx, node);
    }

    static void EnsureRefuel(TravelLegSequenceNode legNode, AirplaneVehicleRagdoll plane)
    {
        for (int i = 0; i < legNode.children.Count; i++)
        {
            if (legNode.children[i] is AircraftRefuelNode existing)
            {
                existing.airplane = plane;
                return;
            }
        }
        var go = new GameObject(RefuelName);
        go.transform.SetParent(legNode.transform, false);
        var node = go.AddComponent<AircraftRefuelNode>();
        node.airplane = plane;
        int queueIdx = -1;
        for (int i = 0; i < legNode.children.Count; i++)
            if (legNode.children[i] is AircraftLandingQueueNode) { queueIdx = i; break; }
        int idx = queueIdx >= 0 ? queueIdx + 1 : Mathf.Max(0, legNode.children.Count - 1);
        legNode.children.Insert(idx, node);
    }
}
