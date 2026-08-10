using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TATransitCard : TravelAgentCard
{
    public DispatchRequest request;
    public BusVehicleRagdoll vehicle;
    public TAVehicleRoute route;

    public TATransitCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "ta_transit";
        traversabilityTag = "transit";
    }

    protected static void Fill(TATransitCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.limits = new SectionLimits { maxForce = 70f, maxTorque = 18f, maxVelocityChange = 1.4f };
    }
}

[Serializable]
public class TAVehicleRouteCard : TATransitCard
{
    public static TAVehicleRouteCard Generate(DispatchRequest request, TAVehicleRoute route = null)
    {
        var c = new TAVehicleRouteCard();
        Fill(c, request, "ta_vehicle_route");
        c.route = route;
        return c;
    }
}

[Serializable]
public class TAVehicleRecallCard : TATransitCard
{
    public static TAVehicleRecallCard Generate(DispatchRequest request)
    {
        var c = new TAVehicleRecallCard();
        Fill(c, request, "ta_vehicle_recall");
        return c;
    }
}

[Serializable]
public class TAVehicleParkHaltCard : TATransitCard
{
    public static TAVehicleParkHaltCard Generate(DispatchRequest request)
    {
        var c = new TAVehicleParkHaltCard();
        Fill(c, request, "ta_vehicle_park_halt");
        return c;
    }
}

[Serializable]
public class TAVehicleReleasePassengersCard : TATransitCard
{
    public TAPassengerReleaseMode releaseMode = TAPassengerReleaseMode.NextStop;
    public List<string> dialogSuggestions = new List<string>();

    public static TAVehicleReleasePassengersCard Generate(DispatchRequest request, TAPassengerReleaseMode mode)
    {
        var c = new TAVehicleReleasePassengersCard();
        Fill(c, request, "ta_vehicle_release_passengers");
        c.releaseMode = mode;
        c.dialogSuggestions.Add(mode == TAPassengerReleaseMode.OnSpotAsap
            ? "Please exit the vehicle now."
            : "We will release passengers at the next stop.");
        return c;
    }
}

[Serializable]
public class TAVehicleSpeakCard : TATransitCard
{
    public string utterance;
    public List<string> dialogSuggestions = new List<string>();

    public static TAVehicleSpeakCard Generate(DispatchRequest request, string utterance = null)
    {
        var c = new TAVehicleSpeakCard();
        Fill(c, request, "ta_vehicle_speak");
        c.utterance = utterance ?? request?.notes ?? "Attention passengers.";
        c.dialogSuggestions.Add(c.utterance);
        return c;
    }
}

[Serializable]
public class TAVehicleMusicCard : TATransitCard
{
    public string trackId;
    public bool play = true;

    public static TAVehicleMusicCard Generate(DispatchRequest request, string trackId = null, bool play = true)
    {
        var c = new TAVehicleMusicCard();
        Fill(c, request, "ta_vehicle_music");
        c.trackId = trackId ?? "cabin_ambient";
        c.play = play;
        return c;
    }

    /// <summary>Vehicle acts as a DAC host — notify audio / instrument proxy.</summary>
    public void ApplyToVehicle(BusVehicleRagdoll bus)
    {
        if (bus == null) return;
        bus.SetCabinMusic(trackId, play);
    }
}

[Serializable]
public class TAVehicleSoundDesignCard : TATransitCard
{
    [Range(0f, 1f)] public float engineBody01 = 0.5f;
    [Range(0f, 1f)] public float cabinHiss01 = 0.2f;
    [Range(0f, 1f)] public float doorThump01 = 0.4f;

    public static TAVehicleSoundDesignCard Generate(DispatchRequest request)
    {
        var c = new TAVehicleSoundDesignCard();
        Fill(c, request, "ta_vehicle_sound_design");
        return c;
    }

    public void ApplyToVehicle(BusVehicleRagdoll bus)
    {
        if (bus == null) return;
        bus.ApplySoundDesign(engineBody01, cabinHiss01, doorThump01);
    }
}

[Serializable]
public class TAVehicleParkCard : TATransitCard
{
    public static TAVehicleParkCard Generate(DispatchRequest request)
    {
        var c = new TAVehicleParkCard();
        Fill(c, request, "ta_vehicle_park");
        return c;
    }
}

[Serializable]
public class TAVehicleBayParkCard : TATransitCard
{
    public static TAVehicleBayParkCard Generate(DispatchRequest request)
    {
        var c = new TAVehicleBayParkCard();
        Fill(c, request, "ta_vehicle_bay_park");
        return c;
    }
}

[Serializable]
public sealed class TARepairPartPreposition
{
    public string partId;
    [Tooltip("Comma-separated spatial prepositions for SG4D actor placement.")]
    public string prepositionsCsv = "front,engine";

    public string[] Prepositions =>
        string.IsNullOrEmpty(prepositionsCsv)
            ? Array.Empty<string>()
            : prepositionsCsv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
}

