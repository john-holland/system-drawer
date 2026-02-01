using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// 4D bounds for spatiotemporal placement: (x,y,z) region and time window [tMin, tMax].
    /// Used by the fourth-dimensional spatial generator and narrative/calendar volumes.
    /// </summary>
    [System.Serializable]
    public struct Bounds4
    {
        public Vector3 center;
        public Vector3 size;
        public float tMin;
        public float tMax;

        public Bounds4(Vector3 center, Vector3 size, float tMin, float tMax)
        {
            this.center = center;
            this.size = size;
            this.tMin = tMin;
            this.tMax = tMax;
        }

        public Vector3 min => center - size * 0.5f;
        public Vector3 max => center + size * 0.5f;
        public float centerT => (tMin + tMax) * 0.5f;
        public float durationT => tMax - tMin;

        public bool Contains(float x, float y, float z, float t)
        {
            Vector3 mn = min;
            Vector3 mx = max;
            return x >= mn.x && x <= mx.x && y >= mn.y && y <= mx.y && z >= mn.z && z <= mx.z
                && t >= tMin && t <= tMax;
        }

        public bool Contains(Vector3 position, float t)
        {
            return Contains(position.x, position.y, position.z, t);
        }

        public bool Overlaps(Bounds4 other)
        {
            Vector3 aMin = min;
            Vector3 aMax = max;
            Vector3 bMin = other.min;
            Vector3 bMax = other.max;
            if (aMin.x > bMax.x || aMax.x < bMin.x) return false;
            if (aMin.y > bMax.y || aMax.y < bMin.y) return false;
            if (aMin.z > bMax.z || aMax.z < bMin.z) return false;
            if (tMin > other.tMax || tMax < other.tMin) return false;
            return true;
        }

        /// <summary>3D bounds at the given time if t is inside [tMin, tMax]; otherwise empty.</summary>
        public Bounds ToBounds(float t)
        {
            if (t >= tMin && t <= tMax)
                return new Bounds(center, size);
            return new Bounds(center, Vector3.zero);
        }

        /// <summary>3D spatial bounds (ignores time).</summary>
        public Bounds ToBounds()
        {
            return new Bounds(center, size);
        }

        public static Bounds4 FromBoundsAndTime(Bounds bounds, float tStart, float tEnd)
        {
            return new Bounds4(bounds.center, bounds.size, tStart, tEnd);
        }
    }
}
