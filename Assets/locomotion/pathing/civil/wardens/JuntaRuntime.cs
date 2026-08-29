using UnityEngine;

/// <summary>
/// Military junta. <see cref="canSuspendConstitution"/> disables Constitution/Rights when active.
/// <see cref="respectsGenevaConventions"/> (default true) is read by <see cref="GenevaConventionWarden"/>.
/// Ties to military checkpoint / embassy army justice.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Junta Runtime")]
public sealed class JuntaRuntime : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01;
    public bool canSuspendConstitution;
    public bool respectsGenevaConventions = true;
    public CheckpointVenueRuntime checkpoint;
    public EmbassyVenueRuntime embassy;

    void Awake()
    {
        if (checkpoint == null)
            checkpoint = GetComponent<CheckpointVenueRuntime>();
        if (embassy == null)
            embassy = GetComponent<EmbassyVenueRuntime>();
    }

    public float Allow01() => canSuspendConstitution ? 0f : lastScore01;
}
