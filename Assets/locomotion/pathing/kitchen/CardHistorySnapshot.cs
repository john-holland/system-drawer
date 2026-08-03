using System;
using UnityEngine;

/// <summary>Plain serializable copy of a physics card — never holds live GoodSection refs.</summary>
[Serializable]
public sealed class CardHistorySnapshot
{
    public string typeName;
    public string displayName;
    public string physicalPathingTag;
    public string actorOrSolverId;
    public long unixMs;
    public bool isChefGoal;
    public bool isCombatGoal;
    public bool isLoveMakingGoal;
    public bool isThreatGoal;
    public bool isJusticeGoal;
    public bool isCivicGoal;
    public bool isCivilGoal;
    public bool isTravelAgentGoal;
    public bool isEatGoal;
    public string dutyOrActivitySummary;
    public string eventKind; // added | removed | pool

    public static CardHistorySnapshot FromCard(GoodSection card, string solverId = null, string eventKind = "pool")
    {
        var snap = new CardHistorySnapshot
        {
            unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            actorOrSolverId = solverId ?? "",
            eventKind = eventKind ?? "pool"
        };
        if (card == null)
        {
            snap.typeName = "null";
            snap.displayName = "(null)";
            return snap;
        }
        snap.typeName = card.GetType().Name;
        snap.displayName = !string.IsNullOrEmpty(card.sectionName) ? card.sectionName : snap.typeName;
        snap.physicalPathingTag = card.physicalPathingTag ?? "";
        snap.isChefGoal = card.isChefGoal || card is ChefCard;
        snap.isCombatGoal = card.isCombatGoal || card is CombatCard;
        snap.isLoveMakingGoal = card.isLoveMakingGoal || card is LoveCard;
        snap.isThreatGoal = card.isThreatGoal || card is ThreatCard;
        snap.isJusticeGoal = card.isJusticeGoal || card is JusticeCard;
        snap.isCivicGoal = card.isCivicGoal || card is CivicCard;
        snap.isCivilGoal = card.isCivilGoal || card is CivilCard;
        snap.isTravelAgentGoal = card.isTravelAgentGoal || card is TravelAgentCard;
        snap.isEatGoal = card.isEatGoal;
        if (card is ChefCard chef)
            snap.dutyOrActivitySummary = chef.DutySummary();
        else if (card is ThreatCard threat)
            snap.dutyOrActivitySummary = $"{threat.threatKind}:{threat.alertLemma}";
        else if (card is JusticeCard justice)
            snap.dutyOrActivitySummary = justice.justiceAction.ToString();
        else if (card is CivicCard civic)
            snap.dutyOrActivitySummary = civic.DutySummary();
        else if (card is CivilCard civil)
            snap.dutyOrActivitySummary = civil.DutySummary();
        else
            snap.dutyOrActivitySummary = card.description ?? "";
        return snap;
    }
}
