using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>TA maintenance request with repair BT hook for sanitation / road assets.</summary>
[Serializable]
public class TAMaintenanceRequest : TravelAgentCard
{
    public DispatchRequest request;
    public string repairBtActionId = "ta_maintenance_repair";
    public float integrityTarget01 = 0.85f;
    public GameObject repairTarget;

    public TAMaintenanceRequest()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "ta_maintenance_request";
        traversabilityTag = "maintenance";
    }

    public static TAMaintenanceRequest Generate(DispatchRequest request)
    {
        var c = new TAMaintenanceRequest();
        c.request = request;
        c.sectionName = "ta_maintenance_request";
        c.description = request != null ? request.kind : "ta_maintenance_request";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        if (!string.IsNullOrEmpty(request?.notes))
            c.repairBtActionId = request.notes;
        return c;
    }

    public void ApplyRepair(VehicleRagdoll vehicle)
    {
        if (vehicle != null)
            vehicle.integrity01 = Mathf.Max(vehicle.integrity01, integrityTarget01);
        SendMessageSafe(repairTarget != null ? repairTarget : vehicle != null ? vehicle.gameObject : null);
    }

    void SendMessageSafe(GameObject go)
    {
        if (go == null) return;
        go.SendMessage("OnNarrativeSchedulerAction", repairBtActionId, SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TARoadWorkDetourLeg
{
    public string routeTag = "suggested-detour";
    public Vector3 detourGoalWorld;
    public bool ignorable = true;
    public string roadSegmentId;
}

/// <summary>Road work request — repair BT + suggested-detour legs (ignorable for AI/planner).</summary>
[Serializable]
public class TARoadWorkRequest : TravelAgentCard
{
    public DispatchRequest request;
    public string repairBtActionId = "ta_road_work_repair";
    public List<TARoadWorkDetourLeg> detours = new List<TARoadWorkDetourLeg>();

    public TARoadWorkRequest()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "ta_road_work";
        traversabilityTag = "road_work";
        waypointGroup = "suggested-detour";
    }

    public static TARoadWorkRequest Generate(DispatchRequest request)
    {
        var c = new TARoadWorkRequest();
        c.request = request;
        c.sectionName = "ta_road_work_request";
        c.description = "road_work";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.detours.Add(new TARoadWorkDetourLeg
        {
            routeTag = "suggested-detour",
            detourGoalWorld = c.goalWorld,
            ignorable = ParseIgnorable(request?.notes)
        });
        return c;
    }

    static bool ParseIgnorable(string notes)
    {
        if (string.IsNullOrEmpty(notes)) return true;
        if (notes.IndexOf("ignorable=false", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (notes.IndexOf("ignorable=0", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    public void RegisterWithTrafficAvoid(TrafficWarden warden)
    {
        if (warden == null) return;
        for (int i = 0; i < detours.Count; i++)
        {
            var d = detours[i];
            if (d == null || d.ignorable) continue;
            warden.SendMessage("OnSuggestedDetour", d.detourGoalWorld, SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool ShouldPlannerIgnoreDetour(int index)
    {
        if (index < 0 || index >= detours.Count || detours[index] == null) return true;
        return detours[index].ignorable;
    }
}
