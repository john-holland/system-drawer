#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

public class GambitAndStairwellTests
{
    [Test]
    public void SlowTimeController_RestoresScale()
    {
        var go = new GameObject("slow");
        var c = go.AddComponent<SlowTimeController>();
        float prev = Time.timeScale;
        Time.timeScale = 1f;
        c.Enter(0.2f);
        Assert.AreEqual(0.2f, Time.timeScale, 0.001f);
        c.Exit();
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
        Time.timeScale = prev;
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VehicleGambitAuthoring_UpsertsNarrowStop()
    {
        var asset = ScriptableObject.CreateInstance<VehicleGambitPathAsset>();
        asset.narrowClearanceThreshold = 1f;
        var regGo = new GameObject("reg");
        var reg = regGo.AddComponent<PathingApertureRegistry>();
        var apGo = new GameObject("ap");
        apGo.transform.position = Vector3.zero;
        var ap = apGo.AddComponent<PathingAperture>();
        ap.apertureId = "gate";
        ap.radius = 0.5f;
        reg.apertures.Add(ap);

        var veh = new GameObject("veh");
        veh.transform.position = Vector3.zero;
        var auth = regGo.AddComponent<VehicleGambitPathAuthoring>();
        auth.pathAsset = asset;
        auth.registry = reg;
        auth.vehicleRoot = veh.transform;
        auth.vehicleHalfExtents = new Vector3(1f, 1f, 2f);

        float clearance = auth.EstimateClearance(ap);
        Assert.Less(clearance, asset.narrowClearanceThreshold);
        asset.UpsertStopFromAperture(ap, clearance);
        Assert.AreEqual(1, asset.stops.Count);
        Assert.AreEqual("gate", asset.stops[0].apertureId);

        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(regGo);
        Object.DestroyImmediate(apGo);
        Object.DestroyImmediate(veh);
    }

    [Test]
    public void GambitPhysicsMaterialAdvisor_SuggestsOnFail()
    {
        var tips = GambitPhysicsMaterialAdvisor.Suggest(false, 0.2f, 0.75f, 12f, GambitAdviceBias.MakeEasier);
        Assert.Greater(tips.Count, 0);
        Assert.IsTrue(tips.Exists(t => t.frictionDelta < 0f));
    }

    [Test]
    public void NarrativeChooseGambitAperture_ScanThenConfirm_Succeeds()
    {
        var root = new GameObject("gambitRoot");
        var bindings = root.AddComponent<NarrativeBindings>();
        var session = root.AddComponent<GambitSelectionSession>();
        var buffer = root.AddComponent<GambitInputTriggerBuffer>();
        var select = root.AddComponent<AngularTargetSelectMode>();
        var slow = root.AddComponent<SlowTimeController>();
        session.inputBuffer = buffer;
        session.selectMode = select;
        session.slowTime = slow;

        var apGo = new GameObject("aperture");
        var ap = apGo.AddComponent<PathingAperture>();
        ap.apertureId = "a1";
        session.candidates.Add(ap);

        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "gambit.session", value = root });
        bindings.RebuildIndex();

