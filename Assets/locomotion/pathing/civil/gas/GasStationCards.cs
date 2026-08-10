using System;
using UnityEngine;

[Serializable]
public class GasStationStoreCard : TravelAgentCard
{
    public GasStationRuntime station;
    public string shelfLemma = GasStationShelfLemmaKeys.EyeLevel;
    public DispatchRequest request;

    public static GasStationStoreCard Generate(DispatchRequest request, GasStationRuntime station = null)
    {
        var c = new GasStationStoreCard();
        c.request = request;
        c.station = station;
        c.sectionName = "gas_store";
        c.description = request != null ? request.kind : "gas_store";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        if (!string.IsNullOrEmpty(request?.notes) && GasStationShelfLemmaKeys.IsShelfLemma(request.notes))
            c.shelfLemma = request.notes;
        return c;
    }

    public StoreShelfSlot ResolveShelf() => station?.FindShelfByLemma(shelfLemma);
}

[Serializable]
public class GasStationRailRefuelCard : TravelAgentCard
{
    public GasStationRuntime station;
    public TrainVehicleRagdoll train;
    public string railSegmentId;
    [Range(0f, 1f)] public float fill01 = 1f;

    public static GasStationRailRefuelCard Generate(DispatchRequest request, GasStationRuntime station = null)
    {
        var c = new GasStationRailRefuelCard();
        c.station = station;
        c.sectionName = "gas_rail_refuel";
        c.description = "rail_parallel_refuel";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.isTravelAgentGoal = true;
        c.railSegmentId = request?.notes;
        return c;
    }

    public bool Apply()
    {
        if (train == null) return false;
        var pump = station?.FindRailPump(railSegmentId ?? train.railSegmentId);
        return pump != null && pump.TryRefuelTrain(train, fill01);
    }
}
