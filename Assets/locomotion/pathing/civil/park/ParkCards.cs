using System;
using UnityEngine;

[Serializable]
public class ParkMaintenanceCard : TravelAgentCard
{
    public ParkRuntime park;
    public DispatchRequest request;

    public static ParkMaintenanceCard Generate(DispatchRequest request, ParkRuntime park = null)
    {
        var c = new ParkMaintenanceCard();
        c.request = request;
        c.park = park;
        c.sectionName = "park_maintenance";
        c.description = "grounds_maintenance";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget
            : (park != null ? park.transform.position : Vector3.zero);
        if (park?.maintenanceDepot != null)
            c.goalWorld = park.maintenanceDepot.position;
        return c;
    }
}

[Serializable]
public class ParkHorticultureCard : TravelAgentCard
{
    public ParkRuntime park;
    public string narrativeActionId = "park_flower_tending";

    public static ParkHorticultureCard Generate(DispatchRequest request, ParkRuntime park = null)
    {
        var c = new ParkHorticultureCard();
        c.park = park;
        c.sectionName = "park_horticulture";
        c.description = request?.notes ?? "horticulture";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget
            : (park != null ? park.transform.position : Vector3.zero);
        if (!string.IsNullOrEmpty(request?.notes))
            c.narrativeActionId = request.notes;
        return c;
    }
}

[Serializable]
public class ParkJusticePatrolCard : TravelAgentCard
{
    public ParkRuntime park;

    public static ParkJusticePatrolCard Generate(DispatchRequest request, ParkRuntime park = null)
    {
        var c = new ParkJusticePatrolCard();
        c.park = park;
        c.sectionName = "park_patrol";
        c.description = "justice_patrol";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget
            : (park != null ? park.transform.position : Vector3.zero);
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, park != null ? park.gameObject : null);
        var lot = park?.PrimaryLot();
        if (lot != null)
            c.goalWorld = lot.ArrivalWorld;
        return c;
    }
}
