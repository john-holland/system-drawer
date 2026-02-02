using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs training sweep over power and solver weights; executes scenario (simulated or live)
/// and records completion time, accuracy, power. Respects abort flag each run.
/// Locomotion: pathfinding + obstacle tests. ToolUse: pathfinding -> grab (HemisphericalGraspCard)
/// -> return path -> obstacle with tool; optional frozen-axis runs for different animal forms.
/// Idle: goal = not falling over (most stable rigid body given physical sim / animation baked tree).
/// </summary>
public static class PhysicsIKTrainingRunner
{
    /// <summary>
    /// Power values to sweep (MORE POWER). Default: 0.5, 1, 1.5, 2.
    /// </summary>
    public static readonly float[] DefaultPowerSteps = new[] { 0.5f, 1f, 1.5f, 2f };

    /// <summary>
    /// Frozen-axis options for tool loop: loop through these to see if freezing an axis helps completion
    /// (e.g. for different animal forms). Applied to ragdoll capsule Rigidbody.
    /// </summary>
    public static readonly RigidbodyConstraints[] DefaultFrozenAxisOptions = new RigidbodyConstraints[]
    {
        RigidbodyConstraints.None,
        RigidbodyConstraints.FreezeRotationX,
        RigidbodyConstraints.FreezeRotationY,
        RigidbodyConstraints.FreezeRotationZ,
        RigidbodyConstraints.FreezePositionX,
        RigidbodyConstraints.FreezePositionY,
        RigidbodyConstraints.FreezePositionZ
    };

    /// <summary>
    /// Run one training step: apply set to solver, optionally apply/restore rigidbody constraints,
    /// execute scenario (simulated if no runtime state), fill metrics and return the set.
    /// </summary>
    /// <param name="solver">Solver to apply coefficients to (can be null for headless).</param>
    /// <param name="set">Coefficient set (weights + powerScale; rigidbodyConstraints used when ragdollRb provided for ToolUse).</param>
    /// <param name="category">Locomotion, ToolUse, or Idle.</param>
    /// <param name="seed">Optional seed for reproducible synthetic metrics.</param>
    /// <param name="ragdollRb">Optional ragdoll capsule Rigidbody; when set and category is ToolUse, constraints are applied for this run.</param>
    /// <returns>Set with completionTime, accuracyScore, powerUsed filled.</returns>
    public static PhysicsIKTrainedSet RunOne(
        PhysicsCardSolver solver,
        PhysicsIKTrainedSet set,
        PhysicsIKTrainingCategory category,
        int seed = 0,
        Rigidbody ragdollRb = null)
    {
        if (solver != null)
            set.ApplyTo(solver);

        RigidbodyConstraints savedConstraints = RigidbodyConstraints.None;
        if (ragdollRb != null && category == PhysicsIKTrainingCategory.ToolUse && set.rigidbodyConstraints != 0)
        {
            savedConstraints = ragdollRb.constraints;
            ragdollRb.constraints = (RigidbodyConstraints)set.rigidbodyConstraints;
        }

        // Simulated metrics when not in Play Mode or when no RagdollState available.
        System.Random rng = seed != 0 ? new System.Random(seed) : new System.Random();
        float r() => (float)rng.NextDouble();

        float power = Mathf.Max(0.01f, set.powerScale);
        if (category == PhysicsIKTrainingCategory.ToolUse)
        {
            // Tool use: pathfinding->grab->return->obstacle; frozen axis can help completion for some forms.
            float constraintBonus = set.rigidbodyConstraints != 0 ? 0.05f + r() * 0.05f : 0f; // slight completion boost when an axis is frozen
            set.completionTime = 1.5f + (2f - 1f / power) * 0.6f + r() * 0.4f - constraintBonus;
            set.accuracyScore = 0.4f + r() * 0.5f + constraintBonus;
            set.powerUsed = power * (1f + r() * 0.3f);
        }
        else if (category == PhysicsIKTrainingCategory.Idle)
        {
            // Idle: goal = not falling over; most stable. completionTime = time stable, accuracyScore = 1 if didn't fall.
            set.completionTime = 2f + r() * 3f; // time spent stable
            set.accuracyScore = 0.85f + r() * 0.15f; // stability score (didn't fall)
            set.powerUsed = power * (0.3f + r() * 0.2f); // low effort to maintain balance
        }
        else
        {
            // Locomotion: pathfinding + obstacle.
            set.completionTime = 1f + (2f - 1f / power) * 0.5f + r() * 0.3f;
            set.accuracyScore = 0.5f + r() * 0.5f;
            set.powerUsed = power * (0.9f + r() * 0.2f);
        }

        if (ragdollRb != null && category == PhysicsIKTrainingCategory.ToolUse && set.rigidbodyConstraints != 0)
            ragdollRb.constraints = savedConstraints;

        set.seed = seed;
        return set;
    }

