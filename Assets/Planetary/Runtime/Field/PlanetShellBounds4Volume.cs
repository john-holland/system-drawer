using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Field
{
    /// <summary>Planet shell patch volume (lat/lon/radius wedge + narrative time window).</summary>
    public sealed class PlanetShellBounds4Volume : MonoBehaviour, ISpatiotemporalVolume
    {
        public PlanetBody planet;
        public float latMinDeg = -10f;
        public float latMaxDeg = 10f;
        public float lonMinDeg = -10f;
        public float lonMaxDeg = 10f;
        public float radiusMin;
        public float radiusMax = 1000f;
        public float tMin;
        public float tMax = float.MaxValue;

        public bool Contains(Vector3 world, float narrativeTime)
        {
            if (narrativeTime < tMin || narrativeTime > tMax)
                return false;
            if (planet == null)
                return false;

            var sc = SphericalCoordinates.FromWorldPosition(
                world, planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
            if (sc.LatitudeDeg < latMinDeg || sc.LatitudeDeg > latMaxDeg)
                return false;
            if (sc.LongitudeDeg < lonMinDeg || sc.LongitudeDeg > lonMaxDeg)
                return false;
            float r = Vector3.Distance(world, planet.PlanetCenter);
            return r >= radiusMin && r <= radiusMax;
        }

        public Bounds ApproximateBounds()
        {
            if (planet == null)
                return new Bounds(transform.position, Vector3.one * radiusMax * 2f);
            return new Bounds(planet.PlanetCenter, Vector3.one * radiusMax * 2.2f);
        }

        public void ExportSamples(List<Vector3> surfacePoints, float narrativeTime)
        {
            if (surfacePoints == null || planet == null)
                return;
            float latMid = (latMinDeg + latMaxDeg) * 0.5f;
            float lonMid = (lonMinDeg + lonMaxDeg) * 0.5f;
            float r = (radiusMin + radiusMax) * 0.5f;
            var sc = new SphericalCoordinates(latMid, lonMid, r);
            surfacePoints.Add(sc.ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg));
        }
    }
}
