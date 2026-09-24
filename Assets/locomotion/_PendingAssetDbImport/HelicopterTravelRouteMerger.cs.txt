using System.Collections.Generic;
using UnityEngine;

/// <summary>Idempotent insert of heli takeoff/landing topology nodes into TravelAgent fly/land legs.</summary>
public static class HelicopterTravelRouteMerger
{
    public const string TakeoffName = "HelicopterTakeoff";
    public const string LandingName = "HelicopterLanding";

    public static void MergeIntoLeg(TravelLegSequenceNode legNode, HelicopterVehicleRagdoll heli, MultiModalSegment seg)
    {
        if (legNode == null || heli == null || seg == null) return;
        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();

        if (seg.mode == TravelLegMode.Fly)
            EnsureTakeoff(legNode, heli);
        if (seg.mode == TravelLegMode.Land || seg.mode == TravelLegMode.Park || seg.mode == TravelLegMode.Fly)
            EnsureLanding(legNode, heli, seg);
    }

    static void EnsureTakeoff(TravelLegSequenceNode legNode, HelicopterVehicleRagdoll heli)
    {
        for (int i = 0; i < legNode.children.Count; i++)
        {
            if (legNode.children[i] is HelicopterTakeoffPlanNode existing)
            {
                existing.helicopter = heli;
                return;
            }
        }
        var go = new GameObject(TakeoffName);
        go.transform.SetParent(legNode.transform, false);
        var node = go.AddComponent<HelicopterTakeoffPlanNode>();
        node.helicopter = heli;
        legNode.children.Insert(0, node);
    }

    static void EnsureLanding(TravelLegSequenceNode legNode, HelicopterVehicleRagdoll heli, MultiModalSegment seg)
    {
        for (int i = 0; i < legNode.children.Count; i++)
        {
            if (legNode.children[i] is HelicopterLandingPlanNode existing)
            {
                existing.helicopter = heli;
                if (!string.IsNullOrEmpty(seg.roadLotId))
                    existing.targetRoadLot = RoadLot.FindById(seg.roadLotId);
                return;
            }
        }
        var go = new GameObject(LandingName);
        go.transform.SetParent(legNode.transform, false);
        var node = go.AddComponent<HelicopterLandingPlanNode>();
        node.helicopter = heli;
        if (!string.IsNullOrEmpty(seg.roadLotId))
            node.targetRoadLot = RoadLot.FindById(seg.roadLotId);
        legNode.children.Add(node);
    }
}
