using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AirplaneConfiguration", menuName = "Locomotion/Civil/Airplane Configuration")]
public sealed class AirplaneConfigurationAsset : ScriptableObject
{
    [Header("Identity")]
    public string planeName = "Airliner";
    public string callsign = "CUU-A1";
    public string prefabId;
    public string[] pilotPersonaKeys = { "pilot" };

    [Header("Fuel / goose")]
    public float fuelTankCapacity = 100f;
    public float fuelStart = 100f;
    public string engineGooseContents;

    [Header("Company")]
    public string parentKitchenCompanyId;
    public string companyId;

    [Header("Aero")]
    public AirplaneWingSurfaceParams leftWing = new AirplaneWingSurfaceParams { surfaceId = "left_wing" };
    public AirplaneWingSurfaceParams rightWing = new AirplaneWingSurfaceParams
    {
        surfaceId = "right_wing",
        centerlineAngleDeg = 180f
    };
    public AirplaneWingSurfaceParams horizontalTail = new AirplaneWingSurfaceParams
    {
        surfaceId = "h_tail",
        spanLength = 12f,
        aspectRatio = 5f
    };
    public AirplaneWingSurfaceParams verticalTail = new AirplaneWingSurfaceParams
    {
        surfaceId = "v_tail",
        spanLength = 8f,
        aspectRatio = 2f,
        centerlineAngleDeg = 90f
    };
    public AirplaneEllipsoidAeroParams fuselageEllipsoid = new AirplaneEllipsoidAeroParams();
    public List<AirplaneJetEngineParams> jets = new List<AirplaneJetEngineParams>();

    [Header("Power")]
    public List<AirplaneBatteryPack> batteries = new List<AirplaneBatteryPack>();
    public List<AirplanePowerSystemDraw> powerSystems = new List<AirplanePowerSystemDraw>();
    public float chargeKwWhenEnginesOn = 25f;

    [Header("Topology")]
    public string noseOpenCloseTopologyId = "concorde_nose";
    public string landingGearOpenCloseTopologyId = "landing_gear";
    public string bathroomDoorOpenCloseTopologyId = "bathroom_door";
    public string ejectorSeatOpenCloseTopologyId;
    public string weaponBayOpenCloseTopologyId;
    public string webtopOpenCloseTopologyId = "cabin_webtop";
    public string seatbackWebtopOpenCloseTopologyId = "seatback_webtop";

    [Header("Cabin counts")]
    public int seatCount = 120;
    public int seatbackWebtopCount = 120;
    public int seatPowerOutletCount = 120;
    public float seatOutletDrawKwEach = 0.05f;
    public float seatbackWebtopDrawKwEach = 0.02f;
    public bool seatbackWebtopsEnabled = true;

    [Header("Music / PA")]
    public AirplaneCabinMusicSource defaultMusicSource = AirplaneCabinMusicSource.Chorus;
    public bool paDucksMusic = true;

    [Header("Route")]
    public bool insertLandingQueue = true;
    public bool insertRefuelBeforePark = true;
    [Range(0f, 1f)] public float refuelFuelThreshold01 = 0.35f;

    [Header("ATC")]
    public string defaultDestinationAtcServiceId;
    public AtcDispatcherDialogueCatalog dialogueCatalog;

    [Header("PixelLight")]
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    [Header("Checklist template")]
    public TSAChecklistCard checklistTemplate;

    public void EnsureDefaults()
    {
        if (leftWing == null) leftWing = new AirplaneWingSurfaceParams { surfaceId = "left_wing" };
        if (rightWing == null)
            rightWing = new AirplaneWingSurfaceParams { surfaceId = "right_wing", centerlineAngleDeg = 180f };
        if (horizontalTail == null)
            horizontalTail = new AirplaneWingSurfaceParams { surfaceId = "h_tail", spanLength = 12f };
        if (verticalTail == null)
            verticalTail = new AirplaneWingSurfaceParams { surfaceId = "v_tail", spanLength = 8f, centerlineAngleDeg = 90f };
        if (fuselageEllipsoid == null) fuselageEllipsoid = new AirplaneEllipsoidAeroParams();
        if (jets == null) jets = new List<AirplaneJetEngineParams>();
        if (jets.Count == 0)
            jets.Add(new AirplaneJetEngineParams { engineId = "jet_l", localPosition = new Vector3(-4f, -1f, 2f) });
        if (batteries == null) batteries = new List<AirplaneBatteryPack>();
        if (batteries.Count == 0)
            batteries.Add(new AirplaneBatteryPack());
        if (powerSystems == null) powerSystems = new List<AirplanePowerSystemDraw>();
        if (powerSystems.Count == 0)
            AirplanePowerBus.FillDefaultPowerSystems(powerSystems);
        if (checklistTemplate == null)
            checklistTemplate = TSAChecklistCard.Generate(null);
        if (dialogueCatalog == null)
            dialogueCatalog = new AtcDispatcherDialogueCatalog();
        dialogueCatalog.EnsureDefaults();
    }

