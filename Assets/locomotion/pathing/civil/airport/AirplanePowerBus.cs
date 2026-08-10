using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Sums enabled system draw, drains batteries, exposes breakdown for Power tab / bio shed.</summary>
[Serializable]
public sealed class AirplanePowerBus
{
    public float totalDrawKw;
    public float charge01 = 1f;
    public float maxDrawKw = 80f;
    public bool shedding;

    public static void FillDefaultPowerSystems(List<AirplanePowerSystemDraw> list)
    {
        if (list == null) return;
        list.Clear();
        void Add(string id, string label, float kw, int prio, AirplanePowerSystemCategory cat, bool comfort = false)
        {
            list.Add(new AirplanePowerSystemDraw
            {
                systemId = id,
                label = label,
                drawKwWhenOn = kw,
                enabled = true,
                shedPriority = prio,
                category = cat,
                passengerComfort = comfort
            });
        }

        Add("seat_power_outlets", "Seat power outlets", 6f, 10, AirplanePowerSystemCategory.Comfort, true);
        Add("seat_aux", "Seat aux inputs", 0.5f, 20, AirplanePowerSystemCategory.Comfort, true);
        Add("seatback_webtops", "Seatback webtops", 2.4f, 30, AirplanePowerSystemCategory.Comfort, true);
        Add("webtops", "Cabin webtops", 1.5f, 40, AirplanePowerSystemCategory.Cabin, true);
        Add("passenger_requirements", "Passenger requirements", 2f, 50, AirplanePowerSystemCategory.Comfort, true);
        Add("music_system", "Cabin music", 0.4f, 60, AirplanePowerSystemCategory.Cabin, true);
        Add("hvac", "HVAC", 8f, 70, AirplanePowerSystemCategory.Cabin, true);
        Add("lights", "Interior / nav lights", 3f, 80, AirplanePowerSystemCategory.Cabin);
        Add("heat", "Cabin heat", 4f, 90, AirplanePowerSystemCategory.Cabin, true);
        Add("computers", "Computers", 2f, 100, AirplanePowerSystemCategory.Avionics);
        Add("instruments", "Instruments", 1.2f, 110, AirplanePowerSystemCategory.Avionics);
        Add("pa_speakers", "PA speakers", 0.8f, 120, AirplanePowerSystemCategory.Cabin);
        Add("engines", "Engine electrical", 5f, 130, AirplanePowerSystemCategory.FlightCritical);
        Add("landing_gear", "Landing gear actuators", 3f, 140, AirplanePowerSystemCategory.FlightCritical);
        Add("weapons_bay", "Weapons bay", 1f, 150, AirplanePowerSystemCategory.Weapons);
    }

    public void ScaleComfortDrawRows(List<AirplanePowerSystemDraw> systems, AirplaneVehicleRagdoll plane)
    {
        if (systems == null || plane == null) return;
        for (int i = 0; i < systems.Count; i++)
        {
            var row = systems[i];
            if (row == null) continue;
            if (row.systemId == "seat_power_outlets")
                row.drawKwWhenOn = Mathf.Max(0f, plane.seatPowerOutletCount * plane.seatOutletDrawKwEach);
            else if (row.systemId == "seatback_webtops")
                row.drawKwWhenOn = plane.seatbackWebtopsEnabled
                    ? Mathf.Max(0f, plane.seatbackWebtopCount * plane.seatbackWebtopDrawKwEach)
                    : 0f;
        }
    }

    public float Tick(float dt, List<AirplaneBatteryPack> packs, List<AirplanePowerSystemDraw> systems, float chargeKw)
    {
        totalDrawKw = 0f;
        maxDrawKw = 0f;
        float capacity = 0f;
        if (packs != null)
        {
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i] == null) continue;
                maxDrawKw += Mathf.Max(0f, packs[i].maxDrawKw);
                capacity += Mathf.Max(0f, packs[i].capacityKwh);
            }
        }

        if (systems != null)
        {
            for (int i = 0; i < systems.Count; i++)
            {
                var s = systems[i];
                if (s != null && s.enabled)
                    totalDrawKw += Mathf.Max(0f, s.drawKwWhenOn);
            }
        }

        float netKwh = (chargeKw - totalDrawKw) * Mathf.Max(0f, dt) / 3600f;
        float charge = 0f;
        if (packs != null && capacity > 1e-4f)
        {
            for (int i = 0; i < packs.Count; i++)
            {
                var p = packs[i];
                if (p == null) continue;
                float share = p.capacityKwh / capacity;
                p.chargeKwh = Mathf.Clamp(p.chargeKwh + netKwh * share, 0f, p.capacityKwh);
                charge += p.chargeKwh;
            }
        }

        charge01 = capacity > 1e-4f ? Mathf.Clamp01(charge / capacity) : 0f;
        shedding = totalDrawKw > maxDrawKw + 0.01f || charge01 < 0.2f;
        return totalDrawKw;
    }

    public List<(string systemId, float drawKw)> GetDrainBreakdown(List<AirplanePowerSystemDraw> systems)
    {
        var result = new List<(string, float)>();
        if (systems == null) return result;
        for (int i = 0; i < systems.Count; i++)
        {
            var s = systems[i];
            if (s == null || !s.enabled) continue;
            result.Add((s.systemId, s.drawKwWhenOn));
        }
        return result;
    }

    public float EstimateHoursToEmpty(List<AirplaneBatteryPack> packs)
    {
        if (totalDrawKw <= 1e-4f) return float.PositiveInfinity;
        float charge = 0f;
        if (packs != null)
            for (int i = 0; i < packs.Count; i++)
                if (packs[i] != null) charge += packs[i].chargeKwh;
        return charge / totalDrawKw;
    }
}
