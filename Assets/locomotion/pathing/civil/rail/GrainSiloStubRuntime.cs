using UnityEngine;

/// <summary>Grain silo stub — load/unload bulk commodities into train car bays.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Grain Silo Stub")]
public sealed class GrainSiloStubRuntime : TrainStationOpsBase
{
    public string commodityKey = "grain";
    public float siloQuantity = 1000f;
    public float transferRatePerOp = 50f;
    public Transform spoutAnchor;
    public BuildingRagdoll buildingRagdoll;

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
        if (activeCar == null)
            activeCar = GetComponentInChildren<TrainCarVehicleRagdoll>();
    }

    public bool LoadIntoCar(TrainCarVehicleRagdoll car, string bayId, float amount)
    {
        amount = Mathf.Min(amount > 0f ? amount : transferRatePerOp, siloQuantity);
        if (amount <= 0f) return false;
        if (!TransferBulk(car, bayId, commodityKey, amount)) return false;
        siloQuantity -= amount;
        car?.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.SiloLoad,
            SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public bool UnloadFromCar(TrainCarVehicleRagdoll car, string bayId, float amount)
    {
        amount = amount > 0f ? amount : transferRatePerOp;
        var bay = car?.FindBay(bayId);
        if (bay == null) return false;
        float take = Mathf.Min(amount, bay.bulkQuantity);
        if (!TransferBulk(car, bayId, commodityKey, -take)) return false;
        siloQuantity += take;
        return true;
    }
}
