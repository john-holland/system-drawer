using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrisonWardenAction
{
    Remuneration = 0,
    Restraint = 1
}

[CreateAssetMenu(fileName = "PrisonWardenLimits", menuName = "Locomotion/Civil/Prison Warden Limits")]
public sealed class PrisonWardenLimits : ScriptableObject
{
    public string wardenId = "warden";
    [Range(0f, 1f)] public float dialog01 = 0.6f;
    [Range(0f, 1f)] public float physical01 = 0.45f;
    [Range(0f, 1f)] public float outing01 = 0.35f;
    [Range(0f, 1f)] public float parole01 = 0.5f;

    public float LimitFor(string axis)
    {
        switch ((axis ?? "").ToLowerInvariant())
        {
            case "dialog": return dialog01;
            case "physical": return physical01;
            case "outing": return outing01;
            case "parole": return parole01;
            default: return 0.5f;
        }
    }
}

/// <summary>Scores travel-agent steps for remuneration vs restraint. Service, not hair UI.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Warden")]
public sealed class PrisonWarden : MonoBehaviour
{
    public PrisonWardenLimits limits;
    public List<PrisonWardenLimits> powerDiamondWardens = new List<PrisonWardenLimits>();
    public WrestlingCard restraintCard;
    [Range(0f, 1f)] public float lastScore01;
    public PrisonWardenAction lastRecommendation = PrisonWardenAction.Remuneration;

    void Awake()
    {
        if (limits == null)
        {
            limits = ScriptableObject.CreateInstance<PrisonWardenLimits>();
            limits.wardenId = gameObject.name;
        }
        if (powerDiamondWardens == null)
            powerDiamondWardens = new List<PrisonWardenLimits>();
        if (powerDiamondWardens.Count == 0 && limits != null)
            powerDiamondWardens.Add(limits);
        if (restraintCard == null)
            restraintCard = new WrestlingCard { sectionName = "prison_warden_restraint" };
    }

    public void Tick(DateTime utcNow, float dt) { }

    public PrisonWardenAction ScoreStep(string axis, float intensity01, bool developerInpaint)
    {
        float limit = limits != null ? limits.LimitFor(axis) : 0.5f;
        lastScore01 = Mathf.Clamp01(intensity01);
        if (developerInpaint)
        {
            lastRecommendation = lastScore01 > limit ? PrisonWardenAction.Restraint : PrisonWardenAction.Remuneration;
            return lastRecommendation;
        }
        lastRecommendation = lastScore01 > limit ? PrisonWardenAction.Restraint : PrisonWardenAction.Remuneration;
        return lastRecommendation;
    }

    public bool OverUpperLimit(string axis, float intensity01)
    {
        float limit = limits != null ? limits.LimitFor(axis) : 0.5f;
        return intensity01 > limit;
    }

    public WrestlingCard RestraintForGuard() => restraintCard;
}
