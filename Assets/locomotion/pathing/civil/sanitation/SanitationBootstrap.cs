using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sanitation Bootstrap")]
public sealed class SanitationBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null) stub.kind = CivilSystemKind.SanitationFacility;
        var runtime = GetComponent<SanitationFacilityRuntime>() ?? gameObject.AddComponent<SanitationFacilityRuntime>();
        runtime.EnsureComponents();
        runtime.SeedCompanyHierarchy();
    }
}
