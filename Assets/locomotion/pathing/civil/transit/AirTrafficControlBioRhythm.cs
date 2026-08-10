using System.Collections.Generic;
using UnityEngine;

/// <summary>ATC peer — corridor priority, landing queue, destination ATC, FacilitateCards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Air Traffic Control Bio Rhythm")]
public sealed class AirTrafficControlBioRhythm : DispatchBioRhythm
{
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public string trafficPeerId = "traffic_warden";
    public string airportPeerId = AirportDispatchKinds.Airport;
    public string defaultDestinationAtcServiceId;
    public AtcDispatcherDialogueCatalog dialogueCatalog = new AtcDispatcherDialogueCatalog();

    readonly List<string> _landingQueue = new List<string>();
    readonly Dictionary<string, AirplaneVehicleRagdoll> _queuedPlanes = new Dictionary<string, AirplaneVehicleRagdoll>();
    string _activeLandingFlightId;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = TADispatchKinds.AirTrafficControl;
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
        if (!subscribedPeerIds.Contains(trafficPeerId))
            subscribedPeerIds.Add(trafficPeerId);
        if (!subscribedPeerIds.Contains(airportPeerId))
            subscribedPeerIds.Add(airportPeerId);
        dialogueCatalog?.EnsureDefaults();
    }

    public bool RequestCorridorPriority(Vector3 worldTarget, string notes = null, float priority01 = 0.75f)
    {
        var hub = CentralDispatchHub.Instance;
        if (hub == null) return false;
        var req = new DispatchRequest
        {
            kind = TADispatchKinds.Reroute,
            worldTarget = worldTarget,
            notes = "atc_corridor|" + (notes ?? ""),
            priority01 = priority01
        };
        bool toTa = hub.RequestCrossDispatch(serviceId, transportationPeerId, req);
        hub.RequestCrossDispatch(serviceId, trafficPeerId, new DispatchRequest
        {
            kind = "route",
            worldTarget = worldTarget,
            notes = "atc_surface_clearance|" + (notes ?? ""),
            priority01 = priority01 * 0.8f
        });
        return toTa;
    }

    public void EnqueueLanding(string flightId, AirplaneVehicleRagdoll plane = null)
    {
        if (string.IsNullOrEmpty(flightId)) return;
        if (!_landingQueue.Contains(flightId))
            _landingQueue.Add(flightId);
        if (plane != null)
            _queuedPlanes[flightId] = plane;
        queueDepth01 = Mathf.Clamp01(_landingQueue.Count / 8f);
    }

    public bool TryClaimLandingSlot(string flightId)
    {
        if (string.IsNullOrEmpty(flightId) || _landingQueue.Count == 0) return false;
        if (_landingQueue[0] != flightId) return false;
        _activeLandingFlightId = flightId;
        _landingQueue.RemoveAt(0);
        _queuedPlanes.Remove(flightId);
        queueDepth01 = Mathf.Clamp01(_landingQueue.Count / 8f);
        return true;
    }

    public IReadOnlyList<string> LandingQueue => _landingQueue;
    public string ActiveLandingFlightId => _activeLandingFlightId;

    public static AirTrafficControlBioRhythm SelectDestinationAtc(
        AirTrafficControlBioRhythm from,
        DispatchRequest request,
        bool preferNearest = false)
    {
        var all = Object.FindObjectsByType<AirTrafficControlBioRhythm>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0) return from;

        bool disaster = preferNearest
                        || (request != null && (
                            string.Equals(request.kind, AirportDispatchKinds.TsaDisaster, System.StringComparison.OrdinalIgnoreCase)
                            || (request.notes ?? "").IndexOf("disaster", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || (request.notes ?? "").IndexOf("potty", System.StringComparison.OrdinalIgnoreCase) >= 0));

        if (!disaster)
        {
            string preferred = from != null ? from.defaultDestinationAtcServiceId : null;
            if (string.IsNullOrEmpty(preferred) && request != null)
                preferred = request.toServiceId;
            if (!string.IsNullOrEmpty(preferred))
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && all[i].serviceId == preferred)
                        return all[i];
            }
            return from ?? all[0];
        }

        Vector3 origin = request != null ? request.worldTarget : (from != null ? from.transform.position : Vector3.zero);
        AirTrafficControlBioRhythm best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < all.Length; i++)
        {
            var atc = all[i];
            if (atc == null) continue;
            float d = (atc.transform.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = atc;
            }
        }
        return best ?? from;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        string dialogueSet = dialogueCatalog?.DialogueSetFor(kind);
        switch (kind)
        {
            case AirportDispatchKinds.AtcAllClear:
                cards.Add(ATCAllClearCard.Generate(request));
                break;
            case AirportDispatchKinds.AtcTakeOff:
            case AirportDispatchKinds.TsaTakeoff:
                cards.Add(ATCTakeOffCard.Generate(request));
                cards.Add(PilotTakeOffCard.Generate(request));
                cards.Add(TSATakeoffCard.Generate(request));
                break;
            case AirportDispatchKinds.AtcReportProblem:
                cards.Add(ATCReportProblemCard.Generate(request));
                break;
            case AirportDispatchKinds.AtcHolding:
                cards.Add(TSAHoldingPatternCard.Generate(request));
                cards.Add(PilotHoldingPatternCard.Generate(request));
                break;
            case AirportDispatchKinds.AtcLanding:
                cards.Add(TSALandingCard.Generate(request));
                cards.Add(PilotLandingCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaDisaster:
            {
                var disaster = TSADisasterCard.Generate(request);
                disaster.ApplyNearestAtc(this);
                cards.Add(disaster);
                break;
            }
            case AirportDispatchKinds.TsaChecklist:
                cards.Add(TSAChecklistCard.Generate(request));
                break;
            case AirportDispatchKinds.PilotTakeOff:
                cards.Add(PilotTakeOffCard.Generate(request));
                break;
            case AirportDispatchKinds.PilotCruise:
                cards.Add(PilotCruiseCard.Generate(request));
                cards.Add(TSACruisingCard.Generate(request));
                break;
            case AirportDispatchKinds.PilotLanding:
                cards.Add(PilotLandingCard.Generate(request));
                break;
            case AirportDispatchKinds.PilotManeuver:
                cards.Add(PilotManeuversCard.Generate(request, "turn_east", "turning due east in high turbulence", "turbulence"));
                break;
            case AirportDispatchKinds.PilotGate:
                cards.Add(PilotGateCard.Generate(request));
                cards.Add(TSAGateCard.Generate(request));
                break;
            default:
                cards.Add(ATCAllClearCard.Generate(request));
                break;
        }
        if (!string.IsNullOrEmpty(dialogueSet))
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] is AirplaneCard ac && string.IsNullOrEmpty(ac.description))
                    ac.description = dialogueSet;
            }
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
