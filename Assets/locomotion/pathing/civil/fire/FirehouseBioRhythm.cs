using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Firehouse dispatch biorhythm — shift cron, truck readiness, water reserve, station siren.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Firehouse Bio Rhythm")]
public sealed class FirehouseBioRhythm : DispatchBioRhythm
{
    public string shiftCron = "* 7-19 * * 1-5";
    [Range(0f, 1f)] public float truckReadiness01 = 1f;
    [Range(0f, 5000f)] public float waterReserveLiters = 2000f;
    public bool stationSirenOn;
    public RestaurantVenueRuntime kitchenVenue;
    public List<string> homePersonaKeys = new List<string>();

    protected override void Awake()
    {
        base.Awake();
        serviceId = "fire_department";
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        governmentAssigned = true;
        if (kitchenVenue == null)
            kitchenVenue = GetComponentInChildren<RestaurantVenueRuntime>();
        if (company != null && company.staff.Count == 0 && staff.Count > 0)
            company.staff.AddRange(staff);
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        hoursCron = shiftCron;
        base.Tick(utcNow, dt);
        bool onShift = CronDue.IsActiveSchedule(shiftCron, utcNow);
        unitsAvailable01 = onShift ? truckReadiness01 : truckReadiness01 * 0.35f;
        if (!onShift && alert01 > 0.4f)
            unitsAvailable01 = Mathf.Max(unitsAvailable01, 0.55f); // call-in boost
        waterReserveLiters = Mathf.Max(0f, waterReserveLiters);
    }

    public bool CanReleaseTruck(float requiredLiters) =>
        unitsAvailable01 > 0.2f && waterReserveLiters + 500f >= requiredLiters;

    public void ConsumeWater(float liters) =>
        waterReserveLiters = Mathf.Max(0f, waterReserveLiters - Mathf.Max(0f, liters));

    public void SetStationSiren(bool on) => stationSirenOn = on;
}
