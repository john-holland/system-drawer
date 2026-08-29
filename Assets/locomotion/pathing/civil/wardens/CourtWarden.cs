using System.Collections.Generic;
using UnityEngine;

public enum CourtWardenAction
{
    Proceed = 0,
    Recess = 1,
    Mistrial = 2
}

public enum CourtRoleKind
{
    Judge = 0,
    Bailiff = 1,
    DefenseLawyer = 2,
    ProsecutionLawyer = 3,
    JuryPool = 4,
    Audience = 5,
    Security = 6
}

/// <summary>
/// Scores trial steps. Roles use company pecking (judge, bailiff, counsel, jury, audience, security).
/// Kangaroo courts bypass Rights/Constitution. Recommendation: Proceed / Recess / Mistrial.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Court Warden")]
public sealed class CourtWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public CourtWardenAction lastAction = CourtWardenAction.Proceed;
    public CourtKind courtKind = CourtKind.American;
    public List<RetinuePeckingEntry> pecking = new List<RetinuePeckingEntry>();
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    public CompanyRegistration company;
    public PoliceCard securityPolice;
    public JusticeCard securityJustice;
    [Range(0f, 1f)] public float docketStress01 = 0.3f;
    [Range(0f, 1f)] public float audienceFill01 = 0.4f;
    public bool jurySeated;

    public bool JuryRequired => CourtKindCoeffs.JuryRequired(courtKind);
    public bool Adversarial => CourtKindCoeffs.Adversarial(courtKind);
    public float Kangaroo01 => CourtKindCoeffs.Kangaroo01(courtKind);

    void Awake()
    {
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        EnsureDefaultPecking();
    }

    public void EnsureDefaultPecking()
    {
        if (pecking != null && pecking.Count > 0)
            return;
        pecking = new List<RetinuePeckingEntry>
        {
            Role("judge", 1),
            Role("bailiff", 4),
            Role("prosecution", 8),
            Role("defense", 9),
            Role("jury", 20),
            Role("audience", 40),
            Role("security", 12)
        };
    }

    public float Allow01()
    {
        Evaluate();
        return lastScore01;
    }

    public float Evaluate()
    {
        EnsureDefaultPecking();
        float procedure = 1f;
        if (JuryRequired && !jurySeated)
            procedure *= 0.55f;
        if (!Adversarial)
            procedure = Mathf.Lerp(procedure, 0.85f, 0.35f);
        procedure = Mathf.Clamp01(procedure - docketStress01 * 0.2f + audienceFill01 * 0.05f);
        if (courtKind == CourtKind.Kangaroo)
            procedure = Mathf.Min(procedure, 0.15f);
        lastScore01 = procedure;
        if (lastScore01 >= 0.67f) lastAction = CourtWardenAction.Proceed;
        else if (lastScore01 >= 0.34f) lastAction = CourtWardenAction.Recess;
        else lastAction = CourtWardenAction.Mistrial;
        return lastScore01;
    }

    static RetinuePeckingEntry Role(string role, int order)
    {
        return new RetinuePeckingEntry { role = role, personaKey = role, peckingOrder = order };
    }
}
