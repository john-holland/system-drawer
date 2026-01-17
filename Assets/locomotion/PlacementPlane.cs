using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placement plane structure for mesh surface analysis.
/// Represents a viable surface for object placement or grasping.
/// </summary>
[System.Serializable]
public class PlacementPlane
{
    [Header("Plane Properties")]
    [Tooltip("Center point of the plane")]
    public Vector3 center;

    [Tooltip("Normal vector (pointing up)")]
    public Vector3 normal;

    [Tooltip("Surface area of the plane")]
    public float area;

    [Tooltip("Angle of plane from horizontal (degrees)")]
    public float angle;

    [Header("Vertices")]
    [Tooltip("Vertices forming the plane")]
    public List<Vector3> vertices = new List<Vector3>();

    [Header("Stability")]
    [Tooltip("Can objects rest here?")]
    public bool isStable;

    [Tooltip("Stability score (0-1, higher = more stable)")]
    [Range(0f, 1f)]
    public float stabilityScore = 0.5f;

    [Header("Grasping")]
    [Tooltip("Can hand grasp from above?")]
    public bool canGraspFromAbove;

    [Tooltip("Can hand grasp from side?")]
    public bool canGraspFromSide;
}
