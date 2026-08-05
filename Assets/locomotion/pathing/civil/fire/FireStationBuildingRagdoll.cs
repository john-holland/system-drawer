using System.Collections.Generic;
using UnityEngine;

/// <summary>Fire station BuildingRagdoll with dispatch bio, amenities, optional rail bay.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fire Station Building Ragdoll")]
public sealed class FireStationBuildingRagdoll : BuildingRagdoll
{
    public FirehouseBioRhythm firehouseBio;
    public FireWarden fireWarden;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public bool useRailPackage;
    public Transform sleepingArea;
    public Transform meetingRoom;
    public Transform office;
    public Transform engineBay;
    public Transform firemanPole;
    public List<FireTruckVehicleRagdoll> trucks = new List<FireTruckVehicleRagdoll>();

    protected override void Awake()
    {
        base.Awake();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (firehouseBio == null)
            firehouseBio = GetComponent<FirehouseBioRhythm>() ?? gameObject.AddComponent<FirehouseBioRhythm>();
        firehouseBio.company = company;
        if (fireWarden == null)
            fireWarden = GetComponent<FireWarden>() ?? gameObject.AddComponent<FireWarden>();
        fireWarden.bio = firehouseBio;
        fireWarden.station = this;
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        amenities.company = company;
        if (useRailPackage && amenities.parkingLot != null)
            amenities.parkingLot.registerWithTravelAgentOnWake = false;
        GetComponentsInChildren(true, trucks);
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        firehouseBio?.Tick(System.DateTime.UtcNow, dt);
        fireWarden?.Tick(dt);
    }

    public void SetOpen(bool open)
    {
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        bio?.NotifyOpen();
        if (!open) bio?.NotifyClosed();
    }
}