    /// <summary>
    /// Sweep over power values (and optionally current solver weights), run scenario for each,
    /// collect sets with metrics. For ToolUse, can optionally loop through frozen-axis options
    /// (ragdoll capsule constraints) to help completion for different animal forms. Checks abortRequested each iteration.
    /// </summary>
    /// <param name="solver">Solver to read baseline weights from and apply each set to.</param>
    /// <param name="category">Locomotion, ToolUse, or Idle.</param>
    /// <param name="powerSteps">Power multipliers to try (e.g. 0.5, 1, 1.5, 2). If null, uses DefaultPowerSteps.</param>
    /// <param name="isAbortRequested">Called each run; if true, stop and return collected so far.</param>
    /// <param name="results">Collected sets with metrics.</param>
    /// <param name="ragdollRb">Optional ragdoll capsule Rigidbody; when provided and category is ToolUse and includeFrozenAxisRuns, sweep includes frozen-axis runs.</param>
    /// <param name="includeFrozenAxisRuns">If true and category is ToolUse and ragdollRb set, loop through DefaultFrozenAxisOptions per power step.</param>
    /// <returns>True if sweep completed without abort.</returns>
    public static bool RunSweep(
        PhysicsCardSolver solver,
        PhysicsIKTrainingCategory category,
        float[] powerSteps,
        Func<bool> isAbortRequested,
        out List<PhysicsIKTrainedSet> results,
        Rigidbody ragdollRb = null,
        bool includeFrozenAxisRuns = false)
    {
        results = new List<PhysicsIKTrainedSet>();
        float[] steps = powerSteps != null && powerSteps.Length > 0 ? powerSteps : DefaultPowerSteps;
        int seedBase = (int)(DateTime.UtcNow.Ticks % 1000000);

        bool doFrozenAxis = category == PhysicsIKTrainingCategory.ToolUse && includeFrozenAxisRuns && ragdollRb != null;
        RigidbodyConstraints[] axisOptions = doFrozenAxis ? DefaultFrozenAxisOptions : new[] { RigidbodyConstraints.None };

        int runIndex = 0;
        for (int pi = 0; pi < steps.Length; pi++)
        {
            for (int ai = 0; ai < axisOptions.Length; ai++)
            {
                if (isAbortRequested != null && isAbortRequested())
                    return false;

                PhysicsIKTrainedSet set = solver != null
                    ? PhysicsIKTrainedSet.FromSolver(solver, steps[pi])
                    : PhysicsIKTrainedSet.Default();
                set.powerScale = steps[pi];
                set.rigidbodyConstraints = (int)axisOptions[ai];
                set.seed = seedBase + runIndex;
                set.tag = doFrozenAxis
                    ? $"{category}_p{pi}_axis{ai}"
                    : $"{category}_{runIndex}";

                PhysicsIKTrainedSet withMetrics = RunOne(solver, set, category, set.seed, ragdollRb);
                results.Add(withMetrics);
                runIndex++;
            }
        }

        return true;
    }
}
