using UnityEngine;

/// <summary>Jetway / terminal gate extension vehicle attached to an airport.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Extension Gate")]
public sealed class AirportExtensionGate : VehicleRagdoll
{
    public string gateId;
    public Transform bridgeTip;
    public Transform terminalAnchor;
    public AirplaneVehicleRagdoll dockedAirplane;
    public bool extended;
    [Range(0f, 1f)] public float extension01;

    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(gateId))
            gateId = vehicleId;
        if (interiors.Find(s => s != null && s.sectionName == "bridge") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "bridge", capacity = 8f });
    }

    public void SetExtended(bool on, float amount01 = 1f)
    {
        extended = on;
        extension01 = on ? Mathf.Clamp01(amount01) : 0f;
        SendMessage(on ? "OnAirportGateExtended" : "OnAirportGateRetracted", this, SendMessageOptions.DontRequireReceiver);
    }

    public void Dock(AirplaneVehicleRagdoll airplane)
    {
        dockedAirplane = airplane;
        if (airplane != null)
            airplane.dockedGate = this;
        SetExtended(true, 1f);
    }
}
