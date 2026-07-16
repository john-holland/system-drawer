using System;
using UnityEngine;

/// <summary>Maps a local instrument surface id onto a remote vehicle surface / manifold slot.</summary>
[Serializable]
public sealed class VehicleInstrumentBinding
{
    [Tooltip("Local VehicleInstrumentMap slot id (e.g. jet.pitch).")]
    public string localSurfaceId;

    [Tooltip("Remote vehicle that receives physics.")]
    public VehicleActor remoteVehicle;

    [Tooltip("Remote instrument / manifold surface id (e.g. ladder.yaw).")]
    public string remoteSurfaceId;

    [Tooltip("Optional force origin on the remote vehicle.")]
    public Transform remoteForceOrigin;

    [Tooltip("Local axis on force origin for thrust/torque.")]
    public Vector3 localForceAxis = Vector3.up;

    [Min(0f)]
    public float maxForceNewtons = 800f;

    public bool applyAsTorque;
}