[Serializable]
public class TAVehicleBayRepairCard : TATransitCard
{
    public List<TARepairPartPreposition> partPrepositions = new List<TARepairPartPreposition>();
    public string openCloseTopologyId = "vehicle_lift";

    public static TAVehicleBayRepairCard Generate(DispatchRequest request, BusVehicleRagdoll vehicle = null)
    {
        var c = new TAVehicleBayRepairCard();
        Fill(c, request, "ta_vehicle_bay_repair");
        c.vehicle = vehicle;
        c.partPrepositions = DefaultPartMap();
        c.goalTarget = vehicle != null ? vehicle.gameObject : null;
        if (vehicle != null)
            c.goalWorld = vehicle.transform.position;
        return c;
    }

    public static List<TARepairPartPreposition> DefaultPartMap()
    {
        return new List<TARepairPartPreposition>
        {
            new TARepairPartPreposition { partId = "carburetor", prepositionsCsv = "front,engine" },
            new TARepairPartPreposition { partId = "axle_rod", prepositionsCsv = "side,wheel" },
            new TARepairPartPreposition { partId = "oil_tank", prepositionsCsv = "under,chassis" }
        };
    }

    public Dictionary<string, string[]> ToSg4DPlacementMap()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < partPrepositions.Count; i++)
        {
            TARepairPartPreposition p = partPrepositions[i];
            if (p == null || string.IsNullOrEmpty(p.partId)) continue;
            map[p.partId] = p.Prepositions;
        }
        return map;
    }

    public TAMaintenanceCard ToMaintenanceCard() =>
        TAMaintenanceCard.GenerateRepair(vehicle);
}

[Serializable]
public class TAVehicleFuelCard : TATransitCard
{
    public string openCloseTopologyId = "fuel_port";
    [Range(0f, 1f)] public float fill01 = 1f;
    public TrainVehicleRagdoll train;

    public static TAVehicleFuelCard Generate(DispatchRequest request, BusVehicleRagdoll vehicle = null)
    {
        var c = new TAVehicleFuelCard();
        Fill(c, request, "ta_vehicle_fuel");
        c.vehicle = vehicle;
        c.goalTarget = vehicle != null ? vehicle.gameObject : null;
        if (vehicle != null)
            c.goalWorld = vehicle.transform.position;
        return c;
    }

    public static TAVehicleFuelCard GenerateForTrain(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = new TAVehicleFuelCard();
        Fill(c, request, "ta_vehicle_fuel");
        c.train = train;
        c.openCloseTopologyId = train != null ? train.fuelPortTopologyId : "fuel01";
        c.goalTarget = train != null ? train.gameObject : null;
        if (train != null)
            c.goalWorld = train.fuelPort != null ? train.fuelPort.position : train.transform.position;
        return c;
    }

    public void ApplyFuel()
    {
        if (vehicle != null)
            vehicle.fuel01 = Mathf.Clamp01(fill01);
        if (train != null)
            train.fuel01 = Mathf.Clamp01(fill01);
    }
}

[Serializable]
public class TAVehicleSchedulingCard : TATransitCard
{
    public string cronExpr = "* 6-22 * * 1-5";
    public string narrativeActionId = "ta_vehicle_schedule";

    public static TAVehicleSchedulingCard Generate(DispatchRequest request, TAVehicleRoute route = null)
    {
        var c = new TAVehicleSchedulingCard();
        Fill(c, request, "ta_vehicle_schedule");
        c.route = route;
        if (route != null)
            c.cronExpr = route.serviceCron;
        return c;
    }

    public static TAVehicleSchedulingCard GenerateHours(DispatchRequest request, string hoursCron)
    {
        var c = Generate(request, null);
        c.sectionName = "ta_vehicle_hours";
        c.cronExpr = hoursCron ?? "* * * * *";
        c.narrativeActionId = "ta_building_hours";
        if (!string.IsNullOrEmpty(request?.notes) && request.notes.StartsWith("hours:", StringComparison.OrdinalIgnoreCase))
            c.cronExpr = request.notes.Substring("hours:".Length).Trim();
        return c;
    }

    public void NotifyScheduler(GameObject host)
    {
        if (host == null) return;
        host.SendMessage("OnNarrativeSchedulerAction", narrativeActionId, SendMessageOptions.DontRequireReceiver);
    }
}

/// <summary>Ground-crew baggage load/unload for transit vehicles.</summary>
[Serializable]
public class TSAGroundCrewCard : TATransitCard
{
    public bool loading = true;
    public string bayId;

    public static TSAGroundCrewCard Generate(BusVehicleRagdoll vehicle, bool loading, string bayId = null)
    {
        var c = new TSAGroundCrewCard();
        Fill(c, null, loading ? "tsa_ground_crew_load" : "tsa_ground_crew_unload");
        c.vehicle = vehicle;
        c.loading = loading;
        c.bayId = bayId;
        c.goalTarget = vehicle != null ? vehicle.gameObject : null;
        if (vehicle != null)
            c.goalWorld = vehicle.transform.position;
        return c;
    }
}
