using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AirplaneCard : TravelAgentCard
{
    public DispatchRequest request;
    public AirplaneVehicleRagdoll airplane;
    public VehicleActorCard vehicleActorCard;
    public WaypointCard waypointCard;
    public CombatCard combat;

    public AirplaneCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "airplane";
        traversabilityTag = "air";
    }

    protected static void Fill(AirplaneCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.limits = new SectionLimits { maxForce = 100f, maxTorque = 28f, maxVelocityChange = 2.5f };
    }
}

[Serializable] public class ATCAllClearCard : AirplaneCard
{
    public static ATCAllClearCard Generate(DispatchRequest request)
    {
        var c = new ATCAllClearCard();
        Fill(c, request, "atc_all_clear");
        return c;
    }
}

[Serializable] public class ATCTakeOffCard : AirplaneCard
{
    public static ATCTakeOffCard Generate(DispatchRequest request)
    {
        var c = new ATCTakeOffCard();
        Fill(c, request, "atc_takeoff");
        return c;
    }
}

[Serializable] public class ATCReportProblemCard : AirplaneCard
{
    public static ATCReportProblemCard Generate(DispatchRequest request)
    {
        var c = new ATCReportProblemCard();
        Fill(c, request, "atc_report_problem");
        return c;
    }
}

[Serializable] public class PilotTakeOffCard : AirplaneCard
{
    public static PilotTakeOffCard Generate(DispatchRequest request, AirplaneVehicleRagdoll plane = null)
    {
        var c = new PilotTakeOffCard();
        Fill(c, request, "pilot_takeoff");
        c.airplane = plane;
        c.waypointCard = WaypointCard.Generate("runway_depart", c.goalWorld);
        return c;
    }
}

[Serializable] public class PilotLockCabinCard : AirplaneCard
{
    public static PilotLockCabinCard Generate(DispatchRequest request, AirplaneVehicleRagdoll plane = null)
    {
        var c = new PilotLockCabinCard();
        Fill(c, request, "pilot_lock_cabin");
        c.airplane = plane;
        return c;
    }

    public void Apply() => airplane?.SetCabinLocked(true);
}

[Serializable] public class PilotActivateTSAAgentCard : AirplaneCard
{
    public static PilotActivateTSAAgentCard Generate(DispatchRequest request)
    {
        var c = new PilotActivateTSAAgentCard();
        Fill(c, request, "pilot_activate_tsa");
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        c.waypointCard = WaypointCard.Generate("tsa_response", c.goalWorld);
        return c;
    }
}

[Serializable] public class PilotHoldingPatternCard : AirplaneCard
{
    public static PilotHoldingPatternCard Generate(DispatchRequest request)
    {
        var c = new PilotHoldingPatternCard();
        Fill(c, request, "pilot_holding");
        return c;
    }
}

[Serializable] public class PilotCruiseCard : AirplaneCard
{
    public static PilotCruiseCard Generate(DispatchRequest request)
    {
        var c = new PilotCruiseCard();
        Fill(c, request, "pilot_cruise");
        return c;
    }
}

[Serializable] public class PilotLandingCard : AirplaneCard
{
    public static PilotLandingCard Generate(DispatchRequest request)
    {
        var c = new PilotLandingCard();
        Fill(c, request, "pilot_landing");
        c.waypointCard = WaypointCard.Generate("runway_arrive", c.goalWorld);
        return c;
    }
}

[Serializable] public class PilotFerryCard : AirplaneCard
{
    public static PilotFerryCard Generate(DispatchRequest request)
    {
        var c = new PilotFerryCard();
        Fill(c, request, "pilot_ferry");
        return c;
    }
}

[Serializable] public class PilotWaitingQueueCard : AirplaneCard
{
    public static PilotWaitingQueueCard Generate(DispatchRequest request)
    {
        var c = new PilotWaitingQueueCard();
        Fill(c, request, "pilot_wait");
        return c;
    }
}

[Serializable] public class PilotParkCard : AirplaneCard
{
    public static PilotParkCard Generate(DispatchRequest request)
    {
        var c = new PilotParkCard();
        Fill(c, request, "pilot_park");
        return c;
    }
}

