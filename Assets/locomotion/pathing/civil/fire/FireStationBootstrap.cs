using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fire Station Bootstrap")]
public sealed class FireStationBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (GetComponent<FireStationBuildingRagdoll>() == null)
            gameObject.AddComponent<FireStationBuildingRagdoll>();
        if (GetComponent<FirehouseBioRhythm>() == null)
            gameObject.AddComponent<FirehouseBioRhythm>();
        if (GetComponent<FireWarden>() == null)
            gameObject.AddComponent<FireWarden>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (CentralDispatchHub.Instance == null && FindFirstObjectByType<CentralDispatchHub>() == null)
        {
            var hubGo = new GameObject("CentralDispatchHub");
            hubGo.AddComponent<CentralDispatchHub>();
        }
    }
}
