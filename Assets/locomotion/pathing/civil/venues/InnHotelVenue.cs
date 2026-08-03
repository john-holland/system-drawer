using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Linens comfort defaults for Inn/Hotel sleep in-paint.</summary>
[Serializable]
public sealed class LinenComfortProfile
{
    public bool comfortable = true;
    public bool soft = true;
    public bool nonSticking = true;
    [Range(0f, 1f)] public float sleepInPaintBias01 = 0.15f;

    public float ComfortScore01()
    {
        float s = 0f;
        if (comfortable) s += 0.4f;
        if (soft) s += 0.35f;
        if (nonSticking) s += 0.25f;
        return Mathf.Clamp01(s + sleepInPaintBias01 * 0.1f);
    }
}

[Serializable]
public sealed class MaintenanceCrewAssignment
{
    public bool hasSuper = true;
    public string companyId;
    public List<string> crewPersonaKeys = new List<string>();
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Inn Bio Rhythm")]
public sealed class InnBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float occupancy01;
    [Range(0f, 1f)] public float housekeepingBacklog01;
    [Range(0f, 1f)] public float guestSatisfaction01 = 0.7f;
    public bool corporateMode; // Hotel = true, Inn = false (nepotism pecking)

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>() ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
    }

    public void Tick(float dt)
    {
        housekeepingBacklog01 = Mathf.MoveTowards(housekeepingBacklog01, occupancy01 * 0.8f, dt * 0.01f);
        guestSatisfaction01 = Mathf.MoveTowards(guestSatisfaction01, 1f - housekeepingBacklog01 * 0.4f, dt * 0.02f);
        if (venueBio != null)
        {
            venueBio.activity01 = occupancy01;
            venueBio.stress01 = housekeepingBacklog01 * 0.5f;
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Inn Hotel Venue")]
public sealed class InnHotelVenueRuntime : MonoBehaviour
{
    public InnBioRhythm bio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public KeycardAccessRegistry keycards;
    public LinenComfortProfile linens = new LinenComfortProfile();
    public MaintenanceCrewAssignment maintenance = new MaintenanceCrewAssignment();
    public bool isHotel;
    public bool isOpen;
    public string lateCheckoutTelecomPolicy = "room_then_cell";
    public SpaVenueRuntime childSpa;

    void Awake()
    {
        if (bio == null) bio = GetComponent<InnBioRhythm>() ?? gameObject.AddComponent<InnBioRhythm>();
        SyncCorporateMode();
        if (amenities == null) amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null) company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (keycards == null) keycards = GetComponent<KeycardAccessRegistry>() ?? gameObject.AddComponent<KeycardAccessRegistry>();
        if (maintenance != null && string.IsNullOrEmpty(maintenance.companyId))
            maintenance.companyId = company.companyId + "_maintenance";
    }

    void OnEnable() => SyncCorporateMode();

    public void SyncCorporateMode()
    {
        if (bio == null) bio = GetComponent<InnBioRhythm>();
        if (bio != null) bio.corporateMode = isHotel;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }

    public HotelCard CheckInStub(string roomId) => HotelCard.Generate("checkin", roomId);
    public HotelCard CheckOutStub(string roomId) => HotelCard.Generate("checkout", roomId);
    public HotelCard WakeupStub(string roomId) => HotelCard.Generate("wakeup", roomId);
    public MaidCard MaidStub(string roomId, bool turndown = false) => MaidCard.Generate(roomId, turndown);

    public void IssueKeycard(string keycardId, string roomNodeId, string guestActorId)
    {
        keycards?.Bind(keycardId, roomNodeId, new List<string> { guestActorId });
    }

    public void NotifyLateCheckout(string roomId, string roomPhoneId, string cellId)
    {
        // Telecom stub: room phone first, then registered cell.
        SendMessage("OnLateCheckoutTelecom", new object[] { roomId, roomPhoneId, cellId, lateCheckoutTelecomPolicy },
            SendMessageOptions.DontRequireReceiver);
    }

    public float SleepInPaintComfort01() => linens != null ? linens.ComfortScore01() : 0.5f;
}
