using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoring row for the Travel Pathing Editor (coordinates, hints, narrative nodes).
/// </summary>
[Serializable]
public class TravelAuthoringRow
{
    public TravelAuthoringRowKind kind = TravelAuthoringRowKind.Coordinate;
    public Vector3 worldPosition;
    public float narrativeTime;
    public string actorMapKey = "";
    public UnityEngine.Object actorReference;
    public string notes = "";
}

public enum TravelAuthoringRowKind
{
    Coordinate,
    Hint,
    Node,
    SpatialNode
}

/// <summary>
/// Optional coordinate binding modes for Continuum / narrative volumes (stub until bridge exists).
/// </summary>
public enum TravelCoordinateMode
{
    World,
    NarrativeVolume,
    ContinuumAssetRef
}

/// <summary>
/// Scene-view preview framing for Travel Pathing Editor.
/// </summary>
public enum TravelPreviewFitMode
{
    EntirePath,
    CurrentSegment
}
