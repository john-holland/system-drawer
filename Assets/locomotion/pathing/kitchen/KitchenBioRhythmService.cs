using UnityEngine;

/// <summary>
/// Kitchen venue bio-rhythm: heat / stress / service pace channels on a LifeSystemsSheet (or venue GO).
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Kitchen Bio Rhythm Service")]
public sealed class KitchenBioRhythmService : MonoBehaviour
{
    public const string ServiceKey = "kitchen.biorhythm";
    public const string ChannelKitchenHeat = "kitchen_heat";
    public const string ChannelKitchenStress = "kitchen_stress";
    public const string ChannelServicePace = "kitchen_service_pace";
    public const string ChannelCleanliness = "kitchen_cleanliness";

    static KitchenBioRhythmService _instance;
    public static KitchenBioRhythmService Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<KitchenBioRhythmService>();
            return _instance;
        }
    }

    public LifeSystemsSheet venueSheet;
    [Range(0f, 1f)] public float heat01 = 0.35f;
    [Range(0f, 1f)] public float stress01 = 0.25f;
    [Range(0f, 1f)] public float servicePace01 = 0.5f;
    [Range(0f, 1f)] public float cleanliness01 = 0.8f;
    public float tickInterval = 0.5f;
    float _accum;

    void Awake()
    {
        _instance = this;
        if (venueSheet == null)
            venueSheet = GetComponent<LifeSystemsSheet>() ?? gameObject.AddComponent<LifeSystemsSheet>();
        venueSheet.EnsureDefaults();
        PushChannels();
    }

    void Update()
    {
        _accum += Time.deltaTime;
        if (_accum < tickInterval) return;
        _accum = 0f;
        Tick(tickInterval);
    }

    public void Tick(float dt)
    {
        // Mild decay toward setpoints
        heat01 = Mathf.MoveTowards(heat01, 0.3f, dt * 0.02f);
        stress01 = Mathf.MoveTowards(stress01, 0.2f, dt * 0.015f);
        servicePace01 = Mathf.MoveTowards(servicePace01, 0.5f, dt * 0.01f);
        cleanliness01 = Mathf.MoveTowards(cleanliness01, 0.75f, dt * 0.005f);
        PushChannels();
        venueSheet?.bioRhythm?.ApplyAmplitudeDelta((servicePace01 - 0.5f) * 0.01f);
    }

    public void NotifyOrderTicket()
    {
        stress01 = Mathf.Clamp01(stress01 + 0.08f);
        servicePace01 = Mathf.Clamp01(servicePace01 + 0.1f);
        PushChannels();
    }

    public void NotifyCookHeat(float delta)
    {
        heat01 = Mathf.Clamp01(heat01 + delta);
        PushChannels();
    }

    public void NotifyCleanAttempt(float delta = 0.12f)
    {
        cleanliness01 = Mathf.Clamp01(cleanliness01 + delta);
        PushChannels();
    }

    public void ApplySmellTint(System.Collections.Generic.IReadOnlyList<string> smells)
    {
        if (smells == null || smells.Count == 0) return;
        heat01 = Mathf.Clamp01(heat01 + 0.03f * smells.Count);
        PushChannels();
    }

    void PushChannels()
    {
        if (venueSheet == null) return;
        venueSheet.EnsureDefaults();
        TrySet(ChannelKitchenHeat, heat01);
        TrySet(ChannelKitchenStress, stress01);
        TrySet(ChannelServicePace, servicePace01);
        TrySet(ChannelCleanliness, cleanliness01);
    }

    void TrySet(string id, float v01)
    {
        // LifeSystemsSheet Set01 only works for catalog channels; store via Adjust if missing.
        if (LifeSystemsChannelCatalog.TryGet(id, out _))
            venueSheet.Set01(id, v01);
        else
        {
            // Best-effort: use adrenaline/fatigue proxies when custom channels absent
            if (id == ChannelKitchenStress)
                venueSheet.Set01(LifeSystemsChannelCatalog.Adrenaline, v01);
            else if (id == ChannelServicePace)
                venueSheet.Set01(LifeSystemsChannelCatalog.Attention, v01);
            else if (id == ChannelCleanliness)
                venueSheet.Set01(LifeSystemsChannelCatalog.Ablution, v01);
            else if (id == ChannelKitchenHeat)
                venueSheet.Set01(LifeSystemsChannelCatalog.Fatigue, Mathf.Clamp01(v01 * 0.5f));
        }
    }
}
