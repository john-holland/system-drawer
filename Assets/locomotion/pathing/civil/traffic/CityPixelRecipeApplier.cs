using UnityEngine;

public static class CityPixelRecipeApplier
{
    public static void Apply(CityPixelGrid grid, RoadLaneConfigAsset config, int frameIndex, int cellX, int cellY)
    {
        if (grid == null || config == null) return;
        grid.EnsureHighwayLayers();
        if (config.recipe == null || config.recipe.Count == 0)
        {
            var stamp = new CityPixelBrushStamp
            {
                frameIndex = frameIndex,
                cellX = cellX,
                cellY = cellY,
                kind = CityPixelBrushKind.RoadLanes,
                laneConfig = config
            };
            grid.SetBrushStampStacked(stamp);
            grid.PaintLayerCell(CityPixelLayerKind.Highway, frameIndex, cellX, cellY);
            return;
        }
        for (int i = 0; i < config.recipe.Count; i++)
        {
            var op = config.recipe[i];
            if (op == null) continue;
            var stamp = new CityPixelBrushStamp
            {
                frameIndex = frameIndex,
                cellX = cellX,
                cellY = cellY,
                kind = op.brushKind,
                laneConfig = config,
                diggable = op.diggable,
                signPrefab = op.prefab
            };
            if (op.raiseLayerOnOverlap)
                grid.SetBrushStampStacked(stamp);
            else
                grid.SetBrushStamp(stamp);
            grid.PaintLayerCell(op.layerKind, frameIndex, cellX, cellY);
        }
    }
}
