using UnityEngine;

/// <summary>Instantiate wall-brush prefabs at occupied construction-plan cells.</summary>
public static class WallBrushCellStamp
{
    public static Vector3 CellWorldCenter(HouseConstructionPlan plan, int x, int y)
    {
        if (plan == null) return Vector3.zero;
        var floor = plan.GetOrCreateFloor(plan.activeFloorIndex);
        return plan.worldOrigin + new Vector3(
            (x + 0.5f) * plan.cellWorldSize,
            HouseFloorIndex.FloorY(plan.activeFloorIndex, floor.storyHeightM),
            (y + 0.5f) * plan.cellWorldSize);
    }

    public static Transform ParentFor(HousingBuildingRagdoll house, HouseWallBrushKind kind)
    {
        if (house == null || house.slots == null) return null;
        switch (kind)
        {
            case HouseWallBrushKind.Electrical:
                return house.slots.electricalConnection;
            case HouseWallBrushKind.Hvac:
                return house.slots.hvacRoot;
            case HouseWallBrushKind.Insulation:
                return house.slots.insulationRoot;
            case HouseWallBrushKind.Plumbing:
            case HouseWallBrushKind.Drywall:
            case HouseWallBrushKind.Slats:
            case HouseWallBrushKind.Studs:
            case HouseWallBrushKind.Custom:
                return house.slots.studsRoot != null ? house.slots.studsRoot : house.slots.electricalConnection;
            default:
                return house.slots.studsRoot;
        }
    }

    public static WallBrushSpec ResolveSpec(WallBrushCatalog catalog, byte cellValue, string layerId)
    {
        if (catalog == null || cellValue == 0) return null;
        var spec = catalog.FindByPaintByte(cellValue);
        if (spec != null) return spec;
        if (cellValue < WallBrushSpec.FirstCatalogPaintByte)
        {
            var kind = KindFromModePaint(cellValue, layerId);
            if (kind.HasValue)
                return catalog.FindByKind(kind.Value);
        }
        return null;
    }

    public static int StampOccupiedCells(
        HouseConstructionPlan plan,
        int layerIndex,
        int frameIndex,
        Transform fallbackRoot,
        HousingBuildingRagdoll house)
    {
        if (plan == null || plan.layers == null || plan.layers.Count == 0)
            return 0;
        plan.EnsureDefaultLayers();
        var catalog = plan.wallBrushes;
        if (catalog != null)
            catalog.EnsureBuiltins();
        layerIndex = Mathf.Clamp(layerIndex, 0, plan.layers.Count - 1);
        var layer = plan.layers[layerIndex];
        if (layer == null || layer.frames == null || layer.frames.Count == 0)
            return 0;
        frameIndex = Mathf.Clamp(frameIndex, 0, layer.frames.Count - 1);
        var frame = layer.frames[frameIndex];
        frame.EnsureSize(plan.width, plan.height);
        int n = 0;
        for (int y = 0; y < plan.height; y++)
        for (int x = 0; x < plan.width; x++)
        {
            byte v = frame.Get(x, y, plan.width);
            if (v == 0) continue;
            var spec = ResolveSpec(catalog, v, layer.layerId);
            if (spec == null || spec.prefab == null) continue;
            var parent = ParentFor(house, spec.kind);
            if (parent == null) parent = fallbackRoot;
            var go = Object.Instantiate(spec.prefab, parent);
            go.name = spec.brushId + "_" + x + "_" + y;
            go.transform.position = CellWorldCenter(plan, x, y);
            n++;
        }
        return n;
    }

    static HouseWallBrushKind? KindFromModePaint(byte cellValue, string layerId)
    {
        int mode = cellValue - 1;
        switch ((HouseFoundationEditorMode)mode)
        {
            case HouseFoundationEditorMode.Electrical: return HouseWallBrushKind.Electrical;
            case HouseFoundationEditorMode.Hvac: return HouseWallBrushKind.Hvac;
            case HouseFoundationEditorMode.Water: return HouseWallBrushKind.Plumbing;
            case HouseFoundationEditorMode.Insulation: return HouseWallBrushKind.Insulation;
            case HouseFoundationEditorMode.Construction:
                if (layerId == "studs") return HouseWallBrushKind.Studs;
                if (layerId == "sheathing") return HouseWallBrushKind.Drywall;
                if (layerId == "insulation") return HouseWallBrushKind.Insulation;
                if (layerId == "rough_mep") return HouseWallBrushKind.Electrical;
                return HouseWallBrushKind.Drywall;
            default:
                return null;
        }
    }
}
