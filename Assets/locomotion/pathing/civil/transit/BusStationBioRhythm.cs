using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Station passenger-flow biorhythm — composes venue bio + TA peer + CommuterCards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Bus Station Bio Rhythm")]
public sealed class BusStationBioRhythm : MonoBehaviour
{
    public string hoursCron = "* 5-23 * * *";
    public CivilVenueBioRhythmService venueBio;
    public TransportationAuthorityBioRhythm authority;
    [Range(0f, 1f)] public float passengerDensity01;
    [Range(0f, 1f)] public float impatience01;
    public bool isOpen;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                       ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (authority == null)
            authority = GetComponent<TransportationAuthorityBioRhythm>();
    }

    public void Tick(DateTime utcNow, float dt)
    {
        isOpen = CronDue.IsActiveSchedule(hoursCron, utcNow);
        if (venueBio != null)
        {
            venueBio.activity01 = isOpen ? Mathf.Clamp01(0.3f + passengerDensity01 * 0.5f) : 0.08f;
            venueBio.stress01 = Mathf.Clamp01(impatience01 * 0.6f + passengerDensity01 * 0.2f);
        }
    }

    public List<GoodSection> FacilitateCommuterCards(GameObject actor, BusVehicleRagdoll bus, string stopId)
    {
        var cards = new List<GoodSection>
        {
            CommuterWaitCard.Generate(actor, bus, stopId),
            CommuterBoardVehicleCard.Generate(actor, bus, stopId),
            CommuterStowLuggageCard.Generate(actor, bus, stopId),
            CommuterFindSeatCard.Generate(actor, bus, stopId),
            CommuterStopRequestCard.Generate(actor, bus, stopId),
            CommuterExitCard.Generate(actor, bus, stopId)
        };
        if (impatience01 > 0.55f)
            cards.Insert(1, CommuterComplaintCard.Generate(actor, bus, stopId));
        return cards;
    }
}
