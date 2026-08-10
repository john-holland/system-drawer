using System.Collections.Generic;
using UnityEngine;

/// <summary>Recycling bring-up — TransferBulk-style ledger for trucks / transfer station.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Recycling Transfer")]
public sealed class SanitationRecyclingTransfer : MonoBehaviour
{
    public string commodityKey = "recyclables";
    public float stockQuantity;
    public Transform bayAnchor;
    public SanitationFacilityRuntime facility;
    public List<string> acceptedKeys = new List<string> { "recyclables", "plastic", "metal", "paper" };

    void Awake()
    {
        if (facility == null)
            facility = GetComponentInParent<SanitationFacilityRuntime>();
    }

    public float TransferBulk(string key, float deltaQuantity)
    {
        if (!string.IsNullOrEmpty(key) && acceptedKeys.Count > 0 && !acceptedKeys.Contains(key))
            return 0f;
        if (!string.IsNullOrEmpty(key))
            commodityKey = key;
        stockQuantity = Mathf.Max(0f, stockQuantity + deltaQuantity);
        SendMessage("OnSanitationTransferBulk", commodityKey + "|" + deltaQuantity,
            SendMessageOptions.DontRequireReceiver);
        return stockQuantity;
    }

    public float UnloadFromTruck(GarbageTruckVehicleRagdoll truck, float maxQty = 50f)
    {
        if (truck?.hopper == null) return 0f;
        float take = Mathf.Min(maxQty, truck.hopper.massKg);
        if (take <= 0f) return 0f;
        truck.hopper.massKg -= take;
        truck.hopper.RebuildParticlesFromMass();
        return TransferBulk(commodityKey, take);
    }
}
