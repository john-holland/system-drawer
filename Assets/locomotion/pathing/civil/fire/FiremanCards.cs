using UnityEngine;

[System.Serializable]
public class FiremanCard : GoodSection
{
    public string duty = "respond";

    public FiremanCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "fireman";
        traversabilityTag = "fireman";
    }

    public static FiremanCard Generate(string duty)
    {
        return new FiremanCard
        {
            duty = duty ?? "respond",
            sectionName = $"fireman_{duty}",
            description = duty,
            isCivilGoal = true,
            limits = new SectionLimits { maxForce = 100f, maxTorque = 30f, maxVelocityChange = 2f }
        };
    }
}

[System.Serializable]
public class FiremanDispatcherCallinRequestCard : FiremanCard
{
    public string personaKey;
    public string telecomTargetId;

    public static FiremanDispatcherCallinRequestCard Generate(string personaKey, string telecomId = null)
    {
        return new FiremanDispatcherCallinRequestCard
        {
            duty = "callin",
            personaKey = personaKey,
            telecomTargetId = telecomId,
            sectionName = "fireman_callin",
            description = "dispatcher_callin_telecom",
            isCivilGoal = true
        };
    }
}

[System.Serializable]
public class FiremanAssembleCard : FiremanCard
{
    public FireTruckVehicleRagdoll truck;

    public static FiremanAssembleCard Generate(FireTruckVehicleRagdoll truck)
    {
        return new FiremanAssembleCard
        {
            duty = "assemble",
            truck = truck,
            sectionName = "fireman_assemble",
            isCivilGoal = true
        };
    }
}

[System.Serializable]
public class FiremanSirensOnCard : FiremanCard
{
    public FireTruckVehicleRagdoll truck;
    public static FiremanSirensOnCard Generate(FireTruckVehicleRagdoll truck) =>
        new FiremanSirensOnCard { duty = "siren_on", truck = truck, sectionName = "fireman_siren_on" };
}

[System.Serializable]
public class FiremanSirenOffCard : FiremanCard
{
    public FireTruckVehicleRagdoll truck;
    public static FiremanSirenOffCard Generate(FireTruckVehicleRagdoll truck) =>
        new FiremanSirenOffCard { duty = "siren_off", truck = truck, sectionName = "fireman_siren_off" };
}

[System.Serializable]
public class FiremanStationSirensOnCard : FiremanCard
{
    public FirehouseBioRhythm bio;
    public static FiremanStationSirensOnCard Generate(FirehouseBioRhythm bio) =>
        new FiremanStationSirensOnCard { duty = "station_siren_on", bio = bio, sectionName = "fireman_station_siren_on" };
}

[System.Serializable]
public class FiremanStationSirenOffCard : FiremanCard
{
    public FirehouseBioRhythm bio;
    public static FiremanStationSirenOffCard Generate(FirehouseBioRhythm bio) =>
        new FiremanStationSirenOffCard { duty = "station_siren_off", bio = bio, sectionName = "fireman_station_siren_off" };
}

[System.Serializable]
public class FiremanPulloutCard : FiremanCard
{
    public FireTruckVehicleRagdoll truck;
    public Transform bayDoor;
    public static FiremanPulloutCard Generate(FireTruckVehicleRagdoll truck, Transform bayDoor = null) =>
        new FiremanPulloutCard { duty = "pullout", truck = truck, bayDoor = bayDoor, sectionName = "fireman_pullout" };
}

[System.Serializable]
public class FiremanDriveToCard : FiremanCard
{
    public FireTruckVehicleRagdoll truck;
    public Vector3 goalWorld;
    public static FiremanDriveToCard Generate(FireTruckVehicleRagdoll truck, Vector3 goal) =>
        new FiremanDriveToCard
        {
            duty = "drive",
            truck = truck,
            goalWorld = goal,
            sectionName = "fireman_drive",
            isTravelAgentGoal = true
        };
}

[System.Serializable]
public class FiremanBodyCarryCard : FiremanCard
{
    public GameObject victim;
    public static FiremanBodyCarryCard Generate(GameObject victim) =>
        new FiremanBodyCarryCard { duty = "body_carry", victim = victim, sectionName = "fireman_body_carry" };
}

[System.Serializable]
public class FiremanDoorDestroyCard : FiremanCard
{
    public GameObject doorOrBarrier;
    public string toolKind = "axe";

    public FiremanDoorDestroyCard()
    {
        isCivicGoal = true;
    }

    public static FiremanDoorDestroyCard Generate(GameObject door, string tool = "axe") =>
        new FiremanDoorDestroyCard
        {
            duty = "door_destroy",
            doorOrBarrier = door,
            toolKind = tool,
            sectionName = $"fireman_door_{tool}",
            isCivicGoal = true,
            isCivilGoal = true
        };
}
