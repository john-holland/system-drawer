using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HouseReferenceSlots
{
    public Transform garbage;
    public Transform garage;
    public Transform driveway;
    public Transform eaves;
    public Transform gutters;
    public Transform egressMain;
    public Transform egressFire;
    public Transform windowsRoot;
    public Transform shed;
    public Transform guestHouse;
    public Transform playhouse;
    public Transform yardToys;
    public Transform gasHookup;
    public Transform oilHookup;
    public Transform meters;
    public Transform generators;
    public Transform rvParking;
    public Transform additionalParking;
    public Transform satelliteDish;
    public Transform cableDemarc;
    public Transform fiberOnt;
    public Transform electricalConnection;
}

/// <summary>House specialization of BuildingRagdoll — domestic bio, family, slots, plumbing group.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Housing Building Ragdoll")]
public sealed class HousingBuildingRagdoll : BuildingRagdoll
{
    public HouseBioRhythm houseBio;
    public FamilyPeckingOrder family;
    public BuildingPlumbingGroup plumbingGroup;
    public HouseReferenceSlots slots = new HouseReferenceSlots();
    public HousingArchitectureLemmaProperties architecture = new HousingArchitectureLemmaProperties
    {
        size = HousingArchitectureSize.GoodSize
    };
    public List<DestructibleLayerRef> overflowLayers = new List<DestructibleLayerRef>();
    public HouseInventoryBinder inventoryBinder;

    void Reset()
    {
        buildingStableId = gameObject.name;
    }

    protected override void Awake()
    {
        base.Awake();
        if (houseBio == null)
            houseBio = GetComponent<HouseBioRhythm>() ?? gameObject.AddComponent<HouseBioRhythm>();
        houseBio.buildingBio = bio;
        if (family == null)
            family = GetComponent<FamilyPeckingOrder>() ?? gameObject.AddComponent<FamilyPeckingOrder>();
        if (plumbingGroup == null)
            plumbingGroup = GetComponent<BuildingPlumbingGroup>() ?? gameObject.AddComponent<BuildingPlumbingGroup>();
        if (inventoryBinder == null)
            inventoryBinder = GetComponent<HouseInventoryBinder>() ?? gameObject.AddComponent<HouseInventoryBinder>();
        ApplyArchitectureScale();
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        houseBio?.Tick(dt);
    }

    public void ApplyArchitectureLemma(string sizeToken)
    {
        architecture.size = HousingArchitectureLemmaProperties.ParseSize(sizeToken);
        ApplyArchitectureScale();
    }

    void ApplyArchitectureScale()
    {
        float s = architecture.FootprintScale();
        // Soft visual hint only — do not force if parent controls scale
        if (transform.localScale == Vector3.one || Mathf.Abs(transform.localScale.x - s) < 0.05f)
            transform.localScale = Vector3.one * s;
    }

    public List<HouseChoreCard> BuildChoreCards() => HouseChoreCatalog.DefaultChores(this);
}
