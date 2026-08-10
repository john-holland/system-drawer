#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GasStationParkTests
{
    [Test]
    public void GasStation_SeedCompanyHierarchy_PublicFuelAuth()
    {
        var root = new GameObject("gas");
        try
        {
            var runtime = root.AddComponent<GasStationRuntime>();
            runtime.EnsureComponents();
            runtime.SeedCompanyHierarchy();
            Assert.AreEqual("public_fuel_auth", runtime.company.companyId);
            Assert.AreEqual("government", runtime.company.parentCompanyId);
            Assert.AreEqual("convenience_store", runtime.store.storeType);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GasStation_FacilitateCards_Fuel_YieldsFuelAndRailCards()
    {
        var root = new GameObject("gas_bio");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var runtime = root.AddComponent<GasStationRuntime>();
            runtime.EnsureComponents();
            var bio = root.GetComponent<GasStationBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = TADispatchKinds.Fuel });
            Assert.IsTrue(cards.Exists(c => c is TAVehicleFuelCard));
            Assert.IsTrue(cards.Exists(c => c is GasStationRailRefuelCard));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GasStation_RailParallel_Refuel_WhenSegmentMatches()
    {
        var root = new GameObject("gas_rail");
        var trainGo = new GameObject("train");
        try
        {
            var station = root.AddComponent<GasStationRuntime>();
            station.EnsureComponents();
            station.linkedTrainCompanyId = "train_co";
            var pumpGo = new GameObject("pump");
            pumpGo.transform.SetParent(root.transform);
            var pump = pumpGo.AddComponent<FuelPumpRuntime>();
            pump.station = station;
            pump.railSegmentId = "seg_a";
            pump.fuelStock01 = 1f;
            station.pumps.Add(pump);

            var train = trainGo.AddComponent<TrainVehicleRagdoll>();
            train.railSegmentId = "seg_a";
            train.fuel01 = 0.1f;
            trainGo.transform.position = pumpGo.transform.position;

            Assert.IsTrue(pump.TryRefuelTrain(train, 1f));
            Assert.AreEqual(1f, train.fuel01, 1e-3f);
            Assert.Less(pump.fuelStock01, 1f);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(trainGo);
        }
    }

    [Test]
    public void GasStation_TopShelfLemma_AlcoholImpliesHighPrice()
    {
        Assert.IsTrue(GasStationShelfLemmaKeys.ImpliesHighPrice(
            GasStationShelfLemmaKeys.TopShelf, "alcohol_beer"));
        Assert.Greater(GasStationShelfLemmaKeys.VerticalBand01(GasStationShelfLemmaKeys.TopShelf), 0.7f);

        var root = new GameObject("gas_shelf");
        try
        {
            var station = root.AddComponent<GasStationRuntime>();
            station.EnsureComponents();
            station.store.shelves.Add(new StoreShelfSlot
            {
                shelfId = "s0",
                commodityKey = "alcohol_wine",
                price = 10f,
                localPosition = new Vector3(0f, 0.9f, 0f)
            });
            var slot = station.FindShelfByLemma(GasStationShelfLemmaKeys.TopShelf);
            Assert.IsNotNull(slot);
            Assert.Greater(slot.price, 10f);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GasStationBootstrap_CreatesRuntime()
    {
        var root = new GameObject("gas_stub");
        try
        {
            var stub = root.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.GasStation;
            root.AddComponent<GasStationBootstrap>().Ensure();
            Assert.IsNotNull(root.GetComponent<GasStationRuntime>());
            Assert.IsNotNull(root.GetComponent<GasStationBioRhythm>());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BuildingRequirementSpec_GasStation_HasPumpIsland()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("gas_station");
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "pump_island"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "front_desk"));
    }

    [Test]
    public void KindFromBuildingType_Park_NotParking()
    {
        Assert.AreEqual(CivilSystemKind.Park, CivilSystemLattice.KindFromBuildingType("city_park"));
        Assert.AreEqual(CivilSystemKind.Park, CivilSystemLattice.KindFromBuildingType("park"));
        Assert.AreNotEqual(CivilSystemKind.Park, CivilSystemLattice.KindFromBuildingType("parking_lot"));
    }

    [Test]
    public void Park_FacilitateCards_MaintenanceAndPatrol()
    {
        var root = new GameObject("park");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var runtime = root.AddComponent<ParkRuntime>();
            runtime.EnsureComponents();
            var bio = root.GetComponent<ParkBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = "park_maintenance" });
            Assert.IsTrue(cards.Exists(c => c is ParkMaintenanceCard));
            var patrol = bio.FacilitateCards(new DispatchRequest { kind = "park_patrol" });
            Assert.IsTrue(patrol.Exists(c => c is ParkJusticePatrolCard));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ParkSignageTrigger_FiresNarrativeOnce()
    {
        var root = new GameObject("sign");
        try
        {
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            var sign = root.AddComponent<ParkSignageTrigger>();
            sign.fireOncePerActor = true;
            sign.narrativeActionId = "park_signage_read";
            Assert.AreEqual("park_signage_read", sign.narrativeActionId);
            sign.ResetFired();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RoadLot_WallSections_SumMustBeOne_ThenBake()
    {
        var root = new GameObject("lot_wall");
        try
        {
            var spline = root.AddComponent<RoadLotBoundarySpline>();
            spline.EnsureClosedLoopDefault();
            spline.ValidateWallSections();
            var mesh = spline.BakeWallMesh();
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
            Assert.IsNotNull(spline.wallMeshCollider);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RoadLot_WallValidate_ThrowsWhenSumNotOne()
    {
        var root = new GameObject("lot_bad");
        try
        {
            var spline = root.AddComponent<RoadLotBoundarySpline>();
            spline.EnsureClosedLoopDefault();
            spline.wallSections.Clear();
            spline.wallSections.Add(new RoadLotWallSection { startT01 = 0f, endT01 = 0.4f });
            Assert.Throws<System.InvalidOperationException>(() => spline.ValidateWallSections());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LotGrass_CutSeverity_BlocksAndForgetsAbove()
    {
        var root = new GameObject("grass");
        try
        {
            var grass = root.AddComponent<LotGrassGrowthController>();
            grass.plantDef = ScriptableObject.CreateInstance<LotGrassPlantDef>();
            grass.plantDef.grownStages.Add(new LotGrassGrowthStage { stageId = "a" });
            grass.plantDef.grownStages.Add(new LotGrassGrowthStage { stageId = "b" });
            grass.stageIndex = 1;
            grass.ApplyCut(Vector3.zero, 0.5f, 0f, 1);
            Assert.AreEqual(0, grass.cuts.Count);
            grass.ApplyCut(Vector3.zero, 0.5f, 0.5f, 1);
            Assert.AreEqual(1, grass.cuts.Count);
            grass.cuts.Add(new LotGrassCutMemory { sectionId = 2, severity01 = 0.2f });
            grass.ForgetCutsAbove(1);
            Assert.IsTrue(grass.cuts[grass.cuts.Count - 1].forgotten);
            Object.DestroyImmediate(grass.plantDef);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TravelAgent_EnrichWalkSegmentWithRoadLot()
    {
        var lotGo = new GameObject("lot");
        var agentGo = new GameObject("agent");
        try
        {
            var lot = lotGo.AddComponent<RoadLot>();
            lot.lotId = "park_lot_1";
            lot.padSize = new Vector3(40f, 2f, 40f);
            lotGo.transform.position = Vector3.zero;
            // Force registry via Awake path
            lotGo.SetActive(false);
            lotGo.SetActive(true);

            var agent = agentGo.AddComponent<TravelAgent>();
            var seg = MultiModalSegment.FromWalk(new List<Vector3> { new Vector3(1f, 0f, 1f) });
            agent.EnrichWalkSegmentWithRoadLot(seg);
            Assert.AreEqual("park_lot_1", seg.roadLotId);
        }
        finally
        {
            Object.DestroyImmediate(lotGo);
            Object.DestroyImmediate(agentGo);
        }
    }

    [Test]
    public void ParkPlantPlan_SerializePlacementSquare_AndFill()
    {
        var plan = ScriptableObject.CreateInstance<ParkPlantPlanAsset>();
        try
        {
            plan.EnsureDefaults();
            plan.width = 8;
            plan.height = 8;
            var sq = plan.AddOrMovePlacementSquare(0, new Vector2Int(2, 3), "oak", new Vector3(1f, 0f, 1.5f));
            Assert.AreEqual("oak", sq.plantSpeciesId);
            Assert.IsTrue(sq.snapped);
            plan.SetCell(0, 0, 0, "lot_grass", Color.green, 0.2f);
            plan.FloodFill(0, 1, 0, "lot_grass", Color.green, 0.2f);
            Assert.IsNotNull(plan.GetCell(0, 1, 0));
        }
        finally
        {
            Object.DestroyImmediate(plan);
        }
    }

    [Test]
    public void FeatureBudgetIds_GasStationAndPark()
    {
        Assert.AreEqual("gas_station", FeatureBudgetIds.GasStation);
        Assert.AreEqual("park", FeatureBudgetIds.Park);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.GasStation));
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.Park));
    }
}
#endif
