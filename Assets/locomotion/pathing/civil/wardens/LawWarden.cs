using System.Collections.Generic;
using UnityEngine;

/// <summary>Checks a LawCard against <see cref="GovernmentModelRagdoll"/> mix through-lines.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Law Warden")]
public sealed class LawWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public LawCard lawCard;
    public ReligiousLawCard religiousLawCard;
    public GovernmentModelRagdoll governmentRagdoll;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();

    void Awake()
    {
        if (governmentRagdoll == null)
            governmentRagdoll = GetComponent<GovernmentModelRagdoll>();
    }

    public float Allow01()
    {
        float statute = lawCard != null ? lawCard.Allow01() : lastScore01;
        float religious = religiousLawCard != null ? religiousLawCard.Allow01() : 1f;
        float mix = governmentRagdoll != null ? governmentRagdoll.ThroughLine01() : 0.5f;
        lastScore01 = Mathf.Clamp01(statute * 0.5f + religious * 0.25f + mix * 0.25f);
        return lastScore01;
    }
}
