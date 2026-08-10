using System.Collections.Generic;
using UnityEngine;

/// <summary>Stretch factory venue — gate + manufacturing line, company, amenities.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Factory/Factory Runtime")]
public class FactoryRuntime : MonoBehaviour
{
    public FactoryBioRhythm bio;
    public CivilVenueAmenities amenities;
    public CompanyRegistration company;
    public BuildingRagdoll buildingRagdoll;
    public Transform gateAnchor;
    public Transform lineAnchor;
    public bool publicFactory = true;
    public bool governmentAssigned;

    [Header("Ownership")]
    public string governmentCompanyId = "government";
    public string publicFactoryCompanyId = "public_factory_co";
    public string privateFactoryCompanyId = "private_factory_co";

    protected virtual void Awake()
    {
        EnsureComponents();
        SeedCompanyHierarchy();
    }

    public virtual void EnsureComponents()
    {
        if (bio == null)
            bio = GetComponent<FactoryBioRhythm>() ?? gameObject.AddComponent<FactoryBioRhythm>();
        bio.factory = this;
        if (amenities == null)
            amenities = GetComponent<CivilVenueAmenities>() ?? gameObject.AddComponent<CivilVenueAmenities>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        amenities.company = company;
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>() ?? gameObject.AddComponent<BuildingRagdoll>();
        if (GetComponent<CentralDispatchHub>() == null && CentralDispatchHub.Instance == null)
            gameObject.AddComponent<CentralDispatchHub>();
    }

    public virtual void SeedCompanyHierarchy()
    {
        if (company == null) return;
        company.companyId = publicFactory ? publicFactoryCompanyId : privateFactoryCompanyId;
        company.parentCompanyId = governmentAssigned ? governmentCompanyId : "";
        if (company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "factory_manager", peckingOrder = 3, personaKey = "factory_manager" });
            company.staff.Add(new RetinuePeckingEntry { role = "line_worker", peckingOrder = 20, personaKey = "line_worker" });
        }
    }

    public virtual void SetOpen(bool open)
    {
        if (bio != null) bio.isOpen = open;
        if (open) amenities?.OnVenueOpen();
        else amenities?.OnVenueClose();
    }
}
