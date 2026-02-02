using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Aggregates training runs: z-score normalization, top-count/composite selection,
/// and range diamond (min/max per coefficient) for naturalized sampling.
/// </summary>
public static class PhysicsIKTrainingAggregator
{
    /// <summary>
    /// Result of aggregation: successful sets and optional range diamond bounds.
    /// </summary>
    public struct AggregationResult
    {
        public PhysicsIKTrainedSet[] successfulSets;
        public PhysicsIKTrainedSet rangeDiamondMin;
        public PhysicsIKTrainedSet rangeDiamondMax;
    }

    /// <summary>
    /// Compute mean of values. Returns 0 if empty.
    /// </summary>
    public static float Mean(float[] values)
    {
        if (values == null || values.Length == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < values.Length; i++) sum += values[i];
        return sum / values.Length;
    }

    /// <summary>
    /// Compute standard deviation (population). Returns 0 if empty or single sample.
    /// </summary>
    public static float StdDev(float[] values)
    {
        if (values == null || values.Length <= 1) return 0f;
        float mean = Mean(values);
        float variance = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            float d = values[i] - mean;
            variance += d * d;
        }
        variance /= values.Length;
        return Mathf.Sqrt(variance);
    }

    /// <summary>
    /// Z-score: (value - mean) / stdDev. Returns 0 if stdDev is 0.
    /// </summary>
    public static float ZScore(float value, float mean, float stdDev)
    {
        if (stdDev <= 0f) return 0f;
        return (value - mean) / stdDev;
    }

    /// <summary>
    /// For success heuristics: lower completionTime is better, higher accuracyScore is better, lower powerUsed is better.
    /// Composite = -zTime + zAccuracy - zPower (so higher composite = better).
    /// We invert time and power so "better" = higher composite.
    /// </summary>
    public static float CompositeScore(float zTime, float zAccuracy, float zPower)
    {
        return -zTime + zAccuracy - zPower;
    }

    /// <summary>
    /// Select successful runs from cohort. Normalizes by cohort std dev, then either:
    /// - composite threshold: keep runs with composite score above threshold, or
    /// - top count: keep the top K runs by composite score.
    /// Returns successful sets and computes range diamond (min/max per coefficient) from them.
    /// </summary>
    /// <param name="runs">All runs (coefficients + completionTime, accuracyScore, powerUsed).</param>
    /// <param name="topCount">If &gt; 0, keep only the top N by composite score. If 0, use compositeThreshold.</param>
    /// <param name="compositeThreshold">If topCount is 0, keep runs with composite score &gt;= this (e.g. 0 = above average).</param>
    /// <param name="result">Filled with successful sets and range diamond min/max.</param>
    /// <returns>True if any successful sets were selected.</returns>
    public static bool SelectSuccessful(
        PhysicsIKTrainedSet[] runs,
        int topCount,
        float compositeThreshold,
        out AggregationResult result)
    {
        result = default;
        result.successfulSets = Array.Empty<PhysicsIKTrainedSet>();
        if (runs == null || runs.Length == 0) return false;

        int n = runs.Length;
        float[] times = new float[n];
        float[] accuracies = new float[n];
        float[] powers = new float[n];
        for (int i = 0; i < n; i++)
        {
            times[i] = runs[i].completionTime;
            accuracies[i] = runs[i].accuracyScore;
            powers[i] = runs[i].powerUsed;
        }

        float meanTime = Mean(times);
        float meanAcc = Mean(accuracies);
        float meanPower = Mean(powers);
        float stdTime = StdDev(times);
        float stdAcc = StdDev(accuracies);
        float stdPower = StdDev(powers);

        // Avoid division by zero; use 1e-6 so z-score doesn't blow up
        if (stdTime <= 0f) stdTime = 1e-6f;
        if (stdAcc <= 0f) stdAcc = 1e-6f;
        if (stdPower <= 0f) stdPower = 1e-6f;

        var scored = new List<(PhysicsIKTrainedSet set, float composite)>();
        for (int i = 0; i < n; i++)
        {
            float zT = ZScore(runs[i].completionTime, meanTime, stdTime);
            float zA = ZScore(runs[i].accuracyScore, meanAcc, stdAcc);
            float zP = ZScore(runs[i].powerUsed, meanPower, stdPower);
            float composite = CompositeScore(zT, zA, zP);
            scored.Add((runs[i], composite));
        }

        scored.Sort((a, b) => b.composite.CompareTo(a.composite)); // descending: best first

        List<PhysicsIKTrainedSet> successful = new List<PhysicsIKTrainedSet>();
        int take = topCount > 0 ? Mathf.Min(topCount, scored.Count) : scored.Count;
        for (int i = 0; i < take; i++)
        {
            if (topCount > 0 || scored[i].composite >= compositeThreshold)
                successful.Add(scored[i].set);
        }

        if (successful.Count == 0) return false;

        result.successfulSets = successful.ToArray();
        ComputeRangeDiamond(result.successfulSets, out result.rangeDiamondMin, out result.rangeDiamondMax);
        return true;
    }

    /// <summary>
    /// Compute axis-aligned min/max per coefficient from the given sets (range diamond).
    /// Metric fields in min/max are not meaningful; only weight and powerScale are used.
    /// </summary>
    public static void ComputeRangeDiamond(
        PhysicsIKTrainedSet[] sets,
        out PhysicsIKTrainedSet minOut,
        out PhysicsIKTrainedSet maxOut)
    {
        minOut = PhysicsIKTrainedSet.Default();
        maxOut = PhysicsIKTrainedSet.Default();
        if (sets == null || sets.Length == 0) return;

        float minDeg = sets[0].degreesWeight, maxDeg = sets[0].degreesWeight;
        float minTorque = sets[0].torqueWeight, maxTorque = sets[0].torqueWeight;
        float minForce = sets[0].forceWeight, maxForce = sets[0].forceWeight;
        float minVel = sets[0].velocityWeight, maxVel = sets[0].velocityWeight;
        float minComfort = sets[0].comfortWeight, maxComfort = sets[0].comfortWeight;
        float minFeas = sets[0].feasibilityWeight, maxFeas = sets[0].feasibilityWeight;
        float minPower = sets[0].powerScale, maxPower = sets[0].powerScale;

        for (int i = 1; i < sets.Length; i++)
        {
            var s = sets[i];
            if (s.degreesWeight < minDeg) minDeg = s.degreesWeight; else if (s.degreesWeight > maxDeg) maxDeg = s.degreesWeight;
            if (s.torqueWeight < minTorque) minTorque = s.torqueWeight; else if (s.torqueWeight > maxTorque) maxTorque = s.torqueWeight;
            if (s.forceWeight < minForce) minForce = s.forceWeight; else if (s.forceWeight > maxForce) maxForce = s.forceWeight;
            if (s.velocityWeight < minVel) minVel = s.velocityWeight; else if (s.velocityWeight > maxVel) maxVel = s.velocityWeight;
            if (s.comfortWeight < minComfort) minComfort = s.comfortWeight; else if (s.comfortWeight > maxComfort) maxComfort = s.comfortWeight;
            if (s.feasibilityWeight < minFeas) minFeas = s.feasibilityWeight; else if (s.feasibilityWeight > maxFeas) maxFeas = s.feasibilityWeight;
            if (s.powerScale < minPower) minPower = s.powerScale; else if (s.powerScale > maxPower) maxPower = s.powerScale;
        }

        minOut.degreesWeight = minDeg; maxOut.degreesWeight = maxDeg;
        minOut.torqueWeight = minTorque; maxOut.torqueWeight = maxTorque;
        minOut.forceWeight = minForce; maxOut.forceWeight = maxForce;
        minOut.velocityWeight = minVel; maxOut.velocityWeight = maxVel;
        minOut.comfortWeight = minComfort; maxOut.comfortWeight = maxComfort;
        minOut.feasibilityWeight = minFeas; maxOut.feasibilityWeight = maxFeas;
        minOut.powerScale = minPower; maxOut.powerScale = maxPower;
    }

    /// <summary>
    /// Sample a coefficient set from the range diamond with optional uniform jitter for naturalized execution.
    /// </summary>
    /// <param name="minSet">Range diamond min (per coefficient).</param>
    /// <param name="maxSet">Range diamond max (per coefficient).</param>
    /// <param name="jitter">0 = use midpoint; 1 = full uniform random in [min,max].</param>
    /// <param name="seed">Optional seed for Random (0 = use time-based).</param>
    public static PhysicsIKTrainedSet SampleFromRangeDiamond(
        PhysicsIKTrainedSet minSet,
        PhysicsIKTrainedSet maxSet,
        float jitter = 0.2f,
        int seed = 0)
    {
        System.Random rng = seed != 0 ? new System.Random(seed) : new System.Random();
        float t() => (float)rng.NextDouble();
        float lerp(float a, float b) => a + (b - a) * (0.5f + (t() - 0.5f) * Mathf.Clamp01(jitter * 2f));
        return new PhysicsIKTrainedSet
        {
            degreesWeight = Mathf.Clamp01(lerp(minSet.degreesWeight, maxSet.degreesWeight)),
            torqueWeight = Mathf.Clamp01(lerp(minSet.torqueWeight, maxSet.torqueWeight)),
            forceWeight = Mathf.Clamp01(lerp(minSet.forceWeight, maxSet.forceWeight)),
            velocityWeight = Mathf.Clamp01(lerp(minSet.velocityWeight, maxSet.velocityWeight)),
            comfortWeight = Mathf.Clamp01(lerp(minSet.comfortWeight, maxSet.comfortWeight)),
            feasibilityWeight = Mathf.Clamp01(lerp(minSet.feasibilityWeight, maxSet.feasibilityWeight)),
            powerScale = Mathf.Max(0.01f, lerp(minSet.powerScale, maxSet.powerScale)),
            completionTime = 0f,
            accuracyScore = 0f,
            powerUsed = 0f,
            seed = seed,
            tag = "naturalized"
        };
    }
}
