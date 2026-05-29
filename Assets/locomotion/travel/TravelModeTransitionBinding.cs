using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps a travel-leg mode change to behavior-tree nodes run once at the boundary.
/// </summary>
[Serializable]
public class TravelModeTransitionBinding
{
    public TravelLegMode fromMode = TravelLegMode.Walk;
    public TravelLegMode toMode = TravelLegMode.Drive;

    [Tooltip("When true, matches any from-mode as long as toMode matches.")]
    public bool matchAnyFrom;

    [Tooltip("Optional template: child BehaviorTreeNodes are cloned under the transition sequence at plan build.")]
    public GameObject activationRoot;

    [Tooltip("Optional direct node templates (cloned per transition so legs do not share execution state).")]
    public List<BehaviorTreeNode> activationNodes = new List<BehaviorTreeNode>();

    /// <summary>First binding where toMode matches and fromMode matches or matchAnyFrom is set.</summary>
    public static bool TryResolve(
        TravelLegMode from,
        TravelLegMode to,
        IReadOnlyList<TravelModeTransitionBinding> bindings,
        out TravelModeTransitionBinding binding)
    {
        binding = null;
        if (bindings == null)
            return false;

        for (int i = 0; i < bindings.Count; i++)
        {
            TravelModeTransitionBinding b = bindings[i];
            if (b == null)
                continue;
            if (b.toMode != to)
                continue;
            if (!b.matchAnyFrom && b.fromMode != from)
                continue;
            binding = b;
            return true;
        }

        return false;
    }
}
