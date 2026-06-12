using UnityEngine;

namespace Roads
{
    /// <summary>Sample along a road spline at a given arc-length distance.</summary>
    public struct RoadSplineSample
    {
        public float distance;
        public float normalizedT;
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 normal;
        public Vector3 binormal;
        public float width;
        public float gradeDegrees;
        public float bankingDegrees;
        public float heightOffset;
    }

    /// <summary>Per-sample frame for mesh ribbon generation.</summary>
    public struct SplinePathSample
    {
        public float distance;
        public float uvAlong;
        public float uvAcross;
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 normal;
        public Vector3 binormal;
        public float widthLeft;
        public float widthRight;
        public bool overhang;
    }
}
