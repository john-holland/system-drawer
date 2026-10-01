using System;
using UnityEngine;

/// <summary>Authored edge-loop face as a radial origin. No mesh refs (BedogaPlacement stays dependency-free).</summary>
[Serializable]
public struct CustomRadialSidePose
{
    public Vector3 origin;
    public Vector3 normal;
    public Vector3 tangent;
    public Bounds jointMiddle;
    public Bounds flyAway;
    [Tooltip("Ring wrap in degrees. 0 = auto (full circle or solver).")]
    public float customAngle;
    public bool hasCustomAngleObject;
    public Vector3 customAngleObjectWorld;
}
