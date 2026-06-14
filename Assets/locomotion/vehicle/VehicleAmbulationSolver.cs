using Planetary.Field;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vehicle-side ambulation / drivetrain steering leaf solver with Do-Not-Path filtering and
/// a simplified Newton-inspired traction pairing (steering demand vs reaction budget).
/// </summary>
public sealed class VehicleAmbulationSolver : MonoBehaviour
{
    [Tooltip("Explicit Do-Not-Path volumes for this vehicle.")]
    public List<DoNotPathRegion> vehicleDoNotPathRegions = new List<DoNotPathRegion>();

    [Tooltip("Merge scene DoNotPathRegion markers.")]
    public bool vehicleIncludeSceneDoNotPathRegions = true;

    public bool IsWorldPositionVehicleDoNotPath(Vector3 worldPosition)
    {
        if (vehicleDoNotPathRegions != null)
        {
            foreach (var r in vehicleDoNotPathRegions)
            {
                if (r != null && r.isActiveAndEnabled && r.ContainsWorldPosition(worldPosition))
                    return true;
            }
        }

        if (vehicleIncludeSceneDoNotPathRegions && DoNotPathRegion.AnyContainsWorld(worldPosition))
            return true;

        return false;
    }

    public bool IsVehicleSegmentDoNotPath(Vector3 fromWorld, Vector3 toWorld, int samples = 12)
    {
        samples = Mathf.Max(2, samples);
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            if (IsWorldPositionVehicleDoNotPath(Vector3.Lerp(fromWorld, toWorld, t)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempt to accept a steering/throttle leaf given traction budget and physics-zone grip.
    /// Models paired lateral reaction vs steer torque demand (oversteer / understeer pruning when demand exceeds reaction).
    /// </summary>
    public bool TrySolveSteeringLeaf(
        float steerDemandSigned01,
        IAmbulationRangePropagator rangePropagator,
        Vector3 sampleWorldPosition,
        float tractionBudget01,
        out float steerCommandSigned01,
        out float throttleForward01)
    {
        steerCommandSigned01 = 0f;
        throttleForward01 = 0f;

        if (IsWorldPositionVehicleDoNotPath(sampleWorldPosition))
            return false;

        PhysicsPathingZone.SampleAt(sampleWorldPosition, out _, out float gripMul);
        float grip01 = Mathf.Clamp01(gripMul);

        var field = Planetary.Field.CanonicalSpatiotemporalField.Resolve();
        if (field != null && field.TrySampleBlended(sampleWorldPosition, Time.time, out Planetary.Field.SpatiotemporalSample sample))
            grip01 = Mathf.Clamp01(sample.surfaceFriction);
        float budget = Mathf.Clamp01(tractionBudget01) * grip01;

        if (rangePropagator != null && rangePropagator.CurrentRange.IsEmpty)
            return false;

        float steerMag = Mathf.Abs(steerDemandSigned01);
        // Newton pairing: excessive steer magnitude vs available side reaction removes the leaf.
        if (steerMag > budget + 1e-4f)
            return false;

        steerCommandSigned01 = Mathf.Sign(steerDemandSigned01) * steerMag;
        throttleForward01 = Mathf.Clamp01(budget - steerMag);
        return true;
    }
}
