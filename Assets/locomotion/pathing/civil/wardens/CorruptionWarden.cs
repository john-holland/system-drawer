using System.Collections.Generic;
using UnityEngine;

/// <summary>Gates subversion/sedition. Spy agency and private-investigator flags raise the score; criminality stays stub.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Corruption Warden")]
public sealed class CorruptionWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01;
    public bool spyConfigured;
    public bool privateInvestigatorConfigured;
    public SpyAgencyVenueRuntime spyAgency;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();
    [Range(0f, 1f)] public float criminalityStub01;

    void Awake()
    {
        if (spyAgency == null)
            spyAgency = GetComponent<SpyAgencyVenueRuntime>();
        if (spyAgency != null)
            spyConfigured = true;
    }

    public float Allow01() => Mathf.Clamp01(1f - Evaluate());

    public float Evaluate()
    {
        float score = lastScore01;
        if (spyConfigured || spyAgency != null)
            score = Mathf.Max(score, 0.45f);
        if (privateInvestigatorConfigured)
            score = Mathf.Max(score, 0.35f);
        score = Mathf.Clamp01(score + criminalityStub01 * 0.1f);
        lastScore01 = score;
        return lastScore01;
    }

    public bool AllowsSubversion() => Allow01() >= 0.33f;
}