    public void ApplyTo(AirplaneVehicleRagdoll plane)
    {
        if (plane == null) return;
        EnsureDefaults();
        plane.planeName = planeName;
        plane.callsign = callsign;
        plane.prefabId = prefabId;
        plane.pilotPersonaKeys = pilotPersonaKeys != null ? (string[])pilotPersonaKeys.Clone() : plane.pilotPersonaKeys;
        plane.fuelTankCapacity = fuelTankCapacity;
        plane.fuel01 = fuelTankCapacity > 1e-4f ? Mathf.Clamp01(fuelStart / fuelTankCapacity) : 0f;
        plane.engineGooseContents = engineGooseContents;
        plane.parentKitchenCompanyId = parentKitchenCompanyId;
        plane.leftWing = leftWing;
        plane.rightWing = rightWing;
        plane.horizontalTail = horizontalTail;
        plane.verticalTail = verticalTail;
        plane.fuselageEllipsoid = fuselageEllipsoid;
        plane.jets = jets != null ? new List<AirplaneJetEngineParams>(jets) : plane.jets;
        plane.batteries = batteries != null ? new List<AirplaneBatteryPack>(batteries) : plane.batteries;
        plane.powerSystems = powerSystems != null ? new List<AirplanePowerSystemDraw>(powerSystems) : plane.powerSystems;
        plane.chargeKwWhenEnginesOn = chargeKwWhenEnginesOn;
        plane.noseOpenCloseTopologyId = noseOpenCloseTopologyId;
        plane.landingGearOpenCloseTopologyId = landingGearOpenCloseTopologyId;
        plane.bathroomDoorOpenCloseTopologyId = bathroomDoorOpenCloseTopologyId;
        plane.ejectorSeatOpenCloseTopologyId = ejectorSeatOpenCloseTopologyId;
        plane.weaponBayOpenCloseTopologyId = weaponBayOpenCloseTopologyId;
        plane.webtopOpenCloseTopologyId = webtopOpenCloseTopologyId;
        plane.seatbackWebtopOpenCloseTopologyId = seatbackWebtopOpenCloseTopologyId;
        plane.seatbackWebtopCount = seatbackWebtopCount;
        plane.seatPowerOutletCount = seatPowerOutletCount;
        plane.seatOutletDrawKwEach = seatOutletDrawKwEach;
        plane.seatbackWebtopDrawKwEach = seatbackWebtopDrawKwEach;
        plane.seatbackWebtopsEnabled = seatbackWebtopsEnabled;
        plane.defaultMusicSource = defaultMusicSource;
        plane.paDucksMusic = paDucksMusic;
        plane.insertLandingQueue = insertLandingQueue;
        plane.insertRefuelBeforePark = insertRefuelBeforePark;
        plane.refuelFuelThreshold01 = refuelFuelThreshold01;
        plane.defaultDestinationAtcServiceId = defaultDestinationAtcServiceId;
        plane.dialogueCatalog = dialogueCatalog;
        plane.checklistTemplate = checklistTemplate;
        if (pixelLightCatalog != null)
            plane.pixelLightCatalog = pixelLightCatalog;
        plane.EnsureSystems();
        plane.RecomputeWingTipCaches();
    }

    public void CaptureFrom(AirplaneVehicleRagdoll plane)
    {
        if (plane == null) return;
        planeName = plane.planeName;
        callsign = plane.callsign;
        prefabId = plane.prefabId;
        pilotPersonaKeys = plane.pilotPersonaKeys != null ? (string[])plane.pilotPersonaKeys.Clone() : pilotPersonaKeys;
        fuelTankCapacity = plane.fuelTankCapacity;
        fuelStart = plane.fuelTankCapacity * Mathf.Clamp01(plane.fuel01);
        engineGooseContents = plane.engineGooseContents;
        parentKitchenCompanyId = plane.parentKitchenCompanyId;
        leftWing = plane.leftWing ?? leftWing;
        rightWing = plane.rightWing ?? rightWing;
        horizontalTail = plane.horizontalTail ?? horizontalTail;
        verticalTail = plane.verticalTail ?? verticalTail;
        fuselageEllipsoid = plane.fuselageEllipsoid ?? fuselageEllipsoid;
        jets = plane.jets != null ? new List<AirplaneJetEngineParams>(plane.jets) : jets;
        batteries = plane.batteries != null ? new List<AirplaneBatteryPack>(plane.batteries) : batteries;
        powerSystems = plane.powerSystems != null ? new List<AirplanePowerSystemDraw>(plane.powerSystems) : powerSystems;
        chargeKwWhenEnginesOn = plane.chargeKwWhenEnginesOn;
        noseOpenCloseTopologyId = plane.noseOpenCloseTopologyId;
        landingGearOpenCloseTopologyId = plane.landingGearOpenCloseTopologyId;
        bathroomDoorOpenCloseTopologyId = plane.bathroomDoorOpenCloseTopologyId;
        ejectorSeatOpenCloseTopologyId = plane.ejectorSeatOpenCloseTopologyId;
        weaponBayOpenCloseTopologyId = plane.weaponBayOpenCloseTopologyId;
        webtopOpenCloseTopologyId = plane.webtopOpenCloseTopologyId;
        seatbackWebtopOpenCloseTopologyId = plane.seatbackWebtopOpenCloseTopologyId;
        seatbackWebtopCount = plane.seatbackWebtopCount;
        seatPowerOutletCount = plane.seatPowerOutletCount;
        seatOutletDrawKwEach = plane.seatOutletDrawKwEach;
        seatbackWebtopDrawKwEach = plane.seatbackWebtopDrawKwEach;
        seatbackWebtopsEnabled = plane.seatbackWebtopsEnabled;
        defaultMusicSource = plane.defaultMusicSource;
        paDucksMusic = plane.paDucksMusic;
        insertLandingQueue = plane.insertLandingQueue;
        insertRefuelBeforePark = plane.insertRefuelBeforePark;
        refuelFuelThreshold01 = plane.refuelFuelThreshold01;
        defaultDestinationAtcServiceId = plane.defaultDestinationAtcServiceId;
        dialogueCatalog = plane.dialogueCatalog ?? dialogueCatalog;
        checklistTemplate = plane.checklistTemplate ?? checklistTemplate;
        if (plane.pixelLightCatalog != null)
            pixelLightCatalog = plane.pixelLightCatalog;
    }
}
