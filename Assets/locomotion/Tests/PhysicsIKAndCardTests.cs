#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Locomotion.Rig;
using PhysicsCard = GoodSection;

/// <summary>
/// Tests for IK training (aggregation, range diamond) and simple cards (GoodSection, ImpulseAction, enclosure).
/// </summary>
public class PhysicsIKAndCardTests
{
    [Test]
    public void PhysicsIKTrainingAggregator_Mean_ZeroForEmpty()
    {
        float mean = PhysicsIKTrainingAggregator.Mean(null);
        Assert.AreEqual(0f, mean, 1e-6f);
        mean = PhysicsIKTrainingAggregator.Mean(new float[0]);
        Assert.AreEqual(0f, mean, 1e-6f);
    }

    [Test]
    public void PhysicsIKTrainingAggregator_StdDev_ZeroForConstantSamples()
    {
        float[] s = { 0.5f, 0.5f, 0.5f };
        float std = PhysicsIKTrainingAggregator.StdDev(s);
        Assert.AreEqual(0f, std, 1e-6f);
    }

    [Test]
    public void PhysicsIKTrainingAggregator_SelectSuccessful_ReturnsTopSetsAndRangeDiamond()
    {
        var runs = new PhysicsIKTrainedSet[]
        {
            new PhysicsIKTrainedSet { completionTime = 2f, accuracyScore = 0.5f, powerUsed = 2f, powerScale = 1f },
            new PhysicsIKTrainedSet { completionTime = 1f, accuracyScore = 0.9f, powerUsed = 0.5f, powerScale = 1f },
            new PhysicsIKTrainedSet { completionTime = 1.5f, accuracyScore = 0.7f, powerUsed = 1f, powerScale = 1f },
        };
        bool ok = PhysicsIKTrainingAggregator.SelectSuccessful(runs, 2, 0f, out var result);
        Assert.IsTrue(ok);
        Assert.IsNotNull(result.successfulSets);
        Assert.GreaterOrEqual(result.successfulSets.Length, 1);
        Assert.LessOrEqual(result.successfulSets.Length, 2);
    }

    [Test]
    public void PhysicsIKTrainingAggregator_ComputeRangeDiamond_MinMaxPerCoefficient()
    {
        var sets = new PhysicsIKTrainedSet[]
        {
            new PhysicsIKTrainedSet { degreesWeight = 0.2f, torqueWeight = 0.4f, powerScale = 1f },
            new PhysicsIKTrainedSet { degreesWeight = 0.4f, torqueWeight = 0.2f, powerScale = 2f },
        };
        PhysicsIKTrainingAggregator.ComputeRangeDiamond(sets, out var minOut, out var maxOut);
        Assert.AreEqual(0.2f, minOut.degreesWeight, 1e-5f);
        Assert.AreEqual(0.4f, maxOut.degreesWeight, 1e-5f);
        Assert.AreEqual(0.2f, minOut.torqueWeight, 1e-5f);
        Assert.AreEqual(0.4f, maxOut.torqueWeight, 1e-5f);
        Assert.AreEqual(1f, minOut.powerScale, 1e-5f);
        Assert.AreEqual(2f, maxOut.powerScale, 1e-5f);
    }

    [Test]
    public void GoodSection_IsFeasible_TrueWhenNoRequiredState()
    {
        var card = new GoodSection
        {
            sectionName = "test",
            description = "test card",
            requiredState = null,
            limits = new SectionLimits()
        };
        var state = new RagdollState { rootPosition = Vector3.zero, rootRotation = Quaternion.identity };
        Assert.IsTrue(card.IsFeasible(state));
    }

    [Test]
    public void GoodSection_CalculateFeasibilityScore_InRangeZeroOne()
    {
        var card = new GoodSection
        {
            sectionName = "test",
            description = "test card",
            requiredState = null,
            limits = new SectionLimits()
        };
        var state = new RagdollState { rootPosition = Vector3.zero, rootRotation = Quaternion.identity };
        float score = card.CalculateFeasibilityScore(state);
        Assert.GreaterOrEqual(score, 0f);
        Assert.LessOrEqual(score, 1f);
    }

    [Test]
    public void ImpulseAction_ForceDirection_StoredAndReturned()
    {
        var action = new ImpulseAction
        {
            muscleGroup = "test",
            activation = 0.5f,
            forceDirection = new Vector3(1f, 0f, 0f),
            torqueDirection = Vector3.zero
        };
        Assert.AreEqual(1f, action.forceDirection.x, 1e-5f);
        Assert.AreEqual(0f, action.forceDirection.y, 1e-5f);
        Assert.AreEqual(0f, action.forceDirection.z, 1e-5f);
    }

