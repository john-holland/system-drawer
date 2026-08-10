using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Train station venue bio — public/private parking, checkpoints, TSA + TA facilitation.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Station Bio Rhythm")]
public sealed class TrainStationBioRhythm : DispatchBioRhythm
{
    public bool publicParking = true;
    public bool privateParking = true;
    public bool checkpointsEnabled = true;
    public AuthWarden checkpointWarden;
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public string dispatchPeerId = TrainDispatchKinds.TrainDispatch;
    public TrainStationRuntime station;

    protected override void Awake()
    {
        serviceId = TrainDispatchKinds.TrainStation;
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (station == null)
            station = GetComponent<TrainStationRuntime>();
        if (checkpointWarden == null)
            checkpointWarden = GetComponent<AuthWarden>() ?? gameObject.AddComponent<AuthWarden>();
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
        if (!subscribedPeerIds.Contains(dispatchPeerId))
            subscribedPeerIds.Add(dispatchPeerId);
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        bool open = station == null || station.isOpen || CronDue.IsActiveSchedule(hoursCron, utcNow);
        unitsAvailable01 = open ? (publicParking || privateParking ? 1f : 0.4f) : 0.15f;
        if (venueBio != null)
        {
            venueBio.activity01 = open ? 0.55f : 0.1f;
            venueBio.stress01 = checkpointsEnabled ? 0.25f : 0.1f;
        }
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        switch (kind)
        {
            case TrainDispatchKinds.Attendant:
                cards.Add(TSATrainEngineerAttendant.Generate(request));
                break;
            case TrainDispatchKinds.TurnstileRequest:
                cards.Add(TrainDispatchTurnstyleRequestCard.Generate(request));
                cards.Add(TrainEngineerTurnstyleCard.Generate(request));
                break;
            case TrainDispatchKinds.YardBackupForward:
                cards.Add(TrainYardBackupForwardCard.Generate(request));
                break;
            case TrainDispatchKinds.YardTowPush:
                cards.Add(TrainYardTowPushCard.Generate(request));
                break;
            case TADispatchKinds.ReleasePassengers:
                cards.Add(TAVehicleReleasePassengersCard.Generate(request, TAPassengerReleaseMode.NextStop));
                break;
            default:
                cards.Add(TSATrainEngineerAttendant.Generate(request));
                cards.Add(TAVehicleRouteCard.Generate(request));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
