using System.Collections.Generic;
using UnityEngine;

/// <summary>Transit bus — seating, talk rules, bathroom, baggage, fuel, telecom/webtop, stop buttons.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Bus Vehicle Ragdoll")]
public sealed class BusVehicleRagdoll : VehicleRagdoll
{
    [Header("Seating")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public List<Transform> stopButtons = new List<Transform>();
    public List<Transform> grabBars = new List<Transform>();
    public List<VehicleGrabHold> grabHolds = new List<VehicleGrabHold>();
    public List<VehicleStrapHold> strapHolds = new List<VehicleStrapHold>();
    public bool standupSupportBars = true;
    public SeatedPelvisPoseCache pelvisPoseCache;

    [Header("Rules")]
    public bool allowPassengerTalk = true;
    public bool quietCar;
    public bool hasBathroom;
    public Transform bathroomAnchor;

    [Header("Baggage / fuel / maintenance")]
    public Transform baggageBay;
    public Transform fuelPort;
    [Range(0f, 1f)] public float fuel01 = 1f;
    public Transform maintenanceLocation;
    public VehicleRepairCenterRuntime preferredRepairCenter;

    [Header("Driver telecom / webtop")]
    public Transform driverSeat;
    public Component telecomBridge;
    public bool webtopRouteMapEnabled = true;
    public string activeRouteId;
    public string cabinMusicTrackId;
    public bool cabinMusicPlaying;

    [Header("Sound design")]
    [Range(0f, 1f)] public float engineBody01 = 0.5f;
    [Range(0f, 1f)] public float cabinHiss01 = 0.2f;
    [Range(0f, 1f)] public float doorThump01 = 0.4f;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "baggage") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "baggage", capacity = 80f });
        if (pelvisPoseCache == null)
            pelvisPoseCache = GetComponent<SeatedPelvisPoseCache>()
                              ?? gameObject.AddComponent<SeatedPelvisPoseCache>();
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (seatAnchors.Count == 0)
            CollectNamedChildren("seat", seatAnchors);
        if (stopButtons.Count == 0)
            CollectNamedChildren("stop", stopButtons);
        if (grabBars.Count == 0)
            CollectNamedChildren("bar", grabBars);
        EnsureSharedHolds();
        if (telecomBridge == null)
            telecomBridge = GetComponent("TelecomUnityBridge");
    }

    public void EnsureSharedHolds()
    {
        if (grabHolds == null) grabHolds = new List<VehicleGrabHold>();
        if (strapHolds == null) strapHolds = new List<VehicleStrapHold>();
        grabHolds.Clear();
        strapHolds.Clear();
        grabHolds.AddRange(GetComponentsInChildren<VehicleGrabHold>(true));
        strapHolds.AddRange(GetComponentsInChildren<VehicleStrapHold>(true));
        if (standupSupportBars && grabHolds.Count == 0 && grabBars.Count > 0)
        {
            for (int i = 0; i < grabBars.Count; i++)
            {
                if (grabBars[i] == null) continue;
                var hold = grabBars[i].GetComponent<VehicleGrabHold>()
                           ?? grabBars[i].gameObject.AddComponent<VehicleGrabHold>();
                hold.EnsureCollider();
                grabHolds.Add(hold);
            }
        }
    }

    void CollectNamedChildren(string token, List<Transform> into)
    {
        var all = GetComponentsInChildren<Transform>(true);
        string t = token.ToLowerInvariant();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.ToLowerInvariant().Contains(t))
                into.Add(all[i]);
        }
    }

    public bool MayTalk() => allowPassengerTalk && !quietCar;

    public Transform ResolveSeatSupport(Transform seat)
    {
        if (seat == null) return null;
        // Prefer grab bar / seat ahead of the actor for seated tool-use IK.
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 origin = seat.position + seat.forward * 0.45f;
        for (int i = 0; i < grabBars.Count; i++)
        {
            Transform b = grabBars[i];
            if (b == null) continue;
            float d = (b.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        for (int i = 0; i < seatAnchors.Count; i++)
        {
            Transform s = seatAnchors[i];
            if (s == null || s == seat) continue;
            float d = (s.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
    }

    public void SetCabinMusic(string trackId, bool play)
    {
        cabinMusicTrackId = trackId;
        cabinMusicPlaying = play;
        SendMessage(play ? "OnBusCabinMusicPlay" : "OnBusCabinMusicStop", trackId ?? "", SendMessageOptions.DontRequireReceiver);
    }

    public void ApplySoundDesign(float engineBody, float cabinHiss, float doorThump)
    {
        engineBody01 = Mathf.Clamp01(engineBody);
        cabinHiss01 = Mathf.Clamp01(cabinHiss);
        doorThump01 = Mathf.Clamp01(doorThump);
        SendMessage("OnBusSoundDesign", this, SendMessageOptions.DontRequireReceiver);
    }

    public void RequestStop(Transform button = null)
    {
        SendMessage("OnBusStopRequested", button != null ? button : transform, SendMessageOptions.DontRequireReceiver);
    }

    public TSAGroundCrewCard MakeBaggageCard(bool loading) =>
        TSAGroundCrewCard.Generate(this, loading, baggageBay != null ? baggageBay.name : "baggage");
}
