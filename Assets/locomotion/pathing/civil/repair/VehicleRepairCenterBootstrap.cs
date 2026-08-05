using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicle Repair Center Bootstrap")]
public sealed class VehicleRepairCenterBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (GetComponent<VehicleRepairCenterRuntime>() == null)
            gameObject.AddComponent<VehicleRepairCenterRuntime>();
        if (GetComponent<VehicleRepairCenterBioRhythm>() == null)
            gameObject.AddComponent<VehicleRepairCenterBioRhythm>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<StoreBase>() == null)
            gameObject.AddComponent<StoreBase>();
        if (GetComponent<BuildingRagdoll>() == null)
            gameObject.AddComponent<BuildingRagdoll>();
    }
}
