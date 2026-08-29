using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

[Serializable]
public sealed class LawTravelStage
{
    public string id;
    public string displayName = "Stage";
    public LawStageKind kind = LawStageKind.Draft;
    public ConversationBusTravelAgent conversationLeg;
    public List<CongressPersonPaperDoll> congresspeople = new List<CongressPersonPaperDoll>();
    public List<SenatePersonPaperDoll> senators = new List<SenatePersonPaperDoll>();
    public List<ExecutiveCard> executives = new List<ExecutiveCard>();
    public List<ReligiousFigure> theocrats = new List<ReligiousFigure>();
    public List<MonarchPaperDoll> monarchs = new List<MonarchPaperDoll>();
    public LawCard lawCard;
    public bool accordionOpen = true;
}

/// <summary>Composes ConversationBus routes as legislative legs. GraphView columns are law stages.</summary>
[AddComponentMenu("Locomotion/Travel/Law Travel Agent")]
public sealed class LawTravelAgent : TravelAgent
{
    public List<LawTravelStage> stages = new List<LawTravelStage>();
    public int selectedStageIndex;
    public ConversationBusTravelAgent conversationBus;
    public ConstitutionWarden constitutionWarden;
    public JusticeWarden justiceWarden;
    public RightsWarden rightsWarden;
    public LoveWarden loveWarden;
    public RomanceWarden romanceWarden;
    public ConsentWarden consentWarden;
    public LawWarden lawWarden;
    public NarrativeCalendarAsset calendar;

    public LawTravelStage SelectedStage =>
        stages != null && selectedStageIndex >= 0 && selectedStageIndex < stages.Count
            ? stages[selectedStageIndex]
            : null;

    void Awake()
    {
        if (conversationBus == null) conversationBus = GetComponent<ConversationBusTravelAgent>();
        if (constitutionWarden == null) constitutionWarden = GetComponent<ConstitutionWarden>();
        if (justiceWarden == null) justiceWarden = GetComponent<JusticeWarden>();
        if (rightsWarden == null) rightsWarden = GetComponent<RightsWarden>();
        if (loveWarden == null) loveWarden = GetComponent<LoveWarden>();
        if (romanceWarden == null) romanceWarden = GetComponent<RomanceWarden>();
        if (consentWarden == null) consentWarden = GetComponent<ConsentWarden>();
        if (lawWarden == null) lawWarden = GetComponent<LawWarden>();
    }

    public LawTravelStage AddStage(LawStageKind kind)
    {
        if (stages == null) stages = new List<LawTravelStage>();
        var stage = new LawTravelStage
        {
            id = Guid.NewGuid().ToString("N"),
            kind = kind,
            displayName = kind.ToString(),
            accordionOpen = true,
            conversationLeg = conversationBus
        };
        stages.Add(stage);
        selectedStageIndex = stages.Count - 1;
        return stage;
    }

    public bool RemoveStageAt(int index)
    {
        if (stages == null || index < 0 || index >= stages.Count) return false;
        stages.RemoveAt(index);
        if (selectedStageIndex >= stages.Count)
            selectedStageIndex = stages.Count - 1;
        return true;
    }

    public float[] DiamondGreen01()
    {
        return new[]
        {
            loveWarden != null ? loveWarden.Allow01() : 0.5f,
            romanceWarden != null ? romanceWarden.Allow01() : 0.5f,
            consentWarden != null ? consentWarden.Allow01() : 0.5f,
            0.5f
        };
    }

    public float[] DiamondRed01()
    {
        return new[]
        {
            constitutionWarden != null ? constitutionWarden.Allow01() : 0.5f,
            justiceWarden != null ? justiceWarden.Allow01() : 0.5f,
            rightsWarden != null ? rightsWarden.Allow01() : 0.5f,
            0.9f
        };
    }

    public float[] DiamondWhite01()
    {
        var stage = SelectedStage;
        float law = stage != null && stage.lawCard != null
            ? stage.lawCard.Allow01()
            : (lawWarden != null ? lawWarden.Allow01() : 0.5f);
        return new[]
        {
            constitutionWarden != null ? constitutionWarden.Allow01() : 0.5f,
            justiceWarden != null ? justiceWarden.Allow01() : 0.5f,
            rightsWarden != null ? rightsWarden.Allow01() : 0.5f,
            law
        };
    }

    public int PrebakeCalendar(NarrativeCalendarAsset target)
    {
        calendar = target != null ? target : calendar;
        int n = 0;
        if (conversationBus != null)
            n += conversationBus.PrebakeCalendar(calendar);
        if (calendar == null) return n;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (s == null) continue;
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = string.IsNullOrEmpty(s.id) ? $"law_stage_{i}" : s.id,
                title = s.displayName,
                tags = new List<string> { "law", s.kind.ToString().ToLowerInvariant() }
            });
            n++;
        }
        return n;
    }
}
