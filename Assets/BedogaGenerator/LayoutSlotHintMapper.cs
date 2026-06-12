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

    static SGBehaviorTreeNode.FitX ToFitX(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Left => SGBehaviorTreeNode.FitX.Left,
        LayoutFitAxis.Right => SGBehaviorTreeNode.FitX.Right,
        _ => SGBehaviorTreeNode.FitX.Center
    };

    static SGBehaviorTreeNode.FitY ToFitY(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Down => SGBehaviorTreeNode.FitY.Down,
        LayoutFitAxis.Up => SGBehaviorTreeNode.FitY.Up,
        _ => SGBehaviorTreeNode.FitY.Center
    };

    static SGBehaviorTreeNode.FitZ ToFitZ(LayoutFitAxis a) => a switch
    {
        LayoutFitAxis.Backward => SGBehaviorTreeNode.FitZ.Backward,
        LayoutFitAxis.Forward => SGBehaviorTreeNode.FitZ.Forward,
        _ => SGBehaviorTreeNode.FitZ.Center
    };
}
