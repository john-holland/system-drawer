using System.Collections.Generic;
using UnityEngine;

/// <summary>Sanitation facility — factory subclass with sorting, poop-quifer, recycling, road crews.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sanitation Facility")]
public sealed class SanitationFacilityRuntime : FactoryRuntime
{
    public SanitationFacilityBioRhythm sanitationBio;
    public TransportationAuthorityBioRhythm authority;
    public SanitationPoopQuifer poopQuifer;
    public SanitationRecyclingTransfer recycling;
    public List<SanitationSortingStation> sortingStations = new List<SanitationSortingStation>();
    public List<SanitationDownflowSection> downflowSections = new List<SanitationDownflowSection>();
    public RoadCareCrewRuntime roadCrew;
    public Transform loadingArea;
    public Transform palletAnchor;
    public Transform trashAnchor;
    public Transform crewDepot;
    public List<VehicleRagdoll> interiorVehicles = new List<VehicleRagdoll>();
    public List<GarbageTruckVehicleRagdoll> dockedTrucks = new List<GarbageTruckVehicleRagdoll>();
    public List<Transform> intermediateStations = new List<Transform>();

    [Header("Gov / IP")]
    public string publicSanitationAuthCompanyId = "public_sanitation_auth";
    public string privateSanitationCompanyId = "private_sanitation_co";
    public string ipv6CityPrefix;
    public string companyIpConfigId;

    protected override void Awake()
    {
        governmentAssigned = true;
        publicFactory = true;
        publicFactoryCompanyId = publicSanitationAuthCompanyId;
        privateFactoryCompanyId = privateSanitationCompanyId;
        base.Awake();
    }

    public override void EnsureComponents()
    {
        if (sanitationBio == null)
            sanitationBio = GetComponent<SanitationFacilityBioRhythm>()
                            ?? gameObject.AddComponent<SanitationFacilityBioRhythm>();
        sanitationBio.facility = this;
        bio = sanitationBio;
        base.EnsureComponents();
        bio = sanitationBio;
        sanitationBio.facility = this;

        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>()
                        ?? FindFirstObjectByType<TransportationAuthorityBioRhythm>();
        if (poopQuifer == null)
            poopQuifer = GetComponentInChildren<SanitationPoopQuifer>()
                         ?? gameObject.AddComponent<SanitationPoopQuifer>();
        poopQuifer.facility = this;
        if (recycling == null)
            recycling = GetComponentInChildren<SanitationRecyclingTransfer>()
                        ?? gameObject.AddComponent<SanitationRecyclingTransfer>();
        recycling.facility = this;
        if (sortingStations.Count == 0)
            sortingStations.AddRange(GetComponentsInChildren<SanitationSortingStation>(true));
        if (roadCrew == null)
            roadCrew = GetComponentInChildren<RoadCareCrewRuntime>()
                       ?? gameObject.AddComponent<RoadCareCrewRuntime>();
        roadCrew.facility = this;
        if (dockedTrucks.Count == 0)
            dockedTrucks.AddRange(GetComponentsInChildren<GarbageTruckVehicleRagdoll>(true));
        if (GetComponent<TrashWarden>() == null)
            gameObject.AddComponent<TrashWarden>();
    }

    public override void SeedCompanyHierarchy()
    {
        if (company == null) return;
        company.companyId = publicFactory ? publicSanitationAuthCompanyId : privateSanitationCompanyId;
        company.parentCompanyId = governmentAssigned ? governmentCompanyId : "";
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "sanitation_manager", peckingOrder = 3, personaKey = "sanitation_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "sorter", peckingOrder = 14, personaKey = "sorter" });
            company.staff.Add(new RetinuePeckingEntry { role = "truck_driver", peckingOrder = 18, personaKey = "garbage_driver" });
            company.staff.Add(new RetinuePeckingEntry { role = "road_crew", peckingOrder = 20, personaKey = "road_crew" });
        }
        if (roadCrew?.company != null && string.IsNullOrEmpty(roadCrew.company.parentCompanyId))
            roadCrew.company.parentCompanyId = company.companyId;
    }

    public override void SetOpen(bool open)
    {
        base.SetOpen(open);
        for (int i = 0; i < sortingStations.Count; i++)
            if (sortingStations[i] != null && open)
                sortingStations[i].sortProgress01 = Mathf.Max(sortingStations[i].sortProgress01, 0.01f);
    }

    public void TickPlant(float dt)
    {
        poopQuifer?.Tick(dt);
        for (int i = 0; i < sortingStations.Count; i++)
            sortingStations[i]?.Tick(dt);
    }
}
