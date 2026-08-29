using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Tracks per-agency alert/threat levels; escalates via ThreatCard + SendThought + pecking order.
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Threat Warden")]
public sealed class ThreatWarden : MonoBehaviour
{
    public const string ServiceKey = "threat.warden";

    static ThreatWarden _instance;
    public static ThreatWarden Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ThreatWarden>();
            return _instance;
        }
    }

    [Header("Context")]
    public GameObject contextOwner;
    public NarrativeScheduler narrativeScheduler;

    [Header("SendThought preferences")]
    public bool emitSendThoughtOnRaise = true;
    public bool emitDialogSuggestions = true;
    public bool scheduleNarrativeAlertEvent = true;

    [Header("Agencies")]
    public List<ThreatAgencyState> agencies = new List<ThreatAgencyState>();
    public ThreatKind lastKind = ThreatKind.Generic;

    [Header("Torture consult")]
    public ConsentWarden consentWarden;
    public RightsWarden rightsWarden;
    public JusticeWarden justiceWarden;
    public RomanceWarden romanceWarden;

    public event Action<ThreatCard> ThreatRaised;

    readonly List<RetinuePeckingEntry> _staff = new List<RetinuePeckingEntry>();

    void Awake()
    {
        _instance = this;
        if (contextOwner == null) contextOwner = gameObject;
        EnsureDefaultAgencies();
    }

    void EnsureDefaultAgencies()
    {
        if (agencies == null) agencies = new List<ThreatAgencyState>();
        if (agencies.Count > 0) return;
        agencies.Add(MakeAgency(ThreatAgencyId.Kitchen));
        agencies.Add(MakeAgency(ThreatAgencyId.BuildingMaintenance));
        agencies.Add(MakeAgency(ThreatAgencyId.FireDepartment));
        agencies.Add(MakeAgency(ThreatAgencyId.Security));
        agencies.Add(MakeAgency(ThreatAgencyId.Owner));
    }

    static ThreatAgencyState MakeAgency(string id) => new ThreatAgencyState
    {
        agencyId = id,
        alertLevel = ThreatAlertLevel.AllClear,
        threatLevel = ThreatLevel.None,
        alertScore01 = 0f,
        threatScore01 = 0f,
        lemmaTag = "all-clear"
    };

    public void SetRetinuePeckingOrder(IEnumerable<RetinuePeckingEntry> entries)
    {
        _staff.Clear();
        if (entries == null) return;
        foreach (var e in entries)
            if (e != null) _staff.Add(e);
        _staff.Sort((a, b) => a.peckingOrder.CompareTo(b.peckingOrder));
    }

    public ThreatAgencyState GetAgency(string agencyId)
    {
        for (int i = 0; i < agencies.Count; i++)
            if (string.Equals(agencies[i].agencyId, agencyId, StringComparison.OrdinalIgnoreCase))
                return agencies[i];
        return default;
    }

    /// <summary>Peak agency threat in 0–1. Missing agencies score 0.</summary>
    public float MaxThreat01()
    {
        if (agencies == null || agencies.Count == 0) return 0f;
        float m = 0f;
        for (int i = 0; i < agencies.Count; i++)
            m = Mathf.Max(m, agencies[i].threatScore01);
        return Mathf.Clamp01(m);
    }

    public void SetLevels(string agencyId, ThreatAlertLevel alert, ThreatLevel threat, float alert01 = -1f, float threat01 = -1f)
    {
        EnsureDefaultAgencies();
        for (int i = 0; i < agencies.Count; i++)
        {
            if (!string.Equals(agencies[i].agencyId, agencyId, StringComparison.OrdinalIgnoreCase))
                continue;
            var s = agencies[i];
            s.alertLevel = alert;
            s.threatLevel = threat;
            s.alertScore01 = alert01 >= 0f ? Mathf.Clamp01(alert01) : AlertToScore(alert);
            s.threatScore01 = threat01 >= 0f ? Mathf.Clamp01(threat01) : ThreatToScore(threat);
            s.lemmaTag = ThreatCard.LemmaFor(alert, ThreatKind.Generic);
            if (threat == ThreatLevel.PotentialIntruders)
                s.lemmaTag = "potential-intruders";
            if (alert == ThreatAlertLevel.UnderAttack)
                s.lemmaTag = "under-attack";
            if (alert == ThreatAlertLevel.AllClear && threat == ThreatLevel.None)
                s.lemmaTag = "all-clear";
            agencies[i] = s;
            return;
        }
        var neu = MakeAgency(agencyId);
        neu.alertLevel = alert;
        neu.threatLevel = threat;
        agencies.Add(neu);
    }

    public static bool IsTorture(
        float threat01,
        ThreatKind kind,
        float? consentAllow01,
        bool? rightsSuspended,
        float? rightsAllow01,
        JusticeWardenAction? justice,
        float? romance01)
    {
        if (kind == ThreatKind.Torture) return true;
        if (threat01 < 0.5f) return false;
        bool coerced = consentAllow01.HasValue && consentAllow01.Value < 0.34f;
        bool rightsBad = rightsSuspended == true
                         || (rightsAllow01.HasValue && rightsAllow01.Value < 0.34f);
        bool unjust = justice == JusticeWardenAction.Restrain;
        bool romanceCoerced = romance01.HasValue && romance01.Value >= 0.62f
                              && consentAllow01.HasValue && consentAllow01.Value < 0.34f;
        return coerced || rightsBad || unjust || romanceCoerced;
    }

    public bool IsTorture()
    {
        var consent = consentWarden != null ? consentWarden : GetComponent<ConsentWarden>();
        var rights = rightsWarden != null ? rightsWarden : GetComponent<RightsWarden>();
        var justice = justiceWarden != null ? justiceWarden : GetComponent<JusticeWarden>();
        var romance = romanceWarden != null ? romanceWarden : GetComponent<RomanceWarden>();
        if (justice != null)
            justice.Evaluate();
        float? consentAllow = consent != null ? consent.Allow01() : (float?)null;
        bool? rightsSusp = rights != null ? rights.Suspended() : (bool?)null;
        float? rightsAllow = rights != null ? rights.Allow01() : (float?)null;
        JusticeWardenAction? just = justice != null ? justice.lastAction : (JusticeWardenAction?)null;
        float? romanceScore = romance != null ? romance.lastScore01 : (float?)null;
        return IsTorture(MaxThreat01(), lastKind, consentAllow, rightsSusp, rightsAllow, just, romanceScore);
    }

    public ThreatCard RaiseThreat(ThreatKind kind, GameObject source = null, string agencyId = null)
    {
        lastKind = kind;
        string agency = agencyId ?? (kind == ThreatKind.Torture ? ThreatAgencyId.Security : ThreatAgencyId.Kitchen);
        ThreatAlertLevel alert = kind == ThreatKind.Fire || kind == ThreatKind.Intruder
            ? ThreatAlertLevel.UnderAttack
            : kind == ThreatKind.Torture
                ? ThreatAlertLevel.Elevated
                : ThreatAlertLevel.OnEdge;
        ThreatLevel threat = kind == ThreatKind.Intruder
            ? ThreatLevel.PotentialIntruders
            : kind == ThreatKind.Fire || kind == ThreatKind.Torture
                ? ThreatLevel.ActiveThreat
                : ThreatLevel.LocalizedHazard;
        SetLevels(agency, alert, threat);

        var card = ThreatCard.Generate(kind, contextOwner != null ? contextOwner : gameObject, source, alert);
        ConsiderThreatCards.AssignToNearestStaff(card, contextOwner != null ? contextOwner : gameObject, _staff);

        if (emitSendThoughtOnRaise)
            EmitThoughtSuggestions(card);
        if (scheduleNarrativeAlertEvent && narrativeScheduler != null)
            ScheduleAlert(card);

        ThreatRaised?.Invoke(card);
        return card;
    }

    public void ClearAgency(string agencyId)
    {
        SetLevels(agencyId, ThreatAlertLevel.AllClear, ThreatLevel.None, 0f, 0f);
    }

    void EmitThoughtSuggestions(ThreatCard card)
    {
        if (!emitDialogSuggestions || card == null) return;
        var nearest = ConsiderThreatCards.FindNearestStaff(contextOwner != null ? contextOwner.transform.position : transform.position, _staff);
        if (nearest == null || nearest.actor == null) return;

        // Prefer narrative SendThought path via reflection-safe dispatch used by SendThoughtAction
        var payload = new NarrativeAlertThoughtPayload
        {
            severity = Mathf.Clamp01(((int)card.alertLevel) / 4f),
            message = $"{card.alertLemma}: {card.threatKind}"
        };
        try
        {
            var dispatchType = Type.GetType("LocomotionThoughtDispatch, Locomotion.Runtime");
            var method = dispatchType?.GetMethod("TrySendThought", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new object[] { gameObject, nearest.actor, (int)NarrativeThoughtType.Alert, payload });
        }
        catch
        {
            // Soft-fail: card assignment still happened
        }
    }

    void ScheduleAlert(ThreatCard card)
    {
        // NarrativeScheduler integration is scene-authored; mark via component message for tests/hooks
        SendMessage("OnThreatWardenScheduleAlert", card, SendMessageOptions.DontRequireReceiver);
    }

    static float AlertToScore(ThreatAlertLevel a) => Mathf.Clamp01((int)a / 4f);
    static float ThreatToScore(ThreatLevel t) => Mathf.Clamp01((int)t / 4f);
}

[Serializable]
public sealed class RetinuePeckingEntry
{
    public string personaKey;
    public string role;
    public int peckingOrder = 100;
    public GameObject actor;
    public string agencyAffinity;
}
