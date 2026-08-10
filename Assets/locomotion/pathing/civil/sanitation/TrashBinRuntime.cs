using UnityEngine;

/// <summary>Fork-liftable trash bin with fill level for TrashWarden predicates.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Trash Bin")]
public sealed class TrashBinRuntime : MonoBehaviour
{
    public string binId = "bin_1";
    [Range(0f, 1f)] public float fill01;
    public bool forkLiftable = true;
    public Transform liftAnchor;
    public HouseBioRhythm house;
    public GarbageBag contents = new GarbageBag();

    void Awake()
    {
        if (liftAnchor == null) liftAnchor = transform;
        if (house == null) house = GetComponentInParent<HouseBioRhythm>();
        if (contents != null && contents.particles.Count == 0)
            contents.RebuildParticlesFromMass();
    }

    void LateUpdate()
    {
        if (house != null)
            fill01 = house.trashFill01;
    }

    public bool IsEmpty => fill01 <= 0.05f || contents.massKg <= 0.05f;

    public float EmptyInto(GarbageBag hopper, float maxKg = 40f)
    {
        float take = Mathf.Min(maxKg, contents.massKg);
        if (take <= 0f)
        {
            fill01 = 0f;
            if (house != null) house.trashFill01 = 0f;
            return 0f;
        }
        contents.massKg -= take;
        contents.RebuildParticlesFromMass();
        hopper?.AcceptMass(take);
        fill01 = contents.massKg > 0.05f ? Mathf.Clamp01(fill01 * 0.5f) : 0f;
        if (house != null) house.trashFill01 = fill01;
        return take;
    }
}
