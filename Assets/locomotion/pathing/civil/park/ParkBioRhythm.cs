using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Park bio — grounds vs building TA cards; peers TA + PersonaDay justice patrol.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Park/Park Bio Rhythm")]
public sealed class ParkBioRhythm : DispatchBioRhythm
{
    public ParkRuntime park;
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public bool isOpen = true;

    protected override void Awake()
    {
        serviceId = "park";
        if (park == null)
            park = GetComponent<ParkRuntime>();
        governmentAssigned = park == null || park.governmentAssigned;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        bool due = CronDue.IsActiveSchedule(hoursCron, utcNow);
        isOpen = due;
        unitsAvailable01 = isOpen ? 1f : 0.2f;
        if (venueBio != null)
        {
            venueBio.activity01 = isOpen ? 0.45f : 0.05f;
            venueBio.stress01 = isOpen ? 0.15f : 0.04f;
        }
        park?.SetOpen(isOpen);
        TickGrass(dt);
    }

    void TickGrass(float dt)
    {
        if (park?.lots == null) return;
        for (int i = 0; i < park.lots.Count; i++)
        {
            var lot = park.lots[i];
            if (lot?.grass != null)
                lot.grass.TickGrowth(dt);
        }
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case "ta_bay_repair":
            case "inspect":
            case "park_maintenance":
                cards.Add(TAMaintenanceCard.GenerateInspect(null));
                cards.Add(ParkMaintenanceCard.Generate(request, park));
                break;
            case "park_patrol":
            case "justice":
                cards.Add(ParkJusticePatrolCard.Generate(request, park));
                break;
            case "park_horticulture":
            case "park_grounds":
                cards.Add(ParkHorticultureCard.Generate(request, park));
                break;
            case TADispatchKinds.Fuel:
                if (park?.attachedGasStation?.bio != null)
                    return park.attachedGasStation.bio.FacilitateCards(request);
                cards.Add(TAVehicleFuelCard.Generate(request));
                break;
            default:
                cards.Add(ParkMaintenanceCard.Generate(request, park));
                cards.Add(ParkHorticultureCard.Generate(request, park));
                cards.Add(ParkJusticePatrolCard.Generate(request, park));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
