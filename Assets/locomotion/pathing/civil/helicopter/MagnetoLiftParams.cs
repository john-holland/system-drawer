using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MagnetoBladeKinkStop
{
    [Range(0f, 1f)] public float t01 = 0.5f;
    public float angleDeg;
}

/// <summary>Property-config lift unit (rotor/prop) — always authoritative at runtime.</summary>
[Serializable]
public sealed class MagnetoLiftParams
{
    public string magnetoId = "magneto_0";
    public float aspectRatio = 8f;
    public float spanLength = 12f;
    public int bladeCount = 4;
    public float collectiveMinDeg = -5f;
    public float collectiveMaxDeg = 15f;
    public float cyclicMaxDeg = 10f;
    public float rpmIdle = 200f;
    public float rpmMax = 400f;
    public float tipTwistDeg;
    public List<MagnetoBladeKinkStop> kinkStops = new List<MagnetoBladeKinkStop>();
    public Vector3 tipEndPositionCache;
    public Vector3 centerlineLocalPos;
    public float centerlineAngleDeg;

    [Header("Turning")]
    [Range(0f, 1f)] public float yawAuthority01 = 0.8f;
    public float tailRotorGain = 1f;
    public float antiTorqueBias;

    [Header("Flapping / winglets")]
    public bool flappingEnabled;
    public string flapOpenCloseTopologyId = "magneto_flap";
    public float wingletTurnDegMin = -20f;
    public float wingletTurnDegMax = 20f;
    public string wingletOpenCloseTopologyId = "magneto_winglet";

    [Header("Efficacy (display)")]
    [Range(0f, 1f)] public float efficacy01 = 1f;
    public float lastAppliedMinLiftN;
    public float lastAppliedMinClimbMs;
    public float lastAppliedMinYawRate;
    public float lastAppliedMinDiskLoading;

    public float DiskArea => Mathf.PI * Mathf.Pow(Mathf.Max(0.1f, spanLength * 0.5f), 2f);

    public float EstimateLiftN(float collective01 = 0.7f)
    {
        float rpm = Mathf.Lerp(rpmIdle, rpmMax, Mathf.Clamp01(collective01));
        float tipSpeed = (rpm / 60f) * Mathf.PI * spanLength;
        return DiskArea * tipSpeed * 0.15f * Mathf.Clamp01(collective01) * Mathf.Max(0.05f, efficacy01);
    }

    public float EstimateClimbMs(float collective01 = 0.7f) =>
        EstimateLiftN(collective01) / 12000f;

    public float EstimateYawRate() =>
        yawAuthority01 * tailRotorGain * 45f;

    public float DiskLoading(float massKg = 2500f) =>
        massKg * 9.81f / Mathf.Max(0.1f, DiskArea);

    public void RecomputeTipEndCache(Transform root)
    {
        Vector3 origin = root != null ? root.TransformPoint(centerlineLocalPos) : centerlineLocalPos;
        Quaternion yaw = Quaternion.Euler(0f, centerlineAngleDeg, 0f);
        Vector3 tipDir = yaw * Vector3.right;
        Vector3 tip = origin + tipDir * (spanLength * 0.5f);
        tip = Quaternion.AngleAxis(tipTwistDeg, tipDir) * (tip - origin) + origin;
        tipEndPositionCache = root != null ? root.InverseTransformPoint(tip) : tip;
    }

    public bool ValidateTipCache(Transform root, float maxDistanceError = 0.5f)
    {
        Vector3 cached = tipEndPositionCache;
        RecomputeTipEndCache(root);
        Vector3 expected = tipEndPositionCache;
        tipEndPositionCache = cached;
        return Vector3.Distance(cached, expected) <= maxDistanceError;
    }

    public void RefreshEfficacyFromLastApplied()
    {
        if (lastAppliedMinLiftN <= 0f && lastAppliedMinClimbMs <= 0f)
        {
            efficacy01 = 1f;
            return;
        }
        float liftRatio = lastAppliedMinLiftN > 1e-3f
            ? EstimateLiftN() / lastAppliedMinLiftN
            : 1f;
        float climbRatio = lastAppliedMinClimbMs > 1e-3f
            ? EstimateClimbMs() / lastAppliedMinClimbMs
            : 1f;
        float yawRatio = lastAppliedMinYawRate > 1e-3f
            ? EstimateYawRate() / lastAppliedMinYawRate
            : 1f;
        efficacy01 = Mathf.Clamp01(Mathf.Min(liftRatio, Mathf.Min(climbRatio, yawRatio)));
    }

    public bool IsEfficacyLowered() => efficacy01 < 0.999f && lastAppliedMinLiftN > 0f;
}

/// <summary>Requirements mode — does not mutate property config until Apply.</summary>
[Serializable]
public sealed class MagnetoLiftRequirements
{
    public float minLiftN = 20000f;
    public float minClimbMs = 2f;
    public float minYawRateDegPerSec = 20f;
    public float minDiskLoading = 20f;
    public float designMassKg = 2500f;

    /// <summary>Writes minimum property values that satisfy these requirements into props.</summary>
    public void ApplyMinimumsTo(MagnetoLiftParams props)
    {
        if (props == null) return;
        props.spanLength = Mathf.Max(props.spanLength, Mathf.Sqrt(Mathf.Max(1f, minLiftN) / (Mathf.PI * 40f)) * 2f);
        props.rpmMax = Mathf.Max(props.rpmMax, 350f);
        props.collectiveMaxDeg = Mathf.Max(props.collectiveMaxDeg, 12f);
        props.yawAuthority01 = Mathf.Max(props.yawAuthority01, Mathf.Clamp01(minYawRateDegPerSec / 45f));
        props.tailRotorGain = Mathf.Max(props.tailRotorGain, 1f);
        float areaNeeded = (designMassKg * 9.81f) / Mathf.Max(1f, minDiskLoading);
        float diameter = 2f * Mathf.Sqrt(areaNeeded / Mathf.PI);
        props.spanLength = Mathf.Max(props.spanLength, diameter);

        // Iterate lightly to meet climb/lift floors.
        for (int i = 0; i < 8; i++)
        {
            if (props.EstimateLiftN() >= minLiftN && props.EstimateClimbMs() >= minClimbMs)
                break;
            props.spanLength *= 1.05f;
            props.rpmMax *= 1.02f;
        }

        props.lastAppliedMinLiftN = minLiftN;
        props.lastAppliedMinClimbMs = minClimbMs;
        props.lastAppliedMinYawRate = minYawRateDegPerSec;
        props.lastAppliedMinDiskLoading = minDiskLoading;
        props.RefreshEfficacyFromLastApplied();
    }

    public bool SatisfiedBy(MagnetoLiftParams props)
    {
        if (props == null) return false;
        return props.EstimateLiftN() >= minLiftN - 1f
               && props.EstimateClimbMs() >= minClimbMs - 0.05f
               && props.EstimateYawRate() >= minYawRateDegPerSec - 0.5f
               && props.DiskLoading(designMassKg) >= minDiskLoading - 0.5f;
    }
}
