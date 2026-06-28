using UnityEngine;

namespace Planetary
{
    /// <summary>Spherical change-of-basis helpers for planet surface vertices (radial normal, lat/lon UV).</summary>
    public static class PlanetSurfaceFrame
    {
        public static Vector3 OutwardNormal(Vector3 surfaceWorld, Vector3 planetCenter)
        {
            Vector3 radial = surfaceWorld - planetCenter;
            if (radial.sqrMagnitude < 1e-12f)
                return Vector3.up;
            return radial.normalized;
        }

        public static Vector2 LatLonToUv(float latDeg, float lonDeg)
        {
            return new Vector2(
                (lonDeg + 180f) / 360f,
                (latDeg + 90f) / 180f);
        }

        public static Vector2 WorldToSphericalUv(
            Vector3 worldPos,
            Vector3 planetCenter,
            Vector3 stablePoleAxis,
            float primeMeridianOffsetDeg)
        {
            var sc = SphericalCoordinates.FromWorldPosition(
                worldPos, planetCenter, stablePoleAxis, primeMeridianOffsetDeg);
            return LatLonToUv(sc.LatitudeDeg, sc.LongitudeDeg);
        }

        /// <summary>Tangent frame at a surface point; Y axis is radial outward from planet center.</summary>
        public static Matrix4x4 ChangeOfBasisFromSpherical(
            Vector3 surfaceWorld,
            Vector3 planetCenter,
            Vector3 stablePoleAxis,
            float primeMeridianOffsetDeg)
        {
            var sc = SphericalCoordinates.FromWorldPosition(
                surfaceWorld, planetCenter, stablePoleAxis, primeMeridianOffsetDeg);
            return sc.ToEuclideanMatrix(planetCenter, stablePoleAxis, primeMeridianOffsetDeg);
        }

        public static bool BasisUpFacesAwayFromCenter(
            Matrix4x4 basis,
            Vector3 planetCenter,
            float minDot = 0.99f)
        {
            Vector3 pos = basis.GetColumn(3);
            Vector3 up = basis.GetColumn(1);
            Vector3 expected = OutwardNormal(pos, planetCenter);
            return Vector3.Dot(up, expected) >= minDot;
        }
    }
}