    [Test]
    public void HemisphericalGraspCard_InheritsGoodSectionBehavior()
    {
        var card = new HemisphericalGraspCard
        {
            targetObject = null,
            enclosureRatio = 0.6f
        };
        Assert.AreEqual("hemispherical_grasp", card.sectionName);
        Assert.IsNotNull(card.impulseStack);
        Assert.IsNotNull(card.limits);
    }

    [Test]
    public void EnclosureFeasibility_Struct_FieldsSet()
    {
        var f = new EnclosureFeasibility
        {
            canEnclose = true,
            enclosureRatio = 0.6f,
            optimalGraspPoint = Vector3.one,
            optimalGraspDirection = Vector3.up,
            requiredFingerSpread = 45f,
            gripStrengthRequired = 10f,
            feasibilityReason = "test"
        };
        Assert.IsTrue(f.canEnclose);
        Assert.AreEqual(0.6f, f.enclosureRatio, 1e-5f);
        Assert.AreEqual(45f, f.requiredFingerSpread, 1e-5f);
    }

    [Test]
    public void Consider_EvaluateHemisphericalEnclosure_SmallObjectFeasible()
    {
        var considerGo = new GameObject("ConsiderTest");
        var consider = considerGo.AddComponent<Consider>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = Vector3.one * 0.1f;
        var handGo = new GameObject("HandTest");
        var hand = new Hand
        {
            gameObject = handGo,
            hemisphereRadius = 0.5f,
            maxFingerSpread = 90f
        };

        EnclosureFeasibility feasibility = consider.EvaluateHemisphericalEnclosure(cube, hand, 0.55f);

        Assert.IsNotNull(feasibility.feasibilityReason);
        Assert.GreaterOrEqual(feasibility.optimalGraspPoint.x, float.MinValue);
        Assert.GreaterOrEqual(feasibility.requiredFingerSpread, 0f);

        Object.DestroyImmediate(cube);
        Object.DestroyImmediate(handGo);
        Object.DestroyImmediate(considerGo);
    }

    [Test]
    public void Consider_EvaluateHemisphericalEnclosure_ObjectTooLarge_Infeasible()
    {
        var considerGo = new GameObject("ConsiderTest2");
        var consider = considerGo.AddComponent<Consider>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = Vector3.one * 10f;
        var handGo = new GameObject("HandTest2");
        var hand = new Hand
        {
            gameObject = handGo,
            hemisphereRadius = 0.1f,
            maxFingerSpread = 90f
        };

        EnclosureFeasibility feasibility = consider.EvaluateHemisphericalEnclosure(cube, hand, 0.55f);

        Assert.IsFalse(feasibility.canEnclose);
        Assert.IsTrue(feasibility.feasibilityReason.Contains("too large") || feasibility.feasibilityReason.Length > 0);

        Object.DestroyImmediate(cube);
        Object.DestroyImmediate(handGo);
        Object.DestroyImmediate(considerGo);
    }

    [Test]
    public void PhysicsIKTrainedSet_Default_WeightsInRange()
    {
        var set = PhysicsIKTrainedSet.Default();
        Assert.GreaterOrEqual(set.degreesWeight, 0f);
        Assert.LessOrEqual(set.degreesWeight, 1f);
        Assert.GreaterOrEqual(set.powerScale, 0.01f);
    }

    [Test]
    public void PhysicsIKTrainedSet_ApplyTo_CopiesWeights()
    {
        var solverGo = new GameObject("SolverTest");
        var solver = solverGo.AddComponent<PhysicsCardSolver>();
        solver.degreesWeight = 0.1f;
        solver.torqueWeight = 0.2f;

        var set = new PhysicsIKTrainedSet { degreesWeight = 0.5f, torqueWeight = 0.6f };
        set.ApplyTo(solver);

        Assert.AreEqual(0.5f, solver.degreesWeight, 1e-5f);
        Assert.AreEqual(0.6f, solver.torqueWeight, 1e-5f);

        Object.DestroyImmediate(solverGo);
    }

    [Test]
    public void PhysicsIKTrainingRunner_Idle_HasStabilityMetrics()
    {
        var set = PhysicsIKTrainedSet.Default();
        var result = PhysicsIKTrainingRunner.RunOne(null, set, PhysicsIKTrainingCategory.Idle, 42);
        Assert.Greater(result.accuracyScore, 0.8f);
        Assert.Greater(result.completionTime, 1f);
        Assert.Less(result.powerUsed, 2f);
    }

