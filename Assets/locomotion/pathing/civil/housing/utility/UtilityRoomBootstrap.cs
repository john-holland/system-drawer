using System.Collections.Generic;
using UnityEngine;

/// <summary>Wires basement utility appliances on floor index 0 and ticks them with the house bio.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Utility Room Bootstrap")]
public sealed class UtilityRoomBootstrap : MonoBehaviour
{
    public HousingBuildingRagdoll house;
    public UtilityBioRhythm utilityBio;
    public FurnaceRuntime furnace;
    public WaterHeaterRuntime heater;
    public RecoupWheelAlternator recoup;
    public JacobsLadderGunkFreeer jacobs;
    public HvacEquipmentRuntime hvac;
    public WaterFilterRuntime filter;
    public BuildingWaterShutoff shutoff;
    public CircuitBreakerPanel panel;
    public SumpPumpRuntime sump;
    public HouseBasementFloodCache floodCache;
    public HouseUtilityTap tap;
    public WallPlugRuntime[] plugs;
    public readonly List<UtilityInstallStep> installSteps = new List<UtilityInstallStep>();

    public void Ensure()
    {
        if (house == null)
            house = GetComponent<HousingBuildingRagdoll>();
        if (utilityBio == null)
            utilityBio = GetComponent<UtilityBioRhythm>() ?? gameObject.AddComponent<UtilityBioRhythm>();
        utilityBio.houseBio = house != null ? house.houseBio : GetComponent<HouseBioRhythm>();
        furnace = EnsureChild<FurnaceRuntime>("furnace");
        heater = EnsureChild<WaterHeaterRuntime>("water_heater");
        recoup = EnsureChild<RecoupWheelAlternator>("imitirrrr__");
        jacobs = EnsureChild<JacobsLadderGunkFreeer>("jacobs_ladder");
        hvac = EnsureChild<HvacEquipmentRuntime>("hvac");
        filter = EnsureChild<WaterFilterRuntime>("water_filter");
        shutoff = EnsureChild<BuildingWaterShutoff>("shutoff");
        panel = GetComponent<CircuitBreakerPanel>() ?? gameObject.AddComponent<CircuitBreakerPanel>();
        sump = EnsureChild<SumpPumpRuntime>("sump_pump");
        floodCache = GetComponent<HouseBasementFloodCache>() ?? gameObject.AddComponent<HouseBasementFloodCache>();
        tap = GetComponent<HouseUtilityTap>() ?? gameObject.AddComponent<HouseUtilityTap>();
        if (house != null)
        {
            recoup.powerBus = house.powerBus;
            sump.powerBus = house.powerBus;
            panel.powerBus = house.powerBus;
            heater.plumbing = house.plumbingGroup;
        }
        furnace.houseBio = utilityBio.houseBio;
        furnace.utilityBio = utilityBio;
        heater.utilityBio = utilityBio;
        heater.shutoff = shutoff;
        recoup.heater = heater;
        jacobs.wheel = recoup;
        jacobs.utilityBio = utilityBio;
        hvac.houseBio = utilityBio.houseBio;
        hvac.utilityBio = utilityBio;
        filter.utilityBio = utilityBio;
        shutoff.utilityBio = utilityBio;
        sump.floodCache = floodCache;
        sump.panel = panel;
        sump.utilityBio = utilityBio;
        floodCache.shutoff = shutoff;
        floodCache.heater = heater;
        floodCache.utilityBio = utilityBio;
        tap.shutoff = shutoff;
        tap.panel = panel;
        utilityBio.panel = panel;
        utilityBio.sump = sump;
        utilityBio.floodCache = floodCache;
        if (plugs == null || plugs.Length == 0)
            plugs = GetComponentsInChildren<WallPlugRuntime>(true);
        RebuildInstallSteps();
    }

    public void Tick(float dt)
    {
        if (utilityBio == null)
            Ensure();
        furnace?.Tick(dt);
        heater?.Tick(dt);
        recoup?.Tick(dt);
        jacobs?.Tick(dt);
        hvac?.Tick(dt);
        filter?.Tick(dt);
        sump?.Tick(dt);
        utilityBio?.Tick(dt);
    }

    public void RequestInstallOpenCloseBt()
    {
        SendMessage("BakeUtilityInstallationOpenClose", this, SendMessageOptions.DontRequireReceiver);
    }

    void RebuildInstallSteps()
    {
        installSteps.Clear();
        AddStep(UtilityLemmaPropertyKeys.Furnace, furnace);
        AddStep(UtilityLemmaPropertyKeys.WaterHeater, heater);
        AddStep(UtilityLemmaPropertyKeys.Imitirrrr, recoup);
        AddStep(UtilityLemmaPropertyKeys.JacobsLadder, jacobs);
        AddStep(UtilityLemmaPropertyKeys.Hvac, hvac);
        AddStep(UtilityLemmaPropertyKeys.WaterFilter, filter);
        AddStep(UtilityLemmaPropertyKeys.Shutoff, shutoff);
        AddStep(UtilityLemmaPropertyKeys.CircuitBreaker, panel);
        AddStep(UtilityLemmaPropertyKeys.SumpPump, sump);
    }

    void AddStep(string id, Component c)
    {
        if (c == null) return;
        installSteps.Add(new UtilityInstallStep { id = id, world = c.transform.position });
    }

    T EnsureChild<T>(string childName) where T : Component
    {
        var t = transform.Find(childName);
        GameObject go = t != null ? t.gameObject : new GameObject(childName);
        if (t == null)
            go.transform.SetParent(transform, false);
        float y = HouseFloorIndex.FloorY(HouseFloorIndex.Basement, 3f, transform.position.y);
        go.transform.localPosition = new Vector3(go.transform.localPosition.x, y - transform.position.y, go.transform.localPosition.z);
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}

[System.Serializable]
public sealed class UtilityInstallStep
{
    public string id;
    public Vector3 world;
}
