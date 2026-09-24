using System.Collections.Generic;
using UnityEngine;

/// <summary>Helicopter / magneto craft — cabin, magnetos, telecom, GPS portal HUD.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Helicopter Vehicle Ragdoll")]
public sealed class HelicopterVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string craftName = "Helicopter";
    public string callsign = "CUU-H1";
    public string prefabId;
    public MagnetoHelicopterConfigurationAsset configurationAsset;

    [Header("Magnetos")]
    public List<MagnetoLiftParams> magnetos = new List<MagnetoLiftParams>();
    public MagnetoLiftParams tailRotor = new MagnetoLiftParams { magnetoId = "tail", spanLength = 2.5f };
    public MagnetoLiftRequirements requirements = new MagnetoLiftRequirements();
    public List<Transform> magnetoAnchors = new List<Transform>();

    [Header("Cabin")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> grabBars = new List<Transform>();
    public List<Transform> standupSupportBarAnchors = new List<Transform>();
    public bool standupSupportBars = true;
    public GrabBarShape grabBarShape = GrabBarShape.Rail;
    public bool hasBathroom;
    public Transform bathroomAnchor;
    public bool bathroomOccupied;
    public string bathroomDoorOpenCloseTopologyId = "bathroom_door";
    public bool hasKitchen;
    public RestaurantVenueRuntime galleyKitchen;
    public CompanyRegistration galleyCompany;
    public string parentKitchenCompanyId;

    [Header("Doors / gear")]
    public string doorOpenCloseTopologyId = "heli_door";
    public BehaviorTree doorOpenCloseBt;
    public string landingGearOpenCloseTopologyId = "heli_landing_gear";
    public BehaviorTree landingGearOverrideBt;
    public bool landingGearDown = true;

    [Header("Instruments")]
    public List<Component> instrumentProxies = new List<Component>();
    public string magnetoCollectiveSurfaceId = HelicopterLemmaPropertyKeys.MagnetoCollective;
    public string magnetoCyclicSurfaceId = HelicopterLemmaPropertyKeys.MagnetoCyclic;
    public string tailRudderSurfaceId = HelicopterLemmaPropertyKeys.TailRudder;
    public string accelerationSurfaceId = HelicopterLemmaPropertyKeys.Acceleration;
    public string airBrakeSurfaceId = HelicopterLemmaPropertyKeys.AirBrake;

    [Header("Telecom / GPS")]
    public Component telecomBridge;
    public Transform gpsWebtopMount;
    public PilotGpsHudWebtop gpsHud;
    public UnityRenderPortal renderPortal;
    public string gpsPortalId = "gps";
    public PilotGpsHudMode defaultHudMode = PilotGpsHudMode.BakedRoute;
    public string webtopUrl = "http://127.0.0.1:5175";

    [Header("Lights / slots")]
    public List<PixelLightGridMountGameObject> lightMounts = new List<PixelLightGridMountGameObject>();
    public List<HelicoptorGridSlotGameObject> gridSlots = new List<HelicoptorGridSlotGameObject>();
    [Tooltip("Per view×scope PixelLight settings + multi grid-slot catalog.")]
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    [Header("Route")]
    public bool insertLandingQueue = true;
    public string activeFlightId;

    protected override void Awake()
    {
        base.Awake();
        EnsureSystems();
        if (magnetos.Count == 0)
            magnetos.Add(new MagnetoLiftParams { magnetoId = "main" });
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (telecomBridge == null)
            telecomBridge = GetComponent("TelecomUnityBridge");
        if (configurationAsset != null)
            configurationAsset.ApplyTo(this);
        for (int i = 0; i < magnetos.Count; i++)
            magnetos[i]?.RecomputeTipEndCache(transform);
    }

    public void EnsureSystems()
    {
        if (gpsHud == null)
            gpsHud = GetComponent<PilotGpsHudWebtop>() ?? gameObject.AddComponent<PilotGpsHudWebtop>();
        gpsHud.helicopter = this;
        gpsHud.mode = defaultHudMode;

        if (renderPortal == null)
            renderPortal = GetComponent<UnityRenderPortal>() ?? gameObject.AddComponent<UnityRenderPortal>();
        renderPortal.portalId = gpsPortalId;
        renderPortal.sourceTexture = gpsHud.displayTexture;
        renderPortal.BindTelecom(telecomBridge);

        if (hasKitchen && galleyKitchen == null)
            galleyKitchen = GetComponentInChildren<RestaurantVenueRuntime>();
        if (galleyCompany == null && galleyKitchen != null)
            galleyCompany = galleyKitchen.GetComponent<CompanyRegistration>();
        if (!string.IsNullOrEmpty(parentKitchenCompanyId) && galleyCompany != null)
            galleyCompany.parentCompanyId = parentKitchenCompanyId;
    }

    public MagnetoLiftParams MainMagneto =>
        magnetos != null && magnetos.Count > 0 ? magnetos[0] : null;

    public void ApplyRequirementsToSelected(int magnetoIndex)
    {
        if (magnetos == null || magnetoIndex < 0 || magnetoIndex >= magnetos.Count) return;
        requirements?.ApplyMinimumsTo(magnetos[magnetoIndex]);
    }

    public void SetLandingGearDown(bool down)
    {
        landingGearDown = down;
        SendMessage("OnNarrativeSchedulerAction",
            down ? HelicopterNarrativeActionIds.GearDown : HelicopterNarrativeActionIds.GearUp,
            SendMessageOptions.DontRequireReceiver);
    }

    public Transform ResolveGrabOrStandup(Transform seat)
    {
        if (seat == null) return null;
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 origin = seat.position + seat.forward * 0.4f;
        void Consider(List<Transform> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                float d = (list[i].position - origin).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = list[i]; }
            }
        }
        Consider(grabBars);
        if (standupSupportBars)
            Consider(standupSupportBarAnchors);
        return best;
    }
}
