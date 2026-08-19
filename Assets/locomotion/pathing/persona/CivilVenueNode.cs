using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One civil venue in the PersonaDayManager lattice.</summary>
[Serializable]
public sealed class CivilVenueNode
{
    public string stableId;
    public CivilSystemKind kind = CivilSystemKind.Generic;
    public string buildingTypeId;
    public GameObject contextOwner;
    [CronExpr] public string hoursCron = "* 8-20 * * *";
    public string troupeId;
    public int developerPriority = 100;
    public float minCausalDepth;
    public List<RetinuePeckingEntry> retinue = new List<RetinuePeckingEntry>();
    public RestaurantVenueRuntime kitchenRuntime;
    public CivilVenueBioRhythmService venueBio;
    public KitchenBioRhythmService kitchenBio;
    public WaypointPlannerInput waypointPlanner;
    public CivilLodTier currentTier = CivilLodTier.Culled;
    public bool isOpen;
    public PersonaRequestBundle lastBundle;

    public Vector3 WorldPosition =>
        contextOwner != null ? contextOwner.transform.position : Vector3.zero;

    public int CountWokenActors()
    {
        int n = 0;
        if (retinue == null) return 0;
        for (int i = 0; i < retinue.Count; i++)
            if (retinue[i]?.actor != null && retinue[i].actor.activeInHierarchy)
                n++;
        return n;
    }
}
