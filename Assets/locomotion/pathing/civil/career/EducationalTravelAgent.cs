using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

[Serializable]
public sealed class EducationalStep
{
    public LearningStationKind station = LearningStationKind.Desk;
    public string credentialId;
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
    [Range(0f, 1f)] public float[] expected01 = { 0.5f, 0.5f, 0.5f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public CareerPlanEffect effect = CareerPlanEffect.None;
    public string targetRoleId;
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public float minSeconds = 1800f;
    public float maxSeconds = 7200f;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);
    public float durationSeconds = 3600f;
    public string eventId;
    public string enablesEventId;
    public Bounds4? spatiotemporalVolume;

    public float[] Expected01() => CivilianPaperDoll.Pad4(expected01, 0.5f);

    public float[] FireLimit01() => CivilianPaperDoll.Pad4(fireLimit01, 0.9f);

    public float DurationSeconds()
    {
        if (timing == EducationalTimingMode.RngRange)
            return Mathf.Max(1f, (minSeconds + maxSeconds) * 0.5f);
        return Mathf.Max(1f, durationSeconds);
    }
}

/// <summary>Travel agent for educational lanes. Developer in-paint overrides predicted station placement.</summary>
[AddComponentMenu("Locomotion/Travel/Educational Travel Agent")]
public sealed class EducationalTravelAgent : TravelAgent
{
    public List<EducationalStep> steps = new List<EducationalStep>();
    public int selectedStepIndex;
    public CareerWarden warden;
    public NarrativeCalendarAsset calendar;
    public CivilianPaperDoll doll;
    public CareerRoleSpec targetRole;

    public EducationalStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count ? steps[selectedStepIndex] : null;

    void Awake()
    {
        if (warden == null)
            warden = GetComponent<CareerWarden>();
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

    public float[] BlueExpected01()
    {
        return doll != null ? doll.Expected01() : new[] { 0.55f, 0.55f, 0.55f, 0.4f };
    }

    public float[] RedFire01()
    {
        return doll != null ? doll.FireLimit01() : new[] { 0.9f, 0.9f, 0.9f, 0.85f };
    }

    public float[] WhiteStep01()
    {
        var step = SelectedStep;
        return step != null ? step.Expected01() : BlueExpected01();
    }

    /// <summary>Build steps from the role lane + missing credentials. Empty when RequireNoPretraining.</summary>
    public List<EducationalStep> ResolvePath(CivilianPaperDoll paperDoll, CareerRoleSpec role)
    {
        doll = paperDoll;
        targetRole = role;
        steps = new List<EducationalStep>();
        if (role == null || role.requireNoPretraining)
            return steps;

        if (role.lane != null && role.lane.goals != null)
        {
            for (int i = 0; i < role.lane.goals.Count; i++)
                steps.Add(FromGoal(role.lane.goals[i], CareerPlanEffect.None, role.roleId));
        }

        AppendMissing(role.certificationIds, LearningStationKind.Certification, paperDoll);
        AppendMissing(role.degreeIds, LearningStationKind.UniversityCourse, paperDoll);

        if (role.requiresManagement)
            steps.Add(NewStep(LearningStationKind.Conversation, "management", CareerPlanEffect.None, role.roleId));
        if (role.requiresHiringManager)
            steps.Add(NewStep(LearningStationKind.Phone, "hiring_manager", CareerPlanEffect.None, role.roleId));

        if (steps.Count > 0)
            steps[steps.Count - 1].effect = CareerPlanEffect.Hire;

        if (paperDoll != null)
        {
            paperDoll.educationalPlan = this;
            paperDoll.employment = CivilianEmploymentStatus.Training;
        }
        return steps;
    }

    public bool CompleteSelected()
    {
        var step = SelectedStep;
        if (step == null) return false;
        if (warden != null)
            warden.ApplyPlanEffect(doll, step.effect, step.targetRoleId);
        return true;
    }

    public int PrebakeCalendar(NarrativeCalendarAsset target)
    {
        calendar = target != null ? target : calendar;
        if (calendar == null) return 0;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        if (calendar.causalLinks == null)
            calendar.causalLinks = new List<NarrativeCausalLink>();
        int n = 0;
        string prevId = null;
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            if (string.IsNullOrEmpty(s.eventId))
                s.eventId = $"education_{s.station}_{i}";
            var wrapped = NarrativeEducationalEvent.FromStep(s, i);
            if (s.timing == EducationalTimingMode.RngRange)
            {
                float span = Mathf.Max(1f, s.maxSeconds - s.minSeconds);
                wrapped.calendarEvent.notes = $"rng {s.minSeconds:0}-{s.maxSeconds:0}s";
                wrapped.calendarEvent.durationSeconds = Mathf.RoundToInt(s.minSeconds + span * 0.5f);
            }
            calendar.events.Add(wrapped.calendarEvent);
            if (s.timing == EducationalTimingMode.Conditional && !string.IsNullOrEmpty(prevId))
            {
                calendar.causalLinks.Add(new NarrativeCausalLink
                {
                    fromEventId = prevId,
                    toEventId = wrapped.calendarEvent.id
                });
            }
            else if (!string.IsNullOrEmpty(s.enablesEventId))
            {
                calendar.causalLinks.Add(new NarrativeCausalLink
                {
                    fromEventId = wrapped.calendarEvent.id,
                    toEventId = s.enablesEventId
                });
            }
            prevId = wrapped.calendarEvent.id;
            n++;
        }
        return n;
    }

    void AppendMissing(string[] ids, LearningStationKind kind, CivilianPaperDoll paperDoll)
    {
        if (ids == null) return;
        for (int i = 0; i < ids.Length; i++)
        {
            if (string.IsNullOrEmpty(ids[i])) continue;
            if (paperDoll != null && paperDoll.HasCredential(ids[i])) continue;
            steps.Add(NewStep(kind, ids[i], CareerPlanEffect.None, targetRole != null ? targetRole.roleId : null));
        }
    }

    static EducationalStep FromGoal(EducationalLaneGoal goal, CareerPlanEffect effect, string roleId)
    {
        if (goal == null) return NewStep(LearningStationKind.Desk, null, effect, roleId);
        var step = NewStep(goal.station, goal.credentialId, effect, roleId);
        step.expected01 = CivilianPaperDoll.Pad4(goal.expected01, 0.5f);
        step.fireLimit01 = CivilianPaperDoll.Pad4(goal.fireLimit01, 0.9f);
        return step;
    }

    static EducationalStep NewStep(LearningStationKind station, string credentialId, CareerPlanEffect effect, string roleId)
    {
        return new EducationalStep
        {
            station = station,
            credentialId = credentialId,
            effect = effect,
            targetRoleId = roleId,
            expected01 = new[] { 0.5f, 0.5f, 0.5f, 0.4f },
            fireLimit01 = new[] { 0.9f, 0.9f, 0.9f, 0.85f }
        };
    }
}
