using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional multi-leg timeline search over landmark chord samples (TimelineMultiModalPlanner).
/// When disabled, GenericTraversibilityPlannerSolver.BuildPlan uses the legacy greedy single-leg pipeline.
/// </summary>
[Serializable]
public struct PlannerTimelineOptions
{
    public const int MaxChordSamples = 24;

    [Tooltip("When true, builds a landmark graph and runs shortest-path (Dijkstra) over Walk/Fly/Drive legs before falling back to greedy tool/acrobatics.")]
    public bool enableMultiLegTimelineSearch;

    [Tooltip("When true, hintEffectiveness01 is taken from hintDifficulty preset instead of the manual slider.")]
    public bool useDifficultyPresetForHints;

    public PlannerHintDifficulty hintDifficulty;

    [Range(0f, 1f)]
    [Tooltip("Scales hint-derived cost adjustments (0 = ignore hint bias, 1 = full). Ignored when useDifficultyPresetForHints is true.")]
    public float hintEffectiveness01;

    [Min(0.1f)]
    public float minDriveLegLength;

    [Min(0.1f)]
    public float minFlyLegLength;

    [Min(0.05f)]
    public float walkSpeed;

    [Min(0.05f)]
    public float driveSpeed;

    [Min(0.05f)]
    public float flySpeed;

    [Min(0f)]
    public float modeChangePenaltySec;

    [Min(0f)]
    public float distanceWeight;

    [Range(0, MaxChordSamples)]
    [Tooltip("Interior samples along start-goal chord (0 = only endpoints as graph nodes).")]
    public int chordSampleCount;

    [NonSerialized]
    public IReadOnlyList<Vector3> extraLandmarks;

    public static PlannerTimelineOptions DefaultLegacy()
    {
        return new PlannerTimelineOptions
        {
            enableMultiLegTimelineSearch = false,
            useDifficultyPresetForHints = false,
            hintEffectiveness01 = 1f,
            minDriveLegLength = 2f,
            minFlyLegLength = 2f,
            walkSpeed = 1.4f,
            driveSpeed = 8f,
            flySpeed = 15f,
            modeChangePenaltySec = 2f,
            distanceWeight = 0.02f,
            chordSampleCount = 6
        };
    }

    public float GetEffectiveHintEffectiveness()
    {
        if (useDifficultyPresetForHints)
        {
            switch (hintDifficulty)
            {
                case PlannerHintDifficulty.Easy:
                    return 1f;
                case PlannerHintDifficulty.Medium:
                    return 0.55f;
                case PlannerHintDifficulty.Hard:
                    return 0f;
                default:
                    return Mathf.Clamp01(hintEffectiveness01);
            }
        }

        return Mathf.Clamp01(hintEffectiveness01);
    }
}
