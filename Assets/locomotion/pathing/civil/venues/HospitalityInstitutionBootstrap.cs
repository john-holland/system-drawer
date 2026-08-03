using UnityEngine;

/// <summary>Attaches kind-appropriate bio/runtime stubs on CivilInstitutionStub wake.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Hospitality Institution Bootstrap")]
public sealed class HospitalityInstitutionBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        EnsurePackage();
    }

    public void EnsurePackage()
    {
        if (stub == null) return;
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();

        switch (stub.kind)
        {
            case CivilSystemKind.NightClub:
                Ensure<NightClubVenueRuntime>();
                Ensure<NightClubBioRhythm>();
                Ensure<MusicAmbianceSchedule>();
                Ensure<BeatQuantizedActionBinder>();
                break;
            case CivilSystemKind.Bar:
                Ensure<BarVenueRuntime>();
                Ensure<BarBioRhythm>();
                Ensure<MusicAmbianceSchedule>();
                Ensure<BeatQuantizedActionBinder>();
                break;
            case CivilSystemKind.Inn:
                EnsureInnHotel(false);
                break;
            case CivilSystemKind.Hotel:
                EnsureInnHotel(true);
                break;
            case CivilSystemKind.MilitaryCheckpoint:
                Ensure<CheckpointVenueRuntime>();
                Ensure<CheckpointBioRhythm>();
                break;
            case CivilSystemKind.SpyAgency:
                Ensure<SpyAgencyVenueRuntime>();
                Ensure<SpyAgencyBioRhythm>();
                Ensure<CompanyRegistration>();
                Ensure<MusicAmbianceSchedule>();
                break;
            case CivilSystemKind.Embassy:
                Ensure<EmbassyVenueRuntime>();
                Ensure<EmbassyBioRhythm>();
                break;
            case CivilSystemKind.GovLegislative:
                Ensure<GovLegislativeVenueRuntime>();
                Ensure<GovernmentLegislativeBuildingBioRhythm>();
                Ensure<CompanyRegistration>();
                break;
            case CivilSystemKind.Monarchic:
                Ensure<MonarchicVenueRuntime>();
                Ensure<MonarchicBuildingBioRhythm>();
                break;
            case CivilSystemKind.Spa:
                Ensure<SpaVenueRuntime>();
                Ensure<SpaBioRhythm>();
                break;
            case CivilSystemKind.PrivateIndustry:
                Ensure<PrivateIndustryVenueRuntime>();
                Ensure<PrivateIndustryBioRhythm>();
                Ensure<CompanyRegistration>();
                break;
            case CivilSystemKind.BarberShop:
                Ensure<BarberShopVenueRuntime>();
                Ensure<BarberBioRhythm>();
                Ensure<CompanyRegistration>();
                break;
        }
    }

    void EnsureInnHotel(bool hotel)
    {
        var rt = Ensure<InnHotelVenueRuntime>();
        rt.isHotel = hotel;
        Ensure<InnBioRhythm>().corporateMode = hotel;
        Ensure<CompanyRegistration>();
        Ensure<KeycardAccessRegistry>();
    }

    T Ensure<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }
}
