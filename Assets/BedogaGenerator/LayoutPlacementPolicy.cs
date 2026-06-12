using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Applies layout frames to spatial generator placement (random if unspecified).</summary>
public static class LayoutPlacementPolicy
{
    public static int ResolvePlacementIndex(SGBehaviorTreeNode node, LayoutPlacementFrame frame, int entityIndex, int seed)
    {
        if (node == null)
            return entityIndex;

        if (node.placeSearchMode == SGBehaviorTreeNode.PlaceSearchMode.Random)
        {
            var rng = new System.Random(seed + entityIndex * 31);
            return rng.Next(0, 64);
        }

        if (frame != null && !frame.HasSpatialRelation)
        {
            var rng = new System.Random(seed + entityIndex * 17);
            return rng.Next(1, 48);
        }

        return entityIndex;
    }

    public static PlacementSlotConfig? ResolveSlotConfig(LayoutPlacementFrame frame, LayoutSpatialRelation relation)
    {
        var hint = SpatialRelationResolver.ToSlotHint(relation);
        if (hint.useRandomSlot && (frame == null || !frame.HasSpatialRelation))
            return null;
        return LayoutSlotHintMapper.ToPlacementSlotConfig(hint);
    }
}
