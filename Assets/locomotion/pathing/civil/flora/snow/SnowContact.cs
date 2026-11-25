using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;
using Weather;

[Serializable]
public sealed class SnowContactBox
{
    public string id;
    public float snowAmount;
    public float extent;
    public ManifoldCellData manifold;
    public Vector3 center;

    public static float ExtentFor(float skinOverlap, float firmness, float stickiness)
    {
        float overlap = Mathf.Max(0.01f, skinOverlap);
        float sticky = 1f + Mathf.Clamp01(stickiness);
        float firm = 1f - 0.75f * Mathf.Clamp01(firmness);
        return overlap * sticky * firm;
    }

    public static void SplitMass(float mass, float push, float firmness, float weave, float stickiness, out float onSurface, out float onSkin)
    {
        float give = Mathf.Clamp01(push) * Mathf.Clamp01(weave) * Mathf.Clamp01(stickiness) * (1f - Mathf.Clamp01(firmness));
        onSkin = Mathf.Max(0f, mass) * give;
        onSurface = Mathf.Max(0f, mass) - onSkin;
    }

    public static SnowContactBox FromSkin(string id, float mass, float push, float firmness, float weave, float stickiness, float overlap, ManifoldCellData cell, Vector3 center)
    {
        SplitMass(mass, push, firmness, weave, stickiness, out float stay, out float moved);
        return new SnowContactBox
        {
            id = id,
            snowAmount = stay + moved,
            extent = ExtentFor(overlap, firmness, stickiness),
            manifold = cell,
            center = center
        };
    }
}

public enum SnowClearKind
{
    Melt = 0,
    Brush = 1,
    Blow = 2
}

[Serializable]
public sealed class SnowFallEvent
{
    public string id;
    public List<string> surfaces = new List<string>();
    public List<string> resolved = new List<string>();
    public NarrativeCalendarEvent calendarEvent;

    public bool IsOpen
    {
        get
        {
            if (surfaces == null || surfaces.Count == 0)
                return false;
            return resolved == null || resolved.Count < surfaces.Count;
        }
    }

    public static SnowFallEvent Open(string id, IList<string> surfaces, NarrativeDateTime when)
    {
        var fall = new SnowFallEvent { id = string.IsNullOrEmpty(id) ? "snowfall" : id };
        if (surfaces != null)
        {
            for (int i = 0; i < surfaces.Count; i++)
            {
                if (!string.IsNullOrEmpty(surfaces[i]))
                    fall.surfaces.Add(surfaces[i]);
            }
        }
        fall.calendarEvent = new NarrativeCalendarEvent
        {
            id = fall.id,
            title = "Snowfall " + fall.id,
            startDateTime = when,
            tags = new List<string> { "snowfall", "melt", "brush", "blow" }
        };
        return fall;
    }

    public bool Resolve(string surface, SnowClearKind kind)
    {
        if (string.IsNullOrEmpty(surface) || surfaces == null || !surfaces.Contains(surface))
            return false;
        if (resolved == null)
            resolved = new List<string>();
        if (!resolved.Contains(surface))
            resolved.Add(surface);
        if (calendarEvent != null && calendarEvent.tags != null && !calendarEvent.tags.Contains(kind.ToString()))
            calendarEvent.tags.Add(kind.ToString());
        return !IsOpen;
    }
}
