using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Textile Mill Bootstrap")]
public sealed class TextileMillBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null && stub.kind == CivilSystemKind.Generic)
            stub.kind = CivilSystemKind.Factory;
        var runtime = GetComponent<FactoryRuntime>() ?? gameObject.AddComponent<FactoryRuntime>();
        runtime.EnsureComponents();
        runtime.SeedCompanyHierarchy();
        if (GetComponent<TextileMillBioRhythm>() == null)
            gameObject.AddComponent<TextileMillBioRhythm>();
        if (GetComponent<StationShiftSchedule>() == null)
            gameObject.AddComponent<StationShiftSchedule>();
        var bio = GetComponent<TextileMillBioRhythm>();
        bio.factory = runtime;
        bio.shifts = GetComponent<StationShiftSchedule>();
    }
}
