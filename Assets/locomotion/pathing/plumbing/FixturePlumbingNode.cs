using UnityEngine;

public enum FixtureKind
{
    Toilet = 0,
    Sink = 1,
    Shower = 2,
    Generic = 3
}

/// <summary>Per-fixture inflow/outflow node with independent hot/cold branches.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Fixture Plumbing Node")]
public sealed class FixturePlumbingNode : MonoBehaviour
{
    public FixtureKind fixtureKind = FixtureKind.Generic;
    public string buildingPlumbingGroupId = "default";
    public string branchIdCold = "cold_a";
    public string branchIdHot = "hot_a";
    public string branchIdDrain = "drain_a";
    [Range(0f, 1f)] public float inflowCold01;
    [Range(0f, 1f)] public float inflowHot01;
    [Range(0f, 1f)] public float outflowDrain01;
    public ClogState clog = new ClogState();
    public MunicipalWaterService water;
    public BuildingPlumbingGroup plumbingGroup;

    [Header("Cross-talk (default off)")]
    public bool sinkGetsHotWhenToiletFlushed;
    public bool showerGetsHotWhenToiletFlushed;

    [Header("Overflow / blow-off")]
    [Tooltip("Toilet: default on. Sink/shower: default off.")]
    public bool overflowJetEnabled;
    [Tooltip("Sink/shower pressure-gauge blow — default off.")]
    public bool pressureGaugeBlowEnabled;
    [Range(0.5f, 2f)] public float blowPressureThreshold = 1.35f;
    public bool nozzlePoppedOff;

    void Awake()
    {
        if (water == null)
            water = MunicipalWaterService.Instance;
        if (plumbingGroup == null)
            plumbingGroup = GetComponentInParent<BuildingPlumbingGroup>();
        if (fixtureKind == FixtureKind.Toilet)
            overflowJetEnabled = true;
        else
            pressureGaugeBlowEnabled = false;
    }

    public float AvailableCold01()
    {
        float shut = ShutoffMul();
        float baseC = water != null ? water.EffectiveCold01() * water.EffectivePressure01() : 1f;
        return Mathf.Clamp01(baseC * (1f - GetCrossHeatSteal01()) * shut);
    }

    public float AvailableHot01()
    {
        float shut = ShutoffMul();
        float municipal = water != null ? water.EffectiveHot01() * water.EffectivePressure01() : 0.85f;
        float heater = plumbingGroup != null && plumbingGroup.heaterHot01 >= 0f
            ? plumbingGroup.heaterHot01
            : municipal;
        return Mathf.Clamp01(heater * shut + GetCrossHeatSpike01());
    }

    float ShutoffMul()
    {
        var off = plumbingGroup != null ? plumbingGroup.shutoff : null;
        if (off == null && plumbingGroup != null)
            off = plumbingGroup.GetComponent<BuildingWaterShutoff>();
        return off != null && !off.open ? 0f : 1f;
    }

    float GetCrossHeatSteal01()
    {
        if (plumbingGroup == null) return 0f;
        if (fixtureKind == FixtureKind.Sink && sinkGetsHotWhenToiletFlushed)
            return plumbingGroup.ToiletFlushCrossTalk01;
        if (fixtureKind == FixtureKind.Shower && showerGetsHotWhenToiletFlushed)
            return plumbingGroup.ToiletFlushCrossTalk01;
        return 0f;
    }

    float GetCrossHeatSpike01()
    {
        if (plumbingGroup == null) return 0f;
        if (fixtureKind == FixtureKind.Sink && sinkGetsHotWhenToiletFlushed)
            return plumbingGroup.ToiletFlushCrossTalk01 * 0.6f;
        if (fixtureKind == FixtureKind.Shower && showerGetsHotWhenToiletFlushed)
            return plumbingGroup.ToiletFlushCrossTalk01 * 0.6f;
        return 0f;
    }

    public void SetInflow(float cold01, float hot01)
    {
        inflowCold01 = Mathf.Clamp01(cold01) * AvailableCold01();
        inflowHot01 = Mathf.Clamp01(hot01) * AvailableHot01();
        MaybeBlowOff();
    }

    public float SetOutflow(float demand01)
    {
        float sewer = water != null ? water.EffectiveSewer01() : 1f;
        float clogMul = clog != null ? clog.OutflowMultiplier() : 1f;
        outflowDrain01 = Mathf.Clamp01(demand01) * sewer * clogMul;
        return outflowDrain01;
    }

    void MaybeBlowOff()
    {
        if (!pressureGaugeBlowEnabled || nozzlePoppedOff) return;
        float p = water != null ? water.EffectivePressure01() : 1f;
        if (p >= blowPressureThreshold)
            nozzlePoppedOff = true;
    }

    public float CombinedInflowLitersPerSec(float maxLps = 0.4f)
    {
        if (nozzlePoppedOff && fixtureKind != FixtureKind.Toilet)
            return maxLps * 1.5f; // uncontrolled spout
        return (inflowCold01 + inflowHot01) * 0.5f * maxLps;
    }
}
