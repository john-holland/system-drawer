using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Kinds of nodes in a card-planning encounter tree.</summary>
public enum CardPlanNodeKind
{
    /// <summary>Covariant card partial (WrestlingCard, SitCard, …).</summary>
    Card,
    /// <summary>Behavior-tree / solver goal type.</summary>
    Goal,
    /// <summary>Narrative / encounter action (slow-time, bite, gambit, …).</summary>
    Action,
    /// <summary>Run children in order.</summary>
    Sequence,
    /// <summary>Choose among children (branch bar).</summary>
    Selector,
    /// <summary>Single selectable branch under a Selector/Sequence.</summary>
    Choice
}

/// <summary>
/// Encounter / narrative action kinds usable in card plans
/// (mirrors Narrative Tree Editor actions without a Narrative asmref).
/// </summary>
public enum CardPlanActionKind
{
    CallMethod,
    SetProperty,
    SpawnPrefab,
    RunBehaviorTree,
    SendThought,
    EnterSlowTimeGambit,
    ChooseGambitAperture,
    CommitGambitPath,
    EnterSlowTimeWrestling,
    ChooseWrestlingCard,
    CommitWrestlingCard,
    WrestlingBioRhythm,
    Bite,
    Chew,
    Swallow,
    AnimationChew,
    Sit,
    Stand,
    Carry,
    Drop,
    PathTo,
    Eat,
    CookDuty,
    PrepPlate,
    PrepServe,
    TearDown,
    WashDish
}

/// <summary>One node in a card plan tree (card, goal, action, or branch container).</summary>
[Serializable]
public class CardPlanNode
{
    public string id = Guid.NewGuid().ToString("N");
    public string label = "Node";
    public CardPlanNodeKind kind = CardPlanNodeKind.Card;
    public bool foldedOut = true;

    [Tooltip("When kind is Card.")]
    public CardPartial cardPartial = new CardPartial();

    [Tooltip("When kind is Goal.")]
    public GoalType goalType = GoalType.Wrestling;

    [Tooltip("When kind is Action.")]
    public CardPlanActionKind actionKind = CardPlanActionKind.ChooseWrestlingCard;

    [Tooltip("Children for Sequence / Selector / Choice nesting.")]
    public List<CardPlanNode> children = new List<CardPlanNode>();

    public bool IsBranchContainer =>
        kind == CardPlanNodeKind.Sequence ||
        kind == CardPlanNodeKind.Selector ||
        kind == CardPlanNodeKind.Choice;

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(label) && label != "Node")
                return label;
            switch (kind)
            {
                case CardPlanNodeKind.Card:
                    return cardPartial != null ? cardPartial.ResolvedName : "Card";
                case CardPlanNodeKind.Goal:
                    return $"Goal:{goalType}";
                case CardPlanNodeKind.Action:
                    return $"Action:{actionKind}";
                case CardPlanNodeKind.Sequence:
                    return "Sequence";
                case CardPlanNodeKind.Selector:
                    return "Selector";
                case CardPlanNodeKind.Choice:
                    return string.IsNullOrEmpty(label) ? "choice" : label;
                default:
                    return kind.ToString();
            }
        }
    }

    public static CardPlanNode NewCard(CardPartial partial)
    {
        return new CardPlanNode
        {
            kind = CardPlanNodeKind.Card,
            label = partial != null ? partial.ResolvedName : "Card",
            cardPartial = partial ?? new CardPartial()
        };
    }

    public static CardPlanNode NewGoal(GoalType goal)
    {
        return new CardPlanNode
        {
            kind = CardPlanNodeKind.Goal,
            label = $"Goal:{goal}",
            goalType = goal
        };
    }

    public static CardPlanNode NewAction(CardPlanActionKind action)
    {
        return new CardPlanNode
        {
            kind = CardPlanNodeKind.Action,
            label = $"Action:{action}",
            actionKind = action
        };
    }

    public static CardPlanNode NewTree(CardPlanNodeKind treeKind, string name = null)
    {
        if (treeKind != CardPlanNodeKind.Sequence &&
            treeKind != CardPlanNodeKind.Selector &&
            treeKind != CardPlanNodeKind.Choice)
            treeKind = CardPlanNodeKind.Sequence;
        return new CardPlanNode
        {
            kind = treeKind,
            label = name ?? treeKind.ToString()
        };
    }
}

/// <summary>
/// Saveable card-plan encounter: ordered (and branched) card partials, goals, and actions
/// for wrestling or any card-type encounter.
/// </summary>
[CreateAssetMenu(fileName = "CardPlan", menuName = "Locomotion/Card Plan", order = 40)]
public class CardPlanAsset : ScriptableObject
{
    [Tooltip("Human-readable plan name.")]
    public string planName = "Card Plan";

    [Tooltip("Default goal context for this plan.")]
    public GoalType defaultGoalType = GoalType.Wrestling;

    [Tooltip("Optional notes for designers.")]
    [TextArea(2, 6)]
    public string notes;

    [Tooltip("Root nodes of the encounter plan (reorderable; may nest Sequence/Selector/Choice).")]
    public List<CardPlanNode> roots = new List<CardPlanNode>();

    /// <summary>Depth-first materialize of all Card nodes into concrete GoodSection instances.</summary>
    public List<GoodSection> MaterializeCards()
    {
        var list = new List<GoodSection>();
        CollectCards(roots, list);
        return list;
    }

    /// <summary>Flatten Choice labels under Selectors for preview text (indent + bar).</summary>
    public string FormatBranchPreview()
    {
        var sb = new System.Text.StringBuilder();
        FormatBranchPreview(roots, 0, sb);
        return sb.ToString();
    }

    static void CollectCards(List<CardPlanNode> nodes, List<GoodSection> into)
    {
        if (nodes == null) return;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (n.kind == CardPlanNodeKind.Card && n.cardPartial != null)
            {
                var card = n.cardPartial.Materialize();
                if (card != null)
                    into.Add(card);
            }
            CollectCards(n.children, into);
        }
    }

    static void FormatBranchPreview(List<CardPlanNode> nodes, int depth, System.Text.StringBuilder sb)
    {
        if (nodes == null) return;
        string indent = new string('\t', depth);
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (n.kind == CardPlanNodeKind.Choice || n.kind == CardPlanNodeKind.Card ||
                n.kind == CardPlanNodeKind.Goal || n.kind == CardPlanNodeKind.Action)
            {
                sb.Append(indent).Append('|').Append(n.DisplayLabel).Append("\r\n");
            }
            else
            {
                sb.Append(indent).Append(n.DisplayLabel).Append("\r\n");
            }
            if (n.children != null && n.children.Count > 0)
                FormatBranchPreview(n.children, depth + 1, sb);
        }
    }
}
