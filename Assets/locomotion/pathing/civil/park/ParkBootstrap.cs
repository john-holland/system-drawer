using UnityEngine;

/// <summary>Attaches park runtime/bio on CivilInstitutionStub wake.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Park/Park Bootstrap")]
public sealed class ParkBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null) stub.kind = CivilSystemKind.Park;
        var runtime = GetComponent<ParkRuntime>() ?? gameObject.AddComponent<ParkRuntime>();
        runtime.EnsureComponents();
        runtime.SeedCompanyHierarchy();
        if (GetComponent<ParkBioRhythm>() == null)
            gameObject.AddComponent<ParkBioRhythm>();
    }
}