[Serializable] public class PilotGateCard : AirplaneCard
{
    public static PilotGateCard Generate(DispatchRequest request)
    {
        var c = new PilotGateCard();
        Fill(c, request, "pilot_gate");
        return c;
    }
}

[Serializable] public class PilotLandingCrewCommCard : AirplaneCard
{
    public static PilotLandingCrewCommCard Generate(DispatchRequest request)
    {
        var c = new PilotLandingCrewCommCard();
        Fill(c, request, "pilot_landing_crew_comm");
        return c;
    }
}

[Serializable]
public class PilotManeuversCard : AirplaneCard
{
    public string maneuverId = "turn_east";
    public List<string> weatherConditionTags = new List<string>();
    public string desiredOutcome = "turning due east in high turbulence";

    public static PilotManeuversCard Generate(DispatchRequest request, string maneuverId, string outcome, params string[] weather)
    {
        var c = new PilotManeuversCard();
        Fill(c, request, "pilot_maneuver");
        c.maneuverId = maneuverId ?? "turn_east";
        c.desiredOutcome = outcome ?? c.desiredOutcome;
        if (weather != null)
            c.weatherConditionTags.AddRange(weather);
        return c;
    }
}

[Serializable] public class TSAHoldingPatternCard : AirplaneCard
{
    public static TSAHoldingPatternCard Generate(DispatchRequest request)
    {
        var c = new TSAHoldingPatternCard();
        Fill(c, request, "tsa_holding");
        return c;
    }
}

[Serializable] public class TSACruisingCard : AirplaneCard
{
    public static TSACruisingCard Generate(DispatchRequest request)
    {
        var c = new TSACruisingCard();
        Fill(c, request, "tsa_cruise");
        return c;
    }
}

[Serializable] public class TSALandingCard : AirplaneCard
{
    public static TSALandingCard Generate(DispatchRequest request)
    {
        var c = new TSALandingCard();
        Fill(c, request, "tsa_landing");
        return c;
    }
}

[Serializable] public class TSARecoveryCrewCard : AirplaneCard
{
    public bool compromisedLanding;
    public bool useTelecom = true;

    public static TSARecoveryCrewCard Generate(DispatchRequest request)
    {
        var c = new TSARecoveryCrewCard();
        Fill(c, request, "tsa_recovery");
        c.compromisedLanding = request != null && (request.notes ?? "").IndexOf("compromised", StringComparison.OrdinalIgnoreCase) >= 0;
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        return c;
    }
}

[Serializable] public class TSAFerryRequestCard : AirplaneCard
{
    public static TSAFerryRequestCard Generate(DispatchRequest request)
    {
        var c = new TSAFerryRequestCard();
        Fill(c, request, "tsa_ferry");
        return c;
    }
}

[Serializable] public class TSAWaitingQueueCard : AirplaneCard
{
    public static TSAWaitingQueueCard Generate(DispatchRequest request)
    {
        var c = new TSAWaitingQueueCard();
        Fill(c, request, "tsa_wait");
        return c;
    }
}

[Serializable] public class TSAParkRequestCard : AirplaneCard
{
    public static TSAParkRequestCard Generate(DispatchRequest request)
    {
        var c = new TSAParkRequestCard();
        Fill(c, request, "tsa_park");
        return c;
    }
}

[Serializable] public class TSAGateCard : AirplaneCard
{
    public static TSAGateCard Generate(DispatchRequest request)
    {
        var c = new TSAGateCard();
        Fill(c, request, "tsa_gate");
        return c;
    }
}

[Serializable]
public class AirPortTerminalGateExtensionBridgeCard : AirplaneCard
{
    public AirportExtensionGate gate;

    public static AirPortTerminalGateExtensionBridgeCard Generate(DispatchRequest request, AirportExtensionGate gate = null)
    {
        var c = new AirPortTerminalGateExtensionBridgeCard();
        Fill(c, request, "airport_gate_bridge");
        c.gate = gate;
        c.goalTarget = gate != null ? gate.gameObject : null;
        return c;
    }

    public void Apply() => gate?.SetExtended(true, 1f);
}
