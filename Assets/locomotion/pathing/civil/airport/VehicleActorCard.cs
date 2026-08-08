using UnityEngine;

/// <summary>Thin TravelAgent card wrapping a VehicleActor for pilot / ground ops composition.</summary>
[System.Serializable]
public class VehicleActorCard : TravelAgentCard
{
    public VehicleActor vehicleActor;

    public VehicleActorCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "vehicle_actor";
        traversabilityTag = "vehicle";
    }

    public static VehicleActorCard Generate(VehicleActor actor, Vector3 goal)
    {
        var c = new VehicleActorCard
        {
            vehicleActor = actor,
            goalWorld = goal,
            goalTarget = actor != null ? actor.gameObject : null,
            sectionName = "vehicle_actor",
            description = "VehicleActor path",
            isTravelAgentGoal = true,
            isCivilGoal = true,
            limits = new SectionLimits { maxForce = 90f, maxTorque = 22f, maxVelocityChange = 2f }
        };
        return c;
    }
}

/// <summary>Thin waypoint guidance card for pilot composition.</summary>
[System.Serializable]
public class WaypointCard : TravelAgentCard
{
    public string waypointGroupId;
    public Vector3 waypointWorld;

    public WaypointCard()
    {
        isTravelAgentGoal = true;
        physicalPathingTag = "waypoint";
        traversabilityTag = "waypoint";
    }

    public static WaypointCard Generate(string groupId, Vector3 world)
    {
        return new WaypointCard
        {
            waypointGroupId = groupId,
            waypointWorld = world,
            goalWorld = world,
            waypointGroup = groupId,
            sectionName = "waypoint",
            description = groupId ?? "waypoint",
            isTravelAgentGoal = true,
            limits = new SectionLimits { maxForce = 70f, maxTorque = 18f, maxVelocityChange = 1.5f }
        };
    }
}
