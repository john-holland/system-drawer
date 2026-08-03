using System.Collections.Generic;
using UnityEngine;

/// <summary>Individual civilian duty card (ChefCard analog for people).</summary>
[System.Serializable]
public class CivilCard : GoodSection
{
    [Header("Civil")]
    public CivilianDutyKind civicDuty = CivilianDutyKind.WorkShift;
    public string personaKey;
    public string venueStableId;
    public GameObject venueOrTarget;
    public string scheduleSlotId;
    public List<string> dutyChecklist = new List<string>();
    [Tooltip("When true, content filters may hide this duty from default catalogs.")]
    public bool crassOrOptional;

    public CivilCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "civil";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "civil";
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return civicDuty.ToString();
        return $"{civicDuty}: {string.Join(",", dutyChecklist)}";
    }

    public static CivilCard Generate(
        CivilianDutyKind duty,
        string personaKey = null,
        GameObject venue = null,
        RagdollState state = null)
    {
        bool optional = duty == CivilianDutyKind.PrivateLeisure || duty == CivilianDutyKind.FakeLibraryCard;
        return new CivilCard
        {
            civicDuty = duty,
            personaKey = personaKey ?? "",
            venueOrTarget = venue,
            sectionName = $"civil_{duty}",
            description = duty.ToString(),
            isCivilGoal = true,
            crassOrOptional = optional,
            physicalPathingTag = $"civil_{duty.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 60f, maxTorque = 20f, maxVelocityChange = 1.5f },
            dutyChecklist = DefaultChecklist(duty)
        };
    }

    public static List<string> DefaultChecklist(CivilianDutyKind duty)
    {
        switch (duty)
        {
            case CivilianDutyKind.Commute:
                return new List<string> { "leave", "travel", "arrive" };
            case CivilianDutyKind.FleeThreat:
                return new List<string> { "assess", "flee", "shelter" };
            case CivilianDutyKind.WorkShift:
                return new List<string> { "clock_in", "duty", "clock_out" };
            default:
                return new List<string> { duty.ToString().ToLowerInvariant() };
        }
    }
}
