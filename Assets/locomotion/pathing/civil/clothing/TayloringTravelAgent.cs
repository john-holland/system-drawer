using System;
using System.Collections.Generic;
using UnityEngine;

public enum TayloringStepKind
{
    Measure = 0,
    Cut = 1,
    Fold = 2,
    Stitch = 3,
    Serge = 4,
    Press = 5,
    Stuff = 6,
    Invert = 7,
    Fit = 8,
    Tag = 9
}

[Serializable]
public sealed class TayloringStep
{
    public TayloringStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float tension01 = 0.45f;
    [Range(0f, 1f)] public float pin01 = 0.5f;
    [Range(0f, 1f)] public float bunch01 = 0.35f;
    [Range(0f, 1f)] public float join01 = 0.6f;
    [Range(0f, 1f)] public float optimalTension01 = 0.4f;
    [Range(0f, 1f)] public float optimalPin01 = 0.55f;
    [Range(0f, 1f)] public float optimalBunch01 = 0.3f;
    [Range(0f, 1f)] public float optimalJoin01 = 0.8f;
    [Range(0f, 1f)] public float limitTension01 = 0.9f;
    [Range(0f, 1f)] public float limitPin01 = 0.15f;
    [Range(0f, 1f)] public float limitBunch01 = 0.85f;
    [Range(0f, 1f)] public float limitJoin01 = 0.2f;
    public string foldCacheId;
    public Mesh bakedMesh;
    public bool bakeComplete;
}

/// <summary>Tayloring line: measure → cut → fold → stitch → serge → press → stuff → invert → fit → tag.</summary>
[AddComponentMenu("Locomotion/Travel/Tayloring Travel Agent")]
public sealed class TayloringTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Tension", "Pin", "Bunch", "Join" };

    public List<TayloringStep> steps = new List<TayloringStep>();
    public int selectedStepIndex;
    public ClothingStoreRagdoll store;
    public TayloringBioRhythm bio;
    public ClothBoltSpec bolt;
    public ThreadSpoolDriver thread;
    public ThreatWarden threatWarden;
    public string threatAgencyId = ThreatAgencyId.BuildingMaintenance;

    public TayloringStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static List<TayloringStep> DefaultPipeline()
    {
        var list = new List<TayloringStep>();
        foreach (TayloringStepKind k in Enum.GetValues(typeof(TayloringStepKind)))
        {
            list.Add(new TayloringStep
            {
                kind = k,
                sgInstanceId = k.ToString().ToLowerInvariant()
            });
        }
        return list;
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (store == null)
            store = GetComponent<ClothingStoreRagdoll>();
        if (bio == null)
            bio = GetComponent<TayloringBioRhythm>();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? ThreatWarden.Instance;
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.4f, 0.55f, 0.3f, 0.8f };
        return new[] { s.optimalTension01, s.optimalPin01, s.optimalBunch01, s.optimalJoin01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.9f, 0.15f, 0.85f, 0.2f };
        return new[] { s.limitTension01, s.limitPin01, s.limitBunch01, s.limitJoin01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.45f, 0.5f, 0.35f, 0.6f };
        return new[] { s.tension01, s.pin01, s.bunch01, s.join01 };
    }

    public float ThreatHalo01()
    {
        if (threatWarden == null) return 0f;
        return threatWarden.GetAgency(threatAgencyId).threatScore01;
    }

    public bool OverLimit()
    {
        var s = SelectedStep;
        if (s == null) return false;
        return s.tension01 > s.limitTension01 + 1e-4f
               || s.pin01 < s.limitPin01 - 1e-4f
               || s.bunch01 > s.limitBunch01 + 1e-4f
               || s.join01 < s.limitJoin01 - 1e-4f
               || ThreatHalo01() > 0.75f
               || (thread != null && thread.IsJammed);
    }

    public float BakeCompleteness01()
    {
        if (steps == null || steps.Count == 0) return 0f;
        int n = 0;
        for (int i = 0; i < steps.Count; i++)
            if (steps[i] != null && steps[i].bakeComplete) n++;
        return n / (float)steps.Count;
    }

    public bool TryCutBolt()
    {
        if (store != null && !store.CanCutBolt())
            return false;
        store?.DebitBolt(1f);
        var s = SelectedStep;
        if (s != null) s.progress01 = Mathf.Max(s.progress01, 0.35f);
        return true;
    }

    public TayloringCutCard CutCard(DispatchRequest request)
        => TayloringCutCard.Generate(request, store);
}
