using Planetary.Composition;
using Planetary.Elemental;
using UnityEngine;
using Weather;

namespace Planetary.Tectonics
{
    public sealed class PlanetInteriorPhysicsUpdater : MonoBehaviour
    {
        public PlanetBody planet;
        public PlateTectonicsPhysicsSolver plateSolver;
        public PlanetaryCompositionProfile compositionProfile;
        public MaterialRegressionService regression = new MaterialRegressionService();
        public ElementalRule[] elementalRules = System.Array.Empty<ElementalRule>();
        [Range(0f, 1f)]
        public float jobProgress;

        public void UpdateInteriorPhysics()
        {
            if (planet == null)
                return;
            jobProgress = 0.2f;
            var plates = RebuildTectonicPlates(clearExisting: true, stepPhysics: true);
            jobProgress = 0.6f;
            RebakeFromPlates(plates);
            jobProgress = 1f;
        }

        /// <summary>Clear existing plates, regress from surface, optionally run one physics step.</summary>
        public PlateDefinition[] RebuildTectonicPlates(bool clearExisting, bool stepPhysics)
        {
            if (planet == null)
                return System.Array.Empty<PlateDefinition>();
            if (plateSolver != null && clearExisting)
                plateSolver.ClearPlates();
            regression.Engine.SetRules(elementalRules);
            var plates = regression.RegressPlatesFromSurface(planet, 8, elementalRules);
            PlateTectonicsPhysicsSolver.ResetPlateStress(plates);
            if (plateSolver != null)
            {
                plateSolver.plates = plates;
                if (stepPhysics)
                    plateSolver.Step(1f, planet.transform.position);
            }
            return plateSolver != null ? plateSolver.plates : plates;
        }

        public void RebakeFromPlates(PlateDefinition[] plates)
        {
            if (planet == null || compositionProfile == null)
                return;
            var estimator = new AtmosphereCompositionEstimator();
            WeatherPhysicsManifold weatherManifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            var atmos = estimator.Estimate(planet, weatherManifold);
            planet.composition = PlanetaryCompositionBaker.Bake(
                planet, planet.planarBase, planet.solverProfile, compositionProfile, atmos, plates);
            planet.ApplyCompositionToVolumeProvider();
            if (planet.sdfLodRenderer != null)
                planet.sdfLodRenderer.Rebake();
        }

        public void ApplyAtmosphereSnapshot(AtmosphereRegressionProfile snapshot)
        {
            if (snapshot == null || compositionProfile == null || planet == null)
                return;
            planet.composition = PlanetaryCompositionBaker.Bake(
                planet, planet.planarBase, planet.solverProfile, compositionProfile, snapshot, plateSolver?.plates);
            planet.ApplyCompositionToVolumeProvider();
            if (planet.sdfLodRenderer != null)
                planet.sdfLodRenderer.Rebake();
        }
    }
}
