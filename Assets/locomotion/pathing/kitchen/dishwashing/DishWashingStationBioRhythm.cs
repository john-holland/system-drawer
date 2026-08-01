using UnityEngine;

/// <summary>Per-station dish bio-rhythm channels; feeds kitchen cleanliness.</summary>
[AddComponentMenu("Locomotion/Kitchen/Dish Washing Station Bio Rhythm")]
public sealed class DishWashingStationBioRhythm : MonoBehaviour
{
    public const string ChannelDirtyBacklog = "dish_dirty_backlog";
    public const string ChannelSinkLoad = "dish_sink_load";
    public const string ChannelWasherCycle = "dish_washer_cycle";
    public const string ChannelDryReady = "dish_dry_ready";
    public const string ChannelThroughput = "dish_throughput";

    public DishWashingStation station;
    public LifeSystemsSheet sheet;
    [Range(0f, 1f)] public float dirtyBacklog01;
    [Range(0f, 1f)] public float sinkLoad01;
    [Range(0f, 1f)] public float washerCycle01;
    [Range(0f, 1f)] public float dryReady01;
    [Range(0f, 1f)] public float throughput01 = 0.5f;
    public float tickInterval = 0.5f;
    float _accum;

    void Awake()
    {
        if (station == null) station = GetComponent<DishWashingStation>();
        if (sheet == null)
            sheet = GetComponent<LifeSystemsSheet>() ?? gameObject.AddComponent<LifeSystemsSheet>();
        sheet.EnsureDefaults();
    }

    void Update()
    {
        _accum += Time.deltaTime;
        if (_accum < tickInterval) return;
        float dt = _accum;
        _accum = 0f;
        Tick(dt);
    }

    public void Tick(float dt)
    {
        SyncFromStation();
        dirtyBacklog01 = Mathf.MoveTowards(dirtyBacklog01, 0.2f, dt * 0.01f);
        sinkLoad01 = Mathf.MoveTowards(sinkLoad01, 0.15f, dt * 0.02f);
        washerCycle01 = Mathf.MoveTowards(washerCycle01, 0f, dt * 0.05f);
        dryReady01 = Mathf.MoveTowards(dryReady01, 0.4f, dt * 0.01f);
        Push();
    }

    public void NotifyDirtySeeded(int count)
    {
        dirtyBacklog01 = Mathf.Clamp01(dirtyBacklog01 + 0.08f * Mathf.Max(0, count));
        Push();
    }

    public void NotifyMove(DishZoneKind from, DishZoneKind to)
    {
        if (from == DishZoneKind.Dirty)
            dirtyBacklog01 = Mathf.Clamp01(dirtyBacklog01 - 0.1f);
        if (to == DishZoneKind.Sink)
            sinkLoad01 = Mathf.Clamp01(sinkLoad01 + 0.12f);
        if (from == DishZoneKind.Sink)
            sinkLoad01 = Mathf.Clamp01(sinkLoad01 - 0.1f);
        if (to == DishZoneKind.Dishwasher)
            washerCycle01 = Mathf.Clamp01(washerCycle01 + 0.35f);
        if (to == DishZoneKind.Dry)
        {
            dryReady01 = Mathf.Clamp01(dryReady01 + 0.15f);
            throughput01 = Mathf.Clamp01(throughput01 + 0.08f);
            KitchenBioRhythmService.Instance?.NotifyCleanAttempt(0.06f);
        }
        Push();
    }

    void SyncFromStation()
    {
        if (station == null) return;
        int dirty = station.Count(DishZoneKind.Dirty);
        int sink = station.Count(DishZoneKind.Sink);
        int dry = station.Count(DishZoneKind.Dry);
        dirtyBacklog01 = Mathf.Clamp01(dirty * 0.15f);
        sinkLoad01 = Mathf.Clamp01(sink * 0.2f);
        dryReady01 = Mathf.Clamp01(dry * 0.15f);
    }

    void Push()
    {
        if (sheet == null) return;
        sheet.EnsureDefaults();
        // Custom channel ids may not be in catalog; Adjust01/Set01 need catalog — store via known proxies.
        sheet.Adjust01(LifeSystemsChannelCatalog.Ablution, (dryReady01 - dirtyBacklog01) * 0.01f);
    }
}
