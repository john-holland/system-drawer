using UnityEngine;

/// <summary>
/// Optional marker component to identify a GameObject as an “actor” that should be reviewed/wired
/// by editor tooling (wizard + matrix).
/// </summary>
public class RagdollActor : BaseAmbulatingActor
{
    [Tooltip("Optional actor display name override.")]
    public string actorNameOverride;

    [Tooltip("When true, merge default get-up BehaviorTree onto Brain at Awake.")]
    public bool enableGetUp = true;

    [Tooltip("Optional override; null uses built-in default prefab / factory.")]
    public BehaviorTree getUpBehaviorTreePrefab;

    bool _getUpMerged;

    /// <summary>True after a successful get-up merge (guards against double-wrap on re-enable).</summary>
    public bool GetUpMerged => _getUpMerged;

    void Awake()
    {
        RagdollGetUpBootstrap.TryMerge(this);
    }

    void OnEnable()
    {
        RagdollGetUpBootstrap.TryMerge(this);
    }

    /// <summary>Called by <see cref="RagdollGetUpBootstrap"/> after a successful merge.</summary>
    public void MarkGetUpMerged()
    {
        _getUpMerged = true;
    }

    /// <summary>Test helper: clear merge guard so TryMerge can run again.</summary>
    public void ResetGetUpMergeForTests()
    {
        _getUpMerged = false;
    }
}
