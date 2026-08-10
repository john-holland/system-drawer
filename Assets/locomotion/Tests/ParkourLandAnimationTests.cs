#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ParkourLandAnimationTests
{
    [Test]
    public void LandImpactCurve_EvaluateAndImpactKeyframes()
    {
        var curve = new LandImpactCurve();
        curve.EnsureExampleCurve();

        Assert.Greater(curve.Evaluate(0.55f), 0.5f);
        List<LandImpactKeyframe> impacts = curve.GetImpactKeyframes();
        Assert.AreEqual(1, impacts.Count);
        Assert.IsTrue(impacts[0].isImpact);
        Assert.AreEqual(0.55f, impacts[0].time01, 0.001f);
    }

    [Test]
    public void ParkourLandAnimationDriver_CategoryForTag_And_IsLandingTag()
    {
        Assert.IsTrue(ParkourLandAnimationDriver.IsLandingTag(ParkourAnimationGroup.SpringLanding));
        Assert.IsTrue(ParkourLandAnimationDriver.IsLandingTag(ParkourAnimationGroup.FallRolls));
        Assert.IsFalse(ParkourLandAnimationDriver.IsLandingTag(ParkourAnimationGroup.WallRun));

        Assert.AreEqual(
            PhysicsIKTrainingCategory.ParkourSpringLanding,
            ParkourLandAnimationDriver.CategoryForTag(ParkourAnimationGroup.SpringLanding));
        Assert.AreEqual(
            PhysicsIKTrainingCategory.ParkourOneLegLanding,
            ParkourLandAnimationDriver.CategoryForTag(ParkourAnimationGroup.OneLegLanding));
        Assert.AreEqual(
            PhysicsIKTrainingCategory.ParkourFallRolls,
            ParkourLandAnimationDriver.CategoryForTag(ParkourAnimationGroup.FallRolls));
        Assert.IsTrue(ParkourLandAnimationDriver.IsLandingCategory(PhysicsIKTrainingCategory.ParkourSpringLanding));
    }

    [Test]
    public void TravelExecutionContext_CarriesAnimationGroupTag()
    {
        var root = new GameObject("land_ctx_root");
        try
        {
            var treeGo = new GameObject("bt");
            treeGo.transform.SetParent(root.transform);
            var tree = treeGo.AddComponent<BehaviorTree>();

            var seg = MultiModalSegment.FromAcrobatics(null, null, Vector3.zero, new Vector3(0f, 0f, 2f));
            seg.animationGroupTag = ParkourAnimationGroup.SpringLanding;

            TravelExecutionContext ctx = TravelExecutionContext.Build(
                tree, null, seg, 0, TravelLegMode.Walk, false,
                TravelLegMode.Walk, TravelLegMode.Acrobatics);

            Assert.AreEqual(ParkourAnimationGroup.SpringLanding, ctx.animationGroupTag);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PrepareLandAnimationNode_SetsGoalTypeLand_ForSpringLanding()
    {
        var root = new GameObject("prepare_land_root");
        try
        {
            var treeGo = new GameObject("bt");
            treeGo.transform.SetParent(root.transform);
            var tree = treeGo.AddComponent<BehaviorTree>();
            treeGo.AddComponent<RagdollSystem>();

            var nodeGo = new GameObject("prepare_land");
            nodeGo.transform.SetParent(treeGo.transform);
            var node = nodeGo.AddComponent<PrepareLandAnimationNode>();

            var seg = MultiModalSegment.FromAcrobatics(null, null, Vector3.zero, new Vector3(1f, 0f, 3f));
            seg.animationGroupTag = ParkourAnimationGroup.SpringLanding;
            seg.segmentEnd = new Vector3(1f, 0f, 3f);
            node.segment = seg;
            node.landDurationSeconds = 0.5f;

            BehaviorTreeStatus status = node.Execute(tree);
            Assert.AreEqual(BehaviorTreeStatus.Success, status);
            Assert.IsNotNull(tree.currentGoal);
            Assert.AreEqual(GoalType.Land, tree.currentGoal.type);
            Assert.AreEqual(seg.segmentEnd, tree.currentGoal.targetPosition);

            var driver = treeGo.GetComponent<ParkourLandAnimationDriver>();
            Assert.IsNotNull(driver);
            Assert.AreEqual(ParkourAnimationGroup.SpringLanding, driver.activeAnimationGroupTag);
            Assert.IsTrue(driver.hasLandingGoal);
            Assert.IsTrue(driver.showGizmo);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ScaleAttenuationByImpact_IncreasesWithImpact()
    {
        float low = ParkourLandAnimationDriver.ScaleAttenuationByImpact(1f, 0f);
        float high = ParkourLandAnimationDriver.ScaleAttenuationByImpact(1f, 1f);
        Assert.Less(low, high);
        Assert.AreEqual(1f, high, 0.001f);
    }

    [Test]
    public void ParkourFallTreeFactory_BuildsPrepareAndLimbPlacement()
    {
        BehaviorTree bt = ParkourFallTreeFactory.Build();
        try
        {
            Assert.IsNotNull(bt);
            Assert.IsNotNull(bt.rootNode);
            Assert.IsInstanceOf<RagdollPlayerSequenceNode>(bt.rootNode);
            Assert.IsNotNull(bt.GetComponent<ParkourLandAnimationDriver>());
            Assert.IsNotNull(bt.GetComponentInChildren<PrepareLandAnimationNode>());
            var place = bt.GetComponentInChildren<ParkourFallLimbPlacementNode>();
            Assert.IsNotNull(place);
            place.OnEnter(bt);
            Assert.AreEqual(4, place.fallCurve.limbs.Count);
            Assert.IsNotNull(place.fallCurve.limbs[0].target);
            Assert.AreEqual(ParkourAnimationGroup.FallRolls, place.animationGroupTag);
        }
        finally
        {
            Object.DestroyImmediate(bt.gameObject);
        }
    }

    [Test]
    public void ParkourFallLimbSlot_SampleLocal_BlendsOffsets()
    {
        var slot = new ParkourFallLimbSlot
        {
            startLocalOffset = Vector3.zero,
            endLocalOffset = new Vector3(0f, -1f, 1f),
            blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
        };
        Vector3 mid = slot.SampleLocal(0.5f);
        Assert.AreEqual(-0.5f, mid.y, 0.05f);
        Assert.AreEqual(0.5f, mid.z, 0.05f);
    }
}
#endif
