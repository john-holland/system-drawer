using System.Collections.Generic;
using UnityEngine;

/// <summary>Civic authority over TownHall / GovLegislative / UnemploymentOffice. Does not replace CareerWarden.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Government Warden")]
public sealed class GovernmentWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public GovLegislativeVenueRuntime legislative;
    public UnemploymentOfficeBootstrap unemployment;
    public GovernmentModelRagdoll ragdoll;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();

    void Awake()
    {
        if (legislative == null)
            legislative = GetComponent<GovLegislativeVenueRuntime>();
        if (unemployment == null)
            unemployment = GetComponent<UnemploymentOfficeBootstrap>();
        if (ragdoll == null)
            ragdoll = GetComponent<GovernmentModelRagdoll>();
    }

    public float Allow01()
    {
        float mix = ragdoll != null ? ragdoll.ThroughLine01() : lastScore01;
        lastScore01 = Mathf.Clamp01(mix);
        return lastScore01;
    }
}
