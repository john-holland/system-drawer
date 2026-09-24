using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VehicleInventoryItem
{
    public string itemId;
    public string label;
    public int count = 1;
}

[Serializable]
public sealed class VehicleInventorySection
{
    public string sectionName = "cabin";
    public float capacity = 20f;
    public List<VehicleInventoryItem> items = new List<VehicleInventoryItem>();
}

/// <summary>Named vehicle with integrity and named interior inventory sections.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicle Ragdoll")]
public class VehicleRagdoll : MonoBehaviour
{
    public string vehicleId;
    public string displayName;
    [Range(0f, 1f)] public float integrity01 = 1f;
    public List<VehicleInventorySection> interiors = new List<VehicleInventorySection>();
    public bool available = true;
    [Tooltip("Optional total interior size cap; 0 = sum of section capacities.")]
    public float totalInteriorSize;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(vehicleId))
            vehicleId = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = vehicleId;
        if (interiors.Count == 0)
        {
            interiors.Add(new VehicleInventorySection { sectionName = "cabin", capacity = 10f });
            interiors.Add(new VehicleInventorySection { sectionName = "cargo", capacity = 40f });
        }
        RecalculateTotalInteriorSize();
    }

    public float ComputeInteriorSizeSum()
    {
        float sum = 0f;
        for (int i = 0; i < interiors.Count; i++)
            if (interiors[i] != null)
                sum += interiors[i].capacity;
        return sum;
    }

    public void RecalculateTotalInteriorSize()
    {
        float sum = ComputeInteriorSizeSum();
        if (totalInteriorSize <= 0f)
            totalInteriorSize = sum;
        else
            totalInteriorSize = Mathf.Max(totalInteriorSize, sum);
    }

    public Dictionary<string, object> ToDto()
    {
        RecalculateTotalInteriorSize();
        var sections = new List<object>();
        for (int i = 0; i < interiors.Count; i++)
        {
            var s = interiors[i];
            if (s == null) continue;
            var items = new List<object>();
            for (int j = 0; j < s.items.Count; j++)
            {
                var it = s.items[j];
                if (it == null) continue;
                items.Add(new Dictionary<string, object>
                {
                    ["itemId"] = it.itemId ?? "",
                    ["label"] = it.label ?? "",
                    ["count"] = it.count
                });
            }
            sections.Add(new Dictionary<string, object>
            {
                ["sectionName"] = s.sectionName ?? "",
                ["capacity"] = s.capacity,
                ["items"] = items
            });
        }
        return new Dictionary<string, object>
        {
            ["vehicleId"] = vehicleId,
            ["displayName"] = displayName,
            ["integrity01"] = integrity01,
            ["totalSize"] = totalInteriorSize,
            ["interiors"] = sections
        };
    }

    public void ApplyDto(Dictionary<string, object> dto)
    {
        if (dto == null) return;
        if (dto.TryGetValue("displayName", out var dn) && dn != null)
            displayName = dn.ToString();
        if (dto.TryGetValue("totalSize", out var ts) && ts != null
            && float.TryParse(ts.ToString(), out float size))
            totalInteriorSize = size;
    }
}
