using UnityEngine;

/// <summary>Road pump pad; optional rail-parallel refill when railSegmentId matches a train.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Gas/Fuel Pump")]
public sealed class FuelPumpRuntime : MonoBehaviour
{
    public string pumpId = "pump_1";
    public Transform nozzleAnchor;
    public bool roadEnabled = true;
    [Tooltip("When set, trains on this rail segment can refill alongside the station.")]
    public string railSegmentId;
    public float railParallelMaxDistM = 8f;
    public Collider railClaimVolume;
    public GasStationRuntime station;
    [Range(0f, 1f)] public float fuelStock01 = 1f;

    void Awake()
    {
        if (station == null)
            station = GetComponentInParent<GasStationRuntime>();
        if (nozzleAnchor == null)
            nozzleAnchor = transform;
    }

    public bool CanServeRoad() => roadEnabled && fuelStock01 > 0.01f;

    public bool CanServeRail(TrainVehicleRagdoll train)
    {
        if (train == null || string.IsNullOrEmpty(railSegmentId)) return false;
        if (fuelStock01 <= 0.01f) return false;
        if (!string.IsNullOrEmpty(train.railSegmentId)
            && !string.Equals(train.railSegmentId, railSegmentId, System.StringComparison.OrdinalIgnoreCase))
            return false;
        float d = Vector3.Distance(transform.position, train.transform.position);
        return d <= railParallelMaxDistM;
    }

    public bool TryRefuelBus(BusVehicleRagdoll bus, float fill01 = 1f)
    {
        if (!CanServeRoad() || bus == null) return false;
        bus.fuel01 = Mathf.Clamp01(fill01);
        DebitFuel(0.05f);
        station?.RecordFuelSale(bus.gameObject, 0.05f);
        return true;
    }

    public bool TryRefuelTrain(TrainVehicleRagdoll train, float fill01 = 1f)
    {
        if (!CanServeRail(train)) return false;
        train.fuel01 = Mathf.Clamp01(fill01);
        DebitFuel(0.12f);
        station?.RecordFuelSale(train.gameObject, 0.12f);
        station?.CreditLinkedTrainCompany(0.12f);
        return true;
    }

    void DebitFuel(float amount01) =>
        fuelStock01 = Mathf.Clamp01(fuelStock01 - Mathf.Max(0f, amount01));
}
