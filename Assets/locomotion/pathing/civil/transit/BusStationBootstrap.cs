using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Bus Station Bootstrap")]
public sealed class BusStationBootstrap : MonoBehaviour
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
        if (GetComponent<BusStationRuntime>() == null)
            gameObject.AddComponent<BusStationRuntime>();
        if (GetComponent<BusStationBioRhythm>() == null)
            gameObject.AddComponent<BusStationBioRhythm>();
        if (GetComponent<TransportationAuthorityBioRhythm>() == null)
            gameObject.AddComponent<TransportationAuthorityBioRhythm>();
        if (GetComponent<MissionControlBioRhythm>() == null)
            gameObject.AddComponent<MissionControlBioRhythm>();
        if (GetComponent<AirTrafficControlBioRhythm>() == null)
            gameObject.AddComponent<AirTrafficControlBioRhythm>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<BuildingRagdoll>() == null)
            gameObject.AddComponent<BuildingRagdoll>();
        if (GetComponent<BusStationSgGenerator>() == null)
            gameObject.AddComponent<BusStationSgGenerator>();
        GetComponent<BusStationRuntime>()?.EnsureComponents();
    }
}
