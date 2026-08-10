using System.Collections.Generic;
using UnityEngine;

/// <summary>Standalone gas station — pumps, front desk, store, kitchen, bathrooms; public/private gov link.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Gas/Gas Station")]
public sealed class GasStationRuntime : MonoBehaviour
{
    public GasStationBioRhythm bio;
    public TransportationAuthorityBioRhythm authority;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public BuildingRagdoll buildingRagdoll;
    public StoreBase store;
    public RestaurantVenueRuntime kitchen;
    public Transform frontDesk;
    public Transform bathroomAnchor;
    public List<FuelPumpRuntime> pumps = new List<FuelPumpRuntime>();
    public bool publicStation = true;
    public bool governmentAssigned = true;

    [Header("Ownership seeds")]
    public string governmentCompanyId = "government";
    public string publicFuelAuthCompanyId = "public_fuel_auth";
    public string privateFuelCompanyId = "private_fuel_co";
    public string linkedTrainCompanyId;

    void Awake()
    {
        EnsureComponents();
        SeedCompanyHierarchy();
    }

    public void EnsureComponents()
    {
        if (bio == null)
            bio = GetComponent<GasStationBioRhythm>() ?? gameObject.AddComponent<GasStationBioRhythm>();
        bio.station = this;
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>()
                        ?? FindFirstObjectByType<TransportationAuthorityBioRhythm>();
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        amenities.company = company;
        amenities.frontDesk = frontDesk != null ? frontDesk : amenities.frontDesk;
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>() ?? gameObject.AddComponent<BuildingRagdoll>();
        if (store == null)
            store = GetComponent<StoreBase>() ?? gameObject.AddComponent<StoreBase>();
        store.storeType = "convenience_store";
        store.builtinPromptKey = "convenience_store";
        if (kitchen == null)
            kitchen = GetComponentInChildren<RestaurantVenueRuntime>();
        if (pumps.Count == 0)
            pumps.AddRange(GetComponentsInChildren<FuelPumpRuntime>(true));
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
    }

    public void SeedCompanyHierarchy()
    {
        if (company == null) return;
        company.companyId = publicStation ? publicFuelAuthCompanyId : privateFuelCompanyId;
        company.parentCompanyId = governmentAssigned ? governmentCompanyId : "";
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "station_manager", peckingOrder = 3, personaKey = "gas_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "clerk", peckingOrder = 15, personaKey = "store_clerk" });
            company.staff.Add(new RetinuePeckingEntry { role = "attendant", peckingOrder = 20, personaKey = "fuel_attendant" });
        }
        if (store != null)
        {
            var storeCo = store.GetComponent<CompanyRegistration>();
            if (storeCo == null) storeCo = store.gameObject.AddComponent<CompanyRegistration>();
            if (string.IsNullOrEmpty(storeCo.parentCompanyId))
                storeCo.parentCompanyId = company.companyId;
        }
        if (kitchen != null)
        {
            var kitchenCo = kitchen.GetComponent<CompanyRegistration>()
                            ?? kitchen.gameObject.AddComponent<CompanyRegistration>();
            if (string.IsNullOrEmpty(kitchenCo.parentCompanyId))
                kitchenCo.parentCompanyId = company.companyId;
        }
    }

    public void SetOpen(bool open)
    {
        if (bio != null) bio.isOpen = open;
        store?.SetOpen(open);
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
        kitchen?.SetOpen(open);
    }

    public void RecordFuelSale(GameObject vehicle, float amount01)
    {
        SendMessage("OnGasFuelSale", vehicle, SendMessageOptions.DontRequireReceiver);
        DebitShelfCommodity("fuel", amount01 * 10f);
    }

    public void CreditLinkedTrainCompany(float amount01)
    {
        if (string.IsNullOrEmpty(linkedTrainCompanyId)) return;
        SendMessage("OnGasCreditTrainCompany", linkedTrainCompanyId + "|" + amount01,
            SendMessageOptions.DontRequireReceiver);
    }

    public void DebitShelfCommodity(string commodityKey, float qty)
    {
        if (store?.shelves == null) return;
        for (int i = 0; i < store.shelves.Count; i++)
        {
            var s = store.shelves[i];
            if (s == null || s.commodityKey != commodityKey) continue;
            s.quantity = Mathf.Max(0f, s.quantity - qty);
            return;
        }
    }

    public StoreShelfSlot FindShelfByLemma(string lemma)
    {
        if (store?.shelves == null) return null;
        float band = GasStationShelfLemmaKeys.VerticalBand01(lemma);
        StoreShelfSlot best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < store.shelves.Count; i++)
        {
            var s = store.shelves[i];
            if (s == null) continue;
            float y01 = Mathf.Clamp01(s.localPosition.y);
            float d = Mathf.Abs(y01 - band);
            if (d < bestDist) { bestDist = d; best = s; }
            if (GasStationShelfLemmaKeys.ImpliesHighPrice(lemma, s.commodityKey))
                s.price = Mathf.Max(s.price, s.price * 1.25f + 1f);
        }
        return best;
    }

    public FuelPumpRuntime FindRailPump(string railSegmentId)
    {
        for (int i = 0; i < pumps.Count; i++)
        {
            var p = pumps[i];
            if (p != null && !string.IsNullOrEmpty(p.railSegmentId)
                && (string.IsNullOrEmpty(railSegmentId)
                    || string.Equals(p.railSegmentId, railSegmentId, System.StringComparison.OrdinalIgnoreCase)))
                return p;
        }
        return null;
    }
}
