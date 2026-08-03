using System;
using UnityEngine;

/// <summary>Night club bio: music-quantized ambiance × occupancy.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Night Club Bio Rhythm")]
public sealed class NightClubBioRhythm : MonoBehaviour
{
    public MusicAmbianceSchedule schedule;
    public BeatQuantizedActionBinder beatBinder;
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float occupancy01 = 0.5f;
    [Range(0f, 1f)] public float grooveScore01 = 0.5f;

    void Awake()
    {
        if (schedule == null) schedule = GetComponent<MusicAmbianceSchedule>();
        if (beatBinder == null) beatBinder = GetComponent<BeatQuantizedActionBinder>();
        if (venueBio == null) venueBio = GetComponent<CivilVenueBioRhythmService>()
            ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(DateTime utcNow)
    {
        schedule?.Tick(utcNow, occupancy01);
        beatBinder?.ApplyBpmFromSchedule(schedule);
        float amb = schedule != null ? schedule.ambianceScore01 : 0.5f;
        grooveScore01 = Mathf.Clamp01(amb * 0.7f + occupancy01 * 0.3f);
        if (venueBio != null)
        {
            venueBio.activity01 = grooveScore01;
            venueBio.pace01 = Mathf.Clamp01(0.4f + grooveScore01 * 0.5f);
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Night Club Venue")]
public sealed class NightClubVenueRuntime : MonoBehaviour
{
    public NightClubBioRhythm bio;
    public CivilVenueAmenities amenities;
    public DanceIkTrainingCatalog danceCatalog;
    public bool isOpen;
    public Transform danceFloor;

    void Awake()
    {
        if (bio == null) bio = GetComponent<NightClubBioRhythm>() ?? gameObject.AddComponent<NightClubBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (danceFloor == null) danceFloor = amenities.danceFloor;
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public NightClubCard FloorDuty() => NightClubCard.Generate("floor", danceFloor);
    public BouncerCard DoorDuty() => BouncerCard.Generate(amenities?.frontDesk != null ? amenities.frontDesk.gameObject : gameObject);
    public ValetCard ValetDuty() => ValetCard.Generate(amenities?.parkingLot);
}
