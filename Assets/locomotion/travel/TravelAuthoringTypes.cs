using System;
using Locomotion.Narrative;
using UnityEngine;/// <summary>
/// Authoring row for the Travel Pathing Editor (coordinates, hints, narrative nodes).
/// </summary>
[Serializable]
public class TravelAuthoringRow
{
    [Tooltip("Row role: world coordinate, planner hint, narrative node, or Bedoga spatial node.")]
    public TravelAuthoringRowKind kind = TravelAuthoringRowKind.Coordinate;

    [Tooltip("World position (or volume-local position when Coordinate mode is not World).")]
    public Vector3 worldPosition;

    [Tooltip("Narrative calendar time (seconds since epoch) for 4D placement along the travel script.")]
    public float narrativeTime;

    [Tooltip("Optional key into the actor map for multibody or formation binding.")]
    public string actorMapKey = "";

    [Tooltip("Optional Unity object reference (node asset, spatial slot, etc.) for this row.")]
    public UnityEngine.Object actorReference;

    [Tooltip("Authoring notes shown only in the Pathing Editor.")]
    public string notes = "";

    [Tooltip("Optional narrative prompt for Node rows.")]
    public NarrativePromptAsset promptRef;
}

public enum TravelAuthoringRowKind
{
    Coordinate,
    Hint,
    Node,
    SpatialNode
}

/// <summary>
/// Optional coordinate binding modes for Continuuuum / narrative volumes (stub until bridge exists).
/// </summary>
public enum TravelCoordinateMode
{
    World,
    NarrativeVolume,
    ContinuuuumAssetRef
}

/// <summary>
/// Scene-view preview framing for Travel Pathing Editor.
/// </summary>
public enum TravelPreviewFitMode
{
    EntirePath,
    CurrentSegment
}
