using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceBuildAutoCloseMode
{
    None,
    OnStopExit,
    AfterChildren,
    OnSequenceEnd,
    Manual
}

[Serializable]
public sealed class SeatStandBridgeSpec
{
    public SurfaceOccupancyMode occupancy = SurfaceOccupancyMode.Sit;
    public float minContactHalfExtent = 0.15f;
    public float maxStackHeight = 1.5f;
    public string[] grabbableSynonyms = { "grabbable", "carryable", "climbable", "chair", "box", "book" };
}

[Serializable]
public sealed class PlaceBuildBeatProfile
{
    public AnimationClip liftClip;
    public AnimationClip placeClip;
    public AnimationClip turnClip;
    public PlaceBuildAutoCloseMode autoClose = PlaceBuildAutoCloseMode.OnSequenceEnd;
}

[Serializable]
public sealed class PlaceBuildTopologyNodeData
{
    public string nodeId = "place_0";
    public string[] targetTags = { "chair", "box", "book" };
    public bool carryable = true;
    public bool climbable;
    public SurfaceOccupancyMode occupyMode = SurfaceOccupancyMode.Sit;
    public Vector3 placeWorldPosition;
    public Quaternion placeWorldRotation = Quaternion.identity;
    public PlaceBuildBeatProfile beat = new PlaceBuildBeatProfile();
    public bool turnInChair;
    public float turnYawDegrees = 45f;
}

/// <summary>Scriptable place/build topology for seat/stand bridges.</summary>
[CreateAssetMenu(fileName = "PlaceBuildTopology", menuName = "Locomotion/Place Build Topology")]
public sealed class PlaceBuildTopologyAsset : ScriptableObject
{
    public List<PlaceBuildTopologyNodeData> nodes = new List<PlaceBuildTopologyNodeData>();
    public SeatStandBridgeSpec bridge = new SeatStandBridgeSpec();
}
