using Planetary.Composition;
using Planetary.Lava;
using SdfMax;
using UnityEngine;
using Weather;

namespace Planetary.Tectonics
{
    /// <summary>
    /// Narrative / gameplay hook to erupt a volcano on a planet surface and rebake SDF composition.
    /// </summary>
    [AddComponentMenu("Planetary/Planet Volcano Controller")]
    public sealed class PlanetVolcanoController : MonoBehaviour
    {
        public PlanetBody planet;
        public LavaPhysicsManifold lavaManifold;
        public WeatherPhysicsManifold weatherManifold;

        [Header("Vent site (lat/lon on reference sphere)")]
        public float latitudeDeg = 12f;
        public float longitudeDeg = 45f;

        [Header("Eruption")]
        public float coneRadiusMeters = 20f;
        public float gasPressure = 150f;
        public float ventTemperatureC = 900f;
        public bool active;

        public bool IsActive => active;

        public void Activate()
        {
            if (planet == null)
                return;
            active = true;
            ApplyEruption();
        }

        public void Deactivate()
        {
            if (planet == null)
                return;
            active = false;
            planet.RebakeComposition();
            if (planet.sdfLodRenderer != null)
                planet.sdfLodRenderer.Rebake();
        }

        public void Toggle()
        {
            if (active)
                Deactivate();
            else
                Activate();
        }

        [ContextMenu("Activate Volcano")]
        void ContextActivate() => Activate();

        [ContextMenu("Deactivate Volcano")]
        void ContextDeactivate() => Deactivate();

        void ApplyEruption()
        {
            planet.RebakeComposition();

            if (planet.composition == null || planet.composition.nodes == null)
                return;

            Vector3 ventWorld = VentWorldPosition();
            var site = new VolcanoSite
            {
                worldPosition = ventWorld,
                radiusMeters = coneRadiusMeters,
                gasPressure = gasPressure
            };

            int root = VolcanoStressSolver.AppendVolcanoConeToGraph(
                planet.composition,
                planet.composition.rootNodeIndex,
                site,
                planet.transform);
            planet.composition.rootNodeIndex = root;

            if (planet.volumeProvider != null)
            {
                planet.volumeProvider.composition = planet.composition;
                planet.volumeProvider.RebuildIfDirty(true);
            }

            if (planet.sdfLodRenderer != null)
                planet.sdfLodRenderer.Rebake();

            StampVentInWeather(ventWorld);
        }

        Vector3 VentWorldPosition()
        {
            float r = planet.PlanetRadius;
            return new SphericalCoordinates(latitudeDeg, longitudeDeg, r)
                .ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
        }

        void StampVentInWeather(Vector3 ventWorld)
        {
            if (weatherManifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            if (weatherManifold == null)
                return;

            var cell = weatherManifold.GetDataAtPosition(ventWorld);
            cell.mode = WeatherMode.MagmaPlume;
            cell.temperature = ventTemperatureC;
            cell.gasPressure = Mathf.Max(cell.gasPressure, gasPressure);
            cell.lavaVelocity = PlanetSurfaceFrame.OutwardNormal(ventWorld, planet.PlanetCenter) * 2f;
            weatherManifold.SetDataAtPosition(ventWorld, cell);

            if (lavaManifold != null)
                lavaManifold.ScanBreaches(null, 0, gasPressure * 0.01f);
        }
    }
}
