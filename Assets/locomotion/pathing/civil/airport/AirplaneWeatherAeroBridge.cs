using UnityEngine;
using Weather.Lod;

/// <summary>Maps fuselage ellipsoid + jet cone onto flying-card lift/drag/thrust slot multipliers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Weather Aero Bridge")]
public sealed class AirplaneWeatherAeroBridge : MonoBehaviour
{
    public AirplaneVehicleRagdoll airplane;
    public FlyingCardConfig flyingConfig;
    public float liftMultiplier = 1f;
    public float dragMultiplier = 1f;
    public float thrustMultiplier = 1f;

    void Awake()
    {
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>() ?? GetComponentInParent<AirplaneVehicleRagdoll>();
        if (flyingConfig == null)
        {
            var solver = GetComponentInParent<PhysicsCardSolver>();
            if (solver != null) flyingConfig = solver.flyingCardConfig;
        }
    }

    public void ApplyAffineFromEllipsoid()
    {
        if (airplane?.fuselageEllipsoid == null) return;
        var egg = airplane.fuselageEllipsoid;
        Vector3 lift = egg.affineLiftDelta;
        Vector3 drag = egg.affineDragDelta;
        liftMultiplier = Mathf.Clamp((lift.x + lift.y + lift.z) / 3f, 0.2f, 3f);
        dragMultiplier = Mathf.Clamp((drag.x + drag.y + drag.z) / 3f, 0.2f, 3f);
        thrustMultiplier = Mathf.Clamp(egg.thrustSlotMultiplier, 0.2f, 3f);

        // todo: what about tail wings and stabilizers?
        if (airplane.leftWing != null)
            airplane.leftWing.ApplyToFlyingConfig(flyingConfig);
        else if (airplane.rightWing != null)
            airplane.rightWing.ApplyToFlyingConfig(flyingConfig);

        if (flyingConfig != null)
        {
            flyingConfig.flapPower = Mathf.Clamp(flyingConfig.flapPower * liftMultiplier, 0f, 2f);
            flyingConfig.jetImpulseStrength = Mathf.Clamp(
                flyingConfig.jetImpulseStrength * thrustMultiplier, 0.1f, 8f);
        }
    }

    public float SampleFuselageShellWeight(Vector3 worldPoint)
    {
        if (airplane?.fuselageEllipsoid == null) return 0f;
        var egg = airplane.fuselageEllipsoid;
        Vector3 center = airplane.transform.TransformPoint(egg.centerLocal);
        Quaternion rot = airplane.transform.rotation * Quaternion.Euler(egg.rotationEuler);
        Vector3 local = Quaternion.Inverse(rot) * (worldPoint - center);
        return WeatherEggBounds.ShellWeight(Vector3.zero, egg.radii, local);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        ApplyAffineFromEllipsoid();
    }
}
