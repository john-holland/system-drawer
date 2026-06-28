using System;
using UnityEngine;

namespace Weather.CloudBake
{
    public enum CloudViewerKind
    {
        Camera,
        WorldPoint,
        Centroid,
        Bounds
    }

    [Serializable]
    public sealed class CloudViewerSpec
    {
        public CloudViewerKind kind = CloudViewerKind.Camera;
        public Camera camera;
        public Vector3 worldPoint;
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        public Vector3 centroid;
        public int boundsRayGrid = 8;

        public Vector3 ResolveOrigin()
        {
            switch (kind)
            {
                case CloudViewerKind.Camera:
                    return camera != null ? camera.transform.position : worldPoint;
                case CloudViewerKind.WorldPoint:
                    return worldPoint;
                case CloudViewerKind.Centroid:
                    return centroid;
                case CloudViewerKind.Bounds:
                    return bounds.center;
                default:
                    return worldPoint;
            }
        }

        public Vector3 ResolveForward()
        {
            if (kind == CloudViewerKind.Camera && camera != null)
                return camera.transform.forward;
            return Vector3.forward;
        }

        public Vector3 ResolveUp()
        {
            if (kind == CloudViewerKind.Camera && camera != null)
                return camera.transform.up;
            return Vector3.up;
        }
    }
}
