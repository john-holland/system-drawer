using System;
using UnityEngine;

public enum TASignKind
{
    Yield = 0,
    Stop = 1,
    SlowChildren = 2,
    BlindDrive = 3,
    Custom = 4
}

/// <summary>One-way street constraint + optional sign prefab.</summary>
[Serializable]
public class TAOneWayStreetCard : TravelAgentCard
{
    public float headingYaw;
    public GameObject signPrefab;

    public TAOneWayStreetCard()
    {
        sectionName = "ta_one_way";
        physicalPathingTag = "ta_one_way";
        preferFlee = false;
    }

    public static TAOneWayStreetCard Generate(float yaw, Vector3 goal, GameObject signPrefab = null)
    {
        return new TAOneWayStreetCard
        {
            headingYaw = yaw,
            signPrefab = signPrefab,
            goalWorld = goal,
            sectionName = "ta_one_way"
        };
    }

    public Vector3 HeadingDirection => Quaternion.Euler(0f, headingYaw, 0f) * Vector3.forward;
}

/// <summary>Detour with signage prefabs and placement prompt.</summary>
[Serializable]
public class TADetourCard : TravelAgentCard
{
    public GameObject[] signagePrefabs;
    public string placementPrompt = "Place detour signage";
    public Vector3 detourGoalWorld;

    public TADetourCard()
    {
        sectionName = "ta_detour";
        physicalPathingTag = "ta_detour";
        preferFlee = false;
    }

    public static TADetourCard Generate(Vector3 detourGoal, string prompt = null, GameObject[] prefabs = null)
    {
        return new TADetourCard
        {
            detourGoalWorld = detourGoal,
            goalWorld = detourGoal,
            placementPrompt = prompt ?? "Place detour signage",
            signagePrefabs = prefabs,
            sectionName = "ta_detour"
        };
    }
}

[Serializable]
public class TAStopSignCard : TravelAgentCard
{
    public GameObject signPrefab;
    public float approachHeadingYaw;
    public float holdSec = 2f;

    public TAStopSignCard()
    {
        sectionName = "ta_stop_sign";
        physicalPathingTag = "ta_stop_sign";
        preferFlee = false;
    }

    public static TAStopSignCard Generate(Vector3 world, float approachYaw, GameObject signPrefab = null)
    {
        return new TAStopSignCard
        {
            goalWorld = world,
            approachHeadingYaw = approachYaw,
            signPrefab = signPrefab,
            sectionName = "ta_stop_sign"
        };
    }
}

[Serializable]
public class TAIntersectionCard : TravelAgentCard
{
    public TrafficLightController lightController;
    public PixelLightPatternAsset pixelLightPattern;
    public PixelLightColorPackage pixelLightColors;
    public TrafficDetailLadderAsset ladderAsset;
    public TrafficLightLadderTiming lightTiming = TrafficLightLadderTiming.Default();

    public TAIntersectionCard()
    {
        sectionName = "ta_intersection";
        physicalPathingTag = "ta_intersection";
        preferFlee = false;
    }

    public static TAIntersectionCard Generate(Vector3 world)
    {
        return new TAIntersectionCard
        {
            goalWorld = world,
            sectionName = "ta_intersection"
        };
    }
}

[Serializable]
public struct TrafficLightLadderTiming
{
    public float mainGreenSec;
    public float sideGreenSec;
    public float yellowSec;
    public float allRedSec;
    public float sideSensorExtendSec;

    public static TrafficLightLadderTiming Default() => new TrafficLightLadderTiming
    {
        mainGreenSec = 12f,
        sideGreenSec = 8f,
        yellowSec = 3f,
        allRedSec = 1.2f,
        sideSensorExtendSec = 4f
    };

    public void ApplyTo(TrafficLightController ctrl)
    {
        if (ctrl == null) return;
        ctrl.mainGreenSec = mainGreenSec;
        ctrl.sideGreenSec = sideGreenSec;
        ctrl.yellowSec = yellowSec;
        ctrl.allRedSec = allRedSec;
        ctrl.sideSensorExtendSec = sideSensorExtendSec;
    }
}

[Serializable]
public class TASchoolBusStopCard : TravelAgentCard
{
    public float stopRadius = 8f;
    public string scheduleCron = "0 7-9 * * 1-5";
    public GameObject busPrefab;
    public Bounds stopZone;

    public TASchoolBusStopCard()
    {
        sectionName = "ta_school_bus_stop";
        physicalPathingTag = "ta_school_bus_stop";
        preferFlee = false;
    }

    public static TASchoolBusStopCard Generate(Vector3 world, float radius = 8f)
    {
        return new TASchoolBusStopCard
        {
            goalWorld = world,
            stopRadius = radius,
            stopZone = new Bounds(world, Vector3.one * radius * 2f),
            sectionName = "ta_school_bus_stop"
        };
    }
}

[Serializable]
public class TABuildingTypeCard : TravelAgentCard
{
    public CivilSystemKind buildingKind = CivilSystemKind.Generic;
    public string buildingTypeId;
    public BuildingRequirementSpec buildingConfig;
    public GameObject buildingPrefab;

    public TABuildingTypeCard()
    {
        sectionName = "ta_building_type";
        physicalPathingTag = "ta_building";
        isCivilGoal = true;
        preferFlee = false;
    }

    public static TABuildingTypeCard Generate(CivilSystemKind kind, Vector3 world, BuildingRequirementSpec config = null)
    {
        return new TABuildingTypeCard
        {
            buildingKind = kind,
            buildingTypeId = config != null ? config.buildingTypeId : kind.ToString(),
            buildingConfig = config,
            goalWorld = world,
            sectionName = "ta_building_type"
        };
    }
}

[Serializable]
public class TASignCard : TravelAgentCard
{
    public TASignKind signKind = TASignKind.Stop;
    public float avoidCostMultiplier = 3f;
    public float slowRadius = 12f;
    public string hintText;
    public GameObject signPrefab;

    public TASignCard()
    {
        sectionName = "ta_sign";
        physicalPathingTag = "ta_sign";
        preferFlee = false;
    }

    public static TASignCard Generate(TASignKind kind, Vector3 world, float costMul = 3f, float radius = 12f)
    {
        return new TASignCard
        {
            signKind = kind,
            goalWorld = world,
            avoidCostMultiplier = costMul,
            slowRadius = radius,
            hintText = kind.ToString(),
            sectionName = "ta_sign_" + kind
        };
    }

    public void ApplyHintsTo(TravelAgent agent)
    {
        if (agent == null || agent.ignoreTrafficAvoidance) return;
        agent.avoidRadius = Mathf.Max(agent.avoidRadius, slowRadius);
        agent.avoidCostMultiplier = Mathf.Max(agent.avoidCostMultiplier, avoidCostMultiplier);
        if (goalTarget != null && !agent.avoidActors.Contains(goalTarget.transform))
            agent.avoidActors.Add(goalTarget.transform);
    }
}
