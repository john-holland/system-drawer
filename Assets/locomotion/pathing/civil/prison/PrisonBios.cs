using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Facility biorhythm for a prison building.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Bio Rhythm")]
public sealed class PrisonBioRhythm : MonoBehaviour
{
    public CompanyRegistration company;
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float occupancy01;
    [Range(0f, 1f)] public float yardActivity01 = 0.35f;
    [Range(0f, 1f)] public float alert01;
    public string hoursCron = "* * * * *";

    void Awake()
    {
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (company != null && company.fundingSources.Count == 0)
            company.fundingSources.Add(new CompanyFundingSource { sourceId = "gov_corrections", label = "Corrections", share01 = 1f });
    }

    public void Tick(DateTime utcNow, float dt)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = open ? Mathf.Clamp01(0.35f + occupancy01 * 0.4f + yardActivity01 * 0.2f) : 0.2f;
            venueBio.stress01 = alert01;
        }
    }
}

/// <summary>Dispatch biorhythm — facilitates prison cards; hub serviceId corrections.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Prison Dispatch Bio Rhythm")]
public sealed class PrisonDispatchBioRhythm : DispatchBioRhythm
{
    public PrisonBioRhythm stationBio;
    public PrisonBuildingRagdoll building;

    protected override void Awake()
    {
        serviceId = "corrections";
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (stationBio == null)
            stationBio = GetComponent<PrisonBioRhythm>();
        if (company == null && stationBio != null)
            company = stationBio.company;
        if (building == null)
            building = GetComponent<PrisonBuildingRagdoll>();
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = PrisonFacilitateCards.ForRequest(this, request);
        cards.AddRange(base.FacilitateCards(request));
        return cards;
    }
}
