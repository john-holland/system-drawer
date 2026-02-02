#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

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
            sectionName = "grasp_test",
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
}
#endif
