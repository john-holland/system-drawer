using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Factory/Factory Bootstrap")]
public sealed class FactoryBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null && stub.kind == CivilSystemKind.Generic)
            stub.kind = CivilSystemKind.Factory;
        if (GetComponent<SanitationFacilityRuntime>() != null) return;
        var runtime = GetComponent<FactoryRuntime>() ?? gameObject.AddComponent<FactoryRuntime>();
        runtime.EnsureComponents();
        runtime.SeedCompanyHierarchy();
    }
}
