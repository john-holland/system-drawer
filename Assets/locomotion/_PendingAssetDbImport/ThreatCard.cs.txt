using System.Collections.Generic;
using UnityEngine;

/// <summary>Threat report card: contacts, tools, resolution hints for ThreatWarden escalation.</summary>
[System.Serializable]
public class ThreatCard : GoodSection
{
    [Header("Threat")]
    public ThreatKind threatKind = ThreatKind.Generic;
    public ThreatAlertLevel alertLevel = ThreatAlertLevel.OnEdge;
    public ThreatLevel threatLevel = ThreatLevel.LocalizedHazard;
    public string alertLemma = "on-edge";
    public GameObject contextOwner;
    public GameObject reportedSource;
    public List<string> telecomContacts = new List<string>();
    public List<string> resolutionHints = new List<string>();
    public float requiredWaterPourLitersPerSec;
    public float requiredBucketVolumeLiters;
    public bool preferExtinguisher;
    public bool preferGrainSmother;

    public ThreatCard()
    {
        isThreatGoal = true;
        physicalPathingTag = "threat";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "threat";
    }

    public bool MeetsThreatRequirements(GameObject actor, GameObject context = null)
    {
        if (actor == null) return false;
        // Anyone in hierarchy can hold the card; tool gates checked by solvers
        return true;
    }

    public static ThreatCard Generate(
        ThreatKind kind,
        GameObject contextOwner,
        GameObject source,
        ThreatAlertLevel alert = ThreatAlertLevel.OnEdge)
    {
        var card = new ThreatCard
        {
            threatKind = kind,
            contextOwner = contextOwner,
            reportedSource = source,
            alertLevel = alert,
            sectionName = $"threat_{kind}",
            description = kind.ToString(),
            isThreatGoal = true,
            physicalPathingTag = $"threat_{kind.ToString().ToLowerInvariant()}",
            alertLemma = LemmaFor(alert, kind),
            telecomContacts = DefaultContacts(kind),
            resolutionHints = DefaultHints(kind)
        };
        if (kind == ThreatKind.Fire || kind == ThreatKind.SmokeDetectorAlarm)
        {
            card.requiredWaterPourLitersPerSec = 0.8f;
            card.requiredBucketVolumeLiters = 8f;
            card.preferExtinguisher = true;
            card.threatLevel = ThreatLevel.ActiveThreat;
        }
        else if (kind == ThreatKind.SmokeDetectorBattery)
        {
            card.threatLevel = ThreatLevel.LocalizedHazard;
            card.telecomContacts.Add(ThreatAgencyId.BuildingMaintenance);
        }
        return card;
    }

    public static string LemmaFor(ThreatAlertLevel alert, ThreatKind kind)
    {
        if (alert == ThreatAlertLevel.AllClear) return "all-clear";
        if (alert == ThreatAlertLevel.UnderAttack) return "under-attack";
        if (kind == ThreatKind.Torture) return LegalLemmaPropertyKeys.Torture;
        if (kind == ThreatKind.Intruder) return "potential-intruders";
        if (alert >= ThreatAlertLevel.OnEdge) return "on-edge";
        return "advisory";
    }

    public static List<string> DefaultContacts(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Fire:
            case ThreatKind.SmokeDetectorAlarm:
                return new List<string> { ThreatAgencyId.FireDepartment, ThreatAgencyId.BuildingMaintenance, ThreatAgencyId.Kitchen };
            case ThreatKind.SmokeDetectorBattery:
            case ThreatKind.EquipmentFault:
                return new List<string> { ThreatAgencyId.BuildingMaintenance };
            case ThreatKind.Intruder:
            case ThreatKind.Torture:
                return new List<string> { ThreatAgencyId.Security, ThreatAgencyId.Owner };
            default:
                return new List<string> { ThreatAgencyId.Owner };
        }
    }

    public static List<string> DefaultHints(ThreatKind kind)
    {
        switch (kind)
        {
            case ThreatKind.Fire:
                return new List<string> { "extinguisher", "water_pour", "grain_smother", "shut_off_heat" };
            case ThreatKind.SmokeDetectorBattery:
                return new List<string> { "replace_battery", "telecom_maintenance" };
            default:
                return new List<string> { "investigate", "telecom" };
        }
    }
}
