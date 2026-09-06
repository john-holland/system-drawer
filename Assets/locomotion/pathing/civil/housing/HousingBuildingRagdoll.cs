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
    public Transform windowSill;
    public Transform windowTrim;
    public Transform windowShutters;
    public Transform windowShade;
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
    public Transform digSite;
    public Transform foundation;
    public Transform studsRoot;
    public Transform insulationRoot;
    public Transform hvacRoot;
    public Transform awning;
    public Transform frontSteps;
    public Transform frontWalk;
    public Transform patio;
    public Transform grass;
    public Transform yardFeatures;
    public Transform railings;
    public Transform deck;
    public Transform fence;
    public Transform garageDoor;
    public RoadLot drivewayLot;
    public RoadLot garageLot;
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
    public HousePowerBus powerBus = new HousePowerBus();
    public HouseEaveWaterCache eaveWater;
    public HouseBasementFloodCache basementFlood;
    public UtilityBioRhythm utilityBio;
    public UtilityRoomBootstrap utilityRoom;
    public HouseUtilityTap utilityTap;
    public HouseInventoryBinder inventoryBinder;
    public GarageDoorDriveLink garageDrive;

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
        if (eaveWater == null)
            eaveWater = GetComponent<HouseEaveWaterCache>() ?? gameObject.AddComponent<HouseEaveWaterCache>();
        eaveWater.house = this;
        if (basementFlood == null)
            basementFlood = GetComponent<HouseBasementFloodCache>() ?? gameObject.AddComponent<HouseBasementFloodCache>();
        if (utilityBio == null)
            utilityBio = GetComponent<UtilityBioRhythm>() ?? gameObject.AddComponent<UtilityBioRhythm>();
        utilityBio.houseBio = houseBio;
        utilityBio.floodCache = basementFlood;
        basementFlood.utilityBio = utilityBio;
        if (utilityRoom == null)
            utilityRoom = GetComponent<UtilityRoomBootstrap>() ?? gameObject.AddComponent<UtilityRoomBootstrap>();
        utilityRoom.house = this;
        utilityRoom.Ensure();
        if (utilityTap == null)
            utilityTap = GetComponent<HouseUtilityTap>() ?? gameObject.AddComponent<HouseUtilityTap>();
        if (garageDrive == null)
            garageDrive = GetComponent<GarageDoorDriveLink>();
        if (garageDrive != null)
        {
            garageDrive.house = this;
            if (garageDrive.doorLeaf == null && slots != null)
                garageDrive.doorLeaf = slots.garageDoor;
        }
        if (powerBus.systems == null || powerBus.systems.Count == 0)
            HousePowerBus.FillDefault(powerBus.systems);
        powerBus.maxDrawKw = CircuitBreakerPanel.MaxDrawKwForAmpacity();
        ApplyArchitectureScale();
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);
        houseBio?.Tick(dt);
        utilityRoom?.Tick(dt);
        powerBus?.Tick();
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

    public List<UtilityCard> BuildUtilityCards() => UtilityCardCatalog.DefaultCards(utilityRoom);
}
