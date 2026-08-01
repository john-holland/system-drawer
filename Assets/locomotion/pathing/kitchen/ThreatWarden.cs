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

    public ThreatCard RaiseThreat(ThreatKind kind, GameObject source = null, string agencyId = null)
    {
        string agency = agencyId ?? ThreatAgencyId.Kitchen;
        ThreatAlertLevel alert = kind == ThreatKind.Fire || kind == ThreatKind.Intruder
            ? ThreatAlertLevel.UnderAttack
            : ThreatAlertLevel.OnEdge;
        ThreatLevel threat = kind == ThreatKind.Intruder
            ? ThreatLevel.PotentialIntruders
            : kind == ThreatKind.Fire
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
