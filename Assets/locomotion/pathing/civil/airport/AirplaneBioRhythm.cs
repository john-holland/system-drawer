using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-plane bio — power bus tick, stress from draw/charge, progressive load shed/restore.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Bio Rhythm")]
public sealed class AirplaneBioRhythm : DispatchBioRhythm
{
    public AirplaneVehicleRagdoll airplane;
    public AirplanePowerBus powerBus = new AirplanePowerBus();
    public bool enginesRunning = true;
    [Range(0f, 1f)] public float shedChargeThreshold01 = 0.25f;
    [Range(0f, 1f)] public float restoreChargeThreshold01 = 0.4f;
    readonly HashSet<string> _shedSystems = new HashSet<string>();

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "airplane_" + gameObject.name;
        base.Awake();
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>() ?? GetComponentInParent<AirplaneVehicleRagdoll>();
        airplane?.EnsureDefaultPowerSystems();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>();
        if (airplane == null || powerBus == null) return;

        airplane.EnsureDefaultPowerSystems();
        powerBus.ScaleComfortDrawRows(airplane.powerSystems, airplane);
        float chargeKw = enginesRunning ? Mathf.Max(0f, airplane.chargeKwWhenEnginesOn) : 0f;
        powerBus.Tick(dt, airplane.batteries, airplane.powerSystems, chargeKw);

        float headroom = powerBus.maxDrawKw > 1e-3f
            ? 1f - Mathf.Clamp01(powerBus.totalDrawKw / powerBus.maxDrawKw)
            : 0f;
        unitsAvailable01 = Mathf.Clamp01(powerBus.charge01 * 0.7f + headroom * 0.3f);
        alert01 = Mathf.Clamp01((1f - powerBus.charge01) * 0.6f + (1f - headroom) * 0.4f);
        if (venueBio != null)
        {
            venueBio.stress01 = alert01;
            venueBio.activity01 = Mathf.Clamp01(0.3f + powerBus.totalDrawKw / Mathf.Max(1f, powerBus.maxDrawKw) * 0.5f);
        }

        bool needShed = powerBus.charge01 < shedChargeThreshold01
                        || powerBus.totalDrawKw > powerBus.maxDrawKw + 0.01f;
        bool canRestore = powerBus.charge01 >= restoreChargeThreshold01
                          && powerBus.totalDrawKw <= powerBus.maxDrawKw * 0.9f;

        if (needShed)
            ShedNext();
        else if (canRestore)
            RestoreNext();

        if (powerBus.charge01 <= CriticalCharge01())
            airplane.NotifyNarrative(AirplaneNarrativeActionIds.BatteryCritical);
    }

    float CriticalCharge01()
    {
        float c = 0.15f;
        if (airplane?.batteries == null) return c;
        for (int i = 0; i < airplane.batteries.Count; i++)
            if (airplane.batteries[i] != null)
                c = Mathf.Min(c, airplane.batteries[i].criticalCharge01);
        return c;
    }

    void ShedNext()
    {
        if (airplane?.powerSystems == null) return;
        AirplanePowerSystemDraw best = null;
        for (int i = 0; i < airplane.powerSystems.Count; i++)
        {
            var s = airplane.powerSystems[i];
            if (s == null || !s.enabled) continue;
            if (s.category == AirplanePowerSystemCategory.FlightCritical) continue;
            if (best == null || s.shedPriority < best.shedPriority)
                best = s;
        }
        if (best == null) return;
        best.enabled = false;
        _shedSystems.Add(best.systemId);
        airplane.ApplyPowerSystemEnabled(best.systemId, false);
        airplane.NotifyNarrative(AirplaneNarrativeActionIds.PowerShed);
        powerBus.shedding = true;
    }

    void RestoreNext()
    {
        if (airplane?.powerSystems == null || _shedSystems.Count == 0) return;
        AirplanePowerSystemDraw best = null;
        for (int i = 0; i < airplane.powerSystems.Count; i++)
        {
            var s = airplane.powerSystems[i];
            if (s == null || s.enabled) continue;
            if (!_shedSystems.Contains(s.systemId)) continue;
            if (best == null || s.shedPriority > best.shedPriority)
                best = s;
        }
        if (best == null) return;
        best.enabled = true;
        _shedSystems.Remove(best.systemId);
        airplane.ApplyPowerSystemEnabled(best.systemId, true);
        airplane.NotifyNarrative(AirplaneNarrativeActionIds.PowerRestore);
        if (_shedSystems.Count == 0)
            powerBus.shedding = false;
    }
}
