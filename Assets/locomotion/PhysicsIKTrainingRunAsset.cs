using System;
using UnityEngine;

/// <summary>
/// One set of solver coefficients and outcome metrics from a training run.
/// Used to persist and later apply tuned weights to PhysicsCardSolver.
/// </summary>
[Serializable]
public struct PhysicsIKTrainedSet
{
    [Header("Solver Weights")]
    [Range(0f, 1f)] public float degreesWeight;
    [Range(0f, 1f)] public float torqueWeight;
    [Range(0f, 1f)] public float forceWeight;
    [Range(0f, 1f)] public float velocityWeight;
    [Range(0f, 1f)] public float comfortWeight;
    [Range(0f, 1f)] public float feasibilityWeight;

    [Header("Power")]
    [Tooltip("Global scale for force/activation (e.g. 1 = default, 2 = MORE POWER)")]
    public float powerScale;

    [Header("Outcome Metrics")]
    public float completionTime;
    public float accuracyScore;
    public float powerUsed;

    [Header("Reproducibility")]
    public int seed;
    public string tag;

    [Header("Ragdoll Constraints (tool / animal form)")]
    [Tooltip("RigidbodyConstraints applied to ragdoll capsule during run (e.g. FreezeRotationZ). Stored as int for serialization.")]
    public int rigidbodyConstraints;

    public static PhysicsIKTrainedSet FromSolver(PhysicsCardSolver solver, float power = 1f)
    {
        if (solver == null)
            return Default();
        return new PhysicsIKTrainedSet
        {
            degreesWeight = solver.degreesWeight,
            torqueWeight = solver.torqueWeight,
            forceWeight = solver.forceWeight,
            velocityWeight = solver.velocityWeight,
            comfortWeight = solver.comfortWeight,
            feasibilityWeight = solver.feasibilityWeight,
            powerScale = power,
            completionTime = 0f,
            accuracyScore = 0f,
            powerUsed = 0f,
            seed = 0,
            tag = "",
            rigidbodyConstraints = 0
        };
    }

    public static PhysicsIKTrainedSet Default()
    {
        return new PhysicsIKTrainedSet
        {
            degreesWeight = 0.3f,
            torqueWeight = 0.3f,
            forceWeight = 0.2f,
            velocityWeight = 0.2f,
            comfortWeight = 0.3f,
            feasibilityWeight = 0.7f,
            powerScale = 1f,
            completionTime = 0f,
            accuracyScore = 0f,
            powerUsed = 0f,
            seed = 0,
            tag = "",
            rigidbodyConstraints = 0
        };
    }

    public void ApplyTo(PhysicsCardSolver solver)
    {
        if (solver == null) return;
        solver.degreesWeight = degreesWeight;
        solver.torqueWeight = torqueWeight;
        solver.forceWeight = forceWeight;
        solver.velocityWeight = velocityWeight;
        solver.comfortWeight = comfortWeight;
        solver.feasibilityWeight = feasibilityWeight;
    }

    /// <summary>Apply stored rigidbody constraints to ragdoll capsule (for tool/animal-form runs).</summary>
    public void ApplyConstraintsTo(Rigidbody rb)
    {
        if (rb == null) return;
        rb.constraints = (RigidbodyConstraints)rigidbodyConstraints;
    }
}

/// <summary>
/// Test category for IK training runs.
/// </summary>
public enum PhysicsIKTrainingCategory
{
    Locomotion,
    ToolUse,
    /// <summary>Goal: not falling over — most stable rigid body given physical sim / animation baked tree.</summary>
    Idle
}

/// <summary>
/// Asset that holds trained coefficient sets from IK animation training runs.
/// References AnimationBehaviorTree (and optionally PhysicsCardSolver); supports overwrite and append.
/// </summary>
[CreateAssetMenu(fileName = "PhysicsIKTrainingRun", menuName = "Locomotion/Physics IK Training Run Asset", order = 1)]
public class PhysicsIKTrainingRunAsset : ScriptableObject
{
    [Header("Metadata")]
    public string displayName = "IK Training Run";
    public string creationTime;

    [Header("References")]
    [Tooltip("Animation tree used for this training run")]
    public AnimationBehaviorTree animationTree;
    [Tooltip("Optional solver reference (scene context)")]
    public PhysicsCardSolver solver;
    public PhysicsIKTrainingCategory testCategory = PhysicsIKTrainingCategory.Locomotion;

    [Header("Trained Sets")]
    [Tooltip("List of coefficient sets with metrics (overwrite replaces this; append adds to it)")]
    public PhysicsIKTrainedSet[] trainedSets = Array.Empty<PhysicsIKTrainedSet>();

    [Header("Range Diamond (optional)")]
    [Tooltip("Min/max per coefficient from successful runs for naturalized sampling")]
    public PhysicsIKTrainedSet rangeDiamondMin;
    public PhysicsIKTrainedSet rangeDiamondMax;

    public void OverwriteWith(PhysicsIKTrainedSet[] sets)
    {
        trainedSets = sets != null ? (PhysicsIKTrainedSet[])sets.Clone() : Array.Empty<PhysicsIKTrainedSet>();
    }

    public void Append(PhysicsIKTrainedSet[] sets)
    {
        if (sets == null || sets.Length == 0) return;
        int oldLen = trainedSets?.Length ?? 0;
        var combined = new PhysicsIKTrainedSet[oldLen + sets.Length];
        if (oldLen > 0)
            Array.Copy(trainedSets, combined, oldLen);
        Array.Copy(sets, 0, combined, oldLen, sets.Length);
        trainedSets = combined;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(creationTime))
            creationTime = DateTime.UtcNow.ToString("o");
    }
}
