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
