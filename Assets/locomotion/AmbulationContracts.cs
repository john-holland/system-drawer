using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>
/// Contracts for ambulation-aware card filtering (full-body extents within limits, not leg-only).
/// </summary>
public static class AmbulationCardClassifier
{
    /// <summary>Muscle group tokens treated as ambulation extents (lower limbs + trunk/core).</summary>
    public static readonly string[] AmbulationExtentKeywords =
    {
        "hip", "knee", "ankle", "foot", "leg", "thigh", "calf", "toe",
        "pelvis", "torso", "spine", "lumbar", "abdomen", "waist", "core", "chest", "elbow", "shoulder", "finger"
    };

    static readonly string[] ArmKeywords =
        { "arm", "hand", "wrist", "elbow", "shoulder", "finger", "thumb", "forearm", "upperarm" };

    static readonly string[] HeadKeywords = { "head", "neck", "jaw", "eye" };

    /// <summary>
    /// True if the card only activates ambulation-extent muscle groups (no arm/hand/head activations).
    /// </summary>
    public static bool IsAmbulationExtentOnlyCard(PhysicsCard card)
    {
        if (card == null || card.impulseStack == null)
            return false;

        bool hasAmbulationActivation = false;

        foreach (var action in card.impulseStack)
        {
            if (action == null || string.IsNullOrEmpty(action.muscleGroup))
                continue;

            string lowerGroup = action.muscleGroup.ToLowerInvariant();

            foreach (var keyword in ArmKeywords)
            {
                if (lowerGroup.Contains(keyword))
                    return false;
            }

            foreach (var keyword in HeadKeywords)
            {
                if (lowerGroup.Contains(keyword))
                    return false;
            }

            foreach (var keyword in AmbulationExtentKeywords)
            {
                if (lowerGroup.Contains(keyword))
                {
                    hasAmbulationActivation = true;
                    break;
                }
            }
        }

        return hasAmbulationActivation;
    }
}

/// <summary>
/// Narrowing state for successive ambulation segments along a path (connected / inverse ranges).
/// </summary>
[Serializable]
public struct AmbulationSegmentRange
{
    public float minValue;
    public float maxValue;

    public static AmbulationSegmentRange Full => new AmbulationSegmentRange { minValue = float.NegativeInfinity, maxValue = float.PositiveInfinity };

    public AmbulationSegmentRange Narrow(float lo, float hi)
    {
        return new AmbulationSegmentRange
        {
            minValue = Mathf.Max(minValue, lo),
            maxValue = Mathf.Min(maxValue, hi)
        };
    }

    public bool IsEmpty => minValue > maxValue + 1e-5f;
}

/// <summary>
/// Optional sink for propagating ambulation range narrowing between path segments.
/// </summary>
public interface IAmbulationRangePropagator
{
    void PushConnectedConstraint(float center, float halfWidth);
    void PushInverseConstraint(float center, float halfWidth);
    AmbulationSegmentRange CurrentRange { get; }
}

/// <summary>
/// Simple range stack used by solvers for sequential leg-of-path narrowing.
/// </summary>
public sealed class AmbulationRangeStack : IAmbulationRangePropagator
{
    readonly Stack<AmbulationSegmentRange> stack = new Stack<AmbulationSegmentRange>();

    public AmbulationSegmentRange CurrentRange => stack.Count > 0 ? stack.Peek() : AmbulationSegmentRange.Full;

    public void PushConnectedConstraint(float center, float halfWidth)
    {
        float lo = center - halfWidth;
        float hi = center + halfWidth;
        var next = CurrentRange.Narrow(lo, hi);
        stack.Push(next);
    }

    /// <summary>Prefer keeping the larger valid interval outside [center ± halfWidth].</summary>
    public void PushInverseConstraint(float center, float halfWidth)
    {
        float lo = center - halfWidth;
        float hi = center + halfWidth;
        var cur = CurrentRange;

        bool leftValid = cur.minValue < lo - 1e-5f;
        bool rightValid = cur.maxValue > hi + 1e-5f;
        if (!leftValid && !rightValid)
            return;

        float leftWidth = leftValid ? Mathf.Max(0f, lo - cur.minValue) : 0f;
        float rightWidth = rightValid ? Mathf.Max(0f, cur.maxValue - hi) : 0f;

        if (leftValid && (!rightValid || leftWidth >= rightWidth))
            stack.Push(cur.Narrow(cur.minValue, lo));
        else if (rightValid)
            stack.Push(cur.Narrow(hi, cur.maxValue));
    }

    public void Pop() { if (stack.Count > 0) stack.Pop(); }

    public void Clear() => stack.Clear();
}
