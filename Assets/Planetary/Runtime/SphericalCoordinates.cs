using UnityEngine;

namespace Planetary
{
    /// <summary>Planet-centric lat/long with Euclidean tangent frame conversion.</summary>
    public readonly struct SphericalCoordinates
    {
        public readonly float LatitudeDeg;
        public readonly float LongitudeDeg;
        public readonly float Radius;

        public SphericalCoordinates(float latDeg, float lonDeg, float radius)
        {
            LatitudeDeg = latDeg;
            LongitudeDeg = lonDeg;
            Radius = radius;
        }

        public Vector3 ToWorldPosition(Vector3 planetCenter, Vector3 stablePoleAxis, float primeMeridianOffsetDeg)
        {
            Vector3 up = stablePoleAxis.sqrMagnitude > 1e-6f ? stablePoleAxis.normalized : Vector3.up;
            Vector3 refForward = Vector3.ProjectOnPlane(Vector3.forward, up);
            if (refForward.sqrMagnitude < 1e-6f)
                refForward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
            else
                refForward.Normalize();
            Quaternion poleFrame = Quaternion.LookRotation(refForward, up);
            float latRad = LatitudeDeg * Mathf.Deg2Rad;
            float lonRad = (LongitudeDeg + primeMeridianOffsetDeg) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(
                Mathf.Cos(latRad) * Mathf.Sin(lonRad),
                Mathf.Sin(latRad),
                Mathf.Cos(latRad) * Mathf.Cos(lonRad));
            return planetCenter + poleFrame * dir * Radius;
        }

        public static SphericalCoordinates FromWorldPosition(
            Vector3 worldPos,
            Vector3 planetCenter,
            Vector3 stablePoleAxis,
            float primeMeridianOffsetDeg)
        {
            Vector3 up = stablePoleAxis.sqrMagnitude > 1e-6f ? stablePoleAxis.normalized : Vector3.up;
            Vector3 refForward = Vector3.ProjectOnPlane(Vector3.forward, up);
            if (refForward.sqrMagnitude < 1e-6f)
                refForward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
            else
                refForward.Normalize();
            Quaternion inv = Quaternion.Inverse(Quaternion.LookRotation(refForward, up));
            Vector3 local = inv * (worldPos - planetCenter);
            float r = local.magnitude;
            float lat = Mathf.Asin(Mathf.Clamp(local.y / Mathf.Max(r, 1e-6f), -1f, 1f)) * Mathf.Rad2Deg;
            float lon = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg - primeMeridianOffsetDeg;
            return new SphericalCoordinates(lat, lon, r);
        }

        public Matrix4x4 ToEuclideanMatrix(Vector3 planetCenter, Vector3 stablePoleAxis, float primeMeridianOffsetDeg)
        {
            Vector3 pos = ToWorldPosition(planetCenter, stablePoleAxis, primeMeridianOffsetDeg);
            Vector3 up = (pos - planetCenter).normalized;
            Vector3 tangent = Vector3.Cross(up, Vector3.forward);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(up, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(up, tangent);
            return Matrix4x4.TRS(pos, Quaternion.LookRotation(tangent, up), Vector3.one);
        }
    }
}
