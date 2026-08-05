using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Facility biorhythm for police station building.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Station Bio Rhythm")]
public sealed class PoliceStationBioRhythm : MonoBehaviour
{
    public CompanyRegistration company;
    public CivilVenueBioRhythmService venueBio;
    [Range(0f, 1f)] public float holdingLoad01;
    [Range(0f, 1f)] public float deskActivity01 = 0.4f;
    [Range(0f, 1f)] public float alert01;
    public bool stationSirenOn;
    public string hoursCron = "* * * * *";

    void Awake()
    {
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (company != null && company.fundingSources.Count == 0)
            company.fundingSources.Add(new CompanyFundingSource { sourceId = "gov_local", label = "Municipal", share01 = 1f });
    }

    public void Tick(DateTime utcNow, float dt)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = open ? Mathf.Clamp01(0.4f + deskActivity01 * 0.3f + holdingLoad01 * 0.2f) : 0.15f;
            venueBio.stress01 = alert01;
        }
    }

    public void SetStationSiren(bool on) => stationSirenOn = on;
}

/// <summary>Dispatch biorhythm — facilitates CopCards; hub serviceId police.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Dispatch Bio Rhythm")]
public sealed class PoliceDispatchBioRhythm : DispatchBioRhythm
{
    public PoliceStationBioRhythm stationBio;
    public ViolenceTelecomHint violenceHint;
    [Tooltip("Default ladder for traffic_detail dispatch requests.")]
    public TrafficDetailLadderAsset trafficDetailLadder;
    int _lastHintCount;

    protected override void Awake()
    {
        serviceId = "police";
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (stationBio == null)
            stationBio = GetComponent<PoliceStationBioRhythm>();
        if (company == null && stationBio != null)
            company = stationBio.company;
        if (violenceHint == null)
            violenceHint = ViolenceTelecomHint.Instance ?? FindFirstObjectByType<ViolenceTelecomHint>();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        IngestViolenceHints();
    }

    void IngestViolenceHints()
    {
        if (violenceHint == null || violenceHint.recentViolenceHints.Count == 0) return;
        if (violenceHint.recentViolenceHints.Count <= _lastHintCount) return;
        _lastHintCount = violenceHint.recentViolenceHints.Count;
        var card = violenceHint.MakePatrolCardFromLatest();
        if (card == null) return;
        Enqueue(new DispatchRequest
        {
            kind = "route",
            priority01 = 0.7f,
            worldTarget = card.goalWorld,
            notes = "violence_telecom"
        });
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;

        bool trafficDetail = (request.kind ?? "").Equals("traffic_detail", System.StringComparison.OrdinalIgnoreCase)
                             || (request.notes ?? "").IndexOf("traffic_detail", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (trafficDetail)
        {
            var ladder = trafficDetailLadder != null ? trafficDetailLadder : TrafficDetailLadderAsset.CreateDefaultRuntime();
            for (int i = 0; i < ladder.steps.Count; i++)
            {
                var detail = DispatchPoliceDetailCard.GenerateFromLadder(ladder, request.worldTarget, i);
                cards.Add(detail);
                cards.AddRange(detail.ExpandStepCards());
            }
            cards.AddRange(base.FacilitateCards(request));
            return cards;
        }

        cards.Add(CopCard.Generate("respond", request.worldTarget));
        if ((request.notes ?? "").Contains("violence"))
            cards.Add(CopPullOverCard.Generate(request.worldTarget));
        cards.AddRange(base.FacilitateCards(request));
        return cards;
    }

    public List<GoodSection> FacilitatePatrolFromHint()
    {
        var patrol = InstitutionBuiltinCards.PolicePatrolFromViolenceHint();
        return new List<GoodSection> { CopCard.Generate("patrol", patrol.goalWorld), patrol };
    }
}
