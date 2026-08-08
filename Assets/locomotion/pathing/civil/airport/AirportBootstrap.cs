using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Bootstrap")]
public sealed class AirportBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
        if (GetComponent<AirportRuntime>() == null)
            gameObject.AddComponent<AirportRuntime>();
        if (GetComponent<AirPortBioRhythm>() == null)
            gameObject.AddComponent<AirPortBioRhythm>();
        if (GetComponent<AirportBuildingRagdoll>() == null)
            gameObject.AddComponent<AirportBuildingRagdoll>();
        if (GetComponent<AirTrafficControlBioRhythm>() == null)
            gameObject.AddComponent<AirTrafficControlBioRhythm>();
        if (GetComponent<TransportationAuthorityBioRhythm>() == null)
            gameObject.AddComponent<TransportationAuthorityBioRhythm>();
        if (GetComponent<MissionControlBioRhythm>() == null)
            gameObject.AddComponent<MissionControlBioRhythm>();
        if (GetComponent<AuthWarden>() == null)
            gameObject.AddComponent<AuthWarden>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<AirportRunwaySgGenerator>() == null)
            gameObject.AddComponent<AirportRunwaySgGenerator>();
        if (GetComponent<PersonaShiftManager>() == null)
            gameObject.AddComponent<PersonaShiftManager>();
        GetComponent<AirportRuntime>()?.EnsureComponents();
    }
}
