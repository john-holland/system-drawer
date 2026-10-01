using System.Collections.Generic;
using UnityEngine;

/// <summary>Airliner — cabin, aero, power, gear, webtop, PixelLight mounts.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Vehicle Ragdoll")]
public sealed class AirplaneVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string planeName = "Airliner";
    public string callsign = "CUU-A1";
    public string prefabId;
    public string[] pilotPersonaKeys = { "pilot" };
    public AirplaneConfigurationAsset configurationAsset;

    [Header("Cabin")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> seatTrays = new List<Transform>();
    public string seatTrayOpenCloseTopologyId = "seat_tray";
    public bool allowPassengerTalk = true;
    public bool hasBathroom = true;
    public Transform bathroomAnchor;
    public bool bathroomOccupied;
    public string bathroomDoorOpenCloseTopologyId = "bathroom_door";

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
    public float fuelTankCapacity = 100f;
    [Range(0f, 1f)] public float fuel01 = 1f;
    public string engineGooseContents;
    public AirportExtensionGate dockedGate;

    [Header("Aero")]
    public AirplaneWingSurfaceParams leftWing = new AirplaneWingSurfaceParams { surfaceId = "left_wing" };
    public AirplaneWingSurfaceParams rightWing = new AirplaneWingSurfaceParams
    {
        surfaceId = "right_wing",
        centerlineAngleDeg = 180f
    };
    public AirplaneWingSurfaceParams horizontalTail = new AirplaneWingSurfaceParams { surfaceId = "h_tail", spanLength = 12f };
    public AirplaneWingSurfaceParams verticalTail = new AirplaneWingSurfaceParams
    {
        surfaceId = "v_tail",
        spanLength = 8f,
        centerlineAngleDeg = 90f
    };
    public AirplaneEllipsoidAeroParams fuselageEllipsoid = new AirplaneEllipsoidAeroParams();
    public List<AirplaneJetEngineParams> jets = new List<AirplaneJetEngineParams>();
    public AirplaneWeatherAeroBridge weatherAeroBridge;

    [Header("Power")]
    public List<AirplaneBatteryPack> batteries = new List<AirplaneBatteryPack>();
    public List<AirplanePowerSystemDraw> powerSystems = new List<AirplanePowerSystemDraw>();
    public float chargeKwWhenEnginesOn = 25f;
    public AirplaneBioRhythm airplaneBio;
    public AirplaneCabinMusicSystem cabinMusicSystem;
    public AirplaneCabinMusicSource defaultMusicSource = AirplaneCabinMusicSource.Chorus;
    public bool paDucksMusic = true;
    public int seatbackWebtopCount = 120;
    public int seatPowerOutletCount = 120;
    public float seatOutletDrawKwEach = 0.05f;
    public float seatbackWebtopDrawKwEach = 0.02f;
    public bool seatbackWebtopsEnabled = true;

    [Header("Topology / systems")]
    public string noseOpenCloseTopologyId = "concorde_nose";
    public string landingGearOpenCloseTopologyId = "landing_gear";
    public BehaviorTree landingGearOverrideBt;
    public bool landingGearDown = true;
    public string ejectorSeatOpenCloseTopologyId;
    public string weaponBayOpenCloseTopologyId;
    public string webtopOpenCloseTopologyId = "cabin_webtop";
    public string seatbackWebtopOpenCloseTopologyId = "seatback_webtop";
    public WebtopUscVideoPlayer webtopPlayer;
    public bool windshieldWipersOn;
    public bool radiationPathingEnabled;

    [Header("Route / ATC")]
    public bool insertLandingQueue = true;
    public bool insertRefuelBeforePark = true;
    [Range(0f, 1f)] public float refuelFuelThreshold01 = 0.35f;
    public string defaultDestinationAtcServiceId;
    public AtcDispatcherDialogueCatalog dialogueCatalog = new AtcDispatcherDialogueCatalog();
    public TSAChecklistCard checklistTemplate;

    [Header("Biplane magnetos / GPS")]
    public List<MagnetoLiftParams> magnetos = new List<MagnetoLiftParams>();
    public PilotGpsHudWebtop gpsHud;
    public UnityRenderPortal renderPortal;

    [Header("PixelLight multi-slot")]
    public List<PixelLightGridMountGameObject> lightMounts = new List<PixelLightGridMountGameObject>();
    public List<HelicoptorGridSlotGameObject> gridSlots = new List<HelicoptorGridSlotGameObject>();
    public PixelLightMultiSlotCatalog pixelLightCatalog;

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
        if (configurationAsset != null)
            configurationAsset.ApplyTo(this);
        else
            EnsureSystems();
        LinkGalleyToAirportKitchen();
        RecomputeWingTipCaches();
    }

    public void EnsureSystems()
    {
        EnsureDefaultPowerSystems();
        if (airplaneBio == null)
            airplaneBio = GetComponent<AirplaneBioRhythm>() ?? gameObject.AddComponent<AirplaneBioRhythm>();
        airplaneBio.airplane = this;

        if (cabinMusicSystem == null)
            cabinMusicSystem = GetComponent<AirplaneCabinMusicSystem>() ?? gameObject.AddComponent<AirplaneCabinMusicSystem>();
        cabinMusicSystem.airplane = this;
        cabinMusicSystem.source = defaultMusicSource;
        cabinMusicSystem.paDucksMusic = paDucksMusic;

        if (weatherAeroBridge == null)
            weatherAeroBridge = GetComponent<AirplaneWeatherAeroBridge>() ?? gameObject.AddComponent<AirplaneWeatherAeroBridge>();
        weatherAeroBridge.airplane = this;

        if (webtopEnabled && webtopPlayer == null)
            webtopPlayer = GetComponent<WebtopUscVideoPlayer>() ?? gameObject.AddComponent<WebtopUscVideoPlayer>();
        if (webtopPlayer != null)
        {
            webtopPlayer.airplane = this;
            webtopPlayer.openCloseTopologyId = webtopOpenCloseTopologyId;
        }

        if (gpsHud == null)
            gpsHud = GetComponent<PilotGpsHudWebtop>();
        if (renderPortal == null)
            renderPortal = GetComponent<UnityRenderPortal>();
        if (dialogueCatalog == null)
            dialogueCatalog = new AtcDispatcherDialogueCatalog();
        dialogueCatalog.EnsureDefaults();
        if (checklistTemplate == null)
            checklistTemplate = TSAChecklistCard.Generate(null);
    }

    public void EnsureDefaultPowerSystems()
    {
        if (batteries == null) batteries = new List<AirplaneBatteryPack>();
        if (batteries.Count == 0)
            batteries.Add(new AirplaneBatteryPack());
        if (powerSystems == null) powerSystems = new List<AirplanePowerSystemDraw>();
        if (powerSystems.Count == 0)
            AirplanePowerBus.FillDefaultPowerSystems(powerSystems);
    }

    public void RecomputeWingTipCaches()
    {
        leftWing?.RecomputeTipEndCache(transform);
        rightWing?.RecomputeTipEndCache(transform);
        horizontalTail?.RecomputeTipEndCache(transform);
        verticalTail?.RecomputeTipEndCache(transform);
        if (magnetos != null)
            for (int i = 0; i < magnetos.Count; i++)
                magnetos[i]?.RecomputeTipEndCache(transform);
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

    public void SetLandingGearDown(bool down)
    {
        landingGearDown = down;
        NotifyNarrative(down ? AirplaneNarrativeActionIds.GearDown : AirplaneNarrativeActionIds.GearUp);
    }

    public void NotifyNarrative(string actionId)
    {
        if (string.IsNullOrEmpty(actionId)) return;
        SendMessage("OnNarrativeSchedulerAction", actionId, SendMessageOptions.DontRequireReceiver);
    }

    public void ApplyPowerSystemEnabled(string systemId, bool enabled)
    {
        if (string.IsNullOrEmpty(systemId)) return;
        if (systemId == "seatback_webtops" && !enabled)
            seatbackWebtopsEnabled = false;
        else if (systemId == "seatback_webtops" && enabled)
            seatbackWebtopsEnabled = true;
        if (systemId == "webtops" && !enabled && webtopPlayer != null && webtopPlayer.playing)
            webtopPlayer.Close();
        if ((systemId == "music_system" || systemId == "pa_speakers") && !enabled)
            SetCabinMusic(cabinMusicTrackId, false);
        if (systemId == "seat_aux" && !enabled && cabinMusicSystem != null
            && cabinMusicSystem.source == AirplaneCabinMusicSource.SeatAux)
            cabinMusicSystem.SetMusicSource(AirplaneCabinMusicSource.Silent);
    }
}
