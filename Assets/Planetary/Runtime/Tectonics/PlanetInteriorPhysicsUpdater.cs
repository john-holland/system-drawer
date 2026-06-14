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
            if (plateSolver != null)
            {
                plateSolver.plates = regression.RegressPlatesFromSurface(planet, 8, elementalRules);
                plateSolver.Step(1f, planet.transform.position);
            }
            jobProgress = 0.6f;
            var estimator = new AtmosphereCompositionEstimator();
            WeatherPhysicsManifold weatherManifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            var atmos = estimator.Estimate(planet, weatherManifold);
            if (compositionProfile != null)
            {
                planet.composition = PlanetaryCompositionBaker.Bake(
                    planet, planet.planarBase, planet.solverProfile, compositionProfile, atmos, plateSolver?.plates);
                planet.RebakeComposition();
                if (planet.sdfLodRenderer != null)
                    planet.sdfLodRenderer.Rebake();
            }
            jobProgress = 1f;
        }
    }
}
