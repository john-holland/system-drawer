using System.Collections.Generic;
using UnityEngine;

/// <summary>Park grounds + buildings; optional attached gas station, maintenance, kitchens, shops, spas.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Park/Park Runtime")]
public sealed class ParkRuntime : MonoBehaviour
{
    public ParkBioRhythm bio;
    public TransportationAuthorityBioRhythm authority;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public BuildingRagdoll buildingRagdoll;
    public List<RoadLot> lots = new List<RoadLot>();
    public List<PlanarSplinePathLocomotion> walkPaths = new List<PlanarSplinePathLocomotion>();
    public GasStationRuntime attachedGasStation;
    public StoreBase infoCenterShop;
    public RestaurantVenueRuntime kitchen;
    public Transform maintenanceDepot;
    public Transform spaAnchor;
    public Transform serviceDesk;
    public List<ParkSignageTrigger> signage = new List<ParkSignageTrigger>();
    public bool publicPark = true;
    public bool governmentAssigned = true;

    [Header("Ownership")]
    public string governmentCompanyId = "government";
    public string parksAuthCompanyId = "public_parks_auth";
    public string privateParksCompanyId = "private_parks_co";

    void Awake()
    {
        EnsureComponents();
        SeedCompanyHierarchy();
    }

    public void EnsureComponents()
    {
        if (bio == null)
            bio = GetComponent<ParkBioRhythm>() ?? gameObject.AddComponent<ParkBioRhythm>();
        bio.park = this;
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>()
                        ?? FindFirstObjectByType<TransportationAuthorityBioRhythm>();
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        amenities.company = company;
        amenities.frontDesk = serviceDesk != null ? serviceDesk : amenities.frontDesk;
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>() ?? gameObject.AddComponent<BuildingRagdoll>();
        if (lots.Count == 0)
            lots.AddRange(GetComponentsInChildren<RoadLot>(true));
        if (walkPaths.Count == 0)
            walkPaths.AddRange(GetComponentsInChildren<PlanarSplinePathLocomotion>(true));
        if (attachedGasStation == null)
            attachedGasStation = GetComponentInChildren<GasStationRuntime>(true);
        if (infoCenterShop == null)
            infoCenterShop = GetComponentInChildren<StoreBase>(true);
        if (kitchen == null)
            kitchen = GetComponentInChildren<RestaurantVenueRuntime>(true);
        if (signage.Count == 0)
            signage.AddRange(GetComponentsInChildren<ParkSignageTrigger>(true));
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
    }

    public void SeedCompanyHierarchy()
    {
        if (company == null) return;
        company.companyId = publicPark ? parksAuthCompanyId : privateParksCompanyId;
        company.parentCompanyId = governmentAssigned ? governmentCompanyId : "";
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "park_manager", peckingOrder = 3, personaKey = "park_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "groundskeeper", peckingOrder = 12, personaKey = "groundskeeper" });
            company.staff.Add(new RetinuePeckingEntry { role = "justice_patrol", peckingOrder = 18, personaKey = "park_patrol" });
            company.staff.Add(new RetinuePeckingEntry { role = "horticulturist", peckingOrder = 22, personaKey = "horticulturist" });
        }
        if (attachedGasStation != null)
        {
            attachedGasStation.EnsureComponents();
            if (attachedGasStation.company != null
                && string.IsNullOrEmpty(attachedGasStation.company.parentCompanyId))
                attachedGasStation.company.parentCompanyId = company.companyId;
        }
        if (infoCenterShop != null)
        {
            var shopCo = infoCenterShop.GetComponent<CompanyRegistration>()
                         ?? infoCenterShop.gameObject.AddComponent<CompanyRegistration>();
            if (string.IsNullOrEmpty(shopCo.parentCompanyId))
                shopCo.parentCompanyId = company.companyId;
        }
        if (kitchen != null)
        {
            var kitchenCo = kitchen.GetComponent<CompanyRegistration>()
                            ?? kitchen.gameObject.AddComponent<CompanyRegistration>();
            if (string.IsNullOrEmpty(kitchenCo.parentCompanyId))
                kitchenCo.parentCompanyId = company.companyId;
        }
    }

    public void SetOpen(bool open)
    {
        if (bio != null) bio.isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        infoCenterShop?.SetOpen(open);
        kitchen?.SetOpen(open);
        attachedGasStation?.SetOpen(open);
    }

    public RoadLot PrimaryLot() => lots != null && lots.Count > 0 ? lots[0] : null;
}
