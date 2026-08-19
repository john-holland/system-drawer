using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using SdfMax;
using UnityEngine;

public sealed class HouseConstructionPlanTests
{
    [Test]
    public void HouseFloorIndex_ParseFormatY()
    {
        Assert.IsTrue(HouseFloorIndex.TryParse("first", out int first));
        Assert.AreEqual(1, first);
        Assert.IsTrue(HouseFloorIndex.TryParse("basement", out int b));
        Assert.AreEqual(0, b);
        Assert.IsTrue(HouseFloorIndex.TryParse("subbasement", out int sb));
        Assert.AreEqual(-1, sb);
        Assert.IsTrue(HouseFloorIndex.TryParse("-2", out int deep));
        Assert.AreEqual(-2, deep);
        Assert.IsTrue(HouseFloorIndex.TryParse("SB", out int sb2));
        Assert.AreEqual(-1, sb2);
        Assert.AreEqual("basement", HouseFloorIndex.Format(0));
        Assert.AreEqual("first", HouseFloorIndex.Format(1));
        Assert.AreEqual(-2.7f, HouseFloorIndex.FloorY(-1, 2.7f), 0.01f);
        Assert.AreEqual(2.7f, HouseFloorIndex.FloorY(1, 2.7f), 0.01f);
        Assert.AreEqual("basement", ElevatorButtonPanel.FormatFloorLabel(0));
    }

    [Test]
    public void ConsiderDiggingCards_FindsGenericVolume()
    {
        var go = new GameObject("soil");
        var vol = go.AddComponent<DiggableVolume>();
        vol.volumeKind = DiggableVolumeKind.Soil;
        vol.diggable = true;
        var scan = new GameObject("scan").AddComponent<ConsiderDiggingCards>();
        scan.scanRadius = 8f;
        var cards = scan.Scan(Vector3.zero);
        Assert.Greater(cards.Count, 0);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(scan.gameObject);
    }

    [Test]
    public void DiggableVolume_ApplyScoop_SubtractsSdf()
    {
        var go = new GameObject("wall");
        var vol = go.AddComponent<DiggableVolume>();
        vol.sdf = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        vol.sdf.nodes.Add(new SdfMaxNode { op = SdfMaxOp.PrimitiveLeaf, primitiveType = SdfPrimitiveType.Box, halfExtents = Vector3.one });
        vol.sdf.rootNodeIndex = 0;
        int n = vol.ApplyScoop(new DigScoopSph(), Vector3.zero, 0.3f);
        Assert.AreEqual(1, n);
        Assert.Greater(vol.sdf.nodes.Count, 1);
        Object.DestroyImmediate(vol.sdf);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ConstructionPlan_LayersAndExport()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.EnsureDefaultLayers();
        Assert.IsTrue(plan.layers.Exists(l => l.kind == HouseConstructionLayerKind.Fences));
        plan.layers.Find(l => l.kind == HouseConstructionLayerKind.Foundation).frames[0].Set(1, 1, plan.width, 1);
        var vols = plan.ExportLayerClustersToBounds4(HouseConstructionLayerKind.Foundation, 0);
        Assert.Greater(vols.Count, 0);
        var sdf = plan.BakeSoftToHard();
        Assert.IsNotNull(sdf);
        Object.DestroyImmediate(plan);
    }