        var ctx = new NarrativeExecutionContext(null, bindings, null);
        var action = new NarrativeChooseGambitApertureAction
        {
            sessionKey = "gambit.session",
            requirePlayerConfirm = true
        };

        Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Running, action.Execute(ctx, null));
        buffer.Inject(GambitInputTriggerKind.MouseScan, ap);
        Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Running, action.Execute(ctx, null));
        buffer.Inject(GambitInputTriggerKind.MouseClickConfirm, ap);
        Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Success, action.Execute(ctx, null));
        Assert.AreEqual(ap, session.selectedAperture);

        slow.Exit();
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(apGo);
    }

    [Test]
    public void NarrativeChooseGambitAperture_Cancel_Fails()
    {
        var root = new GameObject("gambitRoot2");
        var bindings = root.AddComponent<NarrativeBindings>();
        var session = root.AddComponent<GambitSelectionSession>();
        var buffer = root.AddComponent<GambitInputTriggerBuffer>();
        session.inputBuffer = buffer;
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "gambit.session", value = root });
        bindings.RebuildIndex();

        var ctx = new NarrativeExecutionContext(null, bindings, null);
        var action = new NarrativeChooseGambitApertureAction { sessionKey = "gambit.session" };
        Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Running, action.Execute(ctx, null));
        buffer.Inject(GambitInputTriggerKind.MouseClickCancel, null);
        Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Failure, action.Execute(ctx, null));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void RailDeflectionEstimator_FatigueFails_AdrenalineHelps()
    {
        var tired = RailDeflectionSuccessEstimator.Estimate(new RailDeflectionSuccessEstimator.Input
        {
            remainingStairDepthNormalized = 0.8f,
            railingFriction = 0.4f,
            railingMassHint = 50f,
            nightstickImpulse = 10f,
            fatigue01 = 0.9f,
            adrenaline01 = 0f,
            strength01 = 0.6f
        });
        var pumped = RailDeflectionSuccessEstimator.Estimate(new RailDeflectionSuccessEstimator.Input
        {
            remainingStairDepthNormalized = 0.2f,
            railingFriction = 0.2f,
            railingMassHint = 30f,
            nightstickImpulse = 20f,
            fatigue01 = 0.1f,
            adrenaline01 = 0.8f,
            strength01 = 0.7f
        });
        Assert.Greater(pumped.probability, tired.probability);
        Assert.IsTrue(pumped.likelySuccess);
    }

    [Test]
    public void RailDingRadialCache_PrebakeAndLookup()
    {
        var cache = ScriptableObject.CreateInstance<RailDingRadialCache>();
        cache.azimuthBins = 4;
        cache.listenerBands = 2;
        cache.PrebakeRailing("rail_3");
        Assert.IsTrue(cache.TryGet("rail_3", 0, 0, out var e));
        Assert.IsTrue(e.binarySampleId.Contains("rail_3"));
        Object.DestroyImmediate(cache);
    }

    [Test]
    public void StairwellDirector_DeflectAdvancesCursor()
    {
        var topo = ScriptableObject.CreateInstance<StairwellTopologyAsset>();
        topo.floors = new List<StairwellFloorLanding>
        {
            new StairwellFloorLanding { floorIndex = 2, railingIds = new List<string> { "r2" } },
            new StairwellFloorLanding { floorIndex = 1, railingIds = new List<string> { "r1" } }
        };
        var go = new GameObject("dir");
        var dir = go.AddComponent<StairwellNightstickFishDirector>();
        var fatigue = go.AddComponent<MuscularFatigueAdrenalineState>();
        fatigue.strength01 = 1f;
        fatigue.fatigue01 = 0f;
        fatigue.adrenaline01 = 0.5f;
        dir.topology = topo;
        dir.actorState = fatigue;

        var r2 = new GameObject("r2");
        var n2 = r2.AddComponent<StairwellRailingNode>();
        n2.railingId = "r2";
        n2.floorIndex = 2;
        n2.manifoldFriction = 0.1f;
        n2.massHint = 20f;
        var r1 = new GameObject("r1");
        var n1 = r1.AddComponent<StairwellRailingNode>();
        n1.railingId = "r1";
        n1.floorIndex = 1;
        n1.manifoldFriction = 0.1f;
        n1.massHint = 20f;
        dir.railings = new List<StairwellRailingNode> { n2, n1 };

        dir.Begin();
        dir.NotifyCopsDown();
        Assert.AreEqual(StairwellFishPhase.DescendDeflect, dir.phase);
        Assert.IsTrue(dir.TryDeflectCurrent(30f));
        Assert.AreEqual(1, dir.railingCursor);

        Object.DestroyImmediate(topo);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(r2);
        Object.DestroyImmediate(r1);
    }
}
#endif
