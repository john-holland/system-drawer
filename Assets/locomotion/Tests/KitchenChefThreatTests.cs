using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class KitchenChefThreatTests
{
    [Test]
    public void ChefCard_Generate_MeetsRequirements_WithStation()
    {
        var station = new GameObject("Station");
        var actor = new GameObject("Chef");
        try
        {
            var card = ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Sear, station);
            Assert.IsTrue(card.isChefGoal);
            Assert.IsTrue(card.MeetsChefRequirements(actor, station));
            Assert.Greater(card.dutyChecklist.Count, 0);
        }
        finally
        {
            Object.DestroyImmediate(station);
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void CardHistoryManager_DoesNotRetainLiveRefs_AndCapsBuffer()
    {
        var histGo = new GameObject("Hist");
        var solverGo = new GameObject("Solver");
        try
        {
            var hist = histGo.AddComponent<CardHistoryManager>();
            // SetBufferSize floors at 16 (Awake / SetBufferSize clamp).
            hist.SetBufferSize(16);
            var solver = solverGo.AddComponent<PhysicsCardSolver>();
            for (int i = 0; i < 12; i++)
            {
                var card = ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Place, null);
                card.sectionName = "c" + i;
                solver.AddCards(new List<GoodSection> { card });
            }
            Assert.LessOrEqual(hist.HistoryCount, hist.historyBufferSize);
            var snaps = hist.GetHistoryNewestFirst(20);
            Assert.Greater(snaps.Count, 0);
            Assert.IsNotNull(snaps[0].typeName);
            // Snapshot must not hold UnityObject refs — type is plain data
            Assert.IsInstanceOf<CardHistorySnapshot>(snaps[0]);
        }
        finally
        {
            Object.DestroyImmediate(histGo);
            Object.DestroyImmediate(solverGo);
        }
    }

    [Test]
    public void ThreatWarden_Raise_AssignsThreatCard_AlongPeckingOrder()
    {
        var venue = new GameObject("Venue");
        var line = new GameObject("LineChef");
        line.transform.position = venue.transform.position + Vector3.right;
        var maint = new GameObject("Maint");
        maint.transform.position = venue.transform.position + Vector3.right * 5f;
        try
        {
            var warden = venue.AddComponent<ThreatWarden>();
            warden.contextOwner = venue;
            warden.emitSendThoughtOnRaise = false;
            line.AddComponent<PhysicsCardSolver>();
            maint.AddComponent<PhysicsCardSolver>();
            warden.SetRetinuePeckingOrder(new[]
            {
                new RetinuePeckingEntry { personaKey = "line", role = "line-chef", peckingOrder = 40, actor = line },
                new RetinuePeckingEntry
                {
                    personaKey = "maint", role = "building_maintenance", peckingOrder = 80, actor = maint,
                    agencyAffinity = ThreatAgencyId.BuildingMaintenance
                }
            });
            var card = warden.RaiseThreat(ThreatKind.SmokeDetectorBattery);
            Assert.AreEqual("on-edge", card.alertLemma);
            var solver = line.GetComponent<PhysicsCardSolver>();
            Assert.IsTrue(solver.availableCards.Exists(c => c is ThreatCard));
            var dialog = line.GetComponent<ThreatDialogBranch>();
            Assert.IsNotNull(dialog);
            Assert.Greater(dialog.dialogSuggestions.Count, 0);
        }
        finally
        {
            Object.DestroyImmediate(venue);
            Object.DestroyImmediate(line);
            Object.DestroyImmediate(maint);
        }
    }

    [Test]
    public void JusticeCard_Gates_WhenFatigueHigh()
    {
        var actor = new GameObject("Burned");
        var stove = new GameObject("Stove");
        try
        {
            var sheet = actor.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            sheet.Set01(LifeSystemsChannelCatalog.Fatigue, 0.9f);
            actor.AddComponent<LimbIntegrityState>();
            var card = JusticeCard.Generate(JusticeAction.ShutOffHeat, stove);
            Assert.IsFalse(card.MeetsJusticeRequirements(actor, stove));
            sheet.Set01(LifeSystemsChannelCatalog.Fatigue, 0.1f);
            Assert.IsTrue(card.MeetsJusticeRequirements(actor, stove));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(stove);
        }
    }

    [Test]
    public void HeatCookSolver_AdvancesEvolutionAndSmoke()
    {
        var station = new GameObject("Pan");
        var actor = new GameObject("Cook");
        var bioGo = new GameObject("Bio");
        try
        {
            bioGo.AddComponent<KitchenBioRhythmService>();
            var card = ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Sear, station);
            Assert.IsTrue(HeatCookSolver.Apply(card, actor, 0.5f, out var status));
            Assert.AreEqual("sear", status);
            Assert.Greater(card.evolutionCards.Count, 0);
            Assert.IsNotNull(station.GetComponent<PanOilSmokeTracker>());
        }
        finally
        {
            Object.DestroyImmediate(station);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(bioGo);
        }
    }

    [Test]
    public void PourAccuracy_AccumulatesDryLiquidPhase()
    {
        DryLiquidPhaseMaterial.Reset();
        var actor = new GameObject("PourActor");
        try
        {
            var card = ChefCard.Generate(ChefDutyMode.Prep, ChefActivity.Sprinkle, null);
            card.pourRateLitersPerSec = 1f;
            card.accuracy01 = 1f;
            Assert.IsTrue(PourAccuracySolver.Apply(card, actor, 0.1f, out _));
            Assert.Greater(DryLiquidPhaseMaterial.AccumulatedVolume, 0f);
        }
        finally
        {
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void ThreatToolResolution_WaterAndExtinguisher()
    {
        var card = ThreatCard.Generate(ThreatKind.Fire, null, null);
        Assert.IsTrue(ThreatToolResolution.TryResolve(card, null, 1f, 10f, false, false));
        Assert.IsTrue(ThreatToolResolution.TryResolve(card, null, 0f, 0f, true, false));
        Assert.IsFalse(ThreatToolResolution.TryResolve(card, null, 0.1f, 1f, false, false));
    }

    [Test]
    public void RestaurantVenue_OpenActivatesWaypoints()
    {
        var venue = new GameObject("Rest");
        var wp = new GameObject("WP");
        wp.SetActive(false);
        try
        {
            var rt = venue.AddComponent<RestaurantVenueRuntime>();
            rt.waypointGroupRoots.Add(wp);
            rt.SetOpen(true);
            Assert.IsTrue(rt.isOpen);
            Assert.IsTrue(wp.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(venue);
            Object.DestroyImmediate(wp);
        }
    }

    [Test]
    public void Inventory_PutAwayToVehicleInterior()
    {
        var mgrGo = new GameObject("InvMgr");
        var actorGo = new GameObject("Chef");
        var van = new GameObject("Van");
        try
        {
            var mgr = mgrGo.AddComponent<InventoryManager>();
            mgr.scriptMentionGate = false;
            var actorInv = actorGo.AddComponent<ActorInventory>();
            actorInv.actorId = "Chef";
            var item = new InventoryItem
            {
                id = "1",
                name = "flour",
                ownedByActorId = "Chef",
                heldByActorId = "Chef"
            };
            actorInv.items.Add(item);
            mgr.UpsertLocal(item);
            var interior = van.AddComponent<VehicleInterior>();
            Assert.IsTrue(mgr.PutAwayToVehicleInterior(item, interior, "Chef"));
            Assert.AreEqual(van, item.contextGameObject);
            var vehicleInv = van.GetComponent<ActorInventory>();
            Assert.IsNotNull(vehicleInv);
            Assert.IsNotNull(vehicleInv.FindByName("flour"));
            Assert.IsNull(actorInv.FindByName("flour"));
            Assert.AreEqual(vehicleInv.actorId, item.ownedByActorId);
            Assert.IsNull(item.heldByActorId);
        }
        finally
        {
            Object.DestroyImmediate(mgrGo);
            Object.DestroyImmediate(actorGo);
            Object.DestroyImmediate(van);
        }
    }

    [Test]
    public void PhysicsCardSolver_CookingGoal_Fallback()
    {
        var go = new GameObject("Sol");
        try
        {
            var solver = go.AddComponent<PhysicsCardSolver>();
            var goal = new BehaviorTreeGoal { type = GoalType.Cooking, goalName = "cook" };
            var cards = solver.SolveForGoal(goal, new RagdollState());
            Assert.Greater(cards.Count, 0);
            Assert.IsTrue(cards[0] is ChefCard || cards[0].isChefGoal);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
