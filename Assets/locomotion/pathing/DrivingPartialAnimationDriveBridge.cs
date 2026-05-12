using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stub bridge from character animation maps / partial <see cref="RagdollAnimationSet"/> entries to driving phases and mediums.
/// Extend this to query RagdollIKAnimationManager or set managers when integrating.
/// </summary>
public sealed class DrivingPartialAnimationDriveBridge : MonoBehaviour
{
    [Tooltip("Optional driving solver that owns phase mask.")]
    public DrivingPhysicsCardSolver drivingSolver;

    /// <summary>
    /// Placeholder: returns empty until wired to animation discovery.
    /// </summary>
    public IReadOnlyList<string> SelectPartialAnimationKeys(DriveAnimationPhase phase, PhysicalPathingMedium medium)
    {
        return System.Array.Empty<string>();
    }

    /// <summary>
    /// Stub: push desired phase to driving solver mask for testing.
    /// </summary>
    public void ApplyPhaseToSolver(DriveAnimationPhase phase)
    {
        if (drivingSolver != null)
            drivingSolver.activeDrivePhaseMask = phase;
    }
}
