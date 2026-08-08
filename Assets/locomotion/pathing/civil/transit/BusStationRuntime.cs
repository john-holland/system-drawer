using System.Collections.Generic;
using UnityEngine;

/// <summary>Bus depot / station venue — amenities, cafeteria, platforms, TA + repair + SG pack.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Bus Station")]
public sealed class BusStationRuntime : MonoBehaviour
{
    public BusStationBioRhythm bio;
    public TransportationAuthorityBioRhythm authority;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public BuildingRagdoll buildingRagdoll;
    public BusStationSgGenerator sgGenerator;
    public VehicleRepairCenterRuntime nestedRepairCenter;
    public RestaurantVenueRuntime publicCafeteria;
    public RestaurantVenueRuntime privateCafeteria;
    public bool publicKitchen = true;
    public bool privateKitchen;
    public bool publicBathrooms = true;
    public List<Transform> platforms = new List<Transform>();
    public List<Transform> waitingAreas = new List<Transform>();
    public Transform telecomDesk;
    public Transform trash;
    public List<BusVehicleRagdoll> dockedBuses = new List<BusVehicleRagdoll>();

    [Header("Ownership seeds")]
    public string governmentCompanyId = "government";
    public string transitAuthCompanyId = "public_transit_auth";
    public bool privateTransitAuth;

    void Awake()
    {
        EnsureComponents();
        SeedCompanyHierarchy();
        sgGenerator?.ApplySettings();
    }

    public void EnsureComponents()
    {
        if (bio == null)
            bio = GetComponent<BusStationBioRhythm>() ?? gameObject.AddComponent<BusStationBioRhythm>();
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>()
                        ?? gameObject.AddComponent<TransportationAuthorityBioRhythm>();
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        amenities.company = company;
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>() ?? gameObject.AddComponent<BuildingRagdoll>();
        if (sgGenerator == null)
            sgGenerator = GetComponent<BusStationSgGenerator>() ?? gameObject.AddComponent<BusStationSgGenerator>();
        if (nestedRepairCenter == null)
            nestedRepairCenter = GetComponentInChildren<VehicleRepairCenterRuntime>();
        if (publicCafeteria == null)
            publicCafeteria = GetComponentInChildren<RestaurantVenueRuntime>();
        if (GetComponent<MissionControlBioRhythm>() == null)
            gameObject.AddComponent<MissionControlBioRhythm>();
        if (GetComponent<AirTrafficControlBioRhythm>() == null)
            gameObject.AddComponent<AirTrafficControlBioRhythm>();
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
    }

    public void SeedCompanyHierarchy()
    {
        if (company == null) return;
        if (string.IsNullOrEmpty(company.companyId) || company.companyId == gameObject.name)
            company.companyId = transitAuthCompanyId;
        company.parentCompanyId = governmentCompanyId;
        if (privateTransitAuth)
            company.companyId = "private_transit_auth";
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "station_manager", peckingOrder = 3, personaKey = "station_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "dispatcher", peckingOrder = 8, personaKey = "dispatcher" });
            company.staff.Add(new RetinuePeckingEntry { role = "driver", peckingOrder = 20, personaKey = "bus_driver" });
        }
        if (nestedRepairCenter?.company != null &&
            string.IsNullOrEmpty(nestedRepairCenter.company.parentCompanyId))
            nestedRepairCenter.company.parentCompanyId = company.companyId;
        if (publicCafeteria != null)
        {
            var kitchenCo = publicCafeteria.GetComponent<CompanyRegistration>();
            if (kitchenCo != null && string.IsNullOrEmpty(kitchenCo.parentCompanyId))
                kitchenCo.parentCompanyId = company.companyId;
        }
    }

    public void SetOpen(bool open)
    {
        if (bio != null) bio.isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        if (publicKitchen)
            publicCafeteria?.SetOpen(open);
        if (privateKitchen)
            privateCafeteria?.SetOpen(open);
        nestedRepairCenter?.SetOpen(open);
    }

    public void Tick(System.DateTime utcNow, float dt)
    {
        bio?.Tick(utcNow, dt);
        authority?.Tick(utcNow, dt);
        nestedRepairCenter?.Tick(utcNow, dt);
        if (bio != null)
            SetOpen(bio.isOpen);
    }

    public void DockBus(BusVehicleRagdoll bus)
    {
        if (bus == null || dockedBuses.Contains(bus)) return;
        dockedBuses.Add(bus);
        if (authority != null && !authority.fleet.Contains(bus))
            authority.fleet.Add(bus);
    }
}
