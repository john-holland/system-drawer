using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tipping card structure for center of mass-based tipping analysis.
/// Contains tipping direction, viability scores, and post-tip card tree prediction.
/// </summary>
[System.Serializable]
public class TippingCard : GoodSection
{
    [Header("Tipping Properties")]
    [Tooltip("Target object to tip")]
    public GameObject targetObject;

    [Tooltip("Direction to apply force for tipping")]
    public Vector3 tipDirection;

    [Tooltip("Object's center of mass")]
    public Vector3 centerOfMass;

    [Tooltip("Angle to tip (in degrees)")]
    public float tipAngle;

    [Tooltip("Viability score (0-1, how viable this tip is)")]
    [Range(0f, 1f)]
    public float viabilityScore = 0.5f;

    [Tooltip("Torque ratio (applied torque / weight ratio)")]
    public float torqueRatio;

    [Tooltip("Point where force should be applied")]
    public Vector3 contactPoint;

    [Tooltip("Will object require stabilization after tipping?")]
    public bool requiresStabilization;

    [Header("Post-Tip Prediction")]
    [Tooltip("Card tree of possible actions after tipping")]
    public CardTree postTipTree;

    public TippingCard()
    {
        // Initialize as GoodSection
        sectionName = "tipping_card";
        description = "Tip object in specified direction";
        limits = new SectionLimits();
        impulseStack = new List<ImpulseAction>();
    }
}

/// <summary>
/// Card tree structure for post-tip prediction.
/// Represents possible card sequences after an object is tipped.
/// </summary>
[System.Serializable]
public class CardTree
{
    [Tooltip("Root card of this tree")]
    public GoodSection rootCard;

    [Tooltip("Branches from root (card -> list of follow-up cards)")]
    public Dictionary<GoodSection, List<GoodSection>> branches = new Dictionary<GoodSection, List<GoodSection>>();

    /// <summary>
    /// Add a branch to this tree.
    /// </summary>
    public void AddBranch(GoodSection card, List<GoodSection> followUpCards)
    {
        if (card != null)
        {
            if (rootCard == null)
            {
                rootCard = card;
            }

            if (followUpCards != null)
            {
                branches[card] = new List<GoodSection>(followUpCards);
            }
        }
    }

    /// <summary>
    /// Get follow-up cards for a given card.
    /// </summary>
    public List<GoodSection> GetFollowUpCards(GoodSection card)
    {
        if (card != null && branches.TryGetValue(card, out List<GoodSection> followUps))
        {
            return new List<GoodSection>(followUps);
        }
        return new List<GoodSection>();
    }
}
