using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Bar Bio Rhythm")]
public sealed class BarBioRhythm : MonoBehaviour
{
    public MusicAmbianceSchedule schedule;
    public BeatQuantizedActionBinder beatBinder;
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float occupancy01 = 0.4f;
    [Range(0f, 1f)] public float ambiance01 = 0.5f;

    void Awake()
    {
        if (schedule == null) schedule = GetComponent<MusicAmbianceSchedule>();
        if (beatBinder == null) beatBinder = GetComponent<BeatQuantizedActionBinder>();
        if (venueBio == null) venueBio = GetComponent<CivilVenueBioRhythmService>()
            ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (schedule != null && schedule.slots.Count == 0)
        {
            schedule.slots.Add(new MusicAmbianceSlot
            {
                slotId = "happy_hour",
                hoursCron = "* 16-22 * * *",
                ambiance = MusicAmbianceTag.Bar,
                ambianceScoreBias01 = 0.6f
            });
        }
    }

    public void Tick(DateTime utcNow)
    {
        schedule?.Tick(utcNow, occupancy01);
        beatBinder?.ApplyBpmFromSchedule(schedule);
        ambiance01 = schedule != null ? schedule.ambianceScore01 : 0.5f;
        if (venueBio != null)
        {
            venueBio.activity01 = ambiance01;
            venueBio.pace01 = Mathf.Clamp01(0.35f + ambiance01 * 0.4f);
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Bar Venue")]
public sealed class BarVenueRuntime : MonoBehaviour
{
    public BarBioRhythm bio;
    public CivilVenueAmenities amenities;
    public DanceIkTrainingCatalog danceCatalog;
    public Transform barSurface;
    public Transform danceFloor;
    public bool isOpen;
    public bool useBouncer;
    public bool useValet;

    void Awake()
    {
        if (bio == null) bio = GetComponent<BarBioRhythm>() ?? gameObject.AddComponent<BarBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public BarCard ServeDuty() => BarCard.Generate("serve", barSurface);
    public NightClubCard DanceDuty() => NightClubCard.Generate("dance", danceFloor ?? amenities?.danceFloor);
}
