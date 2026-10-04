using System.Collections.Generic;
using UnityEngine;

/// <summary>Building / civic repair &amp; service card (ChefCard analog for civic work).</summary>
[System.Serializable]
public class CivicCard : GoodSection
{
    [Header("Civic")]
    public CivicDutyKind duty = CivicDutyKind.Repair;
    public GameObject buildingOrTarget;
    public GameObject damagedObject;
    public string buildingStableId;
    public string damagedObjectId;
    public string waypointGroup;
    public List<string> dutyChecklist = new List<string>();
    public bool requireRepairZoneOpen = true;

    public CivicCard()
    {
        isCivicGoal = true;
        physicalPathingTag = "civic";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "civic";
    }

    public bool MeetsCivicRequirements(GameObject actor, GameObject target = null)
    {
        if (actor == null) return false;
        if (duty == CivicDutyKind.Inspect)
            return buildingOrTarget != null || target != null || !string.IsNullOrEmpty(buildingStableId);
        return damagedObject != null || target != null || !string.IsNullOrEmpty(damagedObjectId)
               || buildingOrTarget != null;
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return duty.ToString();
        return $"{duty}: {string.Join(",", dutyChecklist)}";
    }

    public static CivicCard Generate(
        CivicDutyKind duty,
        GameObject buildingOrTarget,
        GameObject damagedObject = null,
        RagdollState state = null)
    {
        return new CivicCard
        {
            duty = duty,
            buildingOrTarget = buildingOrTarget,
            damagedObject = damagedObject,
            sectionName = $"civic_{duty}",
            description = duty.ToString(),
            isCivicGoal = true,
            physicalPathingTag = $"civic_{duty.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 100f, maxTorque = 30f, maxVelocityChange = 1.8f },
            dutyChecklist = DefaultChecklist(duty)
        };
    }

    public static List<string> DefaultChecklist(CivicDutyKind duty)
    {
        switch (duty)
        {
            case CivicDutyKind.Inspect:
                return new List<string> { "arrive", "survey", "report" };
            case CivicDutyKind.Repair:
                return new List<string> { "stage", "open_zone", "repair", "verify", "close_zone" };
            case CivicDutyKind.Replace:
                return new List<string> { "remove", "install", "verify" };
            case CivicDutyKind.Secure:
                return new List<string> { "cordon", "hold", "handoff" };
            case CivicDutyKind.Clean:
                return new List<string> { "clear_debris", "wipe", "dispose" };
            default:
                return new List<string> { duty.ToString().ToLowerInvariant() };
        }
    }
}
