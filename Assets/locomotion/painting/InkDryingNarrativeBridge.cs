using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Enqueues ink-dry-start / ink-dry-opaque on an assigned narrative calendar.</summary>
[AddComponentMenu("Locomotion/Painting/Ink Drying Narrative Bridge")]
public sealed class InkDryingNarrativeBridge : MonoBehaviour
{
    public const string DryStartId = "ink-dry-start";
    public const string DryOpaqueId = "ink-dry-opaque";

    public NarrativeCalendarAsset calendar;

    public NarrativeCalendarEvent EnqueueDryStart() =>
        EnsureEvent(DryStartId, "Ink dry start");

    public NarrativeCalendarEvent EnqueueDryOpaque() =>
        EnsureEvent(DryOpaqueId, "Ink dry opaque");

    public NarrativeCalendarEvent EnsureEvent(string id, string title)
    {
        if (calendar == null)
            calendar = GetComponent<NarrativeCalendarAsset>()
                       ?? GetComponentInParent<NarrativeCalendarAsset>();
        if (calendar == null)
            return null;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        for (int i = 0; i < calendar.events.Count; i++)
        {
            if (calendar.events[i] != null && calendar.events[i].id == id)
                return calendar.events[i];
        }
        var evt = new NarrativeCalendarEvent
        {
            id = id,
            title = title,
            tags = new List<string> { "ink", "dry" }
        };
        calendar.events.Add(evt);
        return evt;
    }
}
