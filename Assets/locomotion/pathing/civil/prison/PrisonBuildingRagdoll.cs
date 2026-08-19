using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PrisonCellRef
{
    public string cellId;
    public Transform root;
    public string assignedPrisonerId;
    public string switcherooPackId;
}

/// <summary>Prison BuildingRagdoll with layout refs for cells, yard, cafeteria, clinic, farm, library, chambers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Building Ragdoll")]
public sealed class PrisonBuildingRagdoll : BuildingRagdoll
{
    public PrisonBioRhythm stationBio;
    public PrisonDispatchBioRhythm dispatchBio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public AuthWarden authWarden;
    public PrisonWarden warden;
    public PrisonerSwitcherooCatalog switcherooCatalog;
    public Transform cellsRoot;
    public List<PrisonCellRef> cells = new List<PrisonCellRef>();
    public Transform yard;
    public Transform cafeteria;
    public Transform clinic;
    public Transform farm;
    public Transform library;
    public Transform meetingChamber;
    public Transform groupChamber;
    public Transform interrogation;
    public Transform paroleBoard;
    public Transform wardenOffice;
    public Transform rehabOutingGate;
    public List<PrisonerRecord> roster = new List<PrisonerRecord>();

    protected override void Awake()
    {
        base.Awake();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (stationBio == null)
            stationBio = GetComponent<PrisonBioRhythm>() ?? gameObject.AddComponent<PrisonBioRhythm>();
        stationBio.company = company;
        if (dispatchBio == null)
            dispatchBio = GetComponent<PrisonDispatchBioRhythm>() ?? gameObject.AddComponent<PrisonDispatchBioRhythm>();
        dispatchBio.stationBio = stationBio;
        dispatchBio.company = company;
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        amenities.company = company;
        if (authWarden == null)
            authWarden = GetComponent<AuthWarden>() ?? gameObject.AddComponent<AuthWarden>();
        if (warden == null)
            warden = GetComponent<PrisonWarden>() ?? gameObject.AddComponent<PrisonWarden>();
        SeedAuthZones();
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        var now = DateTime.UtcNow;
        stationBio?.Tick(now, dt);
        dispatchBio?.Tick(now, dt);
        warden?.Tick(now, dt);
        if (stationBio != null)
            stationBio.occupancy01 = roster != null ? Mathf.Clamp01(roster.Count / 40f) : 0f;
    }

    public void SetOpen(bool open)
    {
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        if (open) bio?.NotifyOpen();
        else bio?.NotifyClosed();
    }

    public PrisonerSwitcherooPack ApplySwitcheroo(PrisonerRecord record)
    {
        return switcherooCatalog != null ? switcherooCatalog.ApplyAtSpawn(record) : null;
    }

    void SeedAuthZones()
    {
        if (authWarden == null) return;
        if (authWarden.zones == null)
            authWarden.zones = new List<AuthZone>();
        if (authWarden.zones.Count > 0) return;
        AddZone("cells", cellsRoot, AuthAccessTier.Restricted);
        AddZone("yard", yard, AuthAccessTier.Staff);
        AddZone("warden_office", wardenOffice, AuthAccessTier.Restricted);
        AddZone("interrogation", interrogation, AuthAccessTier.Secure);
    }

    void AddZone(string id, Transform anchor, AuthAccessTier tier)
    {
        authWarden.zones.Add(new AuthZone
        {
            locationId = id,
            anchor = anchor,
            requiredTier = tier,
            publicAccess = false,
            privateIntended = true
        });
    }
}
