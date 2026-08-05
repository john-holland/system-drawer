using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PoliceDeskStation
{
    public string deskId;
    public Transform root;
    public Transform webtop;
    public Transform inventory;
    public Transform telecom;
    public string assignedPersonaKey;
}

[Serializable]
public sealed class PoliceOfficeSuite
{
    public string officeId;
    public Transform root;
    public Transform meetingRoom;
    public Transform telecom;
    public string assignedPersonaKey;
}

[Serializable]
public sealed class PoliceInterrogationRoom
{
    public string roomId;
    public Transform root;
    public Transform seating;
    public bool usePhysicsConstraints = true;
    public float constraintStiffness = 0.6f;
    public bool enableDialog = true;
    public bool enableMusic = true;
    public MusicAmbianceTag musicTag = MusicAmbianceTag.Hushed;
}

/// <summary>Police station BuildingRagdoll with layout refs, dispatch, repair bay.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Station Building Ragdoll")]
public sealed class PoliceStationBuildingRagdoll : BuildingRagdoll
{
    public PoliceStationBioRhythm stationBio;
    public PoliceDispatchBioRhythm dispatchBio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public Transform mainHall;
    public List<PoliceDeskStation> deskStations = new List<PoliceDeskStation>();
    public List<Transform> meetingRooms = new List<Transform>();
    public List<PoliceOfficeSuite> privateOffices = new List<PoliceOfficeSuite>();
    public List<PoliceInterrogationRoom> interrogationRooms = new List<PoliceInterrogationRoom>();
    public Transform holdingCell;
    public Transform vehicleRepairBay;
    public Transform sleepingArea;
    public VehicleRepairCenterRuntime repairBay;
    public List<PoliceCarVehicleRagdoll> cruisers = new List<PoliceCarVehicleRagdoll>();

    protected override void Awake()
    {
        base.Awake();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (stationBio == null)
            stationBio = GetComponent<PoliceStationBioRhythm>() ?? gameObject.AddComponent<PoliceStationBioRhythm>();
        stationBio.company = company;
        if (dispatchBio == null)
            dispatchBio = GetComponent<PoliceDispatchBioRhythm>() ?? gameObject.AddComponent<PoliceDispatchBioRhythm>();
        dispatchBio.stationBio = stationBio;
        dispatchBio.company = company;
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        amenities.company = company;
        if (repairBay == null && vehicleRepairBay != null)
            repairBay = vehicleRepairBay.GetComponent<VehicleRepairCenterRuntime>();
        GetComponentsInChildren(true, cruisers);
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        var now = DateTime.UtcNow;
        stationBio?.Tick(now, dt);
        dispatchBio?.Tick(now, dt);
        if (holdingCell != null && stationBio != null)
            stationBio.holdingLoad01 = Mathf.MoveTowards(stationBio.holdingLoad01, stationBio.holdingLoad01, 0f);
    }

    public void SetOpen(bool open)
    {
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        if (open) bio?.NotifyOpen();
        else bio?.NotifyClosed();
    }

    public TAMaintenanceCard RepairCruiser(PoliceCarVehicleRagdoll cruiser)
    {
        if (repairBay != null)
            return repairBay.Repair(cruiser);
        return TAMaintenanceCard.GenerateRepair(cruiser);
    }
}
