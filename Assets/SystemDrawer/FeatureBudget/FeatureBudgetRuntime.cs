using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("System Drawer/Feature Budget Runtime")]
public sealed class FeatureBudgetRuntime : MonoBehaviour
{
    public FeatureBudgetProfile profile;
    public bool sampleInEditMode;

    readonly FeatureBudgetRatioRegistry _ratioRegistry = new FeatureBudgetRatioRegistry();
    readonly FeatureBudgetAttributor _attributor = new FeatureBudgetAttributor();
    readonly Dictionary<string, float> _msByFeature = new Dictionary<string, float>();
    readonly List<IFeatureGranularityConsumer> _consumers = new List<IFeatureGranularityConsumer>();

    FeatureBudgetGovernor _governor;
    FeatureBudgetRollingHistory _cpuHistory;
    IPlanetRatioSource _planetSource;

    public FeatureBudgetRatioRegistry RatioRegistry => _ratioRegistry;
    public float FrameCpuMs { get; private set; }
    public float RollingCpuMs => _cpuHistory != null ? _cpuHistory.RollingAverage : 0f;
    public FeatureBudgetState BudgetState => _governor != null ? _governor.State : FeatureBudgetState.Normal;
    public IReadOnlyDictionary<string, float> LastMsByFeature => _msByFeature;

    void Awake()
    {
        EnsureProfile();
        _cpuHistory = new FeatureBudgetRollingHistory(profile.rollingWindowFrames);
        _ratioRegistry.LoadFromProfile(profile);
        _governor = new FeatureBudgetGovernor(profile, _ratioRegistry);
        SyncEntryGranularityToRatios();
        FeatureBudget.SetActive(this);
    }

    void SyncEntryGranularityToRatios()
    {
        if (profile?.entries == null)
            return;
        for (int i = 0; i < profile.entries.Count; i++)
        {
            var entry = profile.entries[i];
            _ratioRegistry.SetGranularityForFeature(entry.featureId, entry.granularityLevel);
        }
    }

    void OnDestroy()
    {
        if (FeatureBudget.Active == this)
            FeatureBudget.SetActive(null);
    }

    void OnEnable()
    {
        FeatureBudget.SetActive(this);
        SyncPlanetRatios();
    }

    public void RegisterPlanetSource(IPlanetRatioSource source)
    {
        _planetSource = source;
        SyncPlanetRatios();
    }

    public void RegisterConsumer(IFeatureGranularityConsumer consumer)
    {
        if (consumer != null && !_consumers.Contains(consumer))
            _consumers.Add(consumer);
    }

    public void UnregisterConsumer(IFeatureGranularityConsumer consumer)
    {
        _consumers.Remove(consumer);
    }

    public void SyncPlanetRatios()
    {
        if (_planetSource != null)
            _ratioRegistry.SyncFromPlanetSource(_planetSource, profile);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying && !sampleInEditMode)
            return;
        SampleFrame();
        _governor?.Tick(RollingCpuMs);
        SyncEntryGranularityToRatios();
        PushConsumers();
    }

    void SampleFrame()
    {
        FrameCpuMs = SampleCpuFrameMs();
        _cpuHistory?.Push(FrameCpuMs);
        _attributor.AttributeFrame(profile, _msByFeature);
        UpdateEntryTimings();
    }

    static float SampleCpuFrameMs()
    {
        if (FrameTimingManager.IsFeatureEnabled())
        {
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            uint count = FrameTimingManager.GetLatestTimings(1, timings);
            if (count > 0)
                return (float)timings[0].cpuFrameTime;
        }
        return Time.deltaTime * 1000f;
    }

    void UpdateEntryTimings()
    {
        if (profile?.entries == null)
            return;
        for (int i = 0; i < profile.entries.Count; i++)
        {
            var entry = profile.entries[i];
            if (_msByFeature.TryGetValue(entry.featureId, out float ms))
            {
                entry.lastFrameMs = ms;
                entry.rollingAvgMs = Mathf.Lerp(entry.rollingAvgMs, ms, 0.1f);
            }
        }
    }

    void PushConsumers()
    {
        for (int i = 0; i < _consumers.Count; i++)
            _consumers[i]?.ApplyFeatureGranularity(_ratioRegistry);
    }

    public float GetGranularity(string featureId)
    {
        var entry = profile?.FindEntry(featureId);
        return FeatureBudgetGovernor.GetGranularity(entry);
    }

    public bool IsFeatureActive(string featureId)
    {
        var entry = profile?.FindEntry(featureId);
        return FeatureBudgetGovernor.IsFeatureActive(entry);
    }

    void EnsureProfile()
    {
        if (profile == null)
            profile = ScriptableObject.CreateInstance<FeatureBudgetProfile>();
        profile.EnsureDefaults();
    }
}
