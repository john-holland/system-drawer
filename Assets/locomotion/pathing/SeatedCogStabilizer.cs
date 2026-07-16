using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps actor CoG projected over the sit-surface contact polygon.
/// Emits muscle-group bias impulses when tip risk rises.
/// </summary>
[Serializable]
public sealed class SeatedCogStabilizer
{
    public SitSurfaceContact surface;
    public SurfaceOccupancyMode mode = SurfaceOccupancyMode.Sit;
    [Range(0f, 1f)] public float tipRiskThreshold = 0.55f;
    public float restoreActivation = 0.65f;

    public float LastTipRisk01 { get; private set; }
    public bool LastInsidePolygon { get; private set; }
    public Vector3 LastProjectedCog { get; private set; }
    public Vector3 LastRestoreDir { get; private set; }

    public bool Evaluate(GameObject actorRoot)
    {
        LastTipRisk01 = 1f;
        LastInsidePolygon = false;
        LastProjectedCog = Vector3.zero;
        LastRestoreDir = Vector3.zero;
        if (surface == null || actorRoot == null)
            return false;

        Vector3 cog = ActorPhysicalCentroid.GetWorldCenterOfMass(actorRoot);
        LastInsidePolygon = surface.TryProjectCog(cog, out Vector3 projected, out float tip);
        LastProjectedCog = projected;
        LastTipRisk01 = tip;
        Vector3 center = surface.WorldPlanePoint;
        LastRestoreDir = Vector3.ProjectOnPlane(center - projected, surface.WorldPlaneNormal);
        if (LastRestoreDir.sqrMagnitude > 1e-6f)
            LastRestoreDir.Normalize();
        return LastInsidePolygon;
    }

    /// <summary>
    /// Build corrective impulse actions for tip recovery.
    /// Sit free-hang / high tip: arms + legs + abs. Stand-on: ankles/hips first, then arms/abs.
    /// </summary>
    public List<ImpulseAction> BuildRestoreImpulses(bool feetReachGround)
    {
        var list = new List<ImpulseAction>();
        if (LastTipRisk01 < tipRiskThreshold)
            return list;

        float a = restoreActivation * Mathf.Clamp01(LastTipRisk01);
        Vector3 dir = LastRestoreDir;

        if (mode == SurfaceOccupancyMode.StandOn)
        {
            list.Add(Make("left_ankle", a * 0.7f, dir));
            list.Add(Make("right_ankle", a * 0.7f, dir));
            list.Add(Make("left_hip", a * 0.55f, dir));
            list.Add(Make("right_hip", a * 0.55f, dir));
            if (LastTipRisk01 > 0.75f)
            {
                list.Add(Make("abdomen", a * 0.8f, dir));
                list.Add(Make("left_shoulder", a * 0.5f, dir));
                list.Add(Make("right_shoulder", a * 0.5f, dir));
            }
            return list;
        }

        // Sit: if feet don't reach ground, use arms + legs + abs to balance fort/stack.
        if (!feetReachGround || LastTipRisk01 > tipRiskThreshold)
        {
            list.Add(Make("abdomen", a, dir));
            list.Add(Make("lumbar", a * 0.85f, dir));
            list.Add(Make("left_thigh", a * 0.6f, dir));
            list.Add(Make("right_thigh", a * 0.6f, dir));
            list.Add(Make("left_shoulder", a * 0.7f, dir));
            list.Add(Make("right_shoulder", a * 0.7f, dir));
            list.Add(Make("left_elbow", a * 0.45f, dir));
            list.Add(Make("right_elbow", a * 0.45f, dir));
        }
        return list;
    }

    static ImpulseAction Make(string group, float activation, Vector3 forceDir)
    {
        return new ImpulseAction
        {
            muscleGroup = group,
            activation = Mathf.Clamp01(activation),
            duration = 0.12f,
            forceDirection = forceDir,
            curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };
    }
}
