using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Sanitation bio — peers TA; pickup, sorting, maintenance, road work cards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sanitation Facility Bio Rhythm")]
public sealed class SanitationFacilityBioRhythm : FactoryBioRhythm
{
    public SanitationFacilityRuntime facility;
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;

    protected override void Awake()
    {
        if (facility == null)
            facility = GetComponent<SanitationFacilityRuntime>();
        factory = facility;
        governmentAssigned = facility == null || facility.governmentAssigned;
        serviceId = "sanitation_facility";
        base.Awake();
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        facility?.TickPlant(dt);
        GetComponent<TrashWarden>()?.Tick(dt);
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case "sanitation_pickup":
            case "trash_pickup":
                cards.Add(SanitationPickupCard.Generate(request, facility));
                break;
            case "sanitation_sort":
                cards.Add(SanitationSortCard.Generate(request, facility));
                break;
            case "ta_maintenance_request":
            case "inspect":
            case "ta_bay_repair":
                cards.Add(TAMaintenanceRequest.Generate(request));
                cards.Add(TAMaintenanceCard.GenerateInspect(null));
                break;
            case "ta_road_work_request":
            case "road_work":
                cards.Add(TARoadWorkRequest.Generate(request));
                break;
            default:
                cards.Add(SanitationPickupCard.Generate(request, facility));
                cards.Add(SanitationSortCard.Generate(request, facility));
                cards.Add(FactoryLineCard.Generate(request, facility));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}

[Serializable]
public class SanitationPickupCard : TravelAgentCard
{
    public SanitationFacilityRuntime facility;

    public static SanitationPickupCard Generate(DispatchRequest request, SanitationFacilityRuntime facility = null)
    {
        var c = new SanitationPickupCard();
        c.facility = facility;
        c.sectionName = "sanitation_pickup";
        c.description = "trash_pickup";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        var pts = facility?.roadCrew?.ResolvePickupPoints();
        c.goalWorld = pts != null && pts.Count > 0 ? pts[0].position
            : (request != null ? request.worldTarget
                : facility != null ? facility.transform.position : Vector3.zero);
        return c;
    }
}

[Serializable]
public class SanitationSortCard : TravelAgentCard
{
    public SanitationFacilityRuntime facility;

    public static SanitationSortCard Generate(DispatchRequest request, SanitationFacilityRuntime facility = null)
    {
        var c = new SanitationSortCard();
        c.facility = facility;
        c.sectionName = "sanitation_sort";
        c.description = "sorting_station";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        var st = facility != null && facility.sortingStations.Count > 0 ? facility.sortingStations[0] : null;
        c.goalWorld = st != null && st.loadingArea != null ? st.loadingArea.position
            : (facility != null && facility.loadingArea != null ? facility.loadingArea.position
                : facility != null ? facility.transform.position : Vector3.zero);
        return c;
    }
}
