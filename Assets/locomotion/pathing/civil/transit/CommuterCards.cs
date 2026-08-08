using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommuterCard : TravelAgentCard
{
    public GameObject actor;
    public BusVehicleRagdoll vehicle;
    public string stopId;
    public List<string> lemmaTags = new List<string>();

    public CommuterCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "commuter";
        traversabilityTag = "commuter";
    }

    protected static void Fill(CommuterCard c, GameObject actor, BusVehicleRagdoll vehicle, string stopId, string name)
    {
        c.actor = actor;
        c.vehicle = vehicle;
        c.stopId = stopId;
        c.sectionName = name;
        c.description = name;
        c.goalTarget = vehicle != null ? vehicle.gameObject : actor;
        c.goalWorld = vehicle != null ? vehicle.transform.position : (actor != null ? actor.transform.position : Vector3.zero);
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.limits = new SectionLimits { maxForce = 55f, maxTorque = 14f, maxVelocityChange = 1.1f };
    }
}

[Serializable]
public class CommuterBoardVehicleCard : CommuterCard
{
    public static CommuterBoardVehicleCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterBoardVehicleCard();
        Fill(c, actor, vehicle, stopId, "commuter_board");
        c.lemmaTags.Add("board");
        return c;
    }
}

[Serializable]
public class CommuterWaitCard : CommuterCard
{
    public static CommuterWaitCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterWaitCard();
        Fill(c, actor, vehicle, stopId, "commuter_wait");
        c.lemmaTags.Add(CommuterLemmaPropertyKeys.CheckTime);
        c.lemmaTags.Add(CommuterLemmaPropertyKeys.Impatiently);
        return c;
    }
}

[Serializable]
public class CommuterStowLuggageCard : CommuterCard
{
    public static CommuterStowLuggageCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterStowLuggageCard();
        Fill(c, actor, vehicle, stopId, "commuter_stow_luggage");
        c.lemmaTags.Add("stow");
        return c;
    }
}

[Serializable]
public class CommuterFindSeatCard : CommuterCard
{
    public Transform seatAnchor;
    public Transform supportHandle;

    public static CommuterFindSeatCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterFindSeatCard();
        Fill(c, actor, vehicle, stopId, "commuter_find_seat");
        c.lemmaTags.Add(CommuterLemmaPropertyKeys.Scans);
        c.lemmaTags.Add(CommuterLemmaPropertyKeys.Find);
        if (vehicle != null && vehicle.seatAnchors != null && vehicle.seatAnchors.Count > 0)
        {
            c.seatAnchor = vehicle.seatAnchors[0];
            c.supportHandle = vehicle.ResolveSeatSupport(c.seatAnchor);
            if (c.seatAnchor != null)
                c.goalWorld = c.seatAnchor.position;
        }
        return c;
    }

    public void CachePelvis(Transform pelvis)
    {
        if (vehicle?.pelvisPoseCache == null || seatAnchor == null || pelvis == null) return;
        vehicle.pelvisPoseCache.Capture(pelvis, seatAnchor);
    }
}

[Serializable]
public class CommuterStopRequestCard : CommuterCard
{
    public Transform stopButton;

    public static CommuterStopRequestCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterStopRequestCard();
        Fill(c, actor, vehicle, stopId, "commuter_stop_request");
        if (vehicle != null && vehicle.stopButtons != null && vehicle.stopButtons.Count > 0)
        {
            c.stopButton = vehicle.stopButtons[0];
            c.goalWorld = c.stopButton.position;
        }
        return c;
    }

    public void Apply()
    {
        vehicle?.RequestStop(stopButton);
    }
}

[Serializable]
public class CommuterComplaintCard : CommuterCard
{
    public List<string> dialogSuggestions = new List<string>();

    public static CommuterComplaintCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterComplaintCard();
        Fill(c, actor, vehicle, stopId, "commuter_complaint");
        c.dialogSuggestions.Add("This bus is late.");
        c.dialogSuggestions.Add("Can we get an update on the schedule?");
        c.lemmaTags.Add(CommuterLemmaPropertyKeys.Impatiently);
        return c;
    }
}

[Serializable]
public class CommuterExitCard : CommuterCard
{
    public static CommuterExitCard Generate(GameObject actor, BusVehicleRagdoll vehicle, string stopId)
    {
        var c = new CommuterExitCard();
        Fill(c, actor, vehicle, stopId, "commuter_exit");
        c.lemmaTags.Add("exit");
        return c;
    }
}
