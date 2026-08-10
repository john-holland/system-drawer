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
    Rail,
    ToolBridge,
    Acrobatics,
    Park,
    Land,
    LandWater,
    Moor,
    ParkWater,
    Beach,
    Dock
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

    [Tooltip("Optional road segment id when drive leg follows a baked road spline.")]
    public string roadSegmentId;

    [Tooltip("Optional rail segment id when Rail leg follows a track spline.")]
    public string railSegmentId;

    [Tooltip("Optional train consist id for linked-segment snake multibody.")]
    public string consistId;

    [Tooltip("Optional RoadLot id when drive/land ends on a graded lot pad.")]
    public string roadLotId;

    [Tooltip("Arc-length along road at leg start (meters).")]
    public float distanceAlongStart;

    [Tooltip("Arc-length along road at leg end (meters).")]
    public float distanceAlongEnd;

    [Tooltip("When true, allocate this leg into the reverse budget when building kinematics profile.")]
    public bool reverseLeg;

    [Header("Terminal placement (Park/Land/Moor/…)")]
    public Bounds terminalSlotBounds;
    public Vector3 terminalCentroidWorld;
    public Vector3 terminalUpWorld = Vector3.up;
    public Vector3 terminalSurfaceNormal = Vector3.up;
    public float terminalWaterSurfaceY = float.NaN;
    public float terminalPlaningSpeed;
    public WaterHoldPolicy terminalHoldPolicy = WaterHoldPolicy.Park;
    public int terminalSlotIndex;
    public PlacementSlotConfig placementSlotConfig;
    [NonSerialized] public ParkingZoneVolume parkingZoneRef;
    public float terminalScore;

    [Header("Stunt / risk totals")]
    public TravelPlanRunningTotals runningTotals = TravelPlanRunningTotals.Neutral;
    [Tooltip("Optional stunt zone GameObject (runway / terminus).")]
    public GameObject stuntZoneRef;
    [Tooltip("Optional pathing aperture id for crash/pass-through legs.")]
    public string apertureId;
    [Tooltip("Selected parkour / rope animation group tag for this leg.")]
    public string animationGroupTag;

    public bool HasTerminalPayload =>
        TravelLegModeExtensions.IsTerminalLeg(mode) && terminalCentroidWorld.sqrMagnitude > 1e-8f;

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

    public static MultiModalSegment FromTerminal(
        TravelLegMode terminalMode,
        List<Vector3> path,
        Vector3 centroidWorld,
        PhysicalPathingMedium medium = PhysicalPathingMedium.Unspecified)
    {
        if (medium == PhysicalPathingMedium.Unspecified)
            medium = TravelLegModeExtensions.DefaultMedium(terminalMode);
        return new MultiModalSegment
        {
            mode = terminalMode,
            waypoints = path != null ? new List<Vector3>(path) : new List<Vector3>(),
            segmentEnd = centroidWorld,
            terminalCentroidWorld = centroidWorld,
            medium = medium,
            terminalHoldPolicy = TravelLegModeExtensions.DefaultHoldPolicy(terminalMode),
        };
    }

    public static MultiModalSegment FromPark(List<Vector3> path, Vector3 centroid) =>
        FromTerminal(TravelLegMode.Park, path, centroid, PhysicalPathingMedium.Ground);

    public static MultiModalSegment FromLand(List<Vector3> path, Vector3 centroid) =>
        FromTerminal(TravelLegMode.Land, path, centroid, PhysicalPathingMedium.Ground);

    public static MultiModalSegment FromLandWater(List<Vector3> path, Vector3 centroid) =>
        FromTerminal(TravelLegMode.LandWater, path, centroid, PhysicalPathingMedium.Water);

    public static MultiModalSegment FromMoor(List<Vector3> path, Vector3 centroid)
    {
        var s = FromTerminal(TravelLegMode.Moor, path, centroid, PhysicalPathingMedium.Water);
        s.terminalHoldPolicy = WaterHoldPolicy.Anchor;
        return s;
    }

    public static MultiModalSegment FromParkWater(List<Vector3> path, Vector3 centroid)
    {
        var s = FromTerminal(TravelLegMode.ParkWater, path, centroid, PhysicalPathingMedium.Water);
        s.terminalHoldPolicy = WaterHoldPolicy.Park;
        return s;
    }

    public static MultiModalSegment FromBeach(List<Vector3> path, Vector3 centroid) =>
        FromTerminal(TravelLegMode.Beach, path, centroid, PhysicalPathingMedium.Ground);

    public static MultiModalSegment FromDock(List<Vector3> path, Vector3 centroid) =>
        FromTerminal(TravelLegMode.Dock, path, centroid, PhysicalPathingMedium.Space);

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
            estimatedTimeSec = estimatedTimeSec,
            roadSegmentId = roadSegmentId,
            roadLotId = roadLotId,
            distanceAlongStart = distanceAlongStart,
            distanceAlongEnd = distanceAlongEnd,
            reverseLeg = reverseLeg,
            terminalSlotBounds = terminalSlotBounds,
            terminalCentroidWorld = terminalCentroidWorld,
            terminalUpWorld = terminalUpWorld,
            terminalSurfaceNormal = terminalSurfaceNormal,
            terminalWaterSurfaceY = terminalWaterSurfaceY,
            terminalPlaningSpeed = terminalPlaningSpeed,
            terminalHoldPolicy = terminalHoldPolicy,
            terminalSlotIndex = terminalSlotIndex,
            placementSlotConfig = placementSlotConfig,
            parkingZoneRef = parkingZoneRef,
            terminalScore = terminalScore,
            runningTotals = runningTotals,
            stuntZoneRef = stuntZoneRef,
            apertureId = apertureId,
            animationGroupTag = animationGroupTag,
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

    [Tooltip("Aggregate running totals across all segments.")]
    public TravelPlanRunningTotals planTotals = TravelPlanRunningTotals.Neutral;

    /// <summary>Rejected / alternate forks for broccoli-plume emergence viz.</summary>
    [NonSerialized] public List<MultiModalSegment> rejectedForks;

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
        p.planTotals = planTotals;
        if (segments == null)
            return p;
        foreach (MultiModalSegment seg in segments)
        {
            if (seg == null)
                continue;
            p.segments.Add(seg.CloneShallowRefs());
        }
        if (rejectedForks != null && rejectedForks.Count > 0)
        {
            p.rejectedForks = new List<MultiModalSegment>();
            foreach (MultiModalSegment fork in rejectedForks)
            {
                if (fork != null)
                    p.rejectedForks.Add(fork.CloneShallowRefs());
            }
        }

        return p;
    }

    public void RecomputePlanTotals()
    {
        var acc = TravelPlanRunningTotals.Neutral;
        if (segments == null)
        {
            planTotals = acc;
            return;
        }
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;
            acc = acc.Add(segments[i].runningTotals);
        }
        planTotals = acc;
    }
}
