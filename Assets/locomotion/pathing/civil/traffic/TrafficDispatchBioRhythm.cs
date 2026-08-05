using UnityEngine;

/// <summary>Hub peer for traffic warden — serviceId traffic_warden.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Dispatch Bio Rhythm")]
public sealed class TrafficDispatchBioRhythm : DispatchBioRhythm
{
    public TrafficWarden warden;

    protected override void Awake()
    {
        serviceId = "traffic_warden";
        governmentAssigned = true;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
        if (warden == null)
            warden = GetComponent<TrafficWarden>() ?? FindFirstObjectByType<TrafficWarden>();
    }
}
