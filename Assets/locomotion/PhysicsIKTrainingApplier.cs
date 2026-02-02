using System;
using UnityEngine;

/// <summary>
/// Applies trained coefficient sets from PhysicsIKTrainingRunAsset to PhysicsCardSolver.
/// Can use a specific index, "best" (first in aggregated list), or sample from range diamond for naturalized execution.
/// </summary>
[AddComponentMenu("Locomotion/Physics IK Training Applier")]
public class PhysicsIKTrainingApplier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Training run asset containing trained sets")]
    public PhysicsIKTrainingRunAsset runAsset;
    [Tooltip("Solver to apply coefficients to (if null, finds on same GameObject)")]
    public PhysicsCardSolver solver;
    [Tooltip("Optional ragdoll capsule Rigidbody; when set, applies rigidbodyConstraints from the trained set (tool/animal form).")]
    public Rigidbody ragdollRigidbody;

    [Header("Selection")]
    [Tooltip("How to choose which set to apply")]
    public ApplyMode mode = ApplyMode.UseIndex;
    [Tooltip("Index into trainedSets when mode is UseIndex (0 = first)")]
    public int setIndex = 0;
    [Tooltip("When mode is RangeDiamond, jitter 0=midpoint, 1=full uniform (naturalized)")]
    [Range(0f, 1f)]
    public float rangeDiamondJitter = 0.2f;
    [Tooltip("Optional seed for reproducible sampling from range diamond")]
    public int seed = 0;

    public enum ApplyMode
    {
        UseIndex,
        UseBest,
        RangeDiamond
    }

    private void Awake()
    {
        if (solver == null)
            solver = GetComponent<PhysicsCardSolver>();
    }

    /// <summary>
    /// Apply the chosen set to the solver. Call at runtime or from editor.
    /// </summary>
    public void Apply()
    {
        if (runAsset == null || solver == null) return;

        PhysicsIKTrainedSet set;
        if (mode == ApplyMode.RangeDiamond)
        {
            set = PhysicsIKTrainingAggregator.SampleFromRangeDiamond(
                runAsset.rangeDiamondMin,
                runAsset.rangeDiamondMax,
                rangeDiamondJitter,
                seed != 0 ? seed : (int)(DateTime.UtcNow.Ticks % 1000000));
        }
        else if (mode == ApplyMode.UseBest)
        {
            if (runAsset.trainedSets == null || runAsset.trainedSets.Length == 0) return;
            set = runAsset.trainedSets[0];
        }
        else
        {
            if (runAsset.trainedSets == null || runAsset.trainedSets.Length == 0) return;
            int idx = Mathf.Clamp(setIndex, 0, runAsset.trainedSets.Length - 1);
            set = runAsset.trainedSets[idx];
        }

        set.ApplyTo(solver);
        if (ragdollRigidbody != null)
            set.ApplyConstraintsTo(ragdollRigidbody);
    }

    /// <summary>
    /// Static helper: apply a specific set from an asset to a solver.
    /// </summary>
    public static void ApplySet(PhysicsIKTrainingRunAsset asset, PhysicsCardSolver targetSolver, int index = 0, Rigidbody targetRagdollRb = null)
    {
        if (asset == null || targetSolver == null || asset.trainedSets == null || asset.trainedSets.Length == 0)
            return;
        int idx = Mathf.Clamp(index, 0, asset.trainedSets.Length - 1);
        var s = asset.trainedSets[idx];
        s.ApplyTo(targetSolver);
        if (targetRagdollRb != null)
            s.ApplyConstraintsTo(targetRagdollRb);
    }

    /// <summary>
    /// Static helper: sample from asset's range diamond and apply to solver.
    /// </summary>
    public static void ApplyFromRangeDiamond(PhysicsIKTrainingRunAsset asset, PhysicsCardSolver targetSolver, float jitter = 0.2f, int seed = 0, Rigidbody targetRagdollRb = null)
    {
        if (asset == null || targetSolver == null) return;
        PhysicsIKTrainedSet set = PhysicsIKTrainingAggregator.SampleFromRangeDiamond(
            asset.rangeDiamondMin,
            asset.rangeDiamondMax,
            jitter,
            seed);
        set.ApplyTo(targetSolver);
        if (targetRagdollRb != null)
            set.ApplyConstraintsTo(targetRagdollRb);
    }
}
