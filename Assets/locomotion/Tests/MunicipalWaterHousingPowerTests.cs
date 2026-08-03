using NUnit.Framework;
using UnityEngine;

public sealed class MunicipalWaterHousingPowerTests
{
    [Test]
    public void MunicipalWater_FixtureBranches_DoNotShareHotByDefault()
    {
        var root = new GameObject("plumbing");
        var group = root.AddComponent<BuildingPlumbingGroup>();
        var toiletGo = new GameObject("toilet");
        toiletGo.transform.SetParent(root.transform);
        toiletGo.AddComponent<ToiletFixture>();
        var sinkGo = new GameObject("sink");
        sinkGo.transform.SetParent(root.transform);
        var sink = sinkGo.AddComponent<SinkFixture>();
        sink.plumbing.sinkGetsHotWhenToiletFlushed = false;
        sink.plumbing.plumbingGroup = group;

        group.NotifyToiletFlushed(1f);
        float hotNoFlag = sink.plumbing.AvailableHot01();
        sink.plumbing.sinkGetsHotWhenToiletFlushed = true;
        group.NotifyToiletFlushed(1f);
        float hotWithFlag = sink.plumbing.AvailableHot01();
        Assert.GreaterOrEqual(hotWithFlag, hotNoFlag);
        Assert.AreNotEqual(sink.plumbing.branchIdCold, "cold_a"); // sink branch distinct

        Object.DestroyImmediate(root);
    }

    [Test]
    public void ToiletOverflow_ProgressesCeilingToSky()
    {
        var go = new GameObject("jet");
        var jet = go.AddComponent<ToiletOverflowJetDriver>();
        jet.layers = new System.Collections.Generic.List<DestructibleLayerRef>
        {
            new DestructibleLayerRef { kind = DestructibleLayerKind.Ceiling, destroyed = true },
            new DestructibleLayerRef { kind = DestructibleLayerKind.Roof, destroyed = true },
            new DestructibleLayerRef { kind = DestructibleLayerKind.Sky }
        };
        jet.ResolveTargetLayer();
        Assert.AreEqual(DestructibleLayerKind.Sky, jet.CurrentTargetLayer);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ClogPlungeSnake_CardsClearDrain()
    {
        var go = new GameObject("toilet");
        var fixture = go.AddComponent<ToiletFixture>();
        fixture.plumbing.clog.AccumulateDry(1f);
        Assert.Greater(fixture.plumbing.clog.EffectiveClog01(), 0.5f);
        PlungeToiletCard.Generate(fixture).Apply();
        Assert.Less(fixture.plumbing.clog.EffectiveClog01(), 1f);
        SnakeToiletCard.Generate(fixture).Apply();
        Assert.Less(fixture.plumbing.clog.EffectiveClog01(), 0.5f);
        var solver = go.AddComponent<PhysicsCardSolver>();
        var cards = solver.SolveForGoal(new BehaviorTreeGoal { type = GoalType.Plumbing }, new RagdollState());
        Assert.IsTrue(cards.Count > 0);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HousingBuildingRagdoll_ArchitectureAndChores()
    {
        var go = new GameObject("house");
        var house = go.AddComponent<HousingBuildingRagdoll>();
        house.ApplyArchitectureLemma("mc_mansion");
        Assert.AreEqual(HousingArchitectureSize.McMansion, house.architecture.size);
        Assert.Greater(house.architecture.FootprintScale(), 1f);
        var chores = house.BuildChoreCards();
        Assert.Greater(chores.Count, 0);
        chores[0].Apply();
        Assert.IsTrue(HouseInventoryBinder.IsNestedInventoryName("bedroom2", "bedroom2_dresser2"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PowerLineDecorator_WarnsWithoutSpline()
    {
        var go = new GameObject("power");
        var dec = go.AddComponent<PowerLineRoadsideDecorator>();
        dec.generateOnAwake = false;
        dec.Generate();
        Assert.Greater(dec.warnings.Count, 0);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PowerLineTension_UnbreakableNeverBreaks()
    {
        var go = new GameObject("lemma");
        var lemma = go.AddComponent<PowerLineTensionLemma>();
        lemma.ApplyToken("unbreakable");
        Assert.IsFalse(lemma.ShouldBreakPole(1f));
        lemma.ApplyToken("faulty-standoff");
        Assert.AreEqual(PowerLineStandoffLemma.FaultyStandoff, lemma.lemma);
        Object.DestroyImmediate(go);
    }
}
