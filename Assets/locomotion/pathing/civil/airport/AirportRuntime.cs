using System.Collections.Generic;
using UnityEngine;

/// <summary>Airport venue shell — pickups, kitchens, spas, checkpoints, rooms, debug card-stack host.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Runtime")]
public sealed class AirportRuntime : MonoBehaviour
{
    public AirPortBioRhythm bio;
    public AirportBuildingRagdoll building;
    public AuthWarden authWarden;
    public AirportRunwaySgGenerator runwaySg;
    public TransportationAuthorityBioRhythm authority;
    public AirTrafficControlBioRhythm atc;

    [Header("Surface pickups")]
    public List<BusStationRuntime> busStationPickups = new List<BusStationRuntime>();

    [Header("Amenities")]
    public List<RestaurantVenueRuntime> publicKitchens = new List<RestaurantVenueRuntime>();
    public List<RestaurantVenueRuntime> privateKitchens = new List<RestaurantVenueRuntime>();
    public List<Transform> publicSpas = new List<Transform>();
    public List<Transform> privateSpas = new List<Transform>();
    public List<Transform> checkpoints = new List<Transform>();
    public List<Transform> meetingRoomsTelecom = new List<Transform>();
    public List<Transform> staffOfficesWebtop = new List<Transform>();
    public List<Transform> publicOfficesWebtop = new List<Transform>();
    public List<Transform> seatingAreas = new List<Transform>();
    public List<Transform> bathrooms = new List<Transform>();
    public VehicleRepairCenterRuntime nestedRepair;
    public string kitchenCompanyId = "airport_kitchen";

    [Header("Debug / devops")]
    [Tooltip("Last facilitated card stack for inspector debugging.")]
    public List<string> lastCardStackNames = new List<string>();

    void Awake() => EnsureComponents();

    public void EnsureComponents()
    {
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
        if (bio == null)
            bio = GetComponent<AirPortBioRhythm>() ?? gameObject.AddComponent<AirPortBioRhythm>();
        if (building == null)
            building = GetComponent<AirportBuildingRagdoll>() ?? gameObject.AddComponent<AirportBuildingRagdoll>();
        if (authWarden == null)
            authWarden = GetComponent<AuthWarden>() ?? gameObject.AddComponent<AuthWarden>();
        if (runwaySg == null)
            runwaySg = GetComponent<AirportRunwaySgGenerator>() ?? gameObject.AddComponent<AirportRunwaySgGenerator>();
        if (atc == null)
            atc = GetComponent<AirTrafficControlBioRhythm>() ?? gameObject.AddComponent<AirTrafficControlBioRhythm>();
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>()
                        ?? gameObject.AddComponent<TransportationAuthorityBioRhythm>();
        if (nestedRepair == null)
            nestedRepair = GetComponentInChildren<VehicleRepairCenterRuntime>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<MissionControlBioRhythm>() == null)
            gameObject.AddComponent<MissionControlBioRhythm>();
        LinkPlaneGalleys();
        SeedAuthZones();
        runwaySg.ApplySettings();
    }

    public void LinkPlaneGalleys()
    {
        if (building?.airplanes == null) return;
        for (int i = 0; i < building.airplanes.Count; i++)
            building.airplanes[i]?.LinkGalleyToAirportKitchen(kitchenCompanyId);
    }

    void SeedAuthZones()
    {
        if (authWarden == null) return;
        if (authWarden.zones.Count > 0) return;
        AddZone("security", AuthAccessTier.Secure, false);
        AddZone("staff_office", AuthAccessTier.Staff, false);
        AddZone("public_lobby", AuthAccessTier.Public, true);
        AddZone("gate_desk", AuthAccessTier.Staff, false);
    }

    void AddZone(string id, AuthAccessTier tier, bool pub)
    {
        authWarden.zones.Add(new AuthZone
        {
            locationId = id,
            requiredTier = tier,
            publicAccess = pub,
            privateIntended = !pub
        });
    }

    public void SetOpen(bool open)
    {
        if (bio != null)
            bio.alert01 = open ? bio.alert01 : 0f;
        building?.SetOpen(open);
        for (int i = 0; i < publicKitchens.Count; i++)
            publicKitchens[i]?.SetOpen(open);
        for (int i = 0; i < privateKitchens.Count; i++)
            privateKitchens[i]?.SetOpen(open);
        nestedRepair?.SetOpen(open);
    }

    public void Tick(System.DateTime utcNow, float dt)
    {
        bio?.Tick(utcNow, dt);
        atc?.Tick(utcNow, dt);
        authority?.Tick(utcNow, dt);
        nestedRepair?.Tick(utcNow, dt);
        bool open = bio != null && CronDue.IsActiveSchedule(bio.hoursCron, utcNow);
        SetOpen(open);
    }

    public List<GoodSection> FacilitateAndRecord(DispatchRequest request)
    {
        var cards = bio != null ? bio.FacilitateCards(request) : new List<GoodSection>();
        lastCardStackNames.Clear();
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] != null)
                lastCardStackNames.Add(cards[i].sectionName ?? cards[i].GetType().Name);
        return cards;
    }
}
