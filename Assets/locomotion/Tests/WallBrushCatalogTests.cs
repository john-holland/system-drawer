using NUnit.Framework;
using UnityEngine;

public sealed class WallBrushCatalogTests
{
    [Test]
    public void EnsureBuiltins_ElectricalPlumbingDrywallSlats_TargetLayers()
    {
        var catalog = ScriptableObject.CreateInstance<WallBrushCatalog>();
        catalog.EnsureBuiltins();
        Assert.AreEqual("rough_mep", catalog.FindByKind(HouseWallBrushKind.Electrical).targetLayerId);
        Assert.AreEqual("rough_mep", catalog.FindByKind(HouseWallBrushKind.Plumbing).targetLayerId);
        Assert.AreEqual("rough_mep", catalog.FindByKind(HouseWallBrushKind.Hvac).targetLayerId);
        Assert.AreEqual("insulation", catalog.FindByKind(HouseWallBrushKind.Insulation).targetLayerId);
        Assert.AreEqual("sheathing", catalog.FindByKind(HouseWallBrushKind.Drywall).targetLayerId);
        Assert.AreEqual("studs", catalog.FindByKind(HouseWallBrushKind.Slats).targetLayerId);
        Assert.AreEqual("studs", catalog.FindByKind(HouseWallBrushKind.Studs).targetLayerId);
        Assert.GreaterOrEqual(catalog.FindByKind(HouseWallBrushKind.Electrical).paintByte, WallBrushSpec.FirstCatalogPaintByte);
        DestroyCatalog(catalog);
    }

    [Test]
    public void AddBrush_IncrementsCount_PaintByteAtLeast8()
    {
        var catalog = ScriptableObject.CreateInstance<WallBrushCatalog>();
        catalog.EnsureBuiltins();
        int n = catalog.brushes.Count;
        var spec = catalog.AddBrush(HouseWallBrushKind.Custom, "finish");
        Assert.AreEqual(n + 1, catalog.brushes.Count);
        Assert.AreEqual("finish", spec.targetLayerId);
        Assert.GreaterOrEqual(spec.paintByte, WallBrushSpec.FirstCatalogPaintByte);
        Assert.AreSame(spec, catalog.FindByPaintByte(spec.paintByte));
        DestroyCatalog(catalog);
    }

    [Test]
    public void ColorForCell_Modes1To7_Unchanged_CatalogByteUsesSpec()
    {
        Assert.AreEqual("yellow", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Electrical))));
        Assert.AreEqual("blue", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Hvac))));
        Assert.AreEqual("red", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Insulation))));
        Assert.AreEqual("green", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Yard))));
        Assert.AreEqual(HouseFoundationPalette.EmptyCell, HouseFoundationPalette.ColorForCell(0));

        var catalog = ScriptableObject.CreateInstance<WallBrushCatalog>();
        catalog.EnsureBuiltins();
        var drywall = catalog.FindByKind(HouseWallBrushKind.Drywall);
        Assert.AreEqual(drywall.color, HouseFoundationPalette.ColorForCell(drywall.paintByte, catalog));
        DestroyCatalog(catalog);
    }

    [Test]
    public void Describe_CatalogPaintByte_ShowsWallBrushName()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.width = 4;
        plan.height = 4;
        plan.EnsureDefaultLayers();
        var catalog = ScriptableObject.CreateInstance<WallBrushCatalog>();
        catalog.EnsureBuiltins();
        plan.wallBrushes = catalog;
        var elec = catalog.FindByKind(HouseWallBrushKind.Electrical);
        int layer = -1;
        for (int i = 0; i < plan.layers.Count; i++)
            if (plan.layers[i].layerId == "rough_mep") layer = i;
        plan.layers[layer].frames[0].Set(1, 1, plan.width, elec.paintByte);
        string info = HouseFoundationGridInfo.Describe(
            plan, 1, 1, layer, 0, HouseFoundationBrushKind.Select, HouseFoundationEditorMode.Electrical);
        StringAssert.Contains("Wall brush Electrical [Electrical]", info);
        Object.DestroyImmediate(plan);
        DestroyCatalog(catalog);
    }

    [Test]
    public void StampOccupiedCells_ParentsUnderElectricalSlot()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.width = 2;
        plan.height = 2;
        plan.cellWorldSize = 1f;
        plan.EnsureDefaultLayers();
        var catalog = ScriptableObject.CreateInstance<WallBrushCatalog>();
        catalog.EnsureBuiltins();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var elec = catalog.FindByKind(HouseWallBrushKind.Electrical);
        elec.prefab = prefab;
        plan.wallBrushes = catalog;
        int layer = -1;
        for (int i = 0; i < plan.layers.Count; i++)
            if (plan.layers[i].layerId == "rough_mep") layer = i;
        plan.layers[layer].frames[0].Set(0, 0, plan.width, elec.paintByte);

        var houseGo = new GameObject("house");
        var house = houseGo.AddComponent<HousingBuildingRagdoll>();
        var slot = new GameObject("electrical").transform;
        slot.SetParent(houseGo.transform, false);
        house.slots.electricalConnection = slot;
        var fallback = new GameObject("wall_brushes").transform;

        int n = WallBrushCellStamp.StampOccupiedCells(plan, layer, 0, fallback, house);
        Assert.AreEqual(1, n);
        Assert.AreEqual(1, slot.childCount);
        Assert.AreEqual(0, fallback.childCount);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(houseGo);
        Object.DestroyImmediate(fallback.gameObject);
        Object.DestroyImmediate(plan);
        DestroyCatalog(catalog);
    }

    static void DestroyCatalog(WallBrushCatalog catalog)
    {
        if (catalog == null) return;
        if (catalog.brushes != null)
        {
            for (int i = 0; i < catalog.brushes.Count; i++)
            {
                if (catalog.brushes[i] != null)
                    Object.DestroyImmediate(catalog.brushes[i]);
            }
        }
        Object.DestroyImmediate(catalog);
    }
}
