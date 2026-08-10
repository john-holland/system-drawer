using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Gas station bio — peers TA public/private; facilitates fuel + maintenance + store cards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Gas/Gas Station Bio Rhythm")]
public sealed class GasStationBioRhythm : DispatchBioRhythm
{
    public GasStationRuntime station;
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public bool isOpen = true;

    protected override void Awake()
    {
        serviceId = "gas_station";
        if (station == null)
            station = GetComponent<GasStationRuntime>();
        governmentAssigned = station == null || station.governmentAssigned;
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
        unitsAvailable01 = isOpen ? 1f : 0.15f;
        if (venueBio != null)
        {
            venueBio.activity01 = isOpen ? 0.55f : 0.08f;
            venueBio.stress01 = isOpen ? 0.2f : 0.05f;
        }
        station?.SetOpen(isOpen);
        station?.store?.TickHours(utcNow);
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case TADispatchKinds.Fuel:
            case "gas_fuel":
            case "gas_rail_refuel":
                cards.Add(TAVehicleFuelCard.Generate(request));
                cards.Add(GasStationRailRefuelCard.Generate(request, station));
                break;
            case "ta_bay_repair":
            case "inspect":
                cards.Add(TAMaintenanceCard.GenerateInspect(null));
                break;
            case "gas_store":
                cards.Add(GasStationStoreCard.Generate(request, station));
                break;
            default:
                cards.Add(TAVehicleFuelCard.Generate(request));
                cards.Add(GasStationStoreCard.Generate(request, station));
                break;
        }
        // Peer TA for fleet fuel when available.
        if (station?.authority != null && kind == TADispatchKinds.Fuel)
        {
            var taCards = station.authority.FacilitateCards(request);
            for (int i = 0; i < taCards.Count; i++)
                if (taCards[i] is TAVehicleFuelCard)
                    cards.Add(taCards[i]);
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
