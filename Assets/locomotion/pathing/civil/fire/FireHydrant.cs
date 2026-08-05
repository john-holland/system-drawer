using UnityEngine;

/// <summary>Hydrant with open/close/connect stubs; pressure from MunicipalWaterService.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fire Hydrant")]
public sealed class FireHydrant : MonoBehaviour
{
    public bool isOpen;
    public bool connected;
    public FireTruckVehicleRagdoll connectedTruck;
    public MunicipalWaterService municipalWater;
    public Transform connectPoint;
    [Range(0f, 2f)] public float flowAccess01 = 1f;

    void Awake()
    {
        if (municipalWater == null)
            municipalWater = FindFirstObjectByType<MunicipalWaterService>();
        if (connectPoint == null)
            connectPoint = transform;
    }

    public float EffectiveFlow01()
    {
        float p = municipalWater != null ? municipalWater.EffectivePressure01() : 1f;
        return isOpen && connected ? Mathf.Clamp01(p * 0.5f) * flowAccess01 : 0f;
    }

    public void Open() => isOpen = true;
    public void Close()
    {
        isOpen = false;
        connected = false;
        connectedTruck = null;
    }

    public bool TryConnect(FireTruckVehicleRagdoll truck)
    {
        if (truck == null) return false;
        Open();
        connected = true;
        connectedTruck = truck;
        truck.WindHose(0.1f, true);
        return true;
    }

    public float PumpFillLitersPerSec() => EffectiveFlow01() * 40f;
}
