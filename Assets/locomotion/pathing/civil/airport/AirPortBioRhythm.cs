using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Airport venue biorhythm — symbiotic with ATC + TA; facilitates TSA/gate/baggage cards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Bio Rhythm")]
public sealed class AirPortBioRhythm : DispatchBioRhythm
{
    [CronExpr] public string maintenanceCron = "0 2 * * 0";
    [Range(0f, 1f)] public float terrorLevel01 = 0.2f;
    [Range(0f, 1f)] public float passengerDensity01;
    public AirTrafficControlBioRhythm atc;
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public AirportBuildingRagdoll building;

    protected override void Awake()
    {
        serviceId = AirportDispatchKinds.Airport;
        governmentAssigned = true;
        if (string.IsNullOrEmpty(hoursCron) || hoursCron == "* * * * *")
            hoursCron = "* 5-23 * * *";
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (atc == null)
            atc = GetComponent<AirTrafficControlBioRhythm>() ?? FindFirstObjectByType<AirTrafficControlBioRhythm>();
        if (building == null)
            building = GetComponent<AirportBuildingRagdoll>();
        if (!subscribedPeerIds.Contains(TADispatchKinds.AirTrafficControl))
            subscribedPeerIds.Add(TADispatchKinds.AirTrafficControl);
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
        if (company != null && company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "airport_manager", peckingOrder = 2, personaKey = "airport_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "tsa_agent", peckingOrder = 15, personaKey = "tsa_agent" });
            company.staff.Add(new RetinuePeckingEntry { role = "gate_agent", peckingOrder = 18, personaKey = "gate_agent" });
            company.staff.Add(new RetinuePeckingEntry { role = "ground_crew", peckingOrder = 25, personaKey = "ground_crew" });
            company.staff.Add(new RetinuePeckingEntry { role = "pilot", peckingOrder = 12, personaKey = "pilot" });
        }
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        unitsAvailable01 = open ? Mathf.Clamp01(1f - terrorLevel01 * 0.35f) : 0.2f;
        if (venueBio != null)
        {
            venueBio.activity01 = open ? Mathf.Clamp01(0.35f + passengerDensity01 * 0.5f) : 0.08f;
            venueBio.stress01 = Mathf.Clamp01(terrorLevel01 * 0.7f + passengerDensity01 * 0.2f);
        }
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case AirportDispatchKinds.TsaCheckpoint:
                cards.Add(TSACheckpointCard.Generate(request, terrorLevel01));
                break;
            case AirportDispatchKinds.TsaCheckin:
                cards.Add(TSACheckinProcedureCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaAnnounce:
                cards.Add(TSAAnnouncementAirportCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaPatrol:
                cards.Add(TSAPatrolCard.Generate(request, terrorLevel01));
                break;
            case AirportDispatchKinds.TsaBaggage:
                cards.Add(TSABaggageCrewCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaGateCrew:
                cards.Add(TSAGateCrewCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaGate:
                cards.Add(TSAGateCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaHolding:
                cards.Add(TSAHoldingPatternCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaCruise:
                cards.Add(TSACruisingCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaLanding:
                cards.Add(TSALandingCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaTakeoff:
                cards.Add(TSATakeoffCard.Generate(request));
                cards.Add(PilotTakeOffCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaChecklist:
                cards.Add(TSAChecklistCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaDisaster:
            {
                var disaster = TSADisasterCard.Generate(request);
                disaster.ApplyNearestAtc(atc);
                cards.Add(disaster);
                break;
            }
            case AirportDispatchKinds.TsaRecovery:
                cards.Add(TSARecoveryCrewCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaFerry:
                cards.Add(TSAFerryRequestCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaWait:
                cards.Add(TSAWaitingQueueCard.Generate(request));
                break;
            case AirportDispatchKinds.TsaPark:
                cards.Add(TSAParkRequestCard.Generate(request));
                break;
            case AirportDispatchKinds.GateBridge:
                cards.Add(AirPortTerminalGateExtensionBridgeCard.Generate(request));
                break;
            default:
                cards.Add(TSAAnnouncementAirportCard.Generate(request));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
