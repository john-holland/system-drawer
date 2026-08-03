using UnityEngine;

/// <summary>
/// Stub reserved for later BuildingBeast fiction (occupants / exterior "eating").
/// Does not drive sim in this milestone — soft-referenced from BuildingRagdoll only.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Building Beast (Stub)")]
public sealed class BuildingBeast : MonoBehaviour
{
    [Tooltip("Reserved for later BuildingBeast fiction. No tick logic yet.")]
    public BuildingRagdoll ragdoll;

    [HideInInspector]
    public bool stubOnly = true;

    void Awake()
    {
        if (ragdoll == null)
            ragdoll = GetComponent<BuildingRagdoll>();
    }
}