    [Test]
    public void ConstructionPlan_StampEnvelopeUnionsTorus()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.envelopeLayers.Add(new HouseEnvelopeDisplacementLayer
        {
            floorIndex = 1,
            side = HouseEnvelopeSide.Front,
            height = new float[2, 2]
        });
        var sdf = plan.BakeSoftToHard();
        Assert.Greater(sdf.nodes.Count, 1);
        Assert.IsTrue(sdf.nodes.Exists(n => n.primitiveType == SdfPrimitiveType.DisplacedTorus));
        Object.DestroyImmediate(plan);
    }

    [Test]
    public void ConstructionPlan_ExportNarrativeHasBounds4()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.EnsureDefaultLayers();
        var calGo = new GameObject("cal");
        var calendar = calGo.AddComponent<NarrativeCalendarAsset>();
        int n = plan.ExportNarrativeEvents(calendar);
        Assert.Greater(n, 0);
        Assert.IsTrue(calendar.events[0].spatiotemporalVolume.HasValue);
        Object.DestroyImmediate(calGo);
        Object.DestroyImmediate(plan);
    }

    [Test]
    public void HouseLotConnectivity_DrivewayAndGarage()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 8;
        grid.height = 8;
        grid.EnsureHouseLayers();
        var street = grid.layers.Find(l => l.kind == CityPixelLayerKind.Street);
        var drive = grid.layers.Find(l => l.kind == CityPixelLayerKind.Driveway);
        var garage = grid.layers.Find(l => l.kind == CityPixelLayerKind.Garage);
        street.frames[0].Set(0, 0, grid.width, 1);
        drive.frames[0].Set(1, 0, grid.width, 1);
        garage.frames[0].Set(1, 1, grid.width, 1);
        Assert.IsTrue(HouseLotConnectivity.DrivewayHasValidOutlet(grid, 0, 1, 0));
        Assert.IsTrue(HouseLotConnectivity.GarageHasValidOutlet(grid, 0, 1, 1));
        var isolated = ScriptableObject.CreateInstance<CityPixelGrid>();
        isolated.width = 4;
        isolated.height = 4;
        isolated.EnsureHouseLayers();
        isolated.layers.Find(l => l.kind == CityPixelLayerKind.Driveway).frames[0].Set(2, 2, 4, 1);
        var errors = HouseLotConnectivity.ValidateHouseLotAdjacency(isolated, 0);
        Assert.Greater(errors.Count, 0);
        Object.DestroyImmediate(grid);
        Object.DestroyImmediate(isolated);
    }

    [Test]
    public void InsulationBatt_BakesPleats()
    {
        var r = InsulationBattBaker.BakeSlot(null, 3, true);
        Assert.AreEqual(3, r.pleatLayers);
        Assert.IsTrue(r.inactive);
        Assert.IsNotNull(r.radial);
        Assert.IsNotNull(r.diffuse);
    }

    [Test]
    public void EaveWater_PrebakeWritesFlow()
    {
        var go = new GameObject("house");
        var cache = go.AddComponent<HouseEaveWaterCache>();
        cache.catchmentM2 = 10f;
        cache.Prebake(0.02f);
        Assert.Greater(cache.gutterFlowM3s, 0f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ElectricalSpan_InactivePrebake()
    {
        var go = new GameObject("span");
        var span = go.AddComponent<HouseElectricalSpan>();
        Assert.IsTrue(span.inactivePrebake);
        span.Activate();
        Assert.IsFalse(span.inactivePrebake);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ConstructionTravelAgent_FenceOrderAndHarden()
    {
        var go = new GameObject("ta");
        var agent = go.AddComponent<HouseConstructionTravelAgent>();
        agent.steps.Clear();
        int n = agent.PlanRtsFromFenceRun(3);
        Assert.AreEqual(5, n);
        Assert.AreEqual(HouseConstructionStepKind.FencePost, agent.steps[0].kind);
        Assert.AreEqual(HouseConstructionStepKind.FencePanel, agent.steps[1].kind);
        agent.selectedStepIndex = 0;
        Assert.IsTrue(agent.HardenSelected());
        Assert.AreEqual(1f, agent.steps[0].progress01);
        var blue = agent.BlueOptimal01();
        var red = agent.RedLimit01();
        var white = agent.DashedWhiteActive01();
        Assert.AreEqual(4, blue.Length);
        Assert.AreEqual(4, red.Length);
        Assert.AreEqual(4, white.Length);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ConstructionTravelAgent_GarageAfterDriveway()
    {
        var go = new GameObject("ta");
        var agent = go.AddComponent<HouseConstructionTravelAgent>();
        agent.steps.Clear();
        Assert.AreEqual(0, agent.PlanRtsFromLotOrder(false, true));
        int n = agent.PlanRtsFromLotOrder(true, true);
        Assert.AreEqual(3, n);
        Assert.AreEqual(HouseConstructionStepKind.Driveway, agent.steps[0].kind);
        Assert.AreEqual(HouseConstructionStepKind.GaragePad, agent.steps[1].kind);
        Assert.AreEqual(HouseConstructionStepKind.GarageDoor, agent.steps[2].kind);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DoorwayPortal_RefitRestoresPrebake()
    {
        var portalGo = new GameObject("portal");
        var portal = portalGo.AddComponent<DoorwayEdgePortal>();
        portal.open01 = 1f;
        var bodyGo = new GameObject("body");
        bodyGo.transform.position = Vector3.zero;
        var rb = bodyGo.AddComponent<Rigidbody>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(bodyGo.transform, false);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        var mc = cube.AddComponent<MeshCollider>();
        mc.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        mc.convex = true;
        Mesh original = mc.sharedMesh;
        portal.RefitTransiting(bodyGo.transform);
        Assert.AreSame(original, mc.sharedMesh);
        Object.DestroyImmediate(portalGo);
        Object.DestroyImmediate(bodyGo);
    }

    [Test]
    public void RigidbodyWalk_SkipsPortalOverlay()
    {
        var root = new GameObject("root");
        root.AddComponent<Rigidbody>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(root.transform, false);
        var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = DoorwayEdgePortal.OverlayName;
        overlay.transform.SetParent(root.transform, false);
        var groups = RigidbodyPhysicsWalk.Collect(root.transform, true);
        Assert.Greater(groups.Count, 0);
        Assert.IsFalse(groups[0].meshFilters.Exists(m => m != null && m.gameObject.name == DoorwayEdgePortal.OverlayName));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SoftToHard_CompositeAndTorus()
    {
        Assert.AreEqual(1f, SdfMaxSoftToHardBaker.CompositeHeight(0.2f, 1f, 0), 0.01f);
        var asset = SdfMaxSoftToHardBaker.BakeDisplacedTorus(1f, 0.2f, new float[2, 2]);
        var graph = new SdfMaxExpressionGraph(asset, null, Matrix4x4.identity);
        float onRing = graph.SampleWorld(new Vector3(1f, 0f, 0f), 0f);
        Assert.Less(onRing, 0.25f);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void HouseRequirementSlots_IncludeConstruction()
    {
        var spec = BuildingRequirementSpec.CreateDefault("house", CivilSystemKind.House);
        Assert.IsTrue(spec.slots.Exists(s => s.slotId == "garage_door"));
        Assert.IsTrue(spec.slots.Exists(s => s.slotId == "fence"));
        Assert.IsTrue(spec.slots.Exists(s => s.slotId == "dig_site"));
        Assert.IsTrue(spec.slots.Exists(s => s.slotId == "window_sill"));
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void MoveInCard_Generates()
    {
        var card = MoveInCard.Generate(Vector3.one);
        Assert.AreEqual("move_in", card.sectionName);
        Assert.AreEqual(Vector3.one, card.goalWorld);
    }

    [Test]
    public void ConstructionPhaseCard_Progress()
    {
        var card = ConstructionPhaseCard.GenerateDefault(Vector3.zero);
        card.AddProgress(0.4f);
        Assert.IsFalse(card.IsComplete);
        card.AddProgress(0.7f);
        Assert.IsTrue(card.IsComplete);
    }

    [Test]
    public void YardRailing_BindsFloorPixelLight()
    {
        var go = new GameObject("yard");
        var yard = go.AddComponent<HouseYardFeatures>();
        var floor = new HouseConstructionFloorParams { pixelLightGridW = 4, pixelLightGridH = 6, pixelLightCellSize = 0.2f };
        yard.BindRailingLights(floor);
        Assert.AreEqual(4, yard.railingLights.gridWidth);
        Assert.AreEqual(6, yard.railingLights.gridHeight);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FoundationGridInfo_SelectBrushDescribesOccupiedCell()
    {
        var plan = ScriptableObject.CreateInstance<HouseConstructionPlan>();
        plan.width = 8;
        plan.height = 8;
        plan.cellWorldSize = 1f;
        plan.activeFloorIndex = 1;
        plan.EnsureDefaultLayers();
        int found = -1;
        for (int i = 0; i < plan.layers.Count; i++)
            if (plan.layers[i].layerId == "foundation") found = i;
        Assert.GreaterOrEqual(found, 0);
        plan.layers[found].frames[0].Set(2, 3, plan.width, 1);
        plan.layers[0].frames[0].Set(2, 3, plan.width, 1);

        string none = HouseFoundationGridInfo.Describe(
            plan, -1, -1, found, 0, HouseFoundationBrushKind.Select, HouseFoundationEditorMode.Electrical);
        StringAssert.Contains("No cell selected", none);

        string info = HouseFoundationGridInfo.Describe(
            plan, 2, 3, found, 0, HouseFoundationBrushKind.Select, HouseFoundationEditorMode.Electrical);
        StringAssert.Contains("Cell (2, 3)", info);
        StringAssert.Contains("occupied=yes", info);
        StringAssert.Contains("Brush Select", info);
        StringAssert.Contains("mode Electrical (yellow)", info);
        StringAssert.Contains("Also on:", info);
        Assert.AreEqual("orange", HouseFoundationGridInfo.ColorName(HouseFoundationPalette.BrushColor(HouseFoundationBrushKind.Erase)));
        Assert.AreEqual("rough_mep", HouseFoundationPalette.ModeLayerId(HouseFoundationEditorMode.Electrical));
        Assert.AreEqual("yellow", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Electrical))));
        Assert.AreEqual("blue", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Hvac))));
        Assert.AreEqual("red", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Insulation))));
        Assert.AreEqual("green", HouseFoundationGridInfo.ColorName(
            HouseFoundationPalette.ColorForCell(HouseFoundationPalette.PaintValue(HouseFoundationEditorMode.Yard))));
        Assert.AreEqual(HouseFoundationPalette.EmptyCell, HouseFoundationPalette.ColorForCell(0));
        Object.DestroyImmediate(plan);
    }

    [Test]
    public void VentDuct_FullBore()
    {
        var go = new GameObject("vent");
        var vent = go.AddComponent<HouseVentDuct>();
        vent.EnsureFullBore();
        Assert.IsNotNull(vent.ductCollider);
        Object.DestroyImmediate(go);
    }
}
