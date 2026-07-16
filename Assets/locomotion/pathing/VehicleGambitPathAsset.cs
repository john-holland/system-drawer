using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VehicleGambitStop
{
    public string apertureId;
    public Vector3 positionGoal;
    public int octreeLeafIndex = -1;
    public float clearanceMeters = 0.5f;
}

[Serializable]
public sealed class VehicleGambitSplineNode
{
    public Vector3 position;
    public Vector3 tangent = Vector3.forward;
}

/// <summary>Ordered gambit stops + optional multinode spline for bespoke vehicle paths.</summary>
[CreateAssetMenu(fileName = "VehicleGambitPath", menuName = "Locomotion/Vehicle Gambit Path", order = 120)]
public sealed class VehicleGambitPathAsset : ScriptableObject
{
    public List<VehicleGambitStop> stops = new List<VehicleGambitStop>();
    public List<VehicleGambitSplineNode> splineNodes = new List<VehicleGambitSplineNode>();
    [Min(0.05f)] public float narrowClearanceThreshold = 0.75f;

    public void ClearStops() => stops.Clear();

    public void UpsertStopFromAperture(PathingAperture aperture, float clearance)
    {
        if (aperture == null) return;
        string id = string.IsNullOrEmpty(aperture.apertureId) ? aperture.name : aperture.apertureId;
        for (int i = 0; i < stops.Count; i++)
        {
            if (stops[i].apertureId == id)
            {
                stops[i].positionGoal = aperture.ApproachPointWorld;
                stops[i].clearanceMeters = clearance;
                stops[i].octreeLeafIndex = aperture.octreeLeafIndex;
                return;
            }
        }
        stops.Add(new VehicleGambitStop
        {
            apertureId = id,
            positionGoal = aperture.ApproachPointWorld,
            clearanceMeters = clearance,
            octreeLeafIndex = aperture.octreeLeafIndex
        });
    }
}
