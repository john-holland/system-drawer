using UnityEngine;

/// <summary>Wake/sleep stub runtime for stretch institutions until full BT ships.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Institution Stub")]
public sealed class CivilInstitutionStub : MonoBehaviour
{
    public CivilSystemKind kind = CivilSystemKind.Generic;
    public string buildingTypeId;
    public string hoursCron = "* 8-20 * * *";
    public BuildingRagdoll buildingRagdoll;
    public StoreBase store;
    public bool isAwake;

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
        if (store == null)
            store = GetComponent<StoreBase>();
        if (string.IsNullOrEmpty(buildingTypeId))
            buildingTypeId = kind.ToString().ToLowerInvariant();
        if (IsHospitalityOrSecurityKind(kind) && GetComponent<HospitalityInstitutionBootstrap>() == null)
            gameObject.AddComponent<HospitalityInstitutionBootstrap>();
    }

    static bool IsHospitalityOrSecurityKind(CivilSystemKind k)
    {
        return k == CivilSystemKind.NightClub || k == CivilSystemKind.Bar
            || k == CivilSystemKind.Inn || k == CivilSystemKind.Hotel
            || k == CivilSystemKind.MilitaryCheckpoint || k == CivilSystemKind.SpyAgency
            || k == CivilSystemKind.Embassy || k == CivilSystemKind.GovLegislative
            || k == CivilSystemKind.Monarchic || k == CivilSystemKind.Spa
            || k == CivilSystemKind.PrivateIndustry || k == CivilSystemKind.BarberShop;
    }

    public void SetAwake(bool awake)
    {
        isAwake = awake;
        if (store != null)
            store.SetOpen(awake);
        else if (buildingRagdoll?.bio != null)
        {
            if (awake) buildingRagdoll.bio.NotifyOpen();
            else buildingRagdoll.bio.NotifyClosed();
        }
        var amenities = GetComponent<CivilVenueAmenities>();
        if (amenities != null)
        {
            if (awake) amenities.OnVenueOpen();
            else amenities.OnVenueClose();
        }
        SendMessage(awake ? "OnCivilVenueOpen" : "OnCivilVenueClose", this, SendMessageOptions.DontRequireReceiver);
    }

    public CivilVenueNode ToVenueNode(string stableId = null)
    {
        return new CivilVenueNode
        {
            stableId = string.IsNullOrEmpty(stableId) ? gameObject.name : stableId,
            kind = kind,
            buildingTypeId = buildingTypeId,
            hoursCron = hoursCron,
            contextOwner = gameObject,
            venueBio = GetComponent<CivilVenueBioRhythmService>()
        };
    }
}
