using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DishWashingStationConfig", menuName = "Locomotion/Kitchen/Dish Washing Station Config")]
public sealed class DishWashingStationConfig : ScriptableObject
{
    public string stationId = "dish_pit";
    public bool enableCompostZone;
    public DishScrubMode scrubMode = DishScrubMode.TimingAndFlood;
    public DishFinishPreference finishPreference = DishFinishPreference.Either;
    public float defaultScrubSeconds = 2.5f;
    public float rinseLiters = 0.4f;
    public List<DishZoneBinding> zones = new List<DishZoneBinding>();

    public void EnsureStandardZones()
    {
        if (zones == null) zones = new List<DishZoneBinding>();
        EnsureZone(DishZoneKind.Dirty);
        EnsureZone(DishZoneKind.Sink);
        EnsureZone(DishZoneKind.Dishwasher);
        EnsureZone(DishZoneKind.Dry);
        if (enableCompostZone)
            EnsureZone(DishZoneKind.Compost);
    }

    void EnsureZone(DishZoneKind kind)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i] != null && zones[i].kind == kind) return;
        zones.Add(new DishZoneBinding { kind = kind });
    }
}
