using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Bootstrap")]
public sealed class PrisonBootstrap : MonoBehaviour
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
        if (stub != null && stub.kind == CivilSystemKind.Generic)
            stub.kind = CivilSystemKind.Prison;
        if (stub != null)
            stub.kind = CivilSystemKind.Prison;

        if (GetComponent<PrisonBuildingRagdoll>() == null)
            gameObject.AddComponent<PrisonBuildingRagdoll>();
        if (GetComponent<PrisonBioRhythm>() == null)
            gameObject.AddComponent<PrisonBioRhythm>();
        if (GetComponent<PrisonDispatchBioRhythm>() == null)
            gameObject.AddComponent<PrisonDispatchBioRhythm>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<AuthWarden>() == null)
            gameObject.AddComponent<AuthWarden>();
        if (GetComponent<KeycardLock>() == null)
            gameObject.AddComponent<KeycardLock>();
        if (GetComponent<PrisonWarden>() == null)
            gameObject.AddComponent<PrisonWarden>();
        if (GetComponent<ThreatWarden>() == null)
            gameObject.AddComponent<ThreatWarden>();
        if (GetComponent<GenevaConventionWarden>() == null)
            gameObject.AddComponent<GenevaConventionWarden>();
        var prison = GetComponent<PrisonWarden>();
        var threat = GetComponent<ThreatWarden>();
        var geneva = GetComponent<GenevaConventionWarden>();
        prison.threatWarden = threat;
        prison.genevaWarden = geneva;
        geneva.threatWarden = threat;
        geneva.prisonWarden = prison;
        if (GetComponent<PrisonRetinueClient>() == null)
            gameObject.AddComponent<PrisonRetinueClient>();
        if (CentralDispatchHub.Instance == null && FindFirstObjectByType<CentralDispatchHub>() == null)
        {
            var hubGo = new GameObject("CentralDispatchHub");
            hubGo.AddComponent<CentralDispatchHub>();
        }
    }
}
