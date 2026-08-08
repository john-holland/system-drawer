using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>CarRepair venue biorhythm — bays, commodities, open cron.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicle Repair Center Bio Rhythm")]
public sealed class VehicleRepairCenterBioRhythm : MonoBehaviour
{
    public string hoursCron = "* 8-18 * * 1-6";
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float bayOccupancy01;
    [Range(0f, 1f)] public float commodityDemand01 = 0.4f;
    public bool isOpen;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(DateTime utcNow, float dt)
    {
        isOpen = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = isOpen ? Mathf.Clamp01(0.35f + bayOccupancy01 * 0.4f) : 0.1f;
            venueBio.stress01 = commodityDemand01 * 0.3f;
        }
    }
}

/// <summary>Standalone vehicle repair center (CivilSystemKind.CarRepair). Nestable under BusStation.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicle Repair Center")]
public sealed class VehicleRepairCenterRuntime : MonoBehaviour
{
    public VehicleRepairCenterBioRhythm bio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public StoreBase store;
    public BuildingRagdoll buildingRagdoll;
    public RestaurantVenueRuntime kitchenVenue;
    public bool publicKitchen = true;
    public bool publicBathrooms = true;
    public List<Transform> maintenanceBays = new List<Transform>();
    public Transform retailShelf;
    public Transform trash;
    public List<VehicleRagdoll> vehiclesInBay = new List<VehicleRagdoll>();

    [Header("Ownership")]
    [Tooltip("When set, company.parentCompanyId is seeded to this (e.g. public_transit_auth).")]
    public string parentTransitAuthCompanyId;
    [Tooltip("Optional conglomerate owning repair separately from transit auth.")]
    public string repairConglomerateCompanyId;
    [Tooltip("Optional conglomerate owning the kitchen.")]
    public string kitchenConglomerateCompanyId;
    [Tooltip("Optional conglomerate owning fuel commodities.")]
    public string fuelConglomerateCompanyId;

    void Awake()
    {
        if (bio == null)
            bio = GetComponent<VehicleRepairCenterBioRhythm>()
                ?? gameObject.AddComponent<VehicleRepairCenterBioRhythm>();
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        amenities.company = company;
        if (store == null)
            store = GetComponent<StoreBase>() ?? gameObject.AddComponent<StoreBase>();
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>() ?? gameObject.AddComponent<BuildingRagdoll>();
        if (kitchenVenue == null)
            kitchenVenue = GetComponentInChildren<RestaurantVenueRuntime>();
        if (company.staff.Count == 0)
            company.staff.Add(new RetinuePeckingEntry { role = "mechanic", peckingOrder = 10, personaKey = "mechanic" });
        SeedOwnership();
    }

    public void SeedOwnership()
    {
        if (company == null) return;
        if (!string.IsNullOrEmpty(repairConglomerateCompanyId))
        {
            company.companyId = repairConglomerateCompanyId;
            if (!string.IsNullOrEmpty(parentTransitAuthCompanyId))
                company.parentCompanyId = parentTransitAuthCompanyId;
        }
        else if (!string.IsNullOrEmpty(parentTransitAuthCompanyId) &&
                 string.IsNullOrEmpty(company.parentCompanyId))
        {
            company.parentCompanyId = parentTransitAuthCompanyId;
        }

        var kitchenCo = kitchenVenue != null ? kitchenVenue.GetComponent<CompanyRegistration>() : null;
        if (kitchenCo != null)
        {
            if (!string.IsNullOrEmpty(kitchenConglomerateCompanyId))
                kitchenCo.companyId = kitchenConglomerateCompanyId;
            if (string.IsNullOrEmpty(kitchenCo.parentCompanyId))
                kitchenCo.parentCompanyId = !string.IsNullOrEmpty(parentTransitAuthCompanyId)
                    ? parentTransitAuthCompanyId
                    : company.companyId;
        }

        if (!string.IsNullOrEmpty(fuelConglomerateCompanyId) && store != null)
        {
            var fuelHost = store.GetComponent<CompanyRegistration>();
            if (fuelHost != null)
            {
                fuelHost.companyId = fuelConglomerateCompanyId;
                if (string.IsNullOrEmpty(fuelHost.parentCompanyId))
                    fuelHost.parentCompanyId = parentTransitAuthCompanyId;
            }
        }
    }

    public void SetOpen(bool open)
    {
        bio.isOpen = open;
        store?.SetOpen(open);
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        if (publicKitchen)
            kitchenVenue?.SetOpen(open);
    }

    public void Tick(DateTime utcNow, float dt)
    {
        bio?.Tick(utcNow, dt);
        if (bio != null && bio.isOpen != (store != null && store.isOpen))
            SetOpen(bio.isOpen);
        bio.bayOccupancy01 = vehiclesInBay.Count > 0
            ? Mathf.Clamp01(vehiclesInBay.Count / Mathf.Max(1f, maintenanceBays.Count))
            : 0f;
    }

    public TAMaintenanceCard ServiceBay(VehicleRagdoll vehicle) =>
        TAMaintenanceCard.GenerateBayService(vehicle);

    public TAMaintenanceCard Repair(VehicleRagdoll vehicle) =>
        TAMaintenanceCard.GenerateRepair(vehicle);

    public void AcceptVehicle(VehicleRagdoll vehicle)
    {
        if (vehicle == null || vehiclesInBay.Contains(vehicle)) return;
        vehiclesInBay.Add(vehicle);
    }

    /// <summary>Accept a TA bay park / repair card and enqueue maintenance.</summary>
    public List<GoodSection> AcceptTaBayCards(TAVehicleBayParkCard park, TAVehicleBayRepairCard repair)
    {
        var cards = new List<GoodSection>();
        BusVehicleRagdoll bus = repair != null ? repair.vehicle : null;
        if (bus == null && park?.vehicle != null)
            bus = park.vehicle;
        if (bus != null)
            AcceptVehicle(bus);
        if (park != null)
            cards.Add(park);
        if (repair != null)
        {
            cards.Add(repair);
            cards.Add(repair.ToMaintenanceCard());
        }
        else if (bus != null)
            cards.Add(Repair(bus));
        return cards;
    }
}
