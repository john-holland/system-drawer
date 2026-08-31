using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class HouseUtilityTests
{
    [Test]
    public void ServiceTap_DrivewayFourTouchStreet()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 8;
        grid.height = 8;
        grid.EnsureHouseLayers();
        grid.layers.Find(l => l.kind == CityPixelLayerKind.Street).frames[0].Set(0, 0, grid.width, 1);
        grid.layers.Find(l => l.kind == CityPixelLayerKind.Driveway).frames[0].Set(1, 0, grid.width, 1);
        Assert.IsTrue(HouseServiceTapResolver.TryResolveCell(grid, 0, out int x, out int y));
        Assert.AreEqual(1, x);
        Assert.AreEqual(0, y);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void ServiceTap_FallsBackToFrontWalk()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 4;
        grid.height = 4;
        grid.EnsureHouseLayers();
        var walk = new GameObject("walk");
        walk.transform.position = new Vector3(9f, 0f, 3f);
        var slots = new HouseReferenceSlots { frontWalk = walk.transform };
        Vector3 world = HouseServiceTapResolver.ResolveWorld(grid, 0, slots);
        Assert.AreEqual(walk.transform.position, world);
        Object.DestroyImmediate(walk);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void SeedWaterAndSewer_FromTinyPaintedGrid()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 6;
        grid.height = 6;
        grid.EnsureHouseLayers();
        var street = grid.layers.Find(l => l.kind == CityPixelLayerKind.Street);
        street.frames[0].Set(0, 0, grid.width, 1);
        street.frames[0].Set(1, 0, grid.width, 1);
        street.frames[0].Set(2, 0, grid.width, 1);
        grid.layers.Find(l => l.kind == CityPixelLayerKind.Driveway).frames[0].Set(1, 1, grid.width, 1);

        var waterGo = new GameObject("water");
        var water = waterGo.AddComponent<WaterGraph>();
        var sewerGo = new GameObject("sewer");
        var sewer = sewerGo.AddComponent<SewerGraph>();
        var houseGo = new GameObject("house");
        var house = houseGo.AddComponent<HousingBuildingRagdoll>();

        grid.SeedWaterAndSewerFromGrid(water, sewer, new[] { house }, 0);
        Assert.Greater(water.nodes.Count, 1);
        Assert.IsTrue(water.nodes.Exists(n => n != null && n.isStreetMain));
        Assert.IsTrue(water.nodes.Exists(n => n != null && n.isHouseTap));
        Assert.IsTrue(sewer.nodes.Exists(n => n != null && n.building == houseGo));
        Assert.IsNotNull(house.GetComponent<HouseUtilityTap>());
        Assert.IsNotNull(house.GetComponent<SewerBuildingTap>());

        Object.DestroyImmediate(houseGo);
        Object.DestroyImmediate(waterGo);
        Object.DestroyImmediate(sewerGo);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void CircuitBreaker_100A_RequiresSecondPanel()
    {
        Assert.AreEqual(1, CircuitBreakerPanel.RequiredPanelCount(80f));
        Assert.AreEqual(2, CircuitBreakerPanel.RequiredPanelCount(150f));
        Assert.AreEqual(24f, CircuitBreakerPanel.MaxDrawKwForAmpacity(), 0.01f);
    }

    [Test]
    public void UtilityBioRhythm_WritesHouseComfort()
    {
        var go = new GameObject("bio");
        var house = go.AddComponent<HouseBioRhythm>();
        var util = go.AddComponent<UtilityBioRhythm>();
        util.houseBio = house;
        util.water01 = 1f;
        util.heat01 = 1f;
        util.hvac01 = 1f;
        util.filterClog01 = 0f;
        util.gunk01 = 0f;
        util.panelLoad01 = 0f;
        util.flood01 = 0f;
        util.sewerBackup01 = 0f;
        house.gasAvailable01 = 1f;
        house.oilAvailable01 = 1f;
        house.electricAvailable01 = 1f;
        util.Tick(0.1f);
        Assert.Greater(house.utilityComfort01, 0.7f);
        util.flood01 = 1f;
        util.standingLiters = 200f;
        util.Tick(0.1f);
        Assert.Less(house.utilityComfort01, 1f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void BasementFlood_PrebakeAttempted()
    {
        var go = new GameObject("flood");
        var cache = go.AddComponent<HouseBasementFloodCache>();
        var shut = go.AddComponent<BuildingWaterShutoff>();
        shut.open = false;
        cache.shutoff = shut;
        cache.Prebake();
        Assert.IsTrue(cache.lastPrebakeAttempted);
        Assert.Greater(cache.lastEmittedLiters, 0f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DrainFromFlow_ReducesStandingLiters()
    {
        float standing = 40f;
        float taken = FloodDrainageAmounts.ApplyDrain(ref standing, 12f);
        Assert.AreEqual(12f, taken, 0.01f);
        Assert.AreEqual(28f, standing, 0.01f);

        var go = new GameObject("flood");
        var cache = go.AddComponent<HouseBasementFloodCache>();
        cache.standingLiters = 30f;
        cache.DrainFromFlow(5f, 1f);
        Assert.AreEqual(25f, cache.standingLiters, 0.01f);
        Assert.AreEqual(5f, cache.lastDrainedLiters, 0.01f);
        cache.DrainAmount(10f);
        Assert.AreEqual(15f, cache.standingLiters, 0.01f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Sump_OffBelowMinActivation_AndClampsMaxFlow()
    {
        Assert.AreEqual(0f, FloodDrainageAmounts.SumpFlowLitersPerSecond(10f, 20f, 8f, true));
        Assert.AreEqual(8f, FloodDrainageAmounts.SumpFlowLitersPerSecond(40f, 20f, 8f, true));
        Assert.AreEqual(0f, FloodDrainageAmounts.SumpFlowLitersPerSecond(40f, 20f, 8f, false));

        var go = new GameObject("sump");
        var cache = go.AddComponent<HouseBasementFloodCache>();
        var sump = go.AddComponent<SumpPumpRuntime>();
        sump.floodCache = cache;
        sump.minActivationLiters = 20f;
        sump.maxFlowLitersPerSecond = 8f;
        cache.standingLiters = 10f;
        sump.Tick(1f);
        Assert.IsFalse(sump.lastOn);
        Assert.AreEqual(0f, sump.lastDrainedLitersPerSecond);
        Assert.AreEqual(10f, cache.standingLiters, 0.01f);
        cache.standingLiters = 40f;
        sump.Tick(1f);
        Assert.IsTrue(sump.lastOn);
        Assert.AreEqual(8f, sump.lastDrainedLitersPerSecond, 0.01f);
        Assert.AreEqual(32f, cache.standingLiters, 0.01f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Lemmas_Imitirrrr_CircuitBreaker_SumpPump()
    {
        Assert.AreEqual("imitirrrr", BuiltInSynonyms.CanonicalizeToken("imitirrrr__"));
        Assert.AreEqual("circuit-breaker", BuiltInSynonyms.CanonicalizeToken("circuit_breaker"));
        Assert.AreEqual("sump-pump", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "sump", "pump" }));
        Assert.AreEqual("circuit-breaker", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "circuit", "breaker" }));
        Assert.AreEqual(UtilityLemmaPropertyKeys.Imitirrrr, RecoupWheelAlternator.LemmaId);
        Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("imitirrrr", out _));
        Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("circuit-breaker", out _));
        Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("sump-pump", out _));
    }

    [Test]
    public void FeatureBudget_UtilityRanks()
    {
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        FeatureBudgetEntry util = null, mains = null, flood = null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].featureId == FeatureBudgetIds.HouseUtility) util = entries[i];
            if (entries[i].featureId == FeatureBudgetIds.WaterMains) mains = entries[i];
            if (entries[i].featureId == FeatureBudgetIds.BasementFlood) flood = entries[i];
        }
        Assert.IsNotNull(util);
        Assert.AreEqual(38, util.importanceRank);
        Assert.IsTrue(System.Array.IndexOf(util.perfScopePrefixes, "UtilityBioRhythm") >= 0);
        Assert.IsTrue(System.Array.IndexOf(util.perfScopePrefixes, "SumpPump") >= 0);
        Assert.AreEqual(39, mains.importanceRank);
        Assert.IsTrue(System.Array.IndexOf(mains.perfScopePrefixes, "WaterGraph") >= 0);
        Assert.AreEqual(40, flood.importanceRank);
        Assert.IsTrue(System.Array.IndexOf(flood.perfScopePrefixes, "HouseBasementFloodCache") >= 0);
        Assert.IsTrue(System.Array.IndexOf(flood.perfScopePrefixes, "RollingSphereFloodSimulator") >= 0);
    }

    [Test]
    public void UtilityCard_StreetBreaker_TripsWaterAndPanel()
    {
        var go = new GameObject("room");
        var room = go.AddComponent<UtilityRoomBootstrap>();
        room.Ensure();
        room.shutoff.SetOpen(true);
        room.panel.SetFeed(true);
        var card = UtilityCard.Generate(UtilityCardKind.StreetBuildingWaterBreaker, room);
        card.Apply();
        Assert.IsFalse(room.shutoff.open);
        Assert.IsFalse(room.panel.feedOn);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void IkCatalog_HasPlugAndBreakerEntries()
    {
        var cat = ScriptableObject.CreateInstance<UtilityIkTrainingCatalog>();
        cat.EnsureDefaults();
        Assert.AreEqual(4, cat.entries.Count);
        Assert.AreEqual(UtilityIkTrainingCatalog.PlugIn, cat.entries[0].id);
        Assert.AreEqual(PhysicsIKTrainingCategory.Open, cat.entries[0].category);
        Assert.AreEqual(PhysicsIKTrainingCategory.Close, cat.entries[1].category);
        Object.DestroyImmediate(cat);
    }

    [Test]
    public void WallPlug_ComposesTineCavities()
    {
        var go = new GameObject("plug");
        var plug = go.AddComponent<WallPlugRuntime>();
        var sdf = plug.ComposeTineCavities();
        Assert.IsNotNull(sdf);
        Assert.Greater(sdf.nodes.Count, 2);
        Object.DestroyImmediate(sdf);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HouseChoreCatalog_IncludesUtilityMaintain()
    {
        var chores = HouseChoreCatalog.DefaultChores(null);
        Assert.IsTrue(chores.Exists(c => c.chore == HouseChoreKind.UtilityMaintain));
    }
}
