/// <summary>Maps Bedoga behavior-tree nodes to shared placement slot config.</summary>
public static class PlacementSlotConfigBridge
{
    public static PlacementSlotConfig FromNode(SGBehaviorTreeNode node)
    {
        if (node == null)
            return default;
        return new PlacementSlotConfig
        {
            fitX = (PlacementFitX)(int)node.fitX,
            fitY = (PlacementFitY)(int)node.fitY,
            fitZ = (PlacementFitZ)(int)node.fitZ,
            stackDirection = (PlacementAxisDirection)(int)node.stackDirection,
            wrapDirection = (PlacementAxisDirection)(int)node.wrapDirection,
        };
    }
}
