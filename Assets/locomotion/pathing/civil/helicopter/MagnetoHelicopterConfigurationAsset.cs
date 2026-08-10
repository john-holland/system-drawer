using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagnetoHelicopterConfiguration", menuName = "Locomotion/Civil/Magneto Helicopter Configuration")]
public sealed class MagnetoHelicopterConfigurationAsset : ScriptableObject
{
    public string craftName = "Helicopter";
    public string callsign = "CUU-H1";
    public string prefabId;
    public List<MagnetoLiftParams> magnetos = new List<MagnetoLiftParams>();
    public MagnetoLiftRequirements requirements = new MagnetoLiftRequirements();
    public MagnetoLiftParams tailRotor = new MagnetoLiftParams
    {
        magnetoId = "tail",
        spanLength = 2.5f,
        bladeCount = 2,
        aspectRatio = 4f
    };

    public string landingGearOpenCloseTopologyId = "heli_landing_gear";
    public string doorOpenCloseTopologyId = "heli_door";
    public bool hasBathroom;
    public bool hasKitchen;
    public string parentKitchenCompanyId;
    public int seatCount = 4;
    public bool standupSupportBars = true;
    public GrabBarShape grabBarShape = GrabBarShape.Rail;

    public string gpsPortalId = "gps";
    public PilotGpsHudMode defaultHudMode = PilotGpsHudMode.BakedRoute;
    public int gpsRtWidth = 512;
    public int gpsRtHeight = 512;
    public string webtopUrl = "http://127.0.0.1:5175";

    [Header("PixelLight multi-slot")]
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    public void EnsureDefaults()
    {
        if (magnetos.Count == 0)
            magnetos.Add(new MagnetoLiftParams { magnetoId = "main" });
        if (requirements == null)
            requirements = new MagnetoLiftRequirements();
    }

    public void ApplyTo(HelicopterVehicleRagdoll heli)
    {
        if (heli == null) return;
        EnsureDefaults();
        heli.craftName = craftName;
        heli.callsign = callsign;
        heli.prefabId = prefabId;
        heli.magnetos = new List<MagnetoLiftParams>(magnetos);
        heli.tailRotor = tailRotor;
        heli.requirements = requirements;
        heli.landingGearOpenCloseTopologyId = landingGearOpenCloseTopologyId;
        heli.doorOpenCloseTopologyId = doorOpenCloseTopologyId;
        heli.hasBathroom = hasBathroom;
        heli.hasKitchen = hasKitchen;
        heli.parentKitchenCompanyId = parentKitchenCompanyId;
        heli.standupSupportBars = standupSupportBars;
        heli.grabBarShape = grabBarShape;
        heli.gpsPortalId = gpsPortalId;
        heli.defaultHudMode = defaultHudMode;
        if (pixelLightCatalog != null)
            heli.pixelLightCatalog = pixelLightCatalog;
        heli.EnsureSystems();
        if (heli.gpsHud != null)
        {
            heli.gpsHud.mode = defaultHudMode;
            heli.gpsHud.EnsureRenderTexture(gpsRtWidth, gpsRtHeight);
        }
        if (heli.renderPortal != null)
            heli.renderPortal.portalId = gpsPortalId;
        heli.pixelLightCatalog?.SyncSlotsFromHeli(heli);
    }

    public void CaptureFrom(HelicopterVehicleRagdoll heli)
    {
        if (heli == null) return;
        craftName = heli.craftName;
        callsign = heli.callsign;
        prefabId = heli.prefabId;
        magnetos = heli.magnetos != null ? new List<MagnetoLiftParams>(heli.magnetos) : magnetos;
        tailRotor = heli.tailRotor ?? tailRotor;
        requirements = heli.requirements ?? requirements;
        landingGearOpenCloseTopologyId = heli.landingGearOpenCloseTopologyId;
        doorOpenCloseTopologyId = heli.doorOpenCloseTopologyId;
        hasBathroom = heli.hasBathroom;
        hasKitchen = heli.hasKitchen;
        parentKitchenCompanyId = heli.parentKitchenCompanyId;
        standupSupportBars = heli.standupSupportBars;
        grabBarShape = heli.grabBarShape;
        gpsPortalId = heli.gpsPortalId;
        defaultHudMode = heli.defaultHudMode;
        if (heli.pixelLightCatalog != null)
            pixelLightCatalog = heli.pixelLightCatalog;
    }
}

public enum GrabBarShape
{
    Rail = 0,
    Loop = 1,
    T = 2,
    CustomMesh = 3
}

public enum PilotGpsHudMode
{
    BakedRoute = 0,
    RealtimeIsometric = 1
}
