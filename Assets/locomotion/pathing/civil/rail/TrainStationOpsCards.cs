using System;
using UnityEngine;

/// <summary>BT-callable station / silo / depot cards for train car ops.</summary>
[Serializable]
public class TrainStationOpsCard : TravelAgentCard
{
    public TrainStationOpsBase ops;
    public TrainVehicleRagdoll car;
    public TrainVehicleRagdoll consist;

    protected ITrainStationOps ResolveOps(GameObject host)
    {
        if (ops != null) return ops;
        if (host != null)
        {
            ops = host.GetComponentInParent<TrainStationOpsBase>()
                  ?? host.GetComponent<TrainStationOpsBase>();
        }
        return ops;
    }

    protected TrainVehicleRagdoll ResolveCar(ITrainStationOps o)
    {
        if (car != null) return car;
        return o?.ActiveCar;
    }
}

[Serializable]
public sealed class TrainStationCoupleCard : TrainStationOpsCard
{
    public TrainVehicleRagdoll front;
    public TrainVehicleRagdoll rear;

    public TrainStationCoupleCard()
    {
        sectionName = "train_station_couple";
        description = "Couple two cars at platform";
        physicalPathingTag = "train_couple";
    }

    public bool Execute(GameObject host)
    {
        var o = ResolveOps(host);
        var a = front != null ? front : ResolveCar(o);
        var b = rear;
        if (a == null || b == null) return false;
        return o != null && o.CoupleCars(a, b);
    }
}

[Serializable]
public sealed class TrainStationSwapCarCard : TrainStationOpsCard
{
    public int carIndex;
    public TrainVehicleRagdoll replacement;

    public TrainStationSwapCarCard()
    {
        sectionName = "train_station_swap_car";
        description = "Remove/replace car in consist";
        physicalPathingTag = "train_swap_car";
    }

    public bool Execute(GameObject host)
    {
        var o = ResolveOps(host);
        return o != null && o.SwapCar(carIndex, replacement);
    }
}

[Serializable]
public sealed class TrainStationUnloadBayCard : TrainStationOpsCard
{
    public string bayId = "deck";
    public VehicleRagdoll vehicle;

    public TrainStationUnloadBayCard()
    {
        sectionName = "train_station_unload_bay";
        description = "Unfold bay and unload nested vehicle";
        physicalPathingTag = "train_unload_bay";
    }

    public bool Execute(GameObject host)
    {
        var o = ResolveOps(host);
        var c = ResolveCar(o);
        return o != null && o.UnloadBay(c, bayId, vehicle);
    }
}

[Serializable]
public sealed class TrainStationLimbWorkCard : TrainStationOpsCard
{
    public string limbId = "main_crane";
    public bool unfold = true;

    public TrainStationLimbWorkCard()
    {
        sectionName = "train_station_limb_work";
        description = "Unfold/refold crane or dig limb at siding";
        physicalPathingTag = "train_limb_work";
    }

    public bool Execute(GameObject host)
    {
        var o = ResolveOps(host);
        var c = ResolveCar(o);
        if (o == null || c == null) return false;
        return unfold ? o.UnfoldLimb(c, limbId) : o.RefoldLimb(c, limbId);
    }
}

[Serializable]
public sealed class SiloLoadUnloadCard : TrainStationOpsCard
{
    public string bayId = "deck";
    public float amount;
    public bool loadIntoCar = true;

    public SiloLoadUnloadCard()
    {
        sectionName = "silo_load_unload";
        description = "Silo commodity ↔ car bulk bay";
        physicalPathingTag = "train_silo";
    }

    public bool Execute(GameObject host)
    {
        var silo = host != null
            ? host.GetComponentInParent<GrainSiloStubRuntime>()
            : null;
        if (silo == null) silo = ops as GrainSiloStubRuntime;
        var c = ResolveCar(silo);
        if (silo == null || c == null) return false;
        return loadIntoCar ? silo.LoadIntoCar(c, bayId, amount) : silo.UnloadFromCar(c, bayId, amount);
    }
}

[Serializable]
public sealed class RailDepotReplaceCarCard : TrainStationOpsCard
{
    public bool pullIntoShop = true;
    public int reinsertIndex = -1;

    public RailDepotReplaceCarCard()
    {
        sectionName = "rail_depot_replace_car";
        description = "Depot pull/replace/reinsert car";
        physicalPathingTag = "train_depot_replace";
    }

    public bool Execute(GameObject host)
    {
        var depot = host != null
            ? host.GetComponentInParent<RailMaintenanceDepotStub>()
            : null;
        if (depot == null) depot = ops as RailMaintenanceDepotStub;
        var c = ResolveCar(depot);
        if (depot == null || c == null) return false;
        return pullIntoShop ? depot.PullCarIntoShop(c) : depot.ReinsertCar(c, reinsertIndex);
    }
}

[Serializable]
public sealed class RailDepotLashInspectCard : TrainStationOpsCard
{
    public bool relash;

    public RailDepotLashInspectCard()
    {
        sectionName = "rail_depot_lash_inspect";
        description = "Inspect lash stability; optional re-lash";
        physicalPathingTag = "train_depot_lash";
    }

    public bool Execute(GameObject host, out float stable01)
    {
        stable01 = 1f;
        var depot = host != null
            ? host.GetComponentInParent<RailMaintenanceDepotStub>()
            : null;
        if (depot == null) depot = ops as RailMaintenanceDepotStub;
        var o = (ITrainStationOps)depot ?? ResolveOps(host);
        var c = ResolveCar(o);
        if (o == null || c == null) return false;
        stable01 = o.InspectLashStable01(c);
        if (relash && depot != null)
            depot.Relash(c);
        return true;
    }
}
