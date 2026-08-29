using UnityEngine;

/// <summary>Attaches TheocraticWarden on existing church venues.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Church Theocracy Bootstrap")]
public sealed class ChurchTheocracyBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null)
            stub.kind = CivilSystemKind.Church;
        if (GetComponent<TheocraticWarden>() == null)
            gameObject.AddComponent<TheocraticWarden>();
        if (GetComponent<CivilVenueBioRhythmService>() == null)
            gameObject.AddComponent<CivilVenueBioRhythmService>();
    }
}
