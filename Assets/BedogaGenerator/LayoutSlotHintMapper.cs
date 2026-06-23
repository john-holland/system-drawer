using Locomotion.Narrative;

/// <summary>Maps inference LayoutSlotHint to BedogaGenerator placement enums.</summary>
public static class LayoutSlotHintMapper
{
    public static PlacementSlotConfig ToPlacementSlotConfig(LayoutSlotHint hint)
    {
        return new PlacementSlotConfig
        {
            fitX = ToFitX(hint.fitX),
            fitY = ToFitY(hint.fitY),
            fitZ = ToFitZ(hint.fitZ)
        };
    }

    public static SGBehaviorTreeNode.PlacementMode ToPlacementMode(LayoutSlotHint hint)
    {
        if (string.IsNullOrEmpty(hint.placementModeName))
            return SGBehaviorTreeNode.PlacementMode.In;
        if (System.Enum.TryParse(hint.placementModeName, out SGBehaviorTreeNode.PlacementMode mode))
            return mode;
        return SGBehaviorTreeNode.PlacementMode.In;
    }

    static PlacementFitX ToFitX(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Left => PlacementFitX.Left,
        LayoutFitAxis.Right => PlacementFitX.Right,
        _ => PlacementFitX.Center
    };

    static PlacementFitY ToFitY(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Down => PlacementFitY.Down,
        LayoutFitAxis.Up => PlacementFitY.Up,
        _ => PlacementFitY.Center
    };

    static PlacementFitZ ToFitZ(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Backward => PlacementFitZ.Backward,
        LayoutFitAxis.Forward => PlacementFitZ.Forward,
        _ => PlacementFitZ.Center
    };
}
