using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weather.CloudBake
{
    public enum CloudPropertyClass
    {
        Cumulus,
        Stratus,
        Cirrus,
        Generic
    }

    [Serializable]
    public sealed class CloudSpherePrimitive
    {
        public Vector3 center;
        public float radius = 10f;
        public float density = 0.5f;
        public float moisture = 0.6f;
        public float waterCoupling = 0.2f;
        public CloudPropertyClass propertyClass = CloudPropertyClass.Generic;
        public int columnIndex;
        public int stackIndex;
    }

    [Serializable]
    public sealed class CloudHalfShellStack
    {
        public List<CloudSpherePrimitive> spheres = new List<CloudSpherePrimitive>();
        public Bounds shellBounds;
        public Vector3 shellCentroid;
        public CloudHalfShellConvexion appliedConvexion = new CloudHalfShellConvexion();
        public int spheresPerColumn = 3;
        public float cloudBaseM = 1000f;
        public float cloudTopM = 2000f;

        public void RestorePositions(IReadOnlyList<Vector3> anchorCenters)
        {
            if (anchorCenters == null)
                return;
            int n = Mathf.Min(anchorCenters.Count, spheres.Count);
            for (int i = 0; i < n; i++)
                spheres[i].center = anchorCenters[i];
        }

        public Bounds ComputeBounds()
        {
            if (spheres.Count == 0)
                return shellBounds;
            var b = new Bounds(spheres[0].center, Vector3.one * spheres[0].radius * 2f);
            for (int i = 1; i < spheres.Count; i++)
            {
                var s = spheres[i];
                b.Encapsulate(new Bounds(s.center, Vector3.one * s.radius * 2f));
            }
            shellBounds = b;
            return b;
        }
    }
}
