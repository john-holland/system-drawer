#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RecipeBehaviorTreeTests
{
    [Test]
    public void Ingredient_ValidateAmounts()
    {
        var recipe = ScriptableObject.CreateInstance<RecipeBehaviorTreeAsset>();
        try
        {
            recipe.servesAmount = 2f;
            recipe.ingredients.Add(new RecipeIngredientSpec
            {
                commodity = new RecipeCommoditySpec { displayName = "nanas" },
                amount = 1f
            });
            Assert.IsTrue(recipe.ValidateAmounts(out _));
            recipe.ingredients[0].amount = 0f;
            Assert.IsFalse(recipe.ValidateAmounts(out var err));
            Assert.IsNotNull(err);
        }
        finally
        {
            Object.DestroyImmediate(recipe);
        }
    }

    [Test]
    public void TrayBin_BatchesAndBailoutReduceToSingle()
    {
        var settings = new TrayBinSettings
        {
            maxPlateSlots = 4,
            maxCount = 4,
            allowSinglePersonLoads = true,
            allowSansTrayFallback = true
        };
        var batches = TrayBinAllocator.BuildBatches(4f, 2, settings, 1f);
        Assert.Greater(batches.Count, 0);
        int sum = 0;
        for (int i = 0; i < batches.Count; i++) sum += batches[i].plateCount;
        Assert.AreEqual(TrayBinAllocator.PlatesNeeded(4f, 2, 1f), sum);

        var bail = TrayServeBailout.Evaluate(false, true, false, 3, settings);
        Assert.AreEqual(TrayServeBailReason.AlreadyEaten, bail.reason);
        Assert.AreEqual(3, bail.reducedBatches.Count);
        for (int i = 0; i < bail.reducedBatches.Count; i++)
            Assert.AreEqual(1, bail.reducedBatches[i].plateCount);

        var covered = TrayServeBailout.Evaluate(false, false, true, 2, settings);
        Assert.AreEqual(TrayServeBailReason.PlaceWaypointCovered, covered.reason);
    }

    [Test]
    public void SpecialCommodity_SupplementableResolvesFromBase()
    {
        var go = new GameObject("Inv");
        try
        {
            var inv = go.AddComponent<InventoryManager>();
            inv.scriptMentionGate = false;
            inv.UpsertLocal(new InventoryItem { id = "b1", name = "nanas" });
            var spec = new RecipeCommoditySpec
            {
                displayName = "nanas classic sauce",
                specialOf = "nanas",
                supplementable = true
            };
            var ok = SpecialCommodityResolver.Resolve(spec, inv);
            Assert.IsTrue(ok.ok);
            Assert.IsTrue(ok.supplementedFromBase);
            Assert.AreEqual("nanas classic sauce", ok.item.name);

            var strict = new RecipeCommoditySpec
            {
                displayName = "secret sauce",
                specialOf = "nanas",
                supplementable = false
            };
            var fail = SpecialCommodityResolver.Resolve(strict, inv);
            Assert.IsFalse(fail.ok);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Taste_SourRaisesBp_SpicyRaisesEndorphin()
    {
        var go = new GameObject("Diner");
        try
        {
            var sheet = go.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            float bp0 = sheet.Get01(LifeSystemsChannelCatalog.BloodPressureSys);
            float end0 = sheet.Get01(LifeSystemsChannelCatalog.Endorphin);
            var notes = new List<TasteNoteEntry>
            {
                new TasteNoteEntry { note = TasteNoteId.Sour, intensity01 = 1f },
                new TasteNoteEntry { note = TasteNoteId.Spicy, intensity01 = 1f }
            };
            var applied = TasteNotesApplicator.Apply(sheet, notes, 1f);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.BloodPressureSys), bp0);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Endorphin), end0);
            Assert.GreaterOrEqual(applied.dialogSuggestions.Count, 2);
            StringAssert.Contains("taste", TasteNotesApplicator.BuildLemmaToken(notes, 0.5f));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void DishHanoi_IllegalMoveRejected_LegalAdvancesDry()
    {
        var go = new GameObject("DishPit");
        try
        {
            var station = go.AddComponent<DishWashingStation>();
            station.EnsureZones();
            Assert.IsFalse(DishWashingStation.IsLegalMove(DishZoneKind.Dry, DishZoneKind.Dirty, false));
            Assert.IsTrue(DishWashingStation.IsLegalMove(DishZoneKind.Dirty, DishZoneKind.Sink, false));
            Assert.IsFalse(DishWashingStation.IsLegalMove(DishZoneKind.Dirty, DishZoneKind.Compost, false));
            Assert.IsTrue(DishWashingStation.IsLegalMove(DishZoneKind.Dirty, DishZoneKind.Compost, true));

            station.SeedDirtyFromService(1);
            Assert.IsTrue(station.TryPeekTop(DishZoneKind.Dirty, out string dish));
            Assert.IsFalse(station.TryMove(dish, DishZoneKind.Dirty, DishZoneKind.Dry, out _));
            Assert.IsTrue(station.TryMove(dish, DishZoneKind.Dirty, DishZoneKind.Sink, out _));
            Assert.IsTrue(station.TryMove(dish, DishZoneKind.Sink, DishZoneKind.Dry, out _));
            Assert.AreEqual(1, station.Count(DishZoneKind.Dry));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ZoneSort_DirtyNearestTrashAlongLateral()
    {
        var root = new GameObject("DishLayout");
        try
        {
            var trash = new GameObject("Trash");
            trash.transform.SetParent(root.transform);
            trash.transform.localPosition = Vector3.zero;

            var dirty = new GameObject("Dirty");
            dirty.transform.SetParent(root.transform);
            dirty.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            var dry = new GameObject("Dry");
            dry.transform.SetParent(root.transform);
            dry.transform.localPosition = new Vector3(3f, 0f, 0f);
            var sink = new GameObject("Sink");
            sink.transform.SetParent(root.transform);
            sink.transform.localPosition = new Vector3(1.5f, 0f, 0f);

            var station = root.AddComponent<DishWashingStation>();
            station.trashAnchor = trash.transform;
            station.kitchenLateral = Vector3.right;
            station.runtimeZones.Clear();
            station.runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dry, anchor = dry.transform });
            station.runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dirty, anchor = dirty.transform });
            station.runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Sink, anchor = sink.transform });
            station.runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dishwasher, anchor = sink.transform });
            station.SortZonesNearestTrash();
            Assert.AreEqual(DishZoneKind.Dirty, station.runtimeZones[0].kind);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SeasonPan_WipeOilAfterClean_ResetsSmoke()
    {
        var pan = new GameObject("Pan");
        try
        {
            var tracker = pan.AddComponent<PanOilSmokeTracker>();
            tracker.smoke01 = 0.9f;
            tracker.oil01 = 0.1f;
            var card = ChefSeasonPanCard.Generate(ChefSeasonPanMode.WipeOilAfterClean, pan, 0.4f);
            Assert.IsTrue(ChefSeasonPanSolver.TrySolve(card, 0.2f, out var status));
            Assert.AreEqual("wipe_oil", status);
            Assert.AreEqual(0f, tracker.smoke01, 0.001f);
            Assert.AreEqual(0.4f, tracker.oil01, 0.001f);
            Assert.Contains("wipe_oil", card.dutyChecklist);
        }
        finally
        {
            Object.DestroyImmediate(pan);
        }
    }

    [Test]
    public void DishFinishPreference_DishwasherPathPreferred()
    {
        var go = new GameObject("DishPref");
        try
        {
            var cfg = ScriptableObject.CreateInstance<DishWashingStationConfig>();
            cfg.finishPreference = DishFinishPreference.Dishwasher;
            cfg.enableCompostZone = false;
            cfg.EnsureStandardZones();
            var station = go.AddComponent<DishWashingStation>();
            station.config = cfg;
            station.EnsureZones();
            station.SeedDirtyFromService(1);
            station.TryMove(null, DishZoneKind.Dirty, DishZoneKind.Sink, out _);
            var cards = ConsiderDishwashingCards.GeneratePreferredMoves(station, 8);
            bool hasWasher = false;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].toZone == DishZoneKind.Dishwasher) hasWasher = true;
            Assert.IsTrue(hasWasher);
            Object.DestroyImmediate(cfg);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void RecipeStep_MapsToChefCardActivity()
    {
        var step = new RecipeStepSpec
        {
            label = "sear",
            chefActivity = ChefActivity.Sear,
            narrativeAction = NarrativeMealPrepActionKind.PrepCook
        };
        var card = NarrativeMealPrepAction.MakeChefCard(step);
        Assert.AreEqual(ChefActivity.Sear, card.activity);
        Assert.AreEqual(CardPlanActionKind.CookDuty, NarrativeMealPrepAction.ToCardPlanAction(step.narrativeAction));
        Assert.AreEqual(CardPlanActionKind.WashDish, NarrativeMealPrepAction.ToCardPlanAction(NarrativeMealPrepActionKind.WashDish));
    }

    [Test]
    public void CompostOffByDefault()
    {
        var cfg = ScriptableObject.CreateInstance<DishWashingStationConfig>();
        try
        {
            Assert.IsFalse(cfg.enableCompostZone);
            cfg.EnsureStandardZones();
            bool hasCompost = false;
            for (int i = 0; i < cfg.zones.Count; i++)
                if (cfg.zones[i].kind == DishZoneKind.Compost) hasCompost = true;
            Assert.IsFalse(hasCompost);
        }
        finally
        {
            Object.DestroyImmediate(cfg);
        }
    }

    [Test]
    public void Endorphin_ChannelExists()
    {
        Assert.IsTrue(LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.Endorphin, out _));
    }
}
#endif
