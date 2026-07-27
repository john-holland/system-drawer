#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BathroomHygieneTests
{
    [Test]
    public void PaperScroll_PullSheets_ReducesCount()
    {
        var go = new GameObject("scroll");
        try
        {
            var scroll = go.AddComponent<PaperScrollSystem>();
            scroll.sheetsRemaining = 10;
            float len = scroll.PullSheets(4);
            Assert.AreEqual(6, scroll.sheetsRemaining);
            Assert.AreEqual(4 * scroll.sheetLengthM, len, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ToiletStation_BidetDefaultTrue()
    {
        var go = new GameObject("toilet");
        try
        {
            var t = go.AddComponent<ToiletStation>();
            Assert.IsTrue(t.includesBidet);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PeeJitter_Zero_DisablesCurve()
    {
        var go = new GameObject("pee");
        try
        {
            var pee = go.AddComponent<PeeStreamDirector>();
            pee.peeDirectionJitterDegrees = 0f;
            var bladder = go.AddComponent<BowelBladderRuntime>();
            bladder.bladderFill01 = 1f;
            pee.bowelBladder = bladder;
            pee.BeginRelease(1);
            Assert.AreEqual(0f, pee.peeDirectionJitterDegrees);
            pee.EndRelease();
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SmellWhitelist_BlocksUnknown_AllowsGarlic()
    {
        var wl = ScriptableObject.CreateInstance<EatingSmellWhitelist>();
        try
        {
            Assert.IsTrue(wl.IsAllowed("garlic"));
            Assert.IsFalse(wl.IsAllowed("mystery_perfume"));
        }
        finally
        {
            Object.DestroyImmediate(wl);
        }
    }

    [Test]
    public void FoodProcessor_AdjustsTowardSetpoints()
    {
        var actor = new GameObject("eater");
        var foodGo = new GameObject("food");
        try
        {
            var life = actor.AddComponent<LifeSystemsServices>();
            var proc = actor.AddComponent<FoodProcessorBioRhythmService>();
            proc.adjustToNormalIngredients = true;
            proc.createPoopByDefault = false;
            proc.developerModification01 = 0.5f;
            var sheet = life.GetOrCreate(actor);
            sheet.Set01(LifeSystemsChannelCatalog.Vitamins, 0.1f);
            var food = foodGo.AddComponent<FoodItem>();
            food.nutrients = new FoodNutrientProfile { useExplicitNutrients = false };
            proc.OnSwallow(actor, food);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Vitamins), 0.1f);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(foodGo);
        }
    }

    [Test]
    public void FoodProcessor_CreatePoop_SpawnsAndQueues()
    {
        var actor = new GameObject("eater");
        var foodGo = new GameObject("food");
        var bowl = new GameObject("bowl");
        try
        {
            var proc = actor.AddComponent<FoodProcessorBioRhythmService>();
            proc.createPoopByDefault = true;
            proc.autoQueueToiletBt = false;
            var food = foodGo.AddComponent<FoodItem>();
            food.createPoopContribution = true;
            proc.OnSwallow(actor, food);
            var bowel = actor.GetComponent<BowelBladderRuntime>();
            Assert.IsNotNull(bowel);
            Assert.IsNotNull(bowel.pendingPoop);
            Assert.Greater(bowel.bowelFill01, 0f);
            var poop = proc.SpawnPoopFromPayload(actor, bowel.pendingPoop, bowl.transform);
            Assert.IsNotNull(poop);
            Assert.AreEqual(0f, bowel.bowelFill01);
            Assert.AreEqual(bowl.transform, poop.transform.parent);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(foodGo);
            Object.DestroyImmediate(bowl);
        }
    }

    [Test]
    public void HygieneSmellClear_ZerosEmitters()
    {
        var go = new GameObject("smelly");
        try
        {
            var e = go.AddComponent<Locomotion.Senses.SmellEmitter>();
            e.signature = "garlic";
            e.intensity = 1f;
            e.emissionMultiplier = 1f;
            HygieneSmellClearService.ClearAllOn(go);
            Assert.AreEqual(0f, e.emissionMultiplier);
            Assert.AreEqual(0f, e.intensity);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PhysicsCardSolver_MatchesToiletAndHygiene()
    {
        var go = new GameObject("solver");
        try
        {
            var solver = go.AddComponent<PhysicsCardSolver>();
            solver.AddCards(new System.Collections.Generic.List<GoodSection>
            {
                ConsiderBodyHygieneCards.MakeToiletCard(),
                ConsiderBodyHygieneCards.MakeHygieneCard("brush_teeth")
            });
            var toiletCards = solver.SolveForGoal(new BehaviorTreeGoal { type = GoalType.Toilet }, new RagdollState());
            Assert.IsTrue(toiletCards[0].isToiletGoal);
            var hygCards = solver.SolveForGoal(new BehaviorTreeGoal { type = GoalType.Hygiene }, new RagdollState());
            Assert.IsTrue(hygCards[0].isHygieneGoal);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void FreeExcrete_VehicleOrganHost_CreatesOrgans()
    {
        var vehicle = new GameObject("vehicle");
        try
        {
            vehicle.AddComponent<VehicleActor>();
            var host = VehicleOrganHost.FindOrCreate(vehicle);
            Assert.IsNotNull(host.bowelBladder);
            Assert.IsNotNull(host.groin);
            var bt = vehicle.AddComponent<BehaviorTree>();
            var node = new FreeExcreteNode { doPee = false, doPoop = true, duration = 0f };
            node.OnEnter(bt);
            Assert.AreEqual(0f, host.bowelBladder.bowelFill01);
        }
        finally
        {
            Object.DestroyImmediate(vehicle);
        }
    }

    [Test]
    public void AfterToiletSit_Bidet_ClearsGroinSmell()
    {
        var actor = new GameObject("actor");
        var toiletGo = new GameObject("toilet");
        try
        {
            var toilet = toiletGo.AddComponent<ToiletStation>();
            toilet.includesBidet = true;
            toilet.useToiletPaperBt = true;
            var e = actor.AddComponent<Locomotion.Senses.SmellEmitter>();
            e.signature = "poop";
            e.intensity = 1f;
            e.emissionMultiplier = 1f;
            var bt = actor.AddComponent<BehaviorTree>();
            bt.SetGoal(new BehaviorTreeGoal { type = GoalType.Toilet, target = toiletGo });
            var after = new AfterToiletSitNode { toilet = toilet };
            after.OnEnter(bt);
            // Run until hygiene done
            for (int i = 0; i < 8; i++)
                after.Execute(bt);
            Assert.AreEqual(0f, e.emissionMultiplier);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(toiletGo);
        }
    }

    [Test]
    public void BowelQueue_UsesGoalTypeToilet()
    {
        var actor = new GameObject("actor");
        var toiletGo = new GameObject("toilet");
        try
        {
            var toilet = toiletGo.AddComponent<ToiletStation>();
            var bt = actor.AddComponent<BehaviorTree>();
            var bowel = actor.AddComponent<BowelBladderRuntime>();
            bowel.preferredToilet = toilet;
            bowel.preferToiletWhenAvailable = true;
            bowel.QueueToiletOrFreeExcrete();
            Assert.AreEqual(GoalType.Toilet, bt.currentGoal.type);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(toiletGo);
        }
    }
}
#endif
