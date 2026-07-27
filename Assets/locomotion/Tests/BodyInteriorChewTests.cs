#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BodyInteriorChewTests
{
    [Test]
    public void ToothCatalog_PremolarsAreMolarBack_IncisorsFront()
    {
        Assert.AreEqual(ToothZone.Front, ToothSlot.ZoneFor(ToothKind.CentralIncisor));
        Assert.AreEqual(ToothZone.Front, ToothSlot.ZoneFor(ToothKind.Canine));
        Assert.AreEqual(ToothZone.MolarBack, ToothSlot.ZoneFor(ToothKind.FirstPremolar));
        Assert.AreEqual(ToothZone.MolarBack, ToothSlot.ZoneFor(ToothKind.Wisdom));
    }

    [Test]
    public void PreferredChewSide_SeedBand_IsBetween50And55()
    {
        var go = new GameObject("seed");
        try
        {
            var s = go.AddComponent<DeveloperRespectsSeed>();
            s.Reseed(42);
            Assert.GreaterOrEqual(s.PreferredChewSide01, 0.5f);
            Assert.LessOrEqual(s.PreferredChewSide01, 0.55f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ChewStrategy_Cheese_IncludesParabola_Fruit_IncludesDiscard()
    {
        var cheese = ChewStrategy.PhasesFor(FoodKind.Cheese);
        Assert.Contains(ChewPhase.TongueParabola, cheese);
        var fruit = ChewStrategy.PhasesFor(FoodKind.FruitVegetable);
        Assert.Contains(ChewPhase.DiscardInedible, fruit);
        Assert.Contains(ChewPhase.OpenClosePeel, fruit);
        var meat = ChewStrategy.PhasesFor(FoodKind.Meat);
        Assert.Contains(ChewPhase.ChewMolarsProgressive, meat);
        Assert.IsFalse(meat.Contains(ChewPhase.TongueParabola));
    }

    [Test]
    public void ChewBake_FitsAgainstFrontEllipsoid()
    {
        var actor = new GameObject("Actor");
        var foodGo = new GameObject("Food");
        try
        {
            var mouth = actor.AddComponent<MouthInteriorRuntime>();
            mouth.EnsureDefaultTeeth();
            var food = foodGo.AddComponent<FoodItem>();
            food.biteFitRadius = 0.01f;
            foodGo.transform.position = actor.transform.position;
            var bake = ChewConvexTreeBakeService.Bake(food, mouth);
            Assert.Greater(bake.sections.Count, 0);
            Assert.Greater(bake.frontEllipsoid.size.magnitude, 0f);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(foodGo);
        }
    }

    [Test]
    public void AnimationGroup_DefaultAdultTeeth_Has32()
    {
        Assert.AreEqual(32, ToothCatalog.BuildDefaultAdultSet().Length);
    }

    [Test]
    public void PhysicsCardSolver_MatchesEatGoal()
    {
        var go = new GameObject("solver");
        try
        {
            var solver = go.AddComponent<PhysicsCardSolver>();
            var card = ConsiderBodyHygieneCards.MakeEatCard();
            solver.AddCards(new List<GoodSection> { card });
            var goal = new BehaviorTreeGoal { type = GoalType.Eat, goalName = "eat" };
            var matched = solver.SolveForGoal(goal, new RagdollState());
            Assert.Greater(matched.Count, 0);
            Assert.IsTrue(matched[0].isEatGoal);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Mouth_DriveMolarRoll_SetsJawRoll()
    {
        var go = new GameObject("mouth");
        try
        {
            var mouth = go.AddComponent<MouthInteriorRuntime>();
            mouth.DriveMolarRoll(0.25f, preferRight: true);
            Assert.AreNotEqual(0f, mouth.jawRollDeg);
            Assert.Greater(mouth.jawOpen01, 0f);
            mouth.DriveFrontBite(0.5f);
            Assert.AreEqual(0f, mouth.jawRollDeg);
            Assert.AreEqual(0.5f, mouth.jawOpen01);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void EatingAnimationDriver_MapsTagsToCategories()
    {
        Assert.AreEqual(PhysicsIKTrainingCategory.Bite, EatingAnimationDriver.CategoryForTag("eat.bite"));
        Assert.AreEqual(PhysicsIKTrainingCategory.Swallow, EatingAnimationDriver.CategoryForTag("eat.swallow"));
        Assert.AreEqual(PhysicsIKTrainingCategory.Chew, EatingAnimationDriver.CategoryForTag("eat.chew"));
    }

    [Test]
    public void ToothFaceNormals_BuccalOppositeLingual()
    {
        var go = new GameObject("mouth");
        try
        {
            var mouth = go.AddComponent<MouthInteriorRuntime>();
            mouth.EnsureDefaultTeeth();
            ToothSlot right = null;
            foreach (var t in mouth.EnumeratePresent())
            {
                if (t.side == ToothSide.Right) { right = t; break; }
            }
            Assert.IsNotNull(right);
            mouth.ResolveToothFaceNormals(right, out var buccal, out var lingual, out _);
            Assert.Less(Vector3.Dot(buccal, lingual), -0.5f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
