using UnityEngine;

/// <summary>Retargetable vehicle control surface (local or remote manifold / rigidbody).</summary>
public interface IVehicleControlSurface
{
    string Id { get; }
    string ImpulseChannelKey { get; }
    VehicleActor Owner { get; }
    void ApplyImpulse(float normalized, float dt);
}

/// <summary>Applies thrust/torque to a vehicle Rigidbody from a manifold slot.</summary>
public sealed class VehicleManifoldControlSurface : IVehicleControlSurface
{
    readonly string _id;
    readonly string _channel;
    readonly VehicleActor _owner;
    readonly Rigidbody _body;
    readonly Transform _forceOrigin;
    readonly Vector3 _localForceAxis;
    readonly float _maxForceNewtons;
    readonly bool _asTorque;

    public VehicleManifoldControlSurface(
        string id,
        string impulseChannelKey,
        VehicleActor owner,
        Rigidbody body,
        Transform forceOrigin,
        Vector3 localForceAxis,
        float maxForceNewtons,
        bool asTorque = false)
    {
        _id = id ?? "";
        _channel = impulseChannelKey ?? "";
        _owner = owner;
        _body = body;
        _forceOrigin = forceOrigin;
        _localForceAxis = localForceAxis.sqrMagnitude > 1e-6f ? localForceAxis.normalized : Vector3.forward;
        _maxForceNewtons = Mathf.Max(0f, maxForceNewtons);
        _asTorque = asTorque;
    }

    public string Id => _id;
    public string ImpulseChannelKey => _channel;
    public VehicleActor Owner => _owner;

    public void ApplyImpulse(float normalized, float dt)
    {
        if (_body == null || _maxForceNewtons <= 0f)
            return;
        float mag = Mathf.Clamp(normalized, -1f, 1f) * _maxForceNewtons;
        var origin = _forceOrigin != null ? _forceOrigin : _body.transform;
        var worldDir = origin.TransformDirection(_localForceAxis);
        if (_asTorque)
            _body.AddTorque(worldDir * mag, ForceMode.Force);
        else
            _body.AddForceAtPosition(worldDir * mag, origin.position, ForceMode.Force);
    }
}

/// <summary>No-op surface used when a binding resolves but physics is unavailable.</summary>
public sealed class NullVehicleControlSurface : IVehicleControlSurface
{
    public NullVehicleControlSurface(string id, string channel, VehicleActor owner)
    {
        Id = id ?? "";
        ImpulseChannelKey = channel ?? "";
        Owner = owner;
    }

    public string Id { get; }
    public string ImpulseChannelKey { get; }
    public VehicleActor Owner { get; }
    public void ApplyImpulse(float normalized, float dt) { }
}