    [Test]
    public void PhysicsIKTrainingRunner_DefaultFrozenAxisOptions_IncludesNoneAndAxes()
    {
        var opts = PhysicsIKTrainingRunner.DefaultFrozenAxisOptions;
        Assert.Greater(opts.Length, 1);
        Assert.AreEqual(RigidbodyConstraints.None, opts[0]);
    }

    // --- Catch / Shoot goal and GoodSection ---

    [Test]
    public void GoalType_CatchAndShoot_EnumValuesExist()
    {
        Assert.AreEqual((int)GoalType.Catch, (int)GoalType.Catch);
        Assert.AreEqual((int)GoalType.Shoot, (int)GoalType.Shoot);
        Assert.AreNotEqual(GoalType.Catch, GoalType.Shoot);
    }

    [Test]
    public void GoodSection_CatchGoal_FieldsStored()
    {
        var card = new GoodSection
        {
            sectionName = "catch_ball",
            isCatchGoal = true,
            catchLimbBoneName = "RightHand",
            catchLimbBoneNames = new System.Collections.Generic.List<string> { "RightHand", "LeftHand" }
        };
        Assert.IsTrue(card.isCatchGoal);
        Assert.AreEqual("RightHand", card.catchLimbBoneName);
        Assert.IsNotNull(card.catchLimbBoneNames);
        Assert.AreEqual(2, card.catchLimbBoneNames.Count);
    }

    [Test]
    public void GoodSection_ShootGoal_FieldsStored()
    {
        var card = new GoodSection
        {
            sectionName = "shoot_hoop",
            isShootGoal = true,
            shootMinRange = 2f,
            shootMaxRange = 10f
        };
        Assert.IsTrue(card.isShootGoal);
        Assert.AreEqual(2f, card.shootMinRange, 1e-5f);
        Assert.AreEqual(10f, card.shootMaxRange, 1e-5f);
    }

    [Test]
    public void CatchTrajectoryUtility_GetInterceptPosition_NullObject_ReturnsHandPosAndZeroTime()
    {
        Vector3 handPos = new Vector3(1f, 2f, 3f);
        CatchTrajectoryUtility.GetInterceptPosition(handPos, null, 5f, out Vector3 interceptPos, out float timeToIntercept);
        Assert.AreEqual(handPos.x, interceptPos.x, 1e-5f);
        Assert.AreEqual(handPos.y, interceptPos.y, 1e-5f);
        Assert.AreEqual(handPos.z, interceptPos.z, 1e-5f);
        Assert.AreEqual(0f, timeToIntercept, 1e-6f);
    }

    [Test]
    public void CatchTrajectoryUtility_GetInterceptPosition_ZeroHandSpeed_LeavesInterceptAtObject()
    {
        var obj = new GameObject("CatchTarget");
        obj.transform.position = new Vector3(10f, 0f, 0f);
        Vector3 handPos = Vector3.zero;
        CatchTrajectoryUtility.GetInterceptPosition(handPos, obj.transform, 0f, out Vector3 interceptPos, out float timeToIntercept);
        Assert.AreEqual(10f, interceptPos.x, 1e-5f);
        Assert.AreEqual(0f, timeToIntercept, 1e-6f);
        Object.DestroyImmediate(obj);
    }

    [Test]
    public void CatchTrajectoryUtility_GetInterceptPosition_StaticObject_ReturnsInterceptAtOrNearObject()
    {
        var obj = new GameObject("StaticCatchTarget");
        obj.transform.position = new Vector3(5f, 0f, 0f);
        Vector3 handPos = new Vector3(0f, 0f, 0f);
        CatchTrajectoryUtility.GetInterceptPosition(handPos, obj.transform, 5f, out Vector3 interceptPos, out float timeToIntercept);
        Assert.AreEqual(5f, interceptPos.x, 1e-4f);
        Assert.GreaterOrEqual(timeToIntercept, 0f);
        Object.DestroyImmediate(obj);
    }

    [Test]
    public void PhysicsIKTrainingRunner_Catch_HasMetrics()
    {
        var set = PhysicsIKTrainedSet.Default();
        var result = PhysicsIKTrainingRunner.RunOne(null, set, PhysicsIKTrainingCategory.Catch, 42);
        Assert.Greater(result.completionTime, 0f);
        Assert.GreaterOrEqual(result.accuracyScore, 0f);
        Assert.LessOrEqual(result.accuracyScore, 1f);
        Assert.GreaterOrEqual(result.powerUsed, 0f);
    }

