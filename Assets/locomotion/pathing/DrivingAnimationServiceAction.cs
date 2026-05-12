using System;
using UnityEngine;

/// <summary>
/// Data-driven animation service hook for vehicle instruments (attach/detach limb, clips); paired with <see cref="DriveAnimationPhase"/>.
/// </summary>
[Serializable]
public struct DrivingAnimationServiceActionLimits
{
    public float maxDurationSeconds;
    public float strengthCap01;
    public float minAngleDegrees;
    public float maxAngleDegrees;
    public float cooldownSeconds;
}

/// <summary>
/// Stub kinds for authoring and animation bridges; runtime interpretation comes later.
/// </summary>
public enum DrivingAnimationServiceActionKind
{
    None,
    AttachLimb,
    DetachLimb,
    GripInstrument,
    ReleaseInstrument,
    PlayPartialClip
}

/// <summary>
/// Assigns a character-side animation action to a vehicle instrument slot and phase mask.
/// </summary>
[Serializable]
public class DrivingAnimationServiceAction
{
    public DrivingAnimationServiceActionKind actionKind;
    public string targetInstrumentId;
    public DriveAnimationPhase phaseMask = DriveAnimationPhase.Drive;
    public DrivingAnimationServiceActionLimits limits;
    [Tooltip("Optional animation set / clip name for PlayPartialClip stubs.")]
    public string animationKey;
}
