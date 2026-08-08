using System;
using System.Collections.Generic;
using UnityEngine;

public enum TSASecurityLevel
{
    Standard = 0,
    Elevated = 1,
    High = 2,
    Maximum = 3
}

[Serializable]
public class TSAAirportCard : TravelAgentCard
{
    public DispatchRequest request;
    public List<string> lemmaTags = new List<string>();
    public List<string> dialogSuggestions = new List<string>();

    public TSAAirportCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        isJusticeGoal = true;
        physicalPathingTag = "tsa";
        traversabilityTag = "airport";
    }

    protected static void Fill(TSAAirportCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.limits = new SectionLimits { maxForce = 65f, maxTorque = 16f, maxVelocityChange = 1.2f };
    }
}

[Serializable]
public class TSACheckpointCard : TSAAirportCard
{
    public TSASecurityLevel securityLevel = TSASecurityLevel.Standard;
    public string locationId = "security";

    public static TSACheckpointCard Generate(DispatchRequest request, float terrorLevel01)
    {
        var c = new TSACheckpointCard();
        Fill(c, request, "tsa_checkpoint");
        c.securityLevel = terrorLevel01 > 0.75f ? TSASecurityLevel.Maximum
            : terrorLevel01 > 0.5f ? TSASecurityLevel.High
            : terrorLevel01 > 0.3f ? TSASecurityLevel.Elevated
            : TSASecurityLevel.Standard;
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        return c;
    }
}

[Serializable]
public class TSACheckinProcedureCard : TSAAirportCard
{
    public static TSACheckinProcedureCard Generate(DispatchRequest request)
    {
        var c = new TSACheckinProcedureCard();
        Fill(c, request, "tsa_checkin");
        c.dialogSuggestions.Add("Boarding pass and identification please.");
        return c;
    }
}

[Serializable]
public class TSAAnnouncementAttendantCard : TSAAirportCard
{
    public string utterance = "Now boarding.";

    public static TSAAnnouncementAttendantCard Generate(DispatchRequest request, string utterance = null)
    {
        var c = new TSAAnnouncementAttendantCard();
        Fill(c, request, "tsa_announce_attendant");
        c.utterance = utterance ?? request?.notes ?? c.utterance;
        c.dialogSuggestions.Add(c.utterance);
        return c;
    }
}

[Serializable]
public class TSAAnnouncementAirportCard : TSAAirportCard
{
    public string utterance = "Attention passengers.";

    public static TSAAnnouncementAirportCard Generate(DispatchRequest request)
    {
        var c = new TSAAnnouncementAirportCard();
        Fill(c, request, "tsa_announce_airport");
        c.utterance = request?.notes ?? c.utterance;
        c.dialogSuggestions.Add(c.utterance);
        return c;
    }
}

[Serializable]
public class TSAFlightAttendantServiceCard : TSAAirportCard
{
    public bool meals;
    public bool questionPing;
    public bool askSitDown;
    public bool cleanup;
    public bool itemDelivery;

    public static TSAFlightAttendantServiceCard Generate(DispatchRequest request)
    {
        var c = new TSAFlightAttendantServiceCard();
        Fill(c, request, "tsa_flight_attendant_service");
        c.meals = true;
        c.questionPing = true;
        c.askSitDown = true;
        c.cleanup = true;
        c.itemDelivery = true;
        return c;
    }
}

[Serializable]
public class TSAPassengerPingCard : TSAAirportCard
{
    public bool useIk = true;

    public static TSAPassengerPingCard Generate(GameObject passenger)
    {
        var c = new TSAPassengerPingCard();
        Fill(c, null, "tsa_passenger_ping");
        c.goalTarget = passenger;
        c.useIk = true;
        return c;
    }
}

[Serializable]
public class TSAPassengerSeatTrayCard : TSAAirportCard
{
    public bool raise = true;
    public string openCloseTopologyId = "seat_tray";

