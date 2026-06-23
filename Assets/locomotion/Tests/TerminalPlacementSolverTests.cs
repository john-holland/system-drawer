#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TerminalPlacementSolverTests
{
    [Test]
    public void SlotZeroAnchor_MatchesPlacementSlotConfig()
    {
        var bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f));
        var cfg = new PlacementSlotConfig
        {
            fitX = PlacementFitX.Center,
            fitY = PlacementFitY.Center,
            fitZ = PlacementFitZ.Center
        };
        Assert.IsTrue(PlacementSlotConfig.ComputeSlotCenter3D(
            bounds, new Vector3(4f, 2f, 4f), new Vector3(2f, 1f, 2f), 0, cfg, out Vector3 center));
        Assert.AreEqual(Vector3.zero, center);
    }

    [Test]
    public void TravelLegModeExtensions_IsTerminalLeg()
    {
        Assert.IsTrue(TravelLegModeExtensions.IsTerminalLeg(TravelLegMode.Park));
        Assert.IsTrue(TravelLegModeExtensions.IsTerminalLeg(TravelLegMode.ParkWater));
        Assert.IsFalse(TravelLegModeExtensions.IsTerminalLeg(TravelLegMode.Walk));
    }

    [Test]
    public void ActorPhysicalCentroid_InferDefaultTerminalLeg_Aquaplane()
    {
        var go = new GameObject("boat");
        var actor = go.AddComponent<BaseAmbulatingActor>();
        go.AddComponent<VehicleAquaplaneSolver>();
        Assert.IsTrue(ActorPhysicalCentroid.TryBuildProfile(actor, out ActorPhysicalProfile profile));
        Assert.AreEqual(TravelLegMode.ParkWater, profile.defaultTerminalLeg);
        Assert.IsTrue(profile.hasAquaplaneSolver);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ResolveTerminalLegFromZone_WaterPlaningPark()
    {
        var profile = new ActorPhysicalProfile { hasAquaplaneSolver = true, defaultTerminalLeg = TravelLegMode.ParkWater };
        TravelLegMode leg = ActorPhysicalCentroid.ResolveTerminalLegFromZone(
            TravelLegMode.Walk, TerminalSurfaceKind.WaterPlaningPark, profile);
        Assert.AreEqual(TravelLegMode.ParkWater, leg);
    }

    [Test]
    public void MediumAllowsTerminalLeg_ParkWater_OnWater()
    {
        Assert.IsTrue(PhysicalMediumVolumeRules.MediumAllowsTerminalLeg(
            PhysicalPathingMedium.Water, TravelLegMode.ParkWater));
        Assert.IsFalse(PhysicalMediumVolumeRules.MediumAllowsTerminalLeg(
            PhysicalPathingMedium.Ground, TravelLegMode.Moor));
    }

    [Test]
    public void AquaplaneExecutor_ReachesHoldPhase()
    {
        var seg = MultiModalSegment.FromParkWater(
            new List<Vector3> { Vector3.zero, Vector3.forward * 5f },
            Vector3.forward * 10f);
        seg.terminalHoldPolicy = WaterHoldPolicy.Park;

        var bodyGo = new GameObject("hull");
        var rb = bodyGo.AddComponent<Rigidbody>();
        rb.linearVelocity = Vector3.forward * 2f;

        var exec = new AquaplaneWaterTerminalExecutor(seg, null, rb);
        BehaviorTreeStatus status = BehaviorTreeStatus.Running;
        for (int i = 0; i < 120 && status == BehaviorTreeStatus.Running; i++)
            status = exec.Tick(0.05f);

        Assert.AreEqual(AquaplaneWaterTerminalExecutor.Phase.Complete, exec.CurrentPhase);
        Object.DestroyImmediate(bodyGo);
    }

    [Test]
    public void MoorAndParkWater_DifferHoldPolicy()
    {
        var moor = MultiModalSegment.FromMoor(new List<Vector3>(), Vector3.one);
        var park = MultiModalSegment.FromParkWater(new List<Vector3>(), Vector3.one);
        Assert.AreEqual(WaterHoldPolicy.Anchor, moor.terminalHoldPolicy);
        Assert.AreEqual(WaterHoldPolicy.Park, park.terminalHoldPolicy);
    }

    [Test]
    public void AppendTerminalLegIfEnabled_Disabled_ReturnsSamePlan()
    {
        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3> { Vector3.zero, Vector3.one }));
        var solverGo = new GameObject("solver");
        var solver = solverGo.AddComponent<HierarchicalPathingSolver>();
        var profile = new ActorPhysicalProfile { defaultTerminalLeg = TravelLegMode.Park };
        var result = GenericTraversibilityPlannerSolver.AppendTerminalLegIfEnabled(
            plan, Vector3.zero, Vector3.one * 10f, solver, profile, PlannerTerminalOptions.Disabled);
        Assert.AreEqual(1, result.segments.Count);
        Object.DestroyImmediate(solverGo);
    }

    [Test]
    public void CompositeMultiModalPathNode_BuildsTerminalLegChild()
    {
        var root = new GameObject("root");
        var treeGo = new GameObject("bt");
        treeGo.transform.SetParent(root.transform);
        var tree = treeGo.AddComponent<BehaviorTree>();
        var compositeGo = new GameObject("composite");
        compositeGo.transform.SetParent(root.transform);
        var composite = compositeGo.AddComponent<CompositeMultiModalPathNode>();

        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromPark(
            new List<Vector3> { Vector3.zero, Vector3.forward * 3f },
            Vector3.forward * 5f));

        Assert.IsTrue(composite.BuildChildrenFromPlanForTests(plan, tree));
        Assert.AreEqual(1, composite.children.Count);
        var leg = composite.children[0] as TravelLegSequenceNode;
        Assert.IsNotNull(leg);
        Assert.IsTrue(leg.children != null && leg.children.Count > 0);
        Assert.IsInstanceOf<ExecuteTerminalLegNode>(leg.children[0]);

        Object.DestroyImmediate(root);
    }
}
#endif
