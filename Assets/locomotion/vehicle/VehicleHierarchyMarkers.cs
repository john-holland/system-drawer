using System;
using UnityEngine;

public sealed class VehicleTire : VehiclePartBase { }
public sealed class VehicleWheel : VehiclePartBase { }
public sealed class VehicleHubcap : VehiclePartBase { }
public sealed class VehicleAxle : VehiclePartBase { }
public sealed class VehicleDrivetrain : VehiclePartBase { }
public sealed class VehicleChassis : VehiclePartBase { }
public sealed class VehicleEngine : VehiclePartBase { }
public sealed class VehicleSuspension : VehiclePartBase { }
public sealed class VehicleTransmission : VehiclePartBase { }
public sealed class VehicleSteeringWheel : VehiclePartBase { }
public sealed class VehicleFrame : VehiclePartBase { }
public sealed class VehicleShield : VehiclePartBase { }
public sealed class VehicleFloor : VehiclePartBase { }

/// <summary>Seat volumes + ragdoll occupant anchors for fit/path verification extensions.</summary>
public sealed class VehicleSeating : VehiclePartBase
{
    public Bounds localComfortBounds = new Bounds(Vector3.zero, Vector3.one);
    [Tooltip("Anchors for seated ragdoll roots / IK targets keyed by occupant slot index.")]
    public Transform[] occupantAnchors = Array.Empty<Transform>();

    public Bounds GetWorldComfortBounds()
    {
        return TransformBounds(localComfortBounds, transform);
    }

    static Bounds TransformBounds(Bounds local, Transform t)
    {
        Vector3 center = t.TransformPoint(local.center);
        Vector3 ex = local.extents;
        Vector3 ax = t.TransformVector(new Vector3(ex.x, 0f, 0f));
        Vector3 ay = t.TransformVector(new Vector3(0f, ex.y, 0f));
        Vector3 az = t.TransformVector(new Vector3(0f, 0f, ex.z));
        Vector3 worldExtents = new Vector3(
            Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
            Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
            Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
        return new Bounds(center, worldExtents * 2f);
    }
}

public sealed class VehicleCompartment : VehiclePartBase { }
public sealed class VehicleInterior : VehiclePartBase { }
public sealed class VehicleBody : VehiclePartBase { }
public sealed class VehicleDoor : VehiclePartBase { }
public sealed class VehicleRoof : VehiclePartBase { }
public sealed class VehicleLight : VehiclePartBase { }
public sealed class VehicleHood : VehiclePartBase { }
public sealed class VehicleBackHood : VehiclePartBase { }
public sealed class VehicleGrill : VehiclePartBase { }
public sealed class VehicleBumperGuardFrame : VehiclePartBase { }
public sealed class VehicleBumperShell : VehiclePartBase { }