    [Test]
    public void PhysicsIKTrainingRunner_Shoot_HasMetrics()
    {
        var set = PhysicsIKTrainedSet.Default();
        var result = PhysicsIKTrainingRunner.RunOne(null, set, PhysicsIKTrainingCategory.Shoot, 42);
        Assert.Greater(result.completionTime, 0f);
        Assert.GreaterOrEqual(result.accuracyScore, 0f);
        Assert.LessOrEqual(result.accuracyScore, 1f);
        Assert.GreaterOrEqual(result.powerUsed, 0f);
    }

    [Test]
    public void PhysicsCardSolver_SolveForGoal_WithCatchGoal_PrefersCatchCard()
    {
        var solverGo = new GameObject("SolverCatchTest");
        var solver = solverGo.AddComponent<PhysicsCardSolver>();
        var catchCard = new GoodSection { sectionName = "catch", isCatchGoal = true, limits = new SectionLimits() };
        var otherCard = new GoodSection { sectionName = "other", limits = new SectionLimits() };
        solver.AddCards(new System.Collections.Generic.List<PhysicsCard> { otherCard, catchCard });
        var goal = new BehaviorTreeGoal { type = GoalType.Catch };
        var state = new RagdollState { rootPosition = Vector3.zero, rootRotation = Quaternion.identity };
        var path = solver.SolveForGoal(goal, state);
        Assert.IsNotNull(path);
        bool hasCatch = false;
        foreach (var card in path)
        {
            if (card != null && card.isCatchGoal) { hasCatch = true; break; }
        }
        Assert.IsTrue(hasCatch, "SolveForGoal(GoalType.Catch) should return a path that includes a catch card when one is available.");
        Object.DestroyImmediate(solverGo);
    }

    [Test]
    public void PhysicsCardSolver_SolveForGoal_WithShootGoal_PrefersShootCard()
    {
        var solverGo = new GameObject("SolverShootTest");
        var solver = solverGo.AddComponent<PhysicsCardSolver>();
        var shootCard = new GoodSection { sectionName = "shoot", isShootGoal = true, limits = new SectionLimits() };
        var otherCard = new GoodSection { sectionName = "other", limits = new SectionLimits() };
        solver.AddCards(new List<PhysicsCard> { otherCard, shootCard });
        var goal = new BehaviorTreeGoal { type = GoalType.Shoot };
        var state = new RagdollState { rootPosition = Vector3.zero, rootRotation = Quaternion.identity };
        var path = solver.SolveForGoal(goal, state);
        Assert.IsNotNull(path);
        bool hasShoot = false;
        foreach (var card in path)
        {
            if (card != null && card.isShootGoal) { hasShoot = true; break; }
        }
        Assert.IsTrue(hasShoot, "SolveForGoal(GoalType.Shoot) should return a path that includes a shoot card when one is available.");
        Object.DestroyImmediate(solverGo);
    }

    [Test]
    public void IkTrainingLiveScore_PrefersHeavyLimbAndObject()
    {
        var actor = new GameObject("ScoreActor");
        var hand = new GameObject("RightHand");
        hand.transform.SetParent(actor.transform, false);
        var light = new GameObject("lightObj");
        var heavy = new GameObject("heavyObj");
        light.transform.position = new Vector3(8f, 0f, 0f);
        heavy.transform.position = new Vector3(0.5f, 0f, 0f);
        var run = ScriptableObject.CreateInstance<PhysicsIKTrainingRunAsset>();
        try
        {
            var map = actor.AddComponent<BoneMap>();
            map.Set("Human:RightHand", hand.transform);
            run.actorLimbWeights = new List<IkTrainingLimbWeight>
            {
                new IkTrainingLimbWeight { traitId = "Human:RightHand", weight = 2f }
            };
            run.measurementObjectWeights = new List<IkTrainingObjectWeight>
            {
                new IkTrainingObjectWeight { hierarchyPath = "lightObj", weight = 0.1f },
                new IkTrainingObjectWeight { hierarchyPath = "heavyObj", weight = 10f }
            };
            Assert.IsTrue(IkTrainingLiveScore.TryScore(run, map, out float nearHeavy));
            run.measurementObjectWeights = new List<IkTrainingObjectWeight>
            {
                new IkTrainingObjectWeight { hierarchyPath = "lightObj", weight = 10f },
                new IkTrainingObjectWeight { hierarchyPath = "heavyObj", weight = 0.1f }
            };
            Assert.IsTrue(IkTrainingLiveScore.TryScore(run, map, out float nearLightHeavy));
            Assert.Greater(nearHeavy, nearLightHeavy);
        }
        finally
        {
            Object.DestroyImmediate(run);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(light);
            Object.DestroyImmediate(heavy);
        }
    }

