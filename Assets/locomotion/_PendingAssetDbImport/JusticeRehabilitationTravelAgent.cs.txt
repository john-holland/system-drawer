using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum JusticeRehabStepKind
{
    Arrest = 0,
    Holding = 1,
    Trial = 2,
    Bail = 3,
    Sentencing = 4,
    Intake = 5,
    Custody = 6,
    Parole = 7,
    Rehab = 8,
    Outing = 9
}

[Serializable]
public sealed class JusticeRehabStep
{
    public JusticeRehabStepKind kind;
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
    [Range(0f, 1f)] public float intensity01 = 0.4f;
    public string axis = "dialog";
    public float durationSeconds = 3600f;
    public string timingCron;
    public Bounds4? spatiotemporalVolume;
}

/// <summary>Travel agent encapsulating arrest → parole/rehab/outing with selectable prebaked steps.</summary>
[AddComponentMenu("Locomotion/Travel/Justice Rehabilitation Travel Agent")]
public sealed class JusticeRehabilitationTravelAgent : TravelAgent
{
    public List<JusticeRehabStep> steps = new List<JusticeRehabStep>();
    public int selectedStepIndex;
    public PrisonWarden warden;
    public NarrativeCalendarAsset calendar;

    public JusticeRehabStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count ? steps[selectedStepIndex] : null;

    public static List<JusticeRehabStep> DefaultPipeline()
    {
        var list = new List<JusticeRehabStep>();
        foreach (JusticeRehabStepKind k in Enum.GetValues(typeof(JusticeRehabStepKind)))
            list.Add(new JusticeRehabStep { kind = k, axis = AxisFor(k), intensity01 = 0.4f });
        return list;
    }

    static string AxisFor(JusticeRehabStepKind k)
    {
        switch (k)
        {
            case JusticeRehabStepKind.Outing:
            case JusticeRehabStepKind.Rehab:
                return "outing";
            case JusticeRehabStepKind.Parole:
                return "parole";
            case JusticeRehabStepKind.Arrest:
            case JusticeRehabStepKind.Holding:
                return "physical";
            default:
                return "dialog";
        }
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
        if (warden == null)
            warden = GetComponent<PrisonWarden>();
    }

    public PrisonWardenAction ScoreSelected()
    {
        var step = SelectedStep;
        if (step == null || warden == null) return PrisonWardenAction.Remuneration;
        return warden.ScoreStep(step.axis, step.intensity01, step.hasInpaint);
    }

    public bool SelectedOverLimit()
    {
        var step = SelectedStep;
        if (step == null || warden == null) return false;
        return warden.OverUpperLimit(step.axis, step.intensity01);
    }

    public Vector3 PredictedPlacement()
    {
        var step = SelectedStep;
        if (step == null) return previewGoalWorld;
        return step.predictedWorld.sqrMagnitude > 1e-6f ? step.predictedWorld : previewGoalWorld;
    }

    public Vector3 InpaintPlacement()
    {
        var step = SelectedStep;
        if (step == null || !step.hasInpaint) return PredictedPlacement();
        return step.inpaintWorld;
    }

    public int PrebakeCalendar(NarrativeCalendarAsset target)
    {
        calendar = target != null ? target : calendar;
        if (calendar == null) return 0;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        int n = 0;
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = $"justice_{s.kind}_{i}",
                title = s.kind.ToString(),
                durationSeconds = Mathf.RoundToInt(s.durationSeconds),
                spatiotemporalVolume = s.spatiotemporalVolume,
                tags = new List<string> { "justice", s.kind.ToString().ToLowerInvariant() }
            });
            n++;
        }
        return n;
    }
}
