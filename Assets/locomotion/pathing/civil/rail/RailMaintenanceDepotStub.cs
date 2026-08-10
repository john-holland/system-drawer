using System.Collections.Generic;
using UnityEngine;

/// <summary>Rail maintenance depot stub — pull/replace cars, limbs, bay contents, lash inspect.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Rail Maintenance Depot Stub")]
public sealed class RailMaintenanceDepotStub : TrainStationOpsBase
{
    public List<Transform> shopBays = new List<Transform>();
    public List<TrainCarVehicleRagdoll> carsInShop = new List<TrainCarVehicleRagdoll>();
    public VehicleRepairCenterRuntime nestedRepair;
    public BuildingRagdoll buildingRagdoll;

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
        if (nestedRepair == null)
            nestedRepair = GetComponentInChildren<VehicleRepairCenterRuntime>();
        if (activeConsist == null)
            activeConsist = GetComponentInChildren<TrainConsistRuntime>();
    }

    public bool PullCarIntoShop(TrainCarVehicleRagdoll car)
    {
        if (car == null) return false;
        RemoveCar(car);
        if (!carsInShop.Contains(car))
            carsInShop.Add(car);
        if (shopBays.Count > 0)
        {
            var bay = shopBays[Mathf.Min(carsInShop.Count - 1, shopBays.Count - 1)];
            if (bay != null)
            {
                car.transform.SetParent(bay, true);
                car.transform.localPosition = Vector3.zero;
            }
        }
        activeCar = car;
        car.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.DepotReplace,
            SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public bool ReinsertCar(TrainCarVehicleRagdoll car, int consistIndex = -1)
    {
        if (car == null || activeConsist == null) return false;
        carsInShop.Remove(car);
        car.transform.SetParent(activeConsist.transform, true);
        if (consistIndex < 0)
            activeConsist.AddCar(car);
        else
            activeConsist.InsertCar(consistIndex, car);
        return true;
    }

    public bool Relash(TrainCarVehicleRagdoll car)
    {
        if (car?.lashRuntime == null) return false;
        car.lashRuntime.ApplyProfile(car.lashRuntime.profile, car.defaultStabilityMode);
        car.lashRuntime.TickEvaluate(Vector3.zero);
        return true;
    }
}
