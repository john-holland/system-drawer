using UnityEngine;

/// <summary>Attaches gas station runtime/bio on CivilInstitutionStub wake.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Gas/Gas Station Bootstrap")]
public sealed class GasStationBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null) stub.kind = CivilSystemKind.GasStation;
        var runtime = GetComponent<GasStationRuntime>() ?? gameObject.AddComponent<GasStationRuntime>();
        runtime.EnsureComponents();
        runtime.SeedCompanyHierarchy();
        if (GetComponent<GasStationBioRhythm>() == null)
            gameObject.AddComponent<GasStationBioRhythm>();
    }
}
