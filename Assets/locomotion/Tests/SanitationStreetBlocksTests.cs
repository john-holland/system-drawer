#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SanitationStreetBlocksTests
{
    [Test]
    public void Sanitation_SeedCompanyHierarchy_PublicAuth()
    {
        var root = new GameObject("san");
        try
        {
            var runtime = root.AddComponent<SanitationFacilityRuntime>();
            runtime.EnsureComponents();
            runtime.SeedCompanyHierarchy();
            Assert.AreEqual("public_sanitation_auth", runtime.company.companyId);
            Assert.AreEqual("government", runtime.company.parentCompanyId);
            Assert.IsNotNull(runtime.poopQuifer);
            Assert.IsNotNull(runtime.recycling);
            Assert.IsNotNull(root.GetComponent<TrashWarden>());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Sanitation_FacilitateCards_PickupAndRoadWork()
    {
        var root = new GameObject("san_bio");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var runtime = root.AddComponent<SanitationFacilityRuntime>();
            runtime.EnsureComponents();
            var bio = root.GetComponent<SanitationFacilityBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = "sanitation_pickup" });
            Assert.IsTrue(cards.Exists(c => c is SanitationPickupCard));
            var road = bio.FacilitateCards(new DispatchRequest { kind = TADispatchKinds.RoadWorkRequest });
            Assert.IsTrue(road.Exists(c => c is TARoadWorkRequest));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TrashWarden_ShouldShakeOut_Predicate()
    {
        var root = new GameObject("warden");
        var binGo = new GameObject("bin");
        try
        {
            var warden = root.AddComponent<TrashWarden>();
            var bin = binGo.AddComponent<TrashBinRuntime>();
            bin.fill01 = 0f;
            bin.contents.massKg = 0f;
            Assert.IsTrue(warden.IsBinEmpty(bin));
            Assert.IsFalse(warden.ShouldShakeOut(bin));
            bin.fill01 = 0.5f;
            bin.contents.massKg = 5f;
            Assert.IsTrue(warden.ShouldShakeOut(bin));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(binGo);
        }
    }

    [Test]
    public void GarbageTruck_Compact_IncreasesDensity()
    {
        var root = new GameObject("truck");
        try
        {
            var truck = root.AddComponent<GarbageTruckVehicleRagdoll>();
            truck.hopper.massKg = 20f;
            truck.hopper.densityKgPerM3 = 200f;
            truck.hopper.RebuildParticlesFromMass();
            float before = truck.hopper.densityKgPerM3;
            truck.SetCompactionActive(true);
            truck.hopper.TickSphCompaction(1f);
            Assert.Greater(truck.hopper.compaction01, 0f);
            Assert.GreaterOrEqual(truck.hopper.densityKgPerM3, before);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TARoadWorkRequest_IgnorableDetour()
    {
        var req = TARoadWorkRequest.Generate(new DispatchRequest
        {
            kind = TADispatchKinds.RoadWorkRequest,
            notes = "ignorable=false",
            worldTarget = Vector3.one * 3f
        });
        Assert.IsFalse(req.ShouldPlannerIgnoreDetour(0));
        var ign = TARoadWorkRequest.Generate(new DispatchRequest { kind = "road_work" });
        Assert.IsTrue(ign.ShouldPlannerIgnoreDetour(0));
    }

    [Test]
    public void SewerGraph_ConnectsBuildingsToPlant()
    {
        var plantGo = new GameObject("plant");
        var houseGo = new GameObject("house");
        var graphGo = new GameObject("graph");
        try
        {
            var quifer = plantGo.AddComponent<SanitationPoopQuifer>();
            var graph = graphGo.AddComponent<SewerGraph>();
            graph.plantSink = quifer;
            graph.AddOrGetBuildingNode(houseGo);
            graph.EnsureFullyConnectedToPlant();
            Assert.GreaterOrEqual(graph.nodes.Count, 2);
            Assert.GreaterOrEqual(graph.edges.Count, 1);
            graph.TransmitFromFixture(graph.nodes[0].nodeId, 0.2f, 0.1f);
            Assert.Greater(quifer.stages[0].fill01, 0f);
        }
        finally
        {
            Object.DestroyImmediate(plantGo);
            Object.DestroyImmediate(houseGo);
            Object.DestroyImmediate(graphGo);
        }
    }

    [Test]
    public void StreetBlocks_AutoConnectStreets_MST()
    {
        var plan = ScriptableObject.CreateInstance<StreetBlocksPlanAsset>();
        try
        {
            plan.EnsureDefaultLayers();
            plan.SetCell(2, 0, 0, new StreetBlocksCell { brush = StreetBlocksBrushKind.TwoWayStreet, structureSizeM = 6f });
            plan.SetCell(2, 2, 0, new StreetBlocksCell { brush = StreetBlocksBrushKind.TwoWayStreet, structureSizeM = 6f });
            plan.SetCell(2, 4, 1, new StreetBlocksCell { brush = StreetBlocksBrushKind.Multilane, laneCount = 4, structureSizeM = 12f });
            int n = plan.AutoConnectStreets();
            Assert.AreEqual(2, n);
            Assert.AreEqual(2, plan.streetLinks.Count);
        }
        finally
        {
            Object.DestroyImmediate(plan);
        }
    }

    [Test]
    public void HeightMapInterior_OnMove_MarksDirtyAndRemovesQuad()
    {
        var root = new GameObject("hm");
        var meshGo = new GameObject("mesh");
        try
        {
            meshGo.transform.SetParent(root.transform);
            var mr = meshGo.AddComponent<MeshRenderer>();
            var buf = root.AddComponent<HeightMapInteriorShaderBuffer>();
            buf.RegisterDescendingMesh(mr);
            buf.Prebake();
            Assert.IsFalse(buf.dirty);
            meshGo.transform.position = Vector3.one * 2f;
            buf.OnMove(mr);
            Assert.IsTrue(buf.dirty);
            Assert.Greater(buf.removedQuads.Count, 0);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void KindFromBuildingType_Sanitation()
    {
        Assert.AreEqual(CivilSystemKind.SanitationFacility,
            CivilSystemLattice.KindFromBuildingType("sanitation_facility"));
        Assert.AreEqual(CivilSystemKind.SanitationFacility,
            CivilSystemLattice.KindFromBuildingType("transfer_station"));
        Assert.AreEqual(CivilSystemKind.SanitationFacility,
            CivilSystemLattice.KindFromBuildingType("sewage_plant"));
        Assert.AreEqual(CivilSystemKind.Factory,
            CivilSystemLattice.KindFromBuildingType("widget_factory"));
        var sewageSlots = BuildingRequirementSpec.DefaultSlotsFor("sewage_plant");
        Assert.IsTrue(sewageSlots.Exists(s => s != null && s.slotId == "poop_quifer"));
    }

    [Test]
    public void BuildingRequirementSpec_Sanitation_HasPoopQuifer()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("sanitation_facility");
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "poop_quifer"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "sorting"));
    }

    [Test]
    public void FeatureBudgetIds_SanitationStack()
    {
        Assert.AreEqual("sanitation_facility", FeatureBudgetIds.SanitationFacility);
        Assert.AreEqual("garbage_truck", FeatureBudgetIds.GarbageTruck);
        Assert.AreEqual("sewer_graph", FeatureBudgetIds.SewerGraph);
        Assert.AreEqual("street_blocks", FeatureBudgetIds.StreetBlocks);
        Assert.AreEqual("factory", FeatureBudgetIds.Factory);
        Assert.AreEqual("parkour_fall", FeatureBudgetIds.ParkourFall);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.SanitationFacility));
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.Factory));
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.ParkourFall));
    }

    [Test]
    public void SanitationBootstrap_CreatesRuntime()
    {
        var root = new GameObject("stub_san");
        try
        {
            var stub = root.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.SanitationFacility;
            root.AddComponent<SanitationBootstrap>().Ensure();
            Assert.IsNotNull(root.GetComponent<SanitationFacilityRuntime>());
            Assert.IsNotNull(root.GetComponent<SanitationFacilityBioRhythm>());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
#endif
