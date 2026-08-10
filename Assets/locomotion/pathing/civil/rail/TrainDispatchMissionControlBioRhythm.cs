using System.Collections.Generic;
using UnityEngine;

/// <summary>Train dispatch mission control — engineer / dispatch / follow / plow / justice cards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Dispatch Mission Control Bio Rhythm")]
public sealed class TrainDispatchMissionControlBioRhythm : DispatchBioRhythm
{
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;
    public string stationPeerId = TrainDispatchKinds.TrainStation;
    public TrainVehicleRagdoll activeTrain;

    protected override void Awake()
    {
        serviceId = TrainDispatchKinds.TrainDispatch;
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
        if (!subscribedPeerIds.Contains(stationPeerId))
            subscribedPeerIds.Add(stationPeerId);
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        var train = activeTrain;
        switch (kind)
        {
            case TrainDispatchKinds.EngineerCompose:
                cards.Add(TrainEngineerCard.Generate(request, train));
                break;
            case TrainDispatchKinds.EngineerStart:
                cards.Add(TrainEngineerStartCard.Generate(request, train));
                cards.Add(TrainDispatchStartCard.Generate(request));
                break;
            case TrainDispatchKinds.EngineerStop:
                cards.Add(TrainEngineerStopCard.Generate(request, train));
                cards.Add(TrainDispatchStopCard.Generate(request));
                break;
            case TrainDispatchKinds.DispatchStart:
                cards.Add(TrainDispatchStartCard.Generate(request));
                break;
            case TrainDispatchKinds.DispatchStop:
                cards.Add(TrainDispatchStopCard.Generate(request));
                break;
            case TrainDispatchKinds.SpeedAdjust:
                cards.Add(TrainDispatchSpeedAdjustCard.Generate(request));
                cards.Add(TrainEngineerSpeedAdjustCard.Generate(request, train));
                break;
            case TrainDispatchKinds.EngineerSpeedAdjust:
                cards.Add(TrainEngineerSpeedAdjustCard.Generate(request, train));
                break;
            case TrainDispatchKinds.TrafficStop:
                cards.Add(TrainEngineerTrafficeStopCard.Generate(request));
                break;
            case TrainDispatchKinds.Plow:
                cards.Add(TrainEngineerPlowCard.Generate(request, train));
                break;
            case TrainDispatchKinds.Justice:
                cards.Add(TrainEngineerJusticeCard.Generate(request, train));
                break;
            case TrainDispatchKinds.TurnstileRequest:
                cards.Add(TrainDispatchTurnstyleRequestCard.Generate(request));
                cards.Add(TrainEngineerTurnstyleCard.Generate(request));
                break;
            case TrainDispatchKinds.FollowTrainRequest:
                cards.Add(TrainDispatchFollowTrainRequestCard.Generate(request));
                cards.Add(TrainEngineerFollowTrainCard.Generate(request));
                break;
            case TrainDispatchKinds.FollowTrainEngineer:
                cards.Add(TrainEngineerFollowTrainCard.Generate(request));
                break;
            case TrainDispatchKinds.YardBackupForward:
                cards.Add(TrainYardBackupForwardCard.Generate(request));
                break;
            case TrainDispatchKinds.YardTowPush:
                cards.Add(TrainYardTowPushCard.Generate(request));
                break;
            case TADispatchKinds.Fuel:
            case "gas_rail_refuel":
            case "train_fuel":
                cards.Add(TAVehicleFuelCard.GenerateForTrain(request, train));
                cards.Add(TrainEngineerCard.Generate(request, train));
                break;
            default:
                cards.Add(TrainEngineerCard.Generate(request, train));
                break;
        }
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}
