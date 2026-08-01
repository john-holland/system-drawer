using UnityEngine;

/// <summary>Thin venue biorhythm for non-kitchen civil sites (attention/morale proxies).</summary>
[AddComponentMenu("Locomotion/Persona/Civil Venue Bio Rhythm")]
public sealed class CivilVenueBioRhythmService : MonoBehaviour
{
    public LifeSystemsSheet venueSheet;
    [Range(0f, 1f)] public float activity01 = 0.4f;
    [Range(0f, 1f)] public float stress01 = 0.2f;
    [Range(0f, 1f)] public float pace01 = 0.5f;
    public float tickInterval = 0.5f;
    float _accum;

    void Awake()
    {
        if (venueSheet == null)
            venueSheet = GetComponent<LifeSystemsSheet>() ?? gameObject.AddComponent<LifeSystemsSheet>();
        venueSheet.EnsureDefaults();
        Push();
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
        activity01 = Mathf.MoveTowards(activity01, 0.4f, dt * 0.02f);
        stress01 = Mathf.MoveTowards(stress01, 0.2f, dt * 0.015f);
        pace01 = Mathf.MoveTowards(pace01, 0.5f, dt * 0.01f);
        Push();
        venueSheet?.bioRhythm?.ApplyAmplitudeDelta((pace01 - 0.5f) * 0.01f);
    }

    public void NotifyOpen()
    {
        activity01 = Mathf.Clamp01(activity01 + 0.1f);
        pace01 = Mathf.Clamp01(pace01 + 0.08f);
        Push();
    }

    public void ApplyPersonaSeed(float amplitudeSeed)
    {
        venueSheet?.EnsureDefaults();
        venueSheet?.bioRhythm?.ApplyAmplitudeDelta((Mathf.Clamp01(amplitudeSeed) - 0.5f) * 0.2f);
        Push();
    }

    void Push()
    {
        if (venueSheet == null) return;
        venueSheet.EnsureDefaults();
        venueSheet.Set01(LifeSystemsChannelCatalog.Attention, activity01);
        venueSheet.Set01(LifeSystemsChannelCatalog.Adrenaline, stress01);
        venueSheet.Set01(LifeSystemsChannelCatalog.Morale, pace01);
    }
}
