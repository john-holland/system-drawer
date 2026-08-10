using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AirplaneWingKinkStop
{
    [Range(0f, 1f)] public float t01 = 0.5f;
    public float angleDeg;
}

[Serializable]
public sealed class AirplaneControlSurfaceParams
{
    [Range(0.05f, 0.6f)] public float chordFraction = 0.25f;
    public Vector3 hingeLocal;
    public float deflectionMinDeg = -25f;
    public float deflectionMaxDeg = 25f;
    public float rateDegPerSec = 40f;
}

[Serializable]
public sealed class AirplaneFinParams
{
    public float deflectionMinDeg = -30f;
    public float deflectionMaxDeg = 30f;
    public float sweepDeg;
    public string featureOpenCloseTopologyId = "jet_nozzle";
}

/// <summary>Wing / tail surface — aspect, kink stops, tip cache, aileron/fin.</summary>
[Serializable]
public sealed class AirplaneWingSurfaceParams
{
    public string surfaceId = "wing";
    public float aspectRatio = 8f;
    public float spanLength = 30f;
    public float leadingEdgeAoADeg;
    public float trailingEdgeSweepDeg;
    public List<AirplaneWingKinkStop> kinkStops = new List<AirplaneWingKinkStop>();
    public Vector3 centerlineLocalPos;
    public float centerlineAngleDeg;
    public float tipTwistDeg;
    public Vector3 tipEndPositionCache;
    public AirplaneControlSurfaceParams aileron = new AirplaneControlSurfaceParams();
    public AirplaneFinParams fin = new AirplaneFinParams();

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

    public void ApplyToFlyingConfig(FlyingCardConfig cfg)
    {
        if (cfg == null) return;
        cfg.wingAspectRatio = Mathf.Clamp(aspectRatio, 0.1f, 20f);
    }
}

[Serializable]
public sealed class AirplaneEllipsoidAeroParams
{
    public Vector3 centerLocal;
    public Vector3 radii = new Vector3(4f, 3f, 18f);
    public Vector3 rotationEuler;
    public float conicalNozzleLength = 2f;
    public float conicalNozzleHalfAngleDeg = 12f;
    public Vector3 affineLiftDelta = Vector3.one;
    public Vector3 affineDragDelta = Vector3.one;
    public float thrustSlotMultiplier = 1f;
}

[Serializable]
public sealed class AirplaneJetEngineParams
{
    public string engineId = "jet_0";
    public Vector3 localPosition;
    public Vector3 localEuler;
    public float thrustN = 80000f;
    public string gooseContentsId;
    public string linkedPixelLightMountId;
}

[Serializable]
public sealed class AirplaneBatteryPack
{
    public string packId = "main";
    public float capacityKwh = 120f;
    public float chargeKwh = 120f;
    public float maxDrawKw = 80f;
    [Range(0f, 1f)] public float criticalCharge01 = 0.15f;

    public float Charge01 => capacityKwh > 1e-4f ? Mathf.Clamp01(chargeKwh / capacityKwh) : 0f;
}

public enum AirplanePowerSystemCategory
{
    Comfort = 0,
    Cabin = 1,
    Avionics = 2,
    FlightCritical = 3,
    Weapons = 4
}

[Serializable]
public sealed class AirplanePowerSystemDraw
{
    public string systemId = "lights";
    public string label = "Lights";
    public float drawKwWhenOn = 1f;
    public bool enabled = true;
    public int shedPriority = 80;
    public AirplanePowerSystemCategory category = AirplanePowerSystemCategory.Cabin;
    public bool passengerComfort;
}

public enum AirplaneCabinMusicSource
{
    Silent = 0,
    Chorus = 1,
    PaProgram = 2,
    SeatAux = 3,
    PilotTelecom = 4
}
