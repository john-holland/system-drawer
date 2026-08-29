using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum LegalSystemStationKind
{
    File = 0,
    Hearing = 1,
    Trial = 2,
    Ruling = 3,
    UnemploymentOffice = 4,
    School = 5,
    GovContact = 6,
    Dialog = 7
}

[Serializable]
public sealed class LegalSystemStep
{
    public LegalSystemStationKind station = LegalSystemStationKind.File;
    public string displayName;
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
    public string dialogNodeId;
    public string dialogTreeSetId;
    public string eventId;
    public string enablesEventId;
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public float durationSeconds = 3600f;
    public float minSeconds = 1800f;
    public float maxSeconds = 7200f;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);
    public Transform seatAnchor;
    public AngleBase3D galleryBase;
    [Range(0f, 1f)] public float[] expected01 = { 0.5f, 0.5f, 0.5f, 0.5f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
}

/// <summary>Composed legal path: file → hearing → trial → ruling, with corruption and court gates.</summary>
[AddComponentMenu("Locomotion/Travel/Legal System Travel Agent")]
public sealed class LegalSystemTravelAgent : TravelAgent
{
    public List<LegalSystemStep> steps = new List<LegalSystemStep>();
    public int selectedStepIndex;
    public CourtWarden courtWarden;
    public CorruptionWarden corruptionWarden;
    public LegalBuilding legalBuilding;
    public NarrativeCalendarAsset calendar;
    public bool lastSubversionBlocked;

    public LegalSystemStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    void Awake()
    {
        if (courtWarden == null) courtWarden = GetComponent<CourtWarden>();
        if (corruptionWarden == null) corruptionWarden = GetComponent<CorruptionWarden>();
        if (legalBuilding == null) legalBuilding = GetComponent<LegalBuilding>();
    }

    public List<LegalSystemStep> ResolvePath()
    {
        steps = new List<LegalSystemStep>
        {
            NewStep(LegalSystemStationKind.File, "File"),
            NewStep(LegalSystemStationKind.Hearing, "Hearing"),
            NewStep(LegalSystemStationKind.Trial, "Trial"),
            NewStep(LegalSystemStationKind.Ruling, "Ruling")
        };
        BindCourtroomSeat(steps[2]);
        EnsureCivicContactSteps();
        return steps;
    }

    public void EnsureCivicContactSteps()
    {
        if (steps == null) steps = new List<LegalSystemStep>();
        EnsureStation(LegalSystemStationKind.UnemploymentOffice, "Unemployment Office");
        EnsureStation(LegalSystemStationKind.School, "School");
        EnsureStation(LegalSystemStationKind.GovContact, "Gov Contact");
    }

    void EnsureStation(LegalSystemStationKind station, string name)
    {
        for (int i = 0; i < steps.Count; i++)
            if (steps[i] != null && steps[i].station == station)
                return;
        steps.Add(NewStep(station, name));
    }

    public bool CompleteSelected()
    {
        var step = SelectedStep;
        if (step == null) return false;
        lastSubversionBlocked = false;
        if (corruptionWarden != null && !corruptionWarden.AllowsSubversion())
        {
            lastSubversionBlocked = true;
            return false;
        }
        courtWarden?.Evaluate();
        return true;
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
            if (string.IsNullOrEmpty(s.eventId))
                s.eventId = $"legal_{s.station}_{i}";
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = s.eventId,
                title = string.IsNullOrEmpty(s.displayName) ? s.station.ToString() : s.displayName,
                startDateTime = s.startDateTime,
                durationSeconds = Mathf.RoundToInt(s.durationSeconds),
                tags = new List<string> { "legal", s.station.ToString().ToLowerInvariant() }
            });
            n++;
        }
        return n;
    }

    void BindCourtroomSeat(LegalSystemStep trial)
    {
        if (trial == null) return;
        var seatBt = GetComponent<CourtroomSeatBt>() ?? GetComponentInChildren<CourtroomSeatBt>();
        if (seatBt == null) return;
        seatBt.RebuildAnchors();
        trial.galleryBase = seatBt.FirstGalleryBase();
        var anchors = seatBt.occupantAnchors;
        if (anchors != null && anchors.Length > 0)
            trial.seatAnchor = anchors[0];
    }

    static LegalSystemStep NewStep(LegalSystemStationKind station, string name)
    {
        return new LegalSystemStep
        {
            station = station,
            displayName = name,
            expected01 = new[] { 0.5f, 0.5f, 0.5f, 0.5f },
            fireLimit01 = new[] { 0.9f, 0.9f, 0.9f, 0.85f }
        };
    }
}
