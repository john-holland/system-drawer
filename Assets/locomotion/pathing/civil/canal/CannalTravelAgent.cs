using System;
using System.Collections.Generic;
using UnityEngine;

public enum CannalStepKind
{
    Approach = 0,
    Lock = 1,
    Transit = 2,
    Exit = 3
}

[Serializable]
public sealed class CannalStep
{
    public CannalStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float speed01 = 0.5f;
    [Range(0f, 1f)] public float size01 = 0.5f;
    [Range(0f, 1f)] public float justice01 = 0.8f;
    [Range(0f, 1f)] public float threat01;
    [Range(0f, 1f)] public float optimalSpeed01 = 0.55f;
    [Range(0f, 1f)] public float optimalSize01 = 0.4f;
    [Range(0f, 1f)] public float optimalJustice01 = 1f;
    [Range(0f, 1f)] public float optimalThreat01;
    [Range(0f, 1f)] public float limitSpeed01 = 1f;
    [Range(0f, 1f)] public float limitSize01 = 0.85f;
    [Range(0f, 1f)] public float limitJustice01 = 0.25f;
    [Range(0f, 1f)] public float limitThreat01 = 0.75f;
}

/// <summary>Plans canal transit: approach, lock, transit, exit. Justice/Threat on the diamond.</summary>
[AddComponentMenu("Locomotion/Travel/Cannal Travel Agent")]
public sealed class CannalTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Speed", "Size", "Justice", "Threat" };

    public List<CannalStep> steps = new List<CannalStep>();
    public int selectedStepIndex;
    public CanalRibbonSpec ribbon;
    public CanalLockSpec lockSpec;
    public CanalWaterBakeField waterBake = new CanalWaterBakeField();
    public JusticeWarden justiceWarden;
    public ThreatWarden threatWarden;
    public string threatAgencyId = ThreatAgencyId.BuildingMaintenance;
    public CanalBioRhythm bio;

    public CannalStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static List<CannalStep> DefaultPipeline()
    {
        var list = new List<CannalStep>();
        foreach (CannalStepKind k in Enum.GetValues(typeof(CannalStepKind)))
            list.Add(new CannalStep { kind = k, sgInstanceId = k.ToString().ToLowerInvariant() });
        return list;
    }

    public CannalStep InsertStep(CannalStepKind kind, int afterIndex = -1)
    {
        if (steps == null)
            steps = new List<CannalStep>();
        int insertAt = afterIndex < 0 ? steps.Count : Mathf.Clamp(afterIndex + 1, 0, steps.Count);
        int same = 0;
        for (int i = 0; i < steps.Count; i++)
            if (steps[i] != null && steps[i].kind == kind)
                same++;
        var step = new CannalStep
        {
            kind = kind,
            sgInstanceId = kind.ToString().ToLowerInvariant() + (same == 0 ? "" : "_" + (same + 1))
        };
        steps.Insert(insertAt, step);
        selectedStepIndex = insertAt;
        return step;
    }

    public string DiamondInterpretation()
    {
        var s = SelectedStep;
        var blue = BlueOptimal01();
        var red = RedLimit01();
        var white = DashedWhiteActive01();
        string stepName = s != null ? s.kind.ToString() : "none";
        string status = OverLimit() ? "OVER LIMIT" : "within limits";
        return
            $"step {stepName} · {status}\n" +
            $"Speed  actual {Fmt01(white[0])}  optimal {Fmt01(blue[0])}  limit {Fmt01(red[0])}  (over if actual > limit)\n" +
            $"Size   actual {Fmt01(white[1])}  optimal {Fmt01(blue[1])}  limit {Fmt01(red[1])}  (over if actual > limit)\n" +
            $"Justice actual {Fmt01(white[2])}  optimal {Fmt01(blue[2])}  min {Fmt01(red[2])}  (over if actual < min)\n" +
            $"Threat safety {Fmt01(white[3])}  (white = 1−threat; red halo if threat > step limit {Fmt01(s != null ? s.limitThreat01 : 0.75f)})";
    }

    static string Fmt01(float v) => v.ToString("0.00");

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? ThreatWarden.Instance;
        if (justiceWarden == null)
            justiceWarden = GetComponent<JusticeWarden>();
        if (bio == null)
            bio = GetComponent<CanalBioRhythm>();
    }

    public void BakeWater()
    {
        waterBake ??= new CanalWaterBakeField();
        waterBake.Bake(ribbon);
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.55f, 0.4f, 1f, 1f };
        return new[] { s.optimalSpeed01, s.optimalSize01, s.optimalJustice01, 1f - s.optimalThreat01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 1f, 0.85f, 0.25f, 0.25f };
        return new[] { s.limitSpeed01, s.limitSize01, s.limitJustice01, 1f - s.limitThreat01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.5f, 0.8f, 1f };
        float justice = justiceWarden != null ? justiceWarden.Allow01() : s.justice01;
        float threat = ThreatHalo01();
        return new[] { s.speed01, s.size01, justice, 1f - Mathf.Max(s.threat01, threat) };
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
        return s.speed01 > s.limitSpeed01 + 1e-4f
               || s.size01 > s.limitSize01 + 1e-4f
               || (justiceWarden != null && justiceWarden.Allow01() < s.limitJustice01 - 1e-4f)
               || ThreatHalo01() > s.limitThreat01 + 1e-4f;
    }
}
