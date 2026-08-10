using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TSAChecklistItem
{
    public string id;
    public string label;
    public bool required = true;
    public string narrativeActionId = AirplaneNarrativeActionIds.ChecklistStep;
    public bool completed;
}

[Serializable]
public class TSAChecklistCard : AirplaneCard
{
    public List<string> dutyChecklist = new List<string>();
    public List<TSAChecklistItem> items = new List<TSAChecklistItem>();

    public static TSAChecklistCard Generate(DispatchRequest request)
    {
        var c = new TSAChecklistCard();
        Fill(c, request, "tsa_checklist");
        c.FillDefaults();
        return c;
    }

    public void FillDefaults()
    {
        items = new List<TSAChecklistItem>
        {
            Item("engines", "Engines"),
            Item("attendants", "Cabin attendants"),
            Item("fuel", "Fuel quantity"),
            Item("safety", "Safety brief / webtop"),
            Item("heat", "Cabin heat / pressure"),
            Item("pressure", "Cabin pressure"),
            Item("landing_gear", "Landing gear"),
            Item("atc", "ATC clearance"),
            Item("gps", "GPS / instruments"),
            Item("weather", "Weather"),
            Item("ground_control", "Ground control / crew")
        };
        dutyChecklist = new List<string>();
        for (int i = 0; i < items.Count; i++)
            dutyChecklist.Add(items[i].id);
    }

    static TSAChecklistItem Item(string id, string label) =>
        new TSAChecklistItem { id = id, label = label };

    public void CompleteStep(string id, AirplaneVehicleRagdoll plane = null)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].id != id) continue;
            items[i].completed = true;
            (plane ?? airplane)?.NotifyNarrative(items[i].narrativeActionId ?? AirplaneNarrativeActionIds.ChecklistStep);
            return;
        }
    }
}

[Serializable]
public class TSATakeoffCard : AirplaneCard
{
    public string gearRaiseTopologyId = "landing_gear";
    public BehaviorTree gearRaiseOverrideBt;

    public static TSATakeoffCard Generate(DispatchRequest request, AirplaneVehicleRagdoll plane = null)
    {
        var c = new TSATakeoffCard();
        Fill(c, request, "tsa_takeoff");
        c.airplane = plane;
        if (plane != null && !string.IsNullOrEmpty(plane.landingGearOpenCloseTopologyId))
            c.gearRaiseTopologyId = plane.landingGearOpenCloseTopologyId;
        return c;
    }

    public void Apply()
    {
        airplane?.SetLandingGearDown(false);
        airplane?.NotifyNarrative(AirplaneNarrativeActionIds.Takeoff);
    }
}

[Serializable]
public class TSADisasterCard : AirplaneCard
{
    public string destinationAtcServiceId;
    public string divertReason = "potty";

    public static TSADisasterCard Generate(DispatchRequest request, AirplaneVehicleRagdoll plane = null)
    {
        var c = new TSADisasterCard();
        Fill(c, request, "tsa_disaster");
        c.airplane = plane;
        c.divertReason = request != null && !string.IsNullOrEmpty(request.notes) ? request.notes : "potty";
        return c;
    }

    public void ApplyNearestAtc(AirTrafficControlBioRhythm fromAtc = null)
    {
        Vector3 origin = airplane != null ? airplane.transform.position : goalWorld;
        var dest = AirTrafficControlBioRhythm.SelectDestinationAtc(
            fromAtc, new DispatchRequest
            {
                kind = AirportDispatchKinds.TsaDisaster,
                worldTarget = origin,
                notes = divertReason
            }, preferNearest: true);
        if (dest != null)
        {
            destinationAtcServiceId = dest.serviceId;
            goalWorld = dest.transform.position;
            airplane?.NotifyNarrative(AirplaneNarrativeActionIds.DisasterDivert);
            fromAtc?.RequestCorridorPriority(goalWorld, "disaster|" + divertReason, 0.95f);
            dest.EnqueueLanding(airplane != null ? airplane.activeFlightId : requestIdSafe(), airplane);
        }
    }

    string requestIdSafe() => request != null ? request.requestId : Guid.NewGuid().ToString("N");
}
