using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-level mode for a single leg of a multi-modal travel plan.
/// </summary>
public enum TravelLegMode
{
    Walk,
    Fly,
    Drive,
    ToolBridge,
    Acrobatics
}

/// <summary>
/// One segment of a multi-modal plan (waypoints and/or card bridge).
/// </summary>
[Serializable]
public class MultiModalSegment
{
    public TravelLegMode mode = TravelLegMode.Walk;
    public List<Vector3> waypoints = new List<Vector3>();
    public GoodSection card;
    public List<GameObject> tools = new List<GameObject>();
    public Vector3 segmentEnd;
    public PhysicalPathingMedium medium = PhysicalPathingMedium.Unspecified;
    [NonSerialized] public VehicleActor optionalVehicleHint;

    [Tooltip("Planner-estimated travel time for this leg (seconds), when timeline search fills it.")]
    public float estimatedTimeSec;

    public static MultiModalSegment FromWalk(List<Vector3> path)
    {
        return new MultiModalSegment
        {
            mode = TravelLegMode.Walk,
            waypoints = path != null ? new List<Vector3>(path) : new List<Vector3>(),
            medium = PhysicalPathingMedium.Ground
        };
    }

    public static MultiModalSegment FromFly(List<Vector3> path, PhysicalPathingMedium airMedium = PhysicalPathingMedium.Air)
    {
        return new MultiModalSegment
        {
            mode = TravelLegMode.Fly,
            waypoints = path != null ? new List<Vector3>(path) : new List<Vector3>(),
            medium = airMedium
        };
    }

    public static MultiModalSegment FromDrive(List<Vector3> path, VehicleActor vehicleHint = null)
    {
        return new MultiModalSegment
        {
            mode = TravelLegMode.Drive,
            waypoints = path != null ? new List<Vector3>(path) : new List<Vector3>(),
            medium = PhysicalPathingMedium.Ground,
            optionalVehicleHint = vehicleHint
        };
    }

    public static MultiModalSegment FromToolBridge(GoodSection section, List<GameObject> toolList, Vector3 from, Vector3 to)
    {
        return new MultiModalSegment
        {
            mode = TravelLegMode.ToolBridge,
            card = section,
            tools = toolList != null ? new List<GameObject>(toolList) : new List<GameObject>(),
            waypoints = new List<Vector3> { from, to },
            segmentEnd = to
        };
    }

    public static MultiModalSegment FromAcrobatics(GoodSection section, List<GameObject> toolList, Vector3 from, Vector3 to)
    {
        MultiModalSegment s = FromToolBridge(section, toolList, from, to);
        s.mode = TravelLegMode.Acrobatics;
        return s;
    }

    /// <summary>Deep copy of waypoint list; shallow refs for card/tools.</summary>
    public MultiModalSegment CloneShallowRefs()
    {
        var copy = new MultiModalSegment
        {
            mode = mode,
            card = card,
            segmentEnd = segmentEnd,
            medium = medium,
            optionalVehicleHint = optionalVehicleHint,
            estimatedTimeSec = estimatedTimeSec
        };
        if (waypoints != null)
            copy.waypoints = new List<Vector3>(waypoints);
        else
            copy.waypoints = new List<Vector3>();
        if (tools != null)
            copy.tools = new List<GameObject>(tools);
        else
            copy.tools = new List<GameObject>();
        return copy;
    }
}

/// <summary>
/// Ordered multi-modal travel plan for visualization and CompositeMultiModalPathNode execution.
/// </summary>
[Serializable]
public class GenericMultiModalPathPlan
{
    public List<MultiModalSegment> segments = new List<MultiModalSegment>();

    public bool IsEmpty => segments == null || segments.Count == 0;

    public List<Vector3> FlattenWaypointsForGizmos()
    {
        var list = new List<Vector3>();
        if (segments == null) return list;
        foreach (MultiModalSegment seg in segments)
        {
            if (seg?.waypoints == null) continue;
            foreach (Vector3 w in seg.waypoints)
                list.Add(w);
        }
        return list;
    }

    public GenericMultiModalPathPlan Clone()
    {
        var p = new GenericMultiModalPathPlan();
        if (segments == null)
            return p;
        foreach (MultiModalSegment seg in segments)
        {
            if (seg == null)
                continue;
            p.segments.Add(seg.CloneShallowRefs());
        }

        return p;
    }
}
