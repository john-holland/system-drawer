using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Common bridge between mission control, air traffic control, and surface transit.
/// Owns vehicle routes (cron) and maps hub requests to TA vehicle cards.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Transportation Authority Bio Rhythm")]
public sealed class TransportationAuthorityBioRhythm : DispatchBioRhythm
{
    [Header("Fleet / routes")]
    public List<TAVehicleRoute> vehicleRoutes = new List<TAVehicleRoute>();
    public List<BusVehicleRagdoll> fleet = new List<BusVehicleRagdoll>();
    public string buildingHoursCron = "* 5-23 * * *";
    public string buildingMaintenanceCron = "0 3 * * 0";
    [Range(0f, 1f)] public float fleetReadiness01 = 1f;

    [Header("Peers")]
    public string trafficPeerId = "traffic_warden";

    protected override void Awake()
    {
        serviceId = TADispatchKinds.TransportationAuthority;
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (!subscribedPeerIds.Contains(trafficPeerId))
            subscribedPeerIds.Add(trafficPeerId);
        if (!subscribedPeerIds.Contains(TADispatchKinds.MissionControl))
            subscribedPeerIds.Add(TADispatchKinds.MissionControl);
        if (!subscribedPeerIds.Contains(TADispatchKinds.AirTrafficControl))
            subscribedPeerIds.Add(TADispatchKinds.AirTrafficControl);
        if (company != null && company.staff.Count == 0)
        {
            company.staff.Add(new RetinuePeckingEntry { role = "dispatcher", peckingOrder = 5, personaKey = "dispatcher" });
            company.staff.Add(new RetinuePeckingEntry { role = "driver", peckingOrder = 20, personaKey = "bus_driver" });
        }
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        hoursCron = buildingHoursCron;
        base.Tick(utcNow, dt);
        int ready = 0;
        for (int i = 0; i < fleet.Count; i++)
            if (fleet[i] != null && fleet[i].available && fleet[i].integrity01 > 0.25f)
                ready++;
        fleetReadiness01 = fleet.Count == 0 ? 1f : Mathf.Clamp01(ready / (float)fleet.Count);
        unitsAvailable01 = CronDue.IsActiveSchedule(buildingHoursCron, utcNow)
            ? fleetReadiness01
            : fleetReadiness01 * 0.25f;

        for (int i = 0; i < vehicleRoutes.Count; i++)
        {
            TAVehicleRoute route = vehicleRoutes[i];
            if (route == null || !route.enabled) continue;
            if (route.IsMaintenanceDue(utcNow))
            {
                Enqueue(new DispatchRequest
                {
                    kind = TADispatchKinds.BayRepair,
                    notes = "route:" + route.routeId + "|vehicle:" + route.vehicleId,
                    priority01 = 0.55f
                });
            }
        }
    }

    public TAVehicleRoute FindRoute(string routeId)
    {
        if (string.IsNullOrEmpty(routeId) || vehicleRoutes == null) return null;
        for (int i = 0; i < vehicleRoutes.Count; i++)
        {
            TAVehicleRoute r = vehicleRoutes[i];
            if (r != null && r.routeId == routeId)
                return r;
        }
        return null;
    }

    public BusVehicleRagdoll FindVehicle(string vehicleId)
    {
        if (string.IsNullOrEmpty(vehicleId) || fleet == null) return null;
        for (int i = 0; i < fleet.Count; i++)
        {
            BusVehicleRagdoll v = fleet[i];
            if (v != null && v.vehicleId == vehicleId)
                return v;
        }
        return null;
    }

    public void RequestToTraffic(DispatchRequest request)
    {
        CentralDispatchHub.Instance?.RequestCrossDispatch(serviceId, trafficPeerId, request);
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? TADispatchKinds.Route).ToLowerInvariant();
        switch (kind)
        {
            case TADispatchKinds.Reroute:
            case TADispatchKinds.Route:
                cards.Add(TAVehicleRouteCard.Generate(request, FindRouteFromNotes(request.notes)));
                break;
            case TADispatchKinds.Recall:
                cards.Add(TAVehicleRecallCard.Generate(request));
                break;
            case TADispatchKinds.Halt:
                cards.Add(TAVehicleParkHaltCard.Generate(request));
                break;
            case TADispatchKinds.Hours:
                cards.Add(TAVehicleSchedulingCard.GenerateHours(request, buildingHoursCron));
                break;
            case TADispatchKinds.ReleasePassengers:
                cards.Add(TAVehicleReleasePassengersCard.Generate(request, TAPassengerReleaseMode.NextStop));
                break;
            case TADispatchKinds.Speak:
                cards.Add(TAVehicleSpeakCard.Generate(request));
                break;
            case TADispatchKinds.Music:
                cards.Add(TAVehicleMusicCard.Generate(request));
                break;
            case TADispatchKinds.SoundDesign:
                cards.Add(TAVehicleSoundDesignCard.Generate(request));
                break;
            case TADispatchKinds.Park:
                cards.Add(TAVehicleParkCard.Generate(request));
                break;
            case TADispatchKinds.BayPark:
                cards.Add(TAVehicleBayParkCard.Generate(request));
                break;
            case TADispatchKinds.BayRepair:
                cards.Add(TAVehicleBayRepairCard.Generate(request, FindVehicleFromNotes(request.notes)));
                break;
            case TADispatchKinds.Fuel:
                cards.Add(TAVehicleFuelCard.Generate(request, FindVehicleFromNotes(request.notes)));
                break;
            case TADispatchKinds.Schedule:
                cards.Add(TAVehicleSchedulingCard.Generate(request, FindRouteFromNotes(request.notes)));
                break;
            case "passenger_pickup":
            case "passenger_dropoff":
            case "release_passenger":
                return base.FacilitateCards(request);
            default:
                cards.Add(TAVehicleRouteCard.Generate(request, FindRouteFromNotes(request.notes)));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }

    TAVehicleRoute FindRouteFromNotes(string notes)
    {
        if (string.IsNullOrEmpty(notes)) return null;
        const string key = "route:";
        int i = notes.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int start = i + key.Length;
        int end = notes.IndexOf('|', start);
        string id = end < 0 ? notes.Substring(start) : notes.Substring(start, end - start);
        return FindRoute(id.Trim());
    }

    BusVehicleRagdoll FindVehicleFromNotes(string notes)
    {
        if (string.IsNullOrEmpty(notes)) return null;
        const string key = "vehicle:";
        int i = notes.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int start = i + key.Length;
        int end = notes.IndexOf('|', start);
        string id = end < 0 ? notes.Substring(start) : notes.Substring(start, end - start);
        return FindVehicle(id.Trim());
    }
}