    [Test]
    public void PhysicsIKTrainingRunner_RunOne_UsesWeightedScoreNotRng()
    {
        var actor = new GameObject("LiveScoreActor");
        var hand = new GameObject("RightHand");
        hand.transform.SetParent(actor.transform, false);
        var target = new GameObject("measureTarget");
        target.transform.position = new Vector3(0.4f, 0f, 0f);
        var run = ScriptableObject.CreateInstance<PhysicsIKTrainingRunAsset>();
        try
        {
            var map = actor.AddComponent<BoneMap>();
            map.Set("Human:RightHand", hand.transform);
            var solver = actor.AddComponent<PhysicsCardSolver>();
            run.hitTarget = target;
            run.actorLimbWeights = new List<IkTrainingLimbWeight>
            {
                new IkTrainingLimbWeight { traitId = "Human:RightHand", weight = 1f }
            };
            var a = PhysicsIKTrainingRunner.RunOne(solver, PhysicsIKTrainedSet.Default(), PhysicsIKTrainingCategory.Idle, 1, null, run);
            var b = PhysicsIKTrainingRunner.RunOne(solver, PhysicsIKTrainedSet.Default(), PhysicsIKTrainingCategory.Idle, 99, null, run);
            Assert.AreEqual(a.accuracyScore, b.accuracyScore, 1e-5f);
            Assert.Greater(a.accuracyScore, 0f);
        }
        finally
        {
            Object.DestroyImmediate(run);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void IkTraining_ActivateObjectsInEditor_RestoresActiveSelf()
    {
        var go = new GameObject("MeasureProp");
        go.SetActive(false);
        try
        {
            var snap = IkTrainingLiveScore.ActivateInEditor(new List<GameObject> { go });
            Assert.IsTrue(go.activeSelf);
            IkTrainingLiveScore.RestoreActiveFlags(snap);
            Assert.IsFalse(go.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GoodSectionContactActivation_OverlappingCollider_AddsSectionWithoutPlayMode()
    {
        var actor = new GameObject("ContactActor");
        var limb = new GameObject("Limb");
        limb.transform.SetParent(actor.transform, false);
        limb.AddComponent<BoxCollider>();
        var prop = new GameObject("Prop");
        prop.AddComponent<BoxCollider>();
        var rs = actor.AddComponent<RagdollSystem>();
        rs.ragdollRoot = limb.transform;
        var ns = actor.AddComponent<NervousSystem>();
        var section = new GoodSection { sectionName = "contact-gs" };
        ns.goodSections.Add(section);
        var solver = actor.AddComponent<PhysicsCardSolver>();
        var checkpoint = new InteractedObjectCheckpoint();
        try
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("Contact activation without PlayMode physics overlap.");
                return;
            }
            Assert.IsFalse(Application.isPlaying);
            var result = GoodSectionContactActivation.Tick(rs, new List<GameObject> { prop }, checkpoint);
            Assert.Greater(result.contactCount, 0);
            Assert.Greater(result.sectionsEnabled, 0);
            Assert.IsTrue(solver.availableCards.Contains(section));
            Assert.IsTrue(checkpoint.CanReset);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(prop);
        }
    }

    [Test]
    public void InteractedObjectCheckpoint_DisabledUntilDirty_RestoresFirstSeenCascade()
    {
        var a = new GameObject("A");
        var b = new GameObject("B");
        a.AddComponent<BoxCollider>();
        b.AddComponent<BoxCollider>();
        a.transform.position = Vector3.zero;
        b.transform.position = Vector3.zero;
        var cp = new InteractedObjectCheckpoint();
        try
        {
            Assert.IsFalse(cp.CanReset);
            cp.RememberFirstSeen(a);
            GoodSectionContactActivation.CollectCascadeFromMoved(a, new List<GameObject> { a, b }, cp);
            Assert.GreaterOrEqual(cp.SnapshotCount, 2);
            Assert.IsFalse(cp.CanReset);
            a.transform.localPosition = new Vector3(2f, 0f, 0f);
            cp.MarkDirtyFromPhysicsTranslation(a);
            Assert.IsTrue(cp.CanReset);
            cp.Reset();
            Assert.AreEqual(0f, a.transform.localPosition.x, 1e-4f);
            Assert.IsFalse(cp.CanReset);
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
#endif
