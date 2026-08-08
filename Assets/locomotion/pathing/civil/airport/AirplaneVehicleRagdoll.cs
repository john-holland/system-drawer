using System.Collections.Generic;
using UnityEngine;

/// <summary>Airliner — cabin seating, trays, bathroom, galley, telecom/webtop, baggage.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Vehicle Ragdoll")]
public sealed class AirplaneVehicleRagdoll : VehicleRagdoll
{
    [Header("Cabin")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> seatTrays = new List<Transform>();
    public string seatTrayOpenCloseTopologyId = "seat_tray";
    public bool allowPassengerTalk = true;
    public bool hasBathroom = true;
    public Transform bathroomAnchor;

    [Header("Galley")]
    public RestaurantVenueRuntime galleyKitchen;
    public CompanyRegistration galleyCompany;
    public string parentKitchenCompanyId;

    [Header("Flight ops")]
    public Transform cockpit;
    public Component telecomBridge;
    public bool webtopEnabled = true;
    public string activeFlightId;
    public string cabinMusicTrackId;
    public bool cabinMusicPlaying;
    [Range(0f, 1f)] public float fuel01 = 1f;
    public AirportExtensionGate dockedGate;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "baggage") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "baggage", capacity = 200f });
        if (interiors.Find(s => s != null && s.sectionName == "galley") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "galley", capacity = 40f });
        if (galleyKitchen == null)
            galleyKitchen = GetComponentInChildren<RestaurantVenueRuntime>();
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>();
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (telecomBridge == null)
            telecomBridge = GetComponent("TelecomUnityBridge");
        LinkGalleyToAirportKitchen();
    }

    public void LinkGalleyToAirportKitchen(string airportKitchenCompanyId = null)
    {
        string parent = !string.IsNullOrEmpty(airportKitchenCompanyId)
            ? airportKitchenCompanyId
            : parentKitchenCompanyId;
        if (string.IsNullOrEmpty(parent)) return;
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>()
                            ?? galleyKitchen.gameObject.AddComponent<CompanyRegistration>();
        if (galleyCompany != null)
        {
            galleyCompany.parentCompanyId = parent;
            parentKitchenCompanyId = parent;
        }
    }

    public void SetCabinMusic(string trackId, bool play)
    {
        cabinMusicTrackId = trackId;
        cabinMusicPlaying = play;
        SendMessage(play ? "OnAirplaneCabinMusicPlay" : "OnAirplaneCabinMusicStop",
            trackId ?? "", SendMessageOptions.DontRequireReceiver);
    }

    public void SetCabinLocked(bool locked)
    {
        SendMessage(locked ? "OnAirplaneCabinLocked" : "OnAirplaneCabinUnlocked",
            this, SendMessageOptions.DontRequireReceiver);
    }
}
