using System.Collections.Generic;
using UnityEngine;

/// <summary>Airport building composing airplanes, gate extensions, ground vehicles, and bio/company refs.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Building Ragdoll")]
public sealed class AirportBuildingRagdoll : BuildingRagdoll
{
    public AirPortBioRhythm airportBio;
    public AirTrafficControlBioRhythm atcBio;
    public TransportationAuthorityBioRhythm authority;
    public AuthWarden authWarden;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;

    public List<AirplaneVehicleRagdoll> airplanes = new List<AirplaneVehicleRagdoll>();
    public List<AirportExtensionGate> gates = new List<AirportExtensionGate>();
    public List<VehicleRagdoll> groundVehicles = new List<VehicleRagdoll>();

    protected override void Awake()
    {
        base.Awake();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (airportBio == null)
            airportBio = GetComponent<AirPortBioRhythm>() ?? gameObject.AddComponent<AirPortBioRhythm>();
        airportBio.company = company;
        if (atcBio == null)
            atcBio = GetComponent<AirTrafficControlBioRhythm>() ?? gameObject.AddComponent<AirTrafficControlBioRhythm>();
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>();
        if (authWarden == null)
            authWarden = GetComponent<AuthWarden>() ?? gameObject.AddComponent<AuthWarden>();
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        amenities.company = company;
        GetComponentsInChildren(true, airplanes);
        GetComponentsInChildren(true, gates);
        RefreshGroundVehicles();
    }

    public void RefreshGroundVehicles()
    {
        groundVehicles.Clear();
        var all = GetComponentsInChildren<VehicleRagdoll>(true);
        for (int i = 0; i < all.Length; i++)
        {
            VehicleRagdoll v = all[i];
            if (v == null || v is AirplaneVehicleRagdoll || v is AirportExtensionGate) continue;
            groundVehicles.Add(v);
        }
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        airportBio?.Tick(System.DateTime.UtcNow, dt);
        atcBio?.Tick(System.DateTime.UtcNow, dt);
    }

    public void SetOpen(bool open)
    {
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        if (open) bio?.NotifyOpen();
        else bio?.NotifyClosed();
    }

    /// <summary>Dev-ops access: staff pecking + company id for debugging card stacks.</summary>
    public IReadOnlyList<RetinuePeckingEntry> GetStaffAccess() =>
        company != null ? company.staff : (IReadOnlyList<RetinuePeckingEntry>)System.Array.Empty<RetinuePeckingEntry>();
}
