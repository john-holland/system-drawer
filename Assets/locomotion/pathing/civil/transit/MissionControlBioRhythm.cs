using UnityEngine;

/// <summary>Thin mission-control peer — enqueues reroute / halt / recall / hours onto Transportation Authority.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Mission Control Bio Rhythm")]
public sealed class MissionControlBioRhythm : DispatchBioRhythm
{
    public string transportationPeerId = TADispatchKinds.TransportationAuthority;

    protected override void Awake()
    {
        serviceId = TADispatchKinds.MissionControl;
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (!subscribedPeerIds.Contains(transportationPeerId))
            subscribedPeerIds.Add(transportationPeerId);
    }

    public bool RequestReroute(string routeId, string notes = null, float priority01 = 0.7f) =>
        Forward(TADispatchKinds.Reroute, "route:" + routeId + (string.IsNullOrEmpty(notes) ? "" : "|" + notes), priority01);

    public bool RequestHalt(string vehicleId, float priority01 = 0.85f) =>
        Forward(TADispatchKinds.Halt, "vehicle:" + vehicleId, priority01);

    public bool RequestRecall(string vehicleId, float priority01 = 0.8f) =>
        Forward(TADispatchKinds.Recall, "vehicle:" + vehicleId, priority01);

    public bool RequestHoursChange(string cronExpr, float priority01 = 0.5f) =>
        Forward(TADispatchKinds.Hours, "hours:" + cronExpr, priority01);

    public bool RequestAllVehicleRecall(float priority01 = 0.95f) =>
        Forward(TADispatchKinds.Recall, "all_vehicles", priority01);

    bool Forward(string kind, string notes, float priority01)
    {
        return CentralDispatchHub.Instance != null &&
               CentralDispatchHub.Instance.RequestCrossDispatch(
                   serviceId,
                   transportationPeerId,
                   new DispatchRequest
                   {
                       kind = kind,
                       notes = notes,
                       priority01 = priority01
                   });
    }
}
