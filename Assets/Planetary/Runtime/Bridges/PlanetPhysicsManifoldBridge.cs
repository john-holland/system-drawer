using System;
using System.Collections.Generic;
using UnityEngine;
using Weather;
namespace Planetary.Bridges
{
    /// <summary>
    /// Stamps planet surface cells into <see cref="WeatherPhysicsManifold"/> during composition rebake.
    /// </summary>
    [AddComponentMenu("Planetary/Planet Physics Manifold Bridge")]
    public sealed class PlanetPhysicsManifoldBridge : MonoBehaviour
    {
        public PlanetBody planet;
        public WeatherPhysicsManifold manifold;
        public PlanetSpacetimeEnvelope spacetimeEnvelope;

        [Header("Surface Coefficients")]
        public float surfaceFriction = 0.7f;
        public float surfacePorosity = 0.2f;
        public float atmosphereHeightMeters = 100000f;

        [Header("Stamp Grid")]
        [Tooltip("Samples per axis across planet diameter when stamping.")]
        public int stampGridSteps = 8;

        public void FitManifoldBoundsToPlanet()
        {
            if (manifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out manifold);
            if (manifold == null || planet == null)
                return;

            float r = planet.PlanetRadius;
            if (spacetimeEnvelope != null)
                r += spacetimeEnvelope.atmosphereHeightMeters;
            else
                r += atmosphereHeightMeters;

            Vector3 center = planet.PlanetCenter;
            float diameter = r * 2f;
            manifold.worldBounds = new Bounds(center, new Vector3(diameter, diameter, diameter));
        }

        public void StampFromCompositionBake()
        {
            if (manifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out manifold);
            if (manifold == null || planet == null)
                return;

            FitManifoldBoundsToPlanet();
            int steps = Mathf.Max(2, stampGridSteps);
            Bounds b = manifold.worldBounds;
            Vector3 step = b.size / (steps - 1);

            for (int ix = 0; ix < steps; ix++)
            for (int iz = 0; iz < steps; iz++)
            {
                Vector3 sample = b.min + new Vector3(ix * step.x, b.center.y - planet.PlanetRadius, iz * step.z);
                if (planet.TrySampleHeightAtWorld(sample, out float height, out _))
                {
                    Vector3 dir = (sample - planet.PlanetCenter).normalized;
                    Vector3 surface = planet.PlanetCenter + dir * (planet.PlanetRadius + height);
                    StampCell(surface, surfaceFriction, surfacePorosity);
                }
            }
        }

        void StampCell(Vector3 pos, float friction, float porosity)
        {
            ManifoldCellData data = manifold.GetDataAtPosition(pos);
            data.surfaceFriction = friction;
            data.surfacePorosity = porosity;
            data.mode = WeatherMode.RoadDirt;
            manifold.SetDataAtPosition(pos, data);
        }
    }
}
