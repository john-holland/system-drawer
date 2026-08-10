using System;
using System.Collections.Generic;
using UnityEngine;

public enum SanitationDownflowStage
{
    ShakeFilter = 0,
    Boil = 1,
    FilterExtrude = 2,
    CommodityOut = 3
}

public enum SanitationEgressMode
{
    Truck = 0,
    Train = 1,
    Boat = 2,
    Pipe = 3
}

[Serializable]
public sealed class SanitationDownflowSection
{
    public string sectionId = "downflow_1";
    public SanitationDownflowStage stage = SanitationDownflowStage.ShakeFilter;
    public string commodityKey = "fertilizer";
    public SanitationEgressMode egress = SanitationEgressMode.Truck;
    public Transform anchor;
    [Range(0f, 1f)] public float throughput01 = 0.5f;
}

[Serializable]
public sealed class SanitationPoopStage
{
    public string stageId = "inflow";
    [Range(0f, 1f)] public float fill01;
    public float flowInPerSec = 0.05f;
    public float flowOutPerSec = 0.04f;
    public string commodityOutKey = "fertilizer_slurry";
}

/// <summary>Poop stages inflow → outflow → fertilizer commodities for factories/farms.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Poop Quifer")]
public sealed class SanitationPoopQuifer : MonoBehaviour
{
    public List<SanitationPoopStage> stages = new List<SanitationPoopStage>();
    public string fertilizerCommodityKey = "fertilizer";
    public float fertilizerStock;
    public SanitationFacilityRuntime facility;

    void Awake()
    {
        if (facility == null)
            facility = GetComponentInParent<SanitationFacilityRuntime>();
        if (stages.Count == 0)
        {
            stages.Add(new SanitationPoopStage { stageId = "inflow", flowInPerSec = 0.08f, flowOutPerSec = 0.05f });
            stages.Add(new SanitationPoopStage { stageId = "digest", flowInPerSec = 0.05f, flowOutPerSec = 0.04f });
            stages.Add(new SanitationPoopStage
            {
                stageId = "outflow",
                flowInPerSec = 0.04f,
                flowOutPerSec = 0.03f,
                commodityOutKey = fertilizerCommodityKey
            });
        }
    }

    public void Tick(float dt)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (s == null) continue;
            s.fill01 = Mathf.Clamp01(s.fill01 + s.flowInPerSec * dt - s.flowOutPerSec * dt);
            if (i + 1 < stages.Count && stages[i + 1] != null)
                stages[i + 1].fill01 = Mathf.Clamp01(stages[i + 1].fill01 + s.flowOutPerSec * dt * 0.5f);
        }
        var last = stages.Count > 0 ? stages[stages.Count - 1] : null;
        if (last != null && last.fill01 > 0.2f)
        {
            float produced = last.flowOutPerSec * dt * 10f;
            fertilizerStock += produced;
            last.fill01 = Mathf.Max(0f, last.fill01 - 0.01f * dt);
        }
    }

    public void AcceptInflow(float amount01)
    {
        if (stages.Count == 0) return;
        stages[0].fill01 = Mathf.Clamp01(stages[0].fill01 + Mathf.Max(0f, amount01));
    }

    public float TransferFertilizerTo(float qty)
    {
        float take = Mathf.Min(fertilizerStock, Mathf.Max(0f, qty));
        fertilizerStock -= take;
        return take;
    }
}
