using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sparse / preset card configuration covariant with <see cref="GoodSection"/> subtypes
/// (Unity analogue of Partial&lt;Card&gt;). The <see cref="template"/> holds typed fields;
/// unset scene refs and default scalars are treated as "not authored" when applying.
/// </summary>
[Serializable]
public class CardPartial
{
    [Tooltip("Label shown in the card planning editor.")]
    public string displayName;

    [Tooltip("Covariant card template (WrestlingCard, SitCard, GoodSection, …).")]
    [SerializeReference]
    public GoodSection template;

    public Type CardType => template != null ? template.GetType() : typeof(GoodSection);

    public string ResolvedName
    {
        get
        {
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
            if (template != null && !string.IsNullOrEmpty(template.sectionName))
                return template.sectionName;
            return CardType.Name;
        }
    }

    public GoodSection Materialize()
    {
        return CardPartialClone.Clone(template);
    }

    /// <summary>Copy authored template fields onto an existing card of a compatible type.</summary>
    public void ApplyOnto(GoodSection target)
    {
        if (target == null || template == null)
            return;
        var clone = Materialize();
        if (clone == null)
            return;
        // Prefer typed overwrite via JSON when types match.
        if (target.GetType() == clone.GetType())
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(clone), target);
            return;
        }
        CopyBaseFields(clone, target);
    }

    public static CardPartial FromCard(GoodSection card, string name = null)
    {
        return new CardPartial
        {
            displayName = name ?? card?.sectionName ?? card?.GetType().Name,
            template = CardPartialClone.Clone(card)
        };
    }

    static void CopyBaseFields(GoodSection src, GoodSection dst)
    {
        if (src == null || dst == null) return;
        dst.sectionName = src.sectionName;
        dst.description = src.description;
        dst.physicalPathingMedium = src.physicalPathingMedium;
        dst.physicalPathingTag = src.physicalPathingTag;
        dst.impulseStack = src.impulseStack != null
            ? new List<ImpulseAction>(src.impulseStack)
            : new List<ImpulseAction>();
        dst.connectedSectionNames = src.connectedSectionNames != null
            ? new List<string>(src.connectedSectionNames)
            : new List<string>();
        dst.enablesTraversability = src.enablesTraversability;
        dst.traversabilityMode = src.traversabilityMode;
        dst.traversabilityTag = src.traversabilityTag;
        dst.isThrowGoalOnly = src.isThrowGoalOnly;
        dst.needsToBeThrown = src.needsToBeThrown;
        dst.isCarry = src.isCarry;
        dst.isIsometric = src.isIsometric;
        dst.isPlaceGoal = src.isPlaceGoal;
        dst.isHitGoal = src.isHitGoal;
        dst.isWeightlift = src.isWeightlift;
        dst.isCatchGoal = src.isCatchGoal;
        dst.isShootGoal = src.isShootGoal;
        dst.isSitGoal = src.isSitGoal;
        dst.isStandOnSurfaceGoal = src.isStandOnSurfaceGoal;
        dst.isChairRotateGoal = src.isChairRotateGoal;
        dst.isChairSchoochGoal = src.isChairSchoochGoal;
        dst.isWrestlingGoal = src.isWrestlingGoal;
    }
}

/// <summary>Deep-ish clone for covariant <see cref="GoodSection"/> templates via typed instantiation + JsonUtility.</summary>
public static class CardPartialClone
{
    public static GoodSection Clone(GoodSection src)
    {
        if (src == null)
            return null;
        Type t = src.GetType();
        GoodSection clone;
        try
        {
            clone = (GoodSection)Activator.CreateInstance(t);
        }
        catch
        {
            clone = new GoodSection();
        }
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(src), clone);
        return clone;
    }
}
