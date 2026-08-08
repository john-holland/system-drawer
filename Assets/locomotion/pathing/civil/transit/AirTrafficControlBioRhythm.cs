using System.Collections.Generic;
using UnityEngine;

/// <summary>ATC peer — corridor priority + FacilitateCards for takeoff/landing/holding ops.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Air Traffic Control Bio Rhythm")]
public sealed class AirTrafficControlBioRhythm : DispatchBioRhythm
{
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public string trafficPeerId = "traffic_warden";
    public string airportPeerId = AirportDispatchKinds.Airport;

    protected override void Awake()
    {
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

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case AirportDispatchKinds.AtcAllClear:
                cards.Add(ATCAllClearCard.Generate(request));
                break;
            case AirportDispatchKinds.AtcTakeOff:
                cards.Add(ATCTakeOffCard.Generate(request));
                cards.Add(PilotTakeOffCard.Generate(request));
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
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
