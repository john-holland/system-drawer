using System;
using System.Collections.Generic;
using System.Text;
using Locomotion.Narrative;
using UnityEngine;

public enum ConversationSectionType
{
    Dialog = 0,
    Travel = 1,
    Law = 2,
    Religious = 3,
    Councilor = 4,
    Chancellor = 5,
    Court = 6,
    Government = 7
}

[Serializable]
public sealed class ConversationBusStep
{
    public string displayName = "Step";
    public ConversationSectionType sectionType = ConversationSectionType.Dialog;
    public string dialogNodeId;
    public string dialogTreeSetId;
    public ConversationCard conversationCard;
    public LawConversationCard lawConversationCard;
    public ReligiousLawCard religiousLawCard;
    public LawCard lawCard;
    public CouncilorCard councilorCard;
    public ChancellorCard chancellorCard;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    public bool accordionOpen = true;
    public Vector3 predictedWorld;
    public bool hasInpaint;
    public Vector3 inpaintWorld;
    public string eventId;
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public float durationSeconds = 600f;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);

    public WardenLimitKv AddDefaultLimit()
    {
        if (limits == null) limits = new List<WardenLimitKv>();
        var row = new WardenLimitKv { key = "limit-" + (limits.Count + 1), value01 = 0.5f };
        limits.Add(row);
        return row;
    }
}

/// <summary>Telecom step-by-step dialog tree authoring. Not a transit bus.</summary>
[AddComponentMenu("Locomotion/Travel/Conversation Bus Travel Agent")]
public sealed class ConversationBusTravelAgent : TravelAgent
{
    public List<ConversationBusStep> steps = new List<ConversationBusStep>();
    public int selectedStepIndex;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    public CourtWarden courtWarden;
    public CorruptionWarden corruptionWarden;
    public ConstitutionWarden constitutionWarden;
    public RightsWarden rightsWarden;
    public JusticeWarden justiceWarden;
    public TheocraticWarden theocraticWarden;
    public LoveWarden loveWarden;
    public RomanceWarden romanceWarden;
    public ConsentWarden consentWarden;
    public NarrativeCalendarAsset calendar;
    public List<LawCard> observedLaws = new List<LawCard>();
    public List<string> observedScripture = new List<string>();

    public ConversationBusStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    void Awake()
    {
        if (courtWarden == null) courtWarden = GetComponent<CourtWarden>();
        if (corruptionWarden == null) corruptionWarden = GetComponent<CorruptionWarden>();
        if (constitutionWarden == null) constitutionWarden = GetComponent<ConstitutionWarden>();
        if (rightsWarden == null) rightsWarden = GetComponent<RightsWarden>();
        if (justiceWarden == null) justiceWarden = GetComponent<JusticeWarden>();
        if (theocraticWarden == null) theocraticWarden = GetComponent<TheocraticWarden>();
        if (loveWarden == null) loveWarden = GetComponent<LoveWarden>();
        if (romanceWarden == null) romanceWarden = GetComponent<RomanceWarden>();
        if (consentWarden == null) consentWarden = GetComponent<ConsentWarden>();
    }

    public WardenLimitKv AddDefaultLimit()
    {
        if (limits == null) limits = new List<WardenLimitKv>();
        var row = new WardenLimitKv { key = "agent-limit-" + (limits.Count + 1), value01 = 0.5f };
        limits.Add(row);
        return row;
    }

    public ConversationBusStep AddSection(ConversationSectionType type)
    {
        if (steps == null) steps = new List<ConversationBusStep>();
        var step = new ConversationBusStep
        {
            sectionType = type,
            displayName = type.ToString(),
            accordionOpen = true
        };
        switch (type)
        {
            case ConversationSectionType.Law:
                step.lawCard = ScriptableObject.CreateInstance<LawCard>();
                step.lawCard.statuteId = "draft";
                step.lawConversationCard = ScriptableObject.CreateInstance<LawConversationCard>();
                break;
            case ConversationSectionType.Religious:
                step.religiousLawCard = ScriptableObject.CreateInstance<ReligiousLawCard>();
                break;
            case ConversationSectionType.Councilor:
                step.councilorCard = new CouncilorCard();
                break;
            case ConversationSectionType.Chancellor:
                step.chancellorCard = new ChancellorCard();
                break;
            default:
                step.conversationCard = ScriptableObject.CreateInstance<ConversationCard>();
                break;
        }
        steps.Add(step);
        selectedStepIndex = steps.Count - 1;
        return step;
    }

    public static float WardenOrDefault(float? assigned) => assigned ?? 0.5f;

    public float[] DiamondActual01()
    {
        return new[]
        {
            courtWarden != null ? courtWarden.Allow01() : 0.5f,
            constitutionWarden != null ? constitutionWarden.Allow01() : 0.5f,
            rightsWarden != null ? rightsWarden.Allow01() : 0.5f,
            theocraticWarden != null ? theocraticWarden.Allow01() : 0.5f
        };
    }

    public float[] DiamondLimit01()
    {
        return new[]
        {
            0.9f,
            0.9f,
            0.9f,
            0.9f
        };
    }

    public float[] DiamondGreen01()
    {
        return new[]
        {
            loveWarden != null ? loveWarden.Allow01() : 0.5f,
            romanceWarden != null ? romanceWarden.Allow01() : 0.5f,
            consentWarden != null ? consentWarden.Allow01() : 0.5f,
            justiceWarden != null ? justiceWarden.Allow01() : 0.5f
        };
    }

    public string ComposeDialoguePrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Observed laws:");
        if (observedLaws != null)
        {
            for (int i = 0; i < observedLaws.Count; i++)
            {
                var law = observedLaws[i];
                if (law == null) continue;
                sb.Append("- ").Append(law.statuteId).Append(": ").AppendLine(law.billText);
            }
        }
        sb.AppendLine("Scripture:");
        if (observedScripture != null)
        {
            for (int i = 0; i < observedScripture.Count; i++)
                if (!string.IsNullOrEmpty(observedScripture[i]))
                    sb.Append("- ").AppendLine(observedScripture[i]);
        }
        if (theocraticWarden != null && theocraticWarden.activeScriptureRefs != null)
        {
            for (int i = 0; i < theocraticWarden.activeScriptureRefs.Count; i++)
                if (!string.IsNullOrEmpty(theocraticWarden.activeScriptureRefs[i]))
                    sb.Append("- ").AppendLine(theocraticWarden.activeScriptureRefs[i]);
        }
        var step = SelectedStep;
        if (step != null && step.councilorCard != null && step.councilorCard.laws != null)
        {
            for (int i = 0; i < step.councilorCard.laws.Count; i++)
            {
                var law = step.councilorCard.laws[i];
                if (law != null)
                    sb.Append("- councilor ").AppendLine(law.statuteId);
            }
        }
        return sb.ToString();
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
                s.eventId = $"conversation_{s.sectionType}_{i}";
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = s.eventId,
                title = s.displayName,
                startDateTime = s.startDateTime,
                durationSeconds = Mathf.RoundToInt(s.durationSeconds),
                notes = ComposeDialoguePrompt(),
                tags = new List<string> { "conversation", s.sectionType.ToString().ToLowerInvariant() }
            });
            n++;
        }
        return n;
    }
}
