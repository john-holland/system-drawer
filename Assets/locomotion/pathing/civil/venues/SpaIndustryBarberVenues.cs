using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Spa Bio Rhythm")]
public sealed class SpaBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float calm01 = 0.7f;
    [Range(0f, 1f)] public float occupancy01;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(float dt)
    {
        if (venueBio != null)
        {
            venueBio.activity01 = occupancy01;
            venueBio.pace01 = calm01;
            venueBio.stress01 = Mathf.Clamp01(1f - calm01) * 0.3f;
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Spa Venue")]
public sealed class SpaVenueRuntime : MonoBehaviour
{
    public SpaBioRhythm bio;
    public CivilVenueAmenities amenities;
    public bool isOpen;
    public bool useValet;

    void Awake()
    {
        if (bio == null) bio = GetComponent<SpaBioRhythm>() ?? gameObject.AddComponent<SpaBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public SpaCard Treatment(string kind)
    {
        var w = new WrestlingCard { mode = WrestlingMode.Play, moveKind = WrestlingMoveKind.LockGrapple };
        return SpaCard.Generate(kind, w);
    }

    public ValetCard ValetDuty() => ValetCard.Generate(amenities?.parkingLot);
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Private Industry Bio Rhythm")]
public sealed class PrivateIndustryBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public CompanyRegistration company;
    public bool homeBusiness;
    public string hoursCron = "* 9-17 * * 1-5";

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (company == null) company = GetComponent<CompanyRegistration>();
    }

    public void Tick(DateTime utcNow)
    {
        bool open = homeBusiness || CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
            venueBio.activity01 = open ? 0.55f : 0.1f;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Private Industry Venue")]
public sealed class PrivateIndustryVenueRuntime : MonoBehaviour
{
    public PrivateIndustryBioRhythm bio;
    public CivilVenueAmenities amenities;
    public bool optionalValetOrCheckpoint;
    public bool isOpen;

    void Awake()
    {
        if (bio == null)
            bio = GetComponent<PrivateIndustryBioRhythm>() ?? gameObject.AddComponent<PrivateIndustryBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public MonarchCard OwnerDecorum() => MonarchCard.Generate("office");
    public ValetCard OptionalValet() => ValetCard.Generate(amenities?.parkingLot);
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Barber Bio Rhythm")]
public sealed class BarberBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float chairOccupancy01;
    [Range(0f, 1f)] public float wetStation01;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(float dt)
    {
        if (venueBio != null)
        {
            venueBio.activity01 = chairOccupancy01;
            venueBio.pace01 = Mathf.Clamp01(0.4f + wetStation01 * 0.2f);
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Barber Shop Venue")]
public sealed class BarberShopVenueRuntime : MonoBehaviour
{
    public BarberBioRhythm bio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public bool isOpen;
    public Transform chair;

    void Awake()
    {
        if (bio == null) bio = GetComponent<BarberBioRhythm>() ?? gameObject.AddComponent<BarberBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null) company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public BarberCard CutDuty(string cutId, HairdoBlend wanted = null)
    {
        float wet = bio != null ? bio.wetStation01 : 0f;
        return BarberCard.Generate(cutId, wanted, wet);
    }
}
