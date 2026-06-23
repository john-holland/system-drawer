using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoring volume for terminal parking/landing slots. Reuses Bedoga fit/slot types.
/// </summary>
[DisallowMultipleComponent]
public sealed class ParkingZoneVolume : MonoBehaviour
{
    [Header("Bounds")]
    public SGBehaviorTreeEmptySpace.MeshType meshType = SGBehaviorTreeEmptySpace.MeshType.Base;
    public Vector3 boxSize = Vector3.one * 10f;

    [Header("Medium / surface")]
    public PhysicalPathingMedium medium = PhysicalPathingMedium.Ground;
    public TerminalSurfaceKind terminalSurfaceKind = TerminalSurfaceKind.GroundPad;

    [Header("Slot layout (Bedoga fit)")]
    public SGBehaviorTreeNode.FitX fitX = SGBehaviorTreeNode.FitX.Center;
    public SGBehaviorTreeNode.FitY fitY = SGBehaviorTreeNode.FitY.Center;
    public SGBehaviorTreeNode.FitZ fitZ = SGBehaviorTreeNode.FitZ.Center;
    public SGBehaviorTreeNode.AxisDirection stackDirection = SGBehaviorTreeNode.AxisDirection.PosX;
    public SGBehaviorTreeNode.AxisDirection wrapDirection = SGBehaviorTreeNode.AxisDirection.PosZ;

    [Header("Allowed terminal legs")]
    public List<TravelLegMode> allowedTerminalLegs = new List<TravelLegMode>();

    [Header("Beach")]
    public float maxShoreSlopeDegrees = 12f;
    public float minDepthAtApproachMeters = 1f;

    [Header("Water planing")]
    public float minPlaningRunOutMeters = 25f;
    public bool isShipPort;
    public bool preferAnchor;

    public static event Action<ParkingZoneVolume> Changed;

    public Bounds GetWorldBounds()
    {
        var empty = GetComponent<SGBehaviorTreeEmptySpace>();
        if (empty != null)
            return empty.GetBounds();
        if (meshType == SGBehaviorTreeEmptySpace.MeshType.Box)
        {
            Vector3 worldSize = Vector3.Scale(boxSize, transform.lossyScale);
            return new Bounds(transform.position, worldSize);
        }
        var col = GetComponent<BoxCollider>();
        if (col != null)
            return col.bounds;
        var rend = GetComponent<Renderer>();
        if (rend != null)
            return rend.bounds;
        return new Bounds(transform.position, transform.lossyScale);
    }

    public PlacementSlotConfig GetPlacementSlotConfig()
    {
        return new PlacementSlotConfig
        {
            fitX = fitX,
            fitY = fitY,
            fitZ = fitZ,
            stackDirection = stackDirection,
            wrapDirection = wrapDirection
        };
    }

    public bool AllowsLeg(TravelLegMode leg)
    {
        if (allowedTerminalLegs == null || allowedTerminalLegs.Count == 0)
            return true;
        return allowedTerminalLegs.Contains(leg);
    }

    void OnEnable() => Changed?.Invoke(this);
    void OnDisable() => Changed?.Invoke(this);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Bounds b = GetWorldBounds();
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.1f, 0.4f, 0.9f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
