using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Authors a relationship plan for N arbitrary subjects. Runtime locomotion still uses base
/// <see cref="TravelAgent"/> + composite legs; this agent does not add a second path solver.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Relationship Travel Agent")]
public sealed class RelationshipTravelAgent : TravelAgent
{
    public List<RelationshipStep> steps = new List<RelationshipStep>();
    public int selectedStepIndex;
    public RelationshipRoute route;
    public LoveWarden loveWarden;
    public RomanceWarden romanceWarden;
    public ConsentWarden consentWarden;
    public TheocraticWarden theocraticWarden;
    public JusticeWarden justiceWarden;
    public RightsWarden rightsWarden;
    public ThreatWarden threatWarden;
    public CourtWarden courtWarden;
    public CorruptionWarden corruptionWarden;
    public GovernmentWarden governmentWarden;
    public LawWarden lawWarden;
    public RelationshipBioRhythm bioRhythm;
    public RelationshipRagdoll ragdoll;
    public RelationshipDialogTree dialogTree;
    public NarrativeCalendarAsset calendar;
    public List<GameObject> subjects = new List<GameObject>();
    public bool lastCivilFormAllowed = true;
    public bool lastReligiousFormAllowed = true;

    public RelationshipStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    void Awake()
    {
        if (loveWarden == null) loveWarden = GetComponent<LoveWarden>();
        if (romanceWarden == null) romanceWarden = GetComponent<RomanceWarden>();
        if (consentWarden == null) consentWarden = GetComponent<ConsentWarden>();
        if (bioRhythm == null) bioRhythm = GetComponent<RelationshipBioRhythm>();
        if (ragdoll == null) ragdoll = GetComponent<RelationshipRagdoll>();
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

    public float[] GreenExpected01() =>
        RelationshipPowerDiamond.GreenExpected01(SelectedStep, loveWarden, romanceWarden);

    public float[] RedLimit01() =>
        RelationshipPowerDiamond.RedLimit01(
            SelectedStep, consentWarden, theocraticWarden, justiceWarden, rightsWarden, threatWarden);

    public float[] WhiteActual01() =>
        RelationshipPowerDiamond.WhiteActual01(
            SelectedStep, loveWarden, romanceWarden, consentWarden, theocraticWarden,
            justiceWarden, rightsWarden, threatWarden, bioRhythm);

    /// <summary>
    /// Build composed steps that bring 2+ (or 1+) heterogeneous subjects to a relationship stage.
    /// Missing ragdoll → transform-only placement.
    /// </summary>
    public List<RelationshipStep> ResolvePath(IList<GameObject> nextSubjects, RomanceSeverity target)
    {
        if ((nextSubjects == null || nextSubjects.Count == 0) && route != null && route.subjects != null)
            nextSubjects = route.subjects;
        BindSubjects(nextSubjects);
        RomanceSeverity goal = target;
        steps = new List<RelationshipStep>();
        Vector3 first = ragdoll != null ? ragdoll.PlacementFor(0) : FirstSubjectPosition();
        Vector3 centroid = ragdoll != null ? ragdoll.Centroid() : Centroid();
        if (route != null && route.predictedWorld.sqrMagnitude > 1e-6f)
            centroid = route.predictedWorld;

        steps.Add(NewStep(RelationshipStationKind.Approach, goal, first, 0));
        steps.Add(NewStep(RelationshipStationKind.ShareSpace, goal, centroid, 0));
        var dialog = NewStep(RelationshipStationKind.DialogColumn, goal, centroid, ColumnFor(goal));
        if (dialogTree != null)
        {
            var node = dialogTree.PickInColumn(dialog.dialogColumnIndex);
            if (node != null)
                dialog.dialogNodeId = node.id;
        }
        steps.Add(dialog);

        float consent01 = consentWarden != null ? consentWarden.Evaluate() : 1f;
        bool intimacyOk = consent01 >= 0.35f && goal >= RomanceSeverity.GoingSteady;
        if (intimacyOk)
        {
            Vector3 intimate = centroid + Vector3.right * 0.4f;
            steps.Add(NewStep(RelationshipStationKind.Intimacy, goal, intimate, ColumnFor(goal)));
        }

        if (goal == RomanceSeverity.Newlywed || goal == RomanceSeverity.Married)
        {
            var form = NewStep(RelationshipStationKind.Vow, goal, centroid, ColumnFor(goal));
            if (lawWarden != null && lawWarden.lawCard != null)
            {
                form.station = RelationshipStationKind.License;
                form.lawCard = lawWarden.lawCard;
            }
            if (theocraticWarden != null && lawWarden != null)
                form.religiousLawCard = lawWarden.religiousLawCard;
            form.courtWarden = courtWarden;
            steps.Add(form);
        }

        if (route != null && route.hasInpaint && steps.Count > 0)
        {
            var last = steps[steps.Count - 1];
            last.hasInpaint = true;
            last.inpaintWorld = route.inpaintWorld.sqrMagnitude > 1e-6f ? route.inpaintWorld : last.predictedWorld;
        }

        loveWarden?.Evaluate(subjects);
        romanceWarden?.Evaluate();
        return steps;
    }

    public List<RelationshipStep> ResolvePath(IList<GameObject> nextSubjects)
    {
        RomanceSeverity goal = route != null ? route.targetSeverity : RomanceSeverity.GoingOut;
        return ResolvePath(nextSubjects, goal);
    }

    public bool CompleteSelected()
    {
        var step = SelectedStep;
        if (step == null) return false;
        romanceWarden?.SetStage(step.targetSeverity);
        loveWarden?.ApplyEffect(step.targetSeverity, consentWarden != null ? consentWarden.maxPhysicality01 : 0.5f);
        lastCivilFormAllowed = true;
        lastReligiousFormAllowed = true;
        if (step.lawCard != null || step.station == RelationshipStationKind.License)
        {
            float allow = lawWarden != null ? lawWarden.Allow01() : (step.lawCard != null ? step.lawCard.Allow01() : 1f);
            lastCivilFormAllowed = allow >= 0.33f;
        }
        if (step.religiousLawCard != null || step.station == RelationshipStationKind.Vow)
        {
            float allow = theocraticWarden != null
                ? theocraticWarden.Allow01()
                : (step.religiousLawCard != null ? step.religiousLawCard.Allow01() : 1f);
            lastReligiousFormAllowed = allow >= 0.33f;
        }
        if (step.station == RelationshipStationKind.DialogColumn)
            FireDialog(step);
        return true;
    }

    public void FireDialog(RelationshipStep step)
    {
        if (dialogTree == null || step == null) return;
        var node = dialogTree.FindNode(step.dialogNodeId) ?? dialogTree.PickInColumn(step.dialogColumnIndex);
        if (node == null) return;
        dialogTree.Fire(gameObject, node);
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
                s.eventId = $"relationship_{s.station}_{i}";
            var wrapped = NarrativeRelationshipEvent.FromStep(s, i);
            if (bioRhythm != null && bioRhythm.prebakeTags != null)
            {
                for (int t = 0; t < bioRhythm.prebakeTags.Count; t++)
                {
                    string tag = bioRhythm.prebakeTags[t];
                    if (!string.IsNullOrEmpty(tag) && !wrapped.calendarEvent.tags.Contains(tag))
                        wrapped.calendarEvent.tags.Add(tag);
                }
            }
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

    void BindSubjects(IList<GameObject> next)
    {
        if (subjects == null) subjects = new List<GameObject>();
        subjects.Clear();
        if (next != null)
        {
            for (int i = 0; i < next.Count; i++)
            {
                if (next[i] != null)
                    subjects.Add(next[i]);
            }
        }
        if (ragdoll == null)
            ragdoll = GetComponent<RelationshipRagdoll>() ?? gameObject.AddComponent<RelationshipRagdoll>();
        ragdoll.Bind(subjects);
        bioRhythm?.BindSubjects(subjects);
        if (romanceWarden != null)
            romanceWarden.subjects = new List<GameObject>(subjects);
    }

    Vector3 FirstSubjectPosition()
    {
        if (subjects != null)
        {
            for (int i = 0; i < subjects.Count; i++)
            {
                if (subjects[i] != null)
                    return subjects[i].transform.position;
            }
        }
        return transform.position;
    }

    Vector3 Centroid()
    {
        if (subjects == null || subjects.Count == 0) return transform.position;
        Vector3 acc = Vector3.zero;
        int n = 0;
        for (int i = 0; i < subjects.Count; i++)
        {
            if (subjects[i] == null) continue;
            acc += subjects[i].transform.position;
            n++;
        }
        return n > 0 ? acc / n : transform.position;
    }

    static int ColumnFor(RomanceSeverity s) => Mathf.Clamp((int)s, 0, 12);

    static RelationshipStep NewStep(
        RelationshipStationKind station,
        RomanceSeverity goal,
        Vector3 world,
        int column)
    {
        float aff = RomanceWarden.StageTo01(goal);
        return new RelationshipStep
        {
            station = station,
            targetSeverity = goal,
            predictedWorld = world,
            dialogColumnIndex = column,
            expected01 = new[] { aff, 0.55f, 0.5f, 0.55f },
            fireLimit01 = new[] { 0.9f, 0.9f, 0.9f, 0.85f }
        };
    }
}
