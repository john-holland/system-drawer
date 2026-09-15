using System;
using UnityEngine;

[Serializable]
public sealed class ClothingStoreStations
{
    public Transform cuttingTable;
    public Transform sewing;
    public Transform serger;
    public Transform fitting;
    public Transform dye;
    public Transform stuffing;
}

/// <summary>Clothing-store venue: StoreBase shelves + sewing stations + tayloring bio.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Clothing Store Ragdoll")]
public sealed class ClothingStoreRagdoll : BuildingRagdoll
{
    public StoreBase store;
    public TayloringBioRhythm storeBio;
    public TayloringTravelAgent tayloring;
    public ClothingStoreStations stations = new ClothingStoreStations();
    public CompanyRegistration company;

    protected override void Awake()
    {
        base.Awake();
        if (store == null)
            store = GetComponent<StoreBase>() ?? gameObject.AddComponent<StoreBase>();
        store.storeType = "clothing_store";
        store.builtinPromptKey = "clothing_store";
        store.buildingRagdoll = this;
        if (storeBio == null)
            storeBio = GetComponent<TayloringBioRhythm>() ?? gameObject.AddComponent<TayloringBioRhythm>();
        storeBio.store = store;
        if (tayloring == null)
            tayloring = GetComponent<TayloringTravelAgent>() ?? gameObject.AddComponent<TayloringTravelAgent>();
        tayloring.store = this;
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        store?.TickHours(DateTime.UtcNow);
    }

    public bool CanCutBolt() => store != null && store.HasCommodity(ClothingCommodities.Bolt);

    public bool DebitBolt(float qty = 1f)
    {
        if (!CanCutBolt()) return false;
        store.DebitCommodity(ClothingCommodities.Bolt, qty);
        return true;
    }
}
