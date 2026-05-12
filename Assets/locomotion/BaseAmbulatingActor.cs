using UnityEngine;

/// <summary>
/// Shared root marker/config for ragdoll actors and vehicles using ambulation-aware systems.
/// Optional registration with <see cref="AmbulatingActorRegistrar"/> (SystemDrawer assembly).
/// </summary>
public class BaseAmbulatingActor : MonoBehaviour, ITravelActorRoot
{
    public Transform RootTransform => transform;

    [Tooltip("Optional stable id for tooling (not necessarily unique).")]
    public string ambulatingActorId;

    [Tooltip("Human-readable label for editors and debug.")]
    public string ambulatingDisplayName;
}
