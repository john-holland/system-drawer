using UnityEngine;

/// <summary>
/// Optional marker component to identify a GameObject as an “actor” that should be reviewed/wired
/// by editor tooling (wizard + matrix).
/// </summary>
public class RagdollActor : MonoBehaviour
{
    [Tooltip("Optional actor display name override.")]
    public string actorNameOverride;
}

