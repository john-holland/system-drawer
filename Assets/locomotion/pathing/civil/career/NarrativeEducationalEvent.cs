using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum EducationalTimingMode
{
    RngRange = 0,
    Specific = 1,
    Conditional = 2
}

public enum CareerPlanEffect
{
    None = 0,
    Hire = 1,
    Promote = 2,
    Demote = 3,
    Fire = 4
}

[Serializable]
public sealed class EducationalStep
{
    public LearningStationKind station = LearningStationKind.Desk;
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public CareerPlanEffect effect = CareerPlanEffect.None;
    public string targetRoleId;
    public string eventId;
    public string enablesEventId;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);
    public float minSeconds = 60f;
    public float maxSeconds = 300f;
    public float durationSeconds = 3600f;
    public string credentialId;
    public string courseId;
    [Range(0f, 1f)] public float[] expected01 = { 0.5f, 0.5f, 0.5f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
    public Bounds4? spatiotemporalVolume;

    public float[] Expected01() => CivilianPaperDoll.Pad4(expected01, 0.5f);
    public float[] FireLimit01() => CivilianPaperDoll.Pad4(fireLimit01, 0.9f);

    public float DurationSeconds()
    {
        if (timing == EducationalTimingMode.RngRange)
            return Mathf.Max(1f, (minSeconds + maxSeconds) * 0.5f);
        return durationSeconds > 0f ? durationSeconds : 3600f;
    }
}

/// <summary>Thin wrapper around NarrativeCalendarEvent for educational prebake timing.</summary>
[Serializable]
public sealed class NarrativeEducationalEvent
{
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public CareerPlanEffect effect = CareerPlanEffect.None;
    public string targetRoleId;
    public NarrativeCalendarEvent calendarEvent = new NarrativeCalendarEvent();

    public static NarrativeEducationalEvent FromStep(EducationalStep step, int index)
    {
        var ev = new NarrativeEducationalEvent
        {
            timing = step != null ? step.timing : EducationalTimingMode.Specific,
            effect = step != null ? step.effect : CareerPlanEffect.None,
            targetRoleId = step != null ? step.targetRoleId : null,
            calendarEvent = new NarrativeCalendarEvent
            {
                id = step != null && !string.IsNullOrEmpty(step.eventId)
                    ? step.eventId
                    : $"education_{index}",
                title = step != null ? step.station.ToString() : "education",
                durationSeconds = step != null ? Mathf.RoundToInt(step.DurationSeconds()) : 3600,
                spatiotemporalVolume = step != null ? step.spatiotemporalVolume : null,
                tags = new List<string>
                {
                    "education",
                    "career",
                    step != null ? step.effect.ToString().ToLowerInvariant() : "none",
                    step != null ? step.station.ToString().ToLowerInvariant() : "desk"
                }
            }
        };
        if (step != null && step.timing == EducationalTimingMode.Specific)
            ev.calendarEvent.startDateTime = step.startDateTime;
        return ev;
    }
}
