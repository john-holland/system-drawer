using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Checkpoint Bio Rhythm")]
public sealed class CheckpointBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public string opsCenterDesignation = "ops";
    public string hoursCron = "* * * * *";
    [Range(0f, 1f)] public float alert01 = 0.3f;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(DateTime utcNow)
    {
        bool active = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = active ? Mathf.Clamp01(0.5f + alert01 * 0.4f) : 0.2f;
            venueBio.stress01 = alert01;
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Military Checkpoint Venue")]
public sealed class CheckpointVenueRuntime : MonoBehaviour
{
    public CheckpointBioRhythm bio;
    public CivilVenueAmenities amenities;
    public bool isOpen = true;
    public List<Transform> entrances = new List<Transform>();
    public List<Transform> exits = new List<Transform>();
    public Transform bedsRoot;

    void Awake()
    {
        if (bio == null) bio = GetComponent<CheckpointBioRhythm>() ?? gameObject.AddComponent<CheckpointBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public CheckpointCard GateDuty(string postId) => CheckpointCard.Generate(postId);
    public CheckpointCard OpsDuty() => CheckpointCard.Generate(bio != null ? bio.opsCenterDesignation : "ops", true);
    public JusticeCard EntranceJustice() => JusticeCard.Generate(JusticeAction.SecureArea, gameObject);
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Spy Agency Bio Rhythm")]
public sealed class SpyAgencyBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public CompanyRegistration company;
    public List<string> govIpList = new List<string>();
    public string hoursCron = "* 8-20 * * 1-5";
    [Range(0f, 1f)] public float secrecy01 = 0.8f;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (company == null) company = GetComponent<CompanyRegistration>();
    }

    public void Tick(DateTime utcNow)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = open ? 0.55f : 0.15f;
            venueBio.stress01 = secrecy01 * 0.4f;
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Spy Agency Venue")]
public sealed class SpyAgencyVenueRuntime : MonoBehaviour
{
    public SpyAgencyBioRhythm bio;
    public CivilVenueAmenities amenities;
    public MusicAmbianceSchedule meetingMusic;
    [Range(0f, 1f)] public float staffJusticeSteep01 = 0.9f;
    public bool isOpen;

    void Awake()
    {
        if (bio == null) bio = GetComponent<SpyAgencyBioRhythm>() ?? gameObject.AddComponent<SpyAgencyBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (meetingMusic == null) meetingMusic = GetComponent<MusicAmbianceSchedule>();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    /// <summary>Steep Justice for all staff including kitchen.</summary>
    public JusticeCard StaffJustice(GameObject target = null)
    {
        var c = JusticeCard.Generate(JusticeAction.SecureArea, target ?? gameObject);
        c.violenceThreshold01 = 1f - staffJusticeSteep01 * 0.5f;
        c.sectionName = "spy_staff_justice";
        return c;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Embassy Bio Rhythm")]
public sealed class EmbassyBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public List<string> govLinks = new List<string>();
    [Range(0f, 1f)] public float diplomaticTension01 = 0.3f;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(float dt)
    {
        if (venueBio != null)
        {
            venueBio.stress01 = diplomaticTension01;
            venueBio.activity01 = Mathf.Clamp01(0.4f + diplomaticTension01 * 0.3f);
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Embassy Venue")]
public sealed class EmbassyVenueRuntime : MonoBehaviour
{
    public EmbassyBioRhythm bio;
    public CivilVenueAmenities amenities;
    [Range(0f, 1f)] public float civilianStaffJustice01 = 0.55f;
    [Range(0f, 1f)] public float armyJusticeSteep01 = 0.92f;
    public bool isOpen = true;

    void Awake()
    {
        if (bio == null) bio = GetComponent<EmbassyBioRhythm>() ?? gameObject.AddComponent<EmbassyBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
    }

    public JusticeCard CivilianStaffJustice()
    {
        var c = JusticeCard.Generate(JusticeAction.SecureArea, gameObject);
        c.violenceThreshold01 = civilianStaffJustice01;
        c.sectionName = "embassy_civilian_justice";
        return c;
    }

    public JusticeCard ArmyJustice()
    {
        var c = JusticeCard.Generate(JusticeAction.SecureArea, gameObject);
        c.violenceThreshold01 = 1f - armyJusticeSteep01 * 0.45f;
        c.sectionName = "embassy_army_justice";
        return c;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Government Legislative Bio Rhythm")]
public sealed class GovernmentLegislativeBuildingBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public CompanyRegistration company;
    public string govIpMask = "10.0.0.0/8"; // memo: update with galactic ipv6, 3rd quarter
    public string jurisdictionId = "local";

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (company == null) company = GetComponent<CompanyRegistration>();
    }

    public void Tick(float dt)
    {
        if (venueBio != null)
            venueBio.activity01 = Mathf.MoveTowards(venueBio.activity01, 0.5f, dt * 0.02f);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Gov Legislative Venue")]
public sealed class GovLegislativeVenueRuntime : MonoBehaviour
{
    public GovernmentLegislativeBuildingBioRhythm bio;
    public CivilVenueAmenities amenities;
    public bool isOpen;

    void Awake()
    {
        if (bio == null)
            bio = GetComponent<GovernmentLegislativeBuildingBioRhythm>()
                ?? gameObject.AddComponent<GovernmentLegislativeBuildingBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
    }

    public JusticeCard StateLocalLeJustice()
    {
        var c = JusticeCard.Generate(JusticeAction.CallAuthorities, gameObject);
        c.violenceThreshold01 = 0.5f;
        c.sectionName = "legislative_le_justice";
        return c;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Monarchic Bio Rhythm")]
public sealed class MonarchicBuildingBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public Transform workWaypoint;
    public Transform homeWaypoint;
    [Range(0f, 1f)] public float courtSession01;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (workWaypoint == null) workWaypoint = transform;
        if (homeWaypoint == null) homeWaypoint = transform;
    }

    public void Tick(float dt)
    {
        if (venueBio != null)
            venueBio.activity01 = Mathf.Clamp01(0.35f + courtSession01 * 0.5f);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Monarchic Venue")]
public sealed class MonarchicVenueRuntime : MonoBehaviour
{
    public MonarchicBuildingBioRhythm bio;
    public CivilVenueAmenities amenities;

    void Awake()
    {
        if (bio == null)
            bio = GetComponent<MonarchicBuildingBioRhythm>() ?? gameObject.AddComponent<MonarchicBuildingBioRhythm>();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>();
    }

    public MonarchCard Audience() => MonarchCard.Generate("audience", bio?.workWaypoint, bio?.homeWaypoint);
    public MonarchCard KnightingStub() => MonarchCard.Generate("knighting", bio?.workWaypoint, bio?.homeWaypoint);
    public MonarchCard BowingStub() => MonarchCard.Generate("bowing", bio?.workWaypoint, bio?.homeWaypoint);
}
