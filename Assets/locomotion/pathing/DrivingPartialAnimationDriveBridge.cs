using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves partial animation keys from IK / animation set managers for driving phases.
/// </summary>
public sealed class DrivingPartialAnimationDriveBridge : MonoBehaviour
{
    [Tooltip("Optional driving solver that owns phase mask.")]
    public DrivingPhysicsCardSolver drivingSolver;

    [Tooltip("Optional IK animation manager; falls back to RagdollAnimationSetManager on same hierarchy.")]
    public RagdollIKAnimationManager ikAnimationManager;

    [Tooltip("Optional animation set manager when IK manager is absent.")]
    public RagdollAnimationSetManager animationSetManager;

    void Awake()
    {
        if (ikAnimationManager == null)
            ikAnimationManager = GetComponentInParent<RagdollIKAnimationManager>();
        if (animationSetManager == null)
            animationSetManager = GetComponentInParent<RagdollAnimationSetManager>();
        if (drivingSolver == null)
            drivingSolver = GetComponentInParent<DrivingPhysicsCardSolver>();
    }

    /// <summary>Returns display names / keys for sets matching phase and medium.</summary>
    public IReadOnlyList<string> SelectPartialAnimationKeys(DriveAnimationPhase phase, PhysicalPathingMedium medium)
    {
        var keys = new List<string>();
        CollectKeysFromSets(ikAnimationManager?.GetAvailableAnimations(), phase, medium, keys);
        if (keys.Count == 0 && animationSetManager?.animationSets != null)
            CollectKeysFromSets(animationSetManager.animationSets, phase, medium, keys);
        return keys;
    }

    static void CollectKeysFromSets(
        IReadOnlyList<RagdollAnimationSet> sets,
        DriveAnimationPhase phase,
        PhysicalPathingMedium medium,
        List<string> keys)
    {
        if (sets == null)
            return;

        string phaseToken = phase.ToString();
        for (int i = 0; i < sets.Count; i++)
        {
            RagdollAnimationSet set = sets[i];
            if (set == null || string.IsNullOrEmpty(set.displayName))
                continue;

            if (!NameMatchesPhase(set.displayName, phaseToken))
                continue;

            if (medium != PhysicalPathingMedium.Unspecified &&
                set.displayName.IndexOf(medium.ToString(), StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            keys.Add(set.displayName);
        }
    }

    static bool NameMatchesPhase(string displayName, string phaseToken)
    {
        if (string.IsNullOrEmpty(displayName))
            return false;
        if (displayName.IndexOf(phaseToken, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (phaseToken == nameof(DriveAnimationPhase.Drive) &&
            displayName.IndexOf("Steer", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    /// <summary>Push desired phase to driving solver mask.</summary>
    public void ApplyPhaseToSolver(DriveAnimationPhase phase)
    {
        if (drivingSolver != null)
            drivingSolver.activeDrivePhaseMask = phase;
    }
}
