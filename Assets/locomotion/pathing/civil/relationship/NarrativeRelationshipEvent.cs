using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Thin wrapper around NarrativeCalendarEvent for relationship prebake timing.</summary>
[Serializable]
public sealed class NarrativeRelationshipEvent
{
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public RomanceSeverity targetSeverity = RomanceSeverity.Crush;
    public NarrativeCalendarEvent calendarEvent = new NarrativeCalendarEvent();

    public static NarrativeRelationshipEvent FromStep(RelationshipStep step, int index)
    {
        var ev = new NarrativeRelationshipEvent
        {
            timing = step != null ? step.timing : EducationalTimingMode.Specific,
            targetSeverity = step != null ? step.targetSeverity : RomanceSeverity.Crush,
            calendarEvent = new NarrativeCalendarEvent
            {
                id = step != null && !string.IsNullOrEmpty(step.eventId)
                    ? step.eventId
                    : $"relationship_{index}",
                title = step != null ? step.station.ToString() : "relationship",
                durationSeconds = step != null ? Mathf.RoundToInt(step.DurationSeconds()) : 1800,
                spatiotemporalVolume = step != null ? step.spatiotemporalVolume : null,
                tags = new List<string>
                {
                    "romance",
                    "love",
                    "biorhythm",
                    step != null ? step.station.ToString().ToLowerInvariant() : "approach",
                    step != null ? step.targetSeverity.ToString().ToLowerInvariant() : "crush"
                }
            }
        };
        if (step != null && step.timing == EducationalTimingMode.Specific)
            ev.calendarEvent.startDateTime = step.startDateTime;
        return ev;
    }
}
