using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Station Bootstrap")]
public sealed class PoliceStationBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (GetComponent<PoliceStationBuildingRagdoll>() == null)
            gameObject.AddComponent<PoliceStationBuildingRagdoll>();
        if (GetComponent<PoliceStationBioRhythm>() == null)
            gameObject.AddComponent<PoliceStationBioRhythm>();
        if (GetComponent<PoliceDispatchBioRhythm>() == null)
            gameObject.AddComponent<PoliceDispatchBioRhythm>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (ViolenceTelecomHint.Instance == null && FindFirstObjectByType<ViolenceTelecomHint>() == null)
        {
            var hintGo = new GameObject("ViolenceTelecomHint");
            hintGo.AddComponent<ViolenceTelecomHint>();
        }
        if (CentralDispatchHub.Instance == null && FindFirstObjectByType<CentralDispatchHub>() == null)
        {
            var hubGo = new GameObject("CentralDispatchHub");
            hubGo.AddComponent<CentralDispatchHub>();
        }
    }
}