    public static TSAPassengerSeatTrayCard Generate(bool raise, string topologyId = "seat_tray")
    {
        var c = new TSAPassengerSeatTrayCard();
        Fill(c, null, raise ? "tsa_seat_tray_raise" : "tsa_seat_tray_lower");
        c.raise = raise;
        c.openCloseTopologyId = topologyId;
        return c;
    }
}

[Serializable]
public class TSAPatrolCard : TSAAirportCard
{
    [Range(0f, 1f)] public float violenceThreshold01 = 0.35f;

    public static TSAPatrolCard Generate(DispatchRequest request, float terrorLevel01)
    {
        var c = new TSAPatrolCard();
        Fill(c, request, "tsa_patrol");
        c.violenceThreshold01 = Mathf.Clamp01(0.2f + terrorLevel01 * 0.6f);
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        return c;
    }
}

[Serializable]
public class TSAAttendantGateCard : TSAAirportCard
{
    public string boardingPartyLemma = AirportLemmaPropertyKeys.BoardingParty(1);

    public static TSAAttendantGateCard Generate(DispatchRequest request, int boardingParty = 1)
    {
        var c = new TSAAttendantGateCard();
        Fill(c, request, "tsa_attendant_gate");
        c.boardingPartyLemma = AirportLemmaPropertyKeys.BoardingParty(boardingParty);
        c.lemmaTags.Add(c.boardingPartyLemma);
        return c;
    }
}

[Serializable]
public class TSALadderCrewCard : TSAAirportCard
{
    public bool useVehicleIk = true;

    public static TSALadderCrewCard Generate(DispatchRequest request)
    {
        var c = new TSALadderCrewCard();
        Fill(c, request, "tsa_ladder_crew");
        return c;
    }
}

[Serializable]
public class TSAGateCrewCard : TSAAirportCard
{
    [Range(0f, 1f)] public float galleyCommodityDemand01 = 0.4f;

    public static TSAGateCrewCard Generate(DispatchRequest request)
    {
        var c = new TSAGateCrewCard();
        Fill(c, request, "tsa_gate_crew");
        return c;
    }

    public void ApplyCommodityDemand(AirportRuntime airport)
    {
        if (airport?.bio == null) return;
        airport.bio.passengerDensity01 = Mathf.Clamp01(airport.bio.passengerDensity01 + galleyCommodityDemand01 * 0.1f);
    }
}

[Serializable]
public class GateDeskCrewCard : TSAAirportCard
{
    public int planePassengerCount;
    public int boardingPartyComplete;
    public List<string> crewKnowledgeMap = new List<string>();
    public List<string> passengerKnowledgeMap = new List<string>();

    public static GateDeskCrewCard Generate(DispatchRequest request, int passengers, int party)
    {
        var c = new GateDeskCrewCard();
        Fill(c, request, "gate_desk_crew");
        c.planePassengerCount = passengers;
        c.boardingPartyComplete = party;
        c.lemmaTags.Add(AirportLemmaPropertyKeys.BoardingParty(party));
        c.crewKnowledgeMap.Add("landing_interactions");
        c.passengerKnowledgeMap.Add("group_boarding");
        return c;
    }
}

[Serializable]
public class TSABaggageCrewCard : TSAAirportCard
{
    public bool loading = true;
    public bool pokeTestIk = true;
    public bool animalHandling;
    public string inventoryOpenCloseTopologyId = "baggage_hold";

    public static TSABaggageCrewCard Generate(DispatchRequest request)
    {
        var c = new TSABaggageCrewCard();
        Fill(c, request, "tsa_baggage");
        c.loading = request == null || (request.notes ?? "").IndexOf("unload", StringComparison.OrdinalIgnoreCase) < 0;
        c.lemmaTags.Add(c.loading ? AirportLemmaPropertyKeys.BaggageLoad : AirportLemmaPropertyKeys.BaggageUnload);
        if (request != null && (request.notes ?? "").IndexOf("animal", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            c.animalHandling = true;
            c.lemmaTags.Add(AirportLemmaPropertyKeys.CagePeer);
            c.lemmaTags.Add(AirportLemmaPropertyKeys.AnimalInCage);
        }
        return c;
    }
}
