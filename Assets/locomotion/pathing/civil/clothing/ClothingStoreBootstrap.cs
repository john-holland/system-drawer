using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Clothing Store Bootstrap")]
public sealed class ClothingStoreBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake() => Ensure();

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null) stub.kind = CivilSystemKind.ClothingStore;
        var ragdoll = GetComponent<ClothingStoreRagdoll>() ?? gameObject.AddComponent<ClothingStoreRagdoll>();
        if (ragdoll.store == null)
            ragdoll.store = GetComponent<StoreBase>() ?? gameObject.AddComponent<StoreBase>();
        ragdoll.store.storeType = "clothing_store";
        ragdoll.store.builtinPromptKey = "clothing_store";
        if (ragdoll.store.shelves == null || ragdoll.store.shelves.Count == 0)
            ragdoll.store.FillShelvesFromCatalog(ClothingCommodities.All, 8);
        if (GetComponent<TayloringBioRhythm>() == null)
            gameObject.AddComponent<TayloringBioRhythm>();
        if (GetComponent<TayloringTravelAgent>() == null)
            gameObject.AddComponent<TayloringTravelAgent>();
        if (GetComponent<StationShiftSchedule>() == null)
            gameObject.AddComponent<StationShiftSchedule>();
        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        var company = GetComponent<CompanyRegistration>();
        if (string.IsNullOrEmpty(company.companyId))
            company.companyId = "clothing_store";
    }
}
