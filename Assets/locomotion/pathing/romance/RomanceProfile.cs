using System;
using System.Collections.Generic;
using UnityEngine;

public enum RomanceType { Intellectual, Physical, Mixed }
public enum RomanceDirection { Requited, Unrequited, Unannounced }
public enum RomanceMesh { Monogamist, NonMonogamist }
public enum RomanceFidelity { Faithful, Cheating }
public enum RomanceBaggage { None, Ex, Divorced, SeparatedLegal, SeparatedEmotional }

/// <summary>Physical intimacy ladder (baseball metaphor). Default NA = not applicable / unset.</summary>
public enum RomanceBase
{
    NA,
    First,
    Second,
    Third,
    Home
}

/// <summary>Relationship stage ladder from friend-zone through marriage and dissolution.</summary>
public enum RomanceSeverity
{
    FriendZone,
    Notion,
    Crush,
    GoingOut,
    GoingSteady,
    HotAndHeavy,
    OnAgainOffAgain,
    Newlywed,
    Married,
    OnTheRocks,
    Estranged,
    Separated,
    Divorced
}

public enum RomanceGroupDynamics
{
    CrowdLoveMaking,
    DullRampantAcceptance,
    CausalAcceptance,
    SocietalImpact
}

[Serializable]
public sealed class RomanceAdmirerLink
{
    public GameObject admirer;
    public GameObject option;
    public RomanceType type = RomanceType.Mixed;
    public RomanceDirection direction = RomanceDirection.Unannounced;
    public float strength01 = 0.4f;
    public RomanceBase baseReached = RomanceBase.NA;
}

/// <summary>Per-actor romance profile for consent, severity, fidelity, baggage.</summary>
[AddComponentMenu("Locomotion/Romance/Romance Profile")]
public sealed class RomanceProfile : MonoBehaviour
{
    public RomanceType type = RomanceType.Mixed;
    public RomanceDirection direction = RomanceDirection.Unannounced;
    public RomanceMesh mesh = RomanceMesh.Monogamist;
    public RomanceFidelity fidelity = RomanceFidelity.Faithful;
    public RomanceBaggage baggage = RomanceBaggage.None;
    public RomanceSeverity severity = RomanceSeverity.Notion;
    [Tooltip("Physical intimacy base reached with primary partner. Default NA.")]
    public RomanceBase baseReached = RomanceBase.NA;
    public List<RomanceAdmirerLink> admirers = new List<RomanceAdmirerLink>();
    public List<GameObject> consentedPartners = new List<GameObject>();
    public bool defaultConsentWithSteadyPartner = true;
    public float relationshipAgeDays;
    [Range(0f, 1f)] public float sexTalkExplicitness01 = 0.2f;

    public bool AllowsIntimacyWith(GameObject other)
    {
        if (other == null) return false;
        if (consentedPartners != null)
        {
            for (int i = 0; i < consentedPartners.Count; i++)
                if (consentedPartners[i] == other)
                    return true;
        }
        if (defaultConsentWithSteadyPartner &&
            IsActiveCoupleSeverity(severity) &&
            IsLinkedTo(other))
            return true;
        return severity >= RomanceSeverity.Crush &&
               severity < RomanceSeverity.OnTheRocks &&
               direction == RomanceDirection.Requited;
    }

    /// <summary>Going out through married (inclusive); excludes rocks / estranged / separated / divorced.</summary>
    public static bool IsActiveCoupleSeverity(RomanceSeverity s) =>
        s >= RomanceSeverity.GoingOut && s <= RomanceSeverity.Married;

    public bool IsLinkedTo(GameObject other)
    {
        if (admirers == null) return false;
        for (int i = 0; i < admirers.Count; i++)
        {
            var a = admirers[i];
            if (a == null) continue;
            if (a.admirer == other || a.option == other) return true;
        }
        return false;
    }

    public float Explicitness01()
    {
        float age = Mathf.Clamp01(relationshipAgeDays / 365f);
        float sev = SeverityExplicitnessWeight(severity);
        float bases = BaseExplicitnessWeight(baseReached);
        return Mathf.Clamp01(sexTalkExplicitness01 * 0.4f + age * 0.2f + sev * 0.2f + bases * 0.2f);
    }

    /// <summary>Resolve base for a specific partner (admirer link), else profile default.</summary>
    public RomanceBase BaseWith(GameObject other)
    {
        if (other != null && admirers != null)
        {
            for (int i = 0; i < admirers.Count; i++)
            {
                var a = admirers[i];
                if (a == null) continue;
                if (a.admirer == other || a.option == other)
                    return a.baseReached;
            }
        }
        return baseReached;
    }

    public static float BaseExplicitnessWeight(RomanceBase b)
    {
        switch (b)
        {
            case RomanceBase.First: return 0.25f;
            case RomanceBase.Second: return 0.5f;
            case RomanceBase.Third: return 0.75f;
            case RomanceBase.Home: return 1f;
            default: return 0f; // NA
        }
    }

    /// <summary>Peaks around hot-and-heavy / newlywed / married; drops after rocks.</summary>
    public static float SeverityExplicitnessWeight(RomanceSeverity s)
    {
        switch (s)
        {
            case RomanceSeverity.FriendZone: return 0.05f;
            case RomanceSeverity.Notion: return 0.15f;
            case RomanceSeverity.Crush: return 0.3f;
            case RomanceSeverity.GoingOut: return 0.45f;
            case RomanceSeverity.GoingSteady: return 0.55f;
            case RomanceSeverity.HotAndHeavy: return 0.95f;
            case RomanceSeverity.OnAgainOffAgain: return 0.7f;
            case RomanceSeverity.Newlywed: return 0.9f;
            case RomanceSeverity.Married: return 0.85f;
            case RomanceSeverity.OnTheRocks: return 0.35f;
            case RomanceSeverity.Estranged: return 0.2f;
            case RomanceSeverity.Separated: return 0.15f;
            case RomanceSeverity.Divorced: return 0.1f;
            default: return 0.25f;
        }
    }
}
