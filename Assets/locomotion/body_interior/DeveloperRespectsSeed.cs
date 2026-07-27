using UnityEngine;

/// <summary>
/// Developer-respects system-drawer seed for reproducible hygiene/chew randomization.
/// Preferred chewing side is drawn once in [0.50, 0.55] from this seed.
/// </summary>
[AddComponentMenu("Locomotion/Body Interior/Developer Respects Seed")]
public sealed class DeveloperRespectsSeed : MonoBehaviour
{
    public int seed = 1337;

    [Tooltip("Cached preferred chew side bias in [0.50, 0.55]. 0.5 = left-leaning edge, 0.55 = right-leaning.")]
    [Range(0.5f, 0.55f)]
    public float preferredChewSide01 = 0.525f;

    bool _resolved;

    public int Seed => seed;

    public float PreferredChewSide01
    {
        get
        {
            EnsureResolved();
            return preferredChewSide01;
        }
    }

    /// <summary>True when preferred side leans right of midpoint.</summary>
    public bool PreferRightSide => PreferredChewSide01 >= 0.525f;

    public void EnsureResolved()
    {
        if (_resolved) return;
        var rng = new System.Random(seed);
        preferredChewSide01 = 0.5f + (float)rng.NextDouble() * 0.05f;
        _resolved = true;
    }

    public void Reseed(int newSeed)
    {
        seed = newSeed;
        _resolved = false;
        EnsureResolved();
    }

    public static DeveloperRespectsSeed FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        var existing = host.GetComponent<DeveloperRespectsSeed>();
        if (existing != null)
        {
            existing.EnsureResolved();
            return existing;
        }
        var s = host.AddComponent<DeveloperRespectsSeed>();
        s.EnsureResolved();
        return s;
    }
}
