using Planetary.Composition;
using Planetary.Lava;
using Planetary.Tectonics;
using UnityEngine;

namespace Planetary.Rendering
{
    public sealed class PlanetarySimulationScheduler : MonoBehaviour
    {
        public HorizonLodSettings lodSettings;
        public PlanetBody planet;
        public PlateTectonicsPhysicsSolver plateSolver;
        public LavaPhysicsManifold lava;
        public Transform player;
        public float midZonePlateStepInterval = 5f;
        float _plateTimer;

        void Update()
        {
            if (planet == null || player == null)
                return;
            var lod = new PlanetaryHorizonLodController(lodSettings);
            float alt = PlanetaryHorizonLodController.ComputeAltitudeMsl(
                player.position, planet.PlanetCenter, planet.PlanetRadius, 0f);
            float distKm = Mathf.Max(0f, Vector3.Distance(player.position, planet.PlanetCenter) - planet.PlanetRadius) * 0.001f;
            var tier = lod.SelectLod(distKm, alt, 1000f, 3000f);
            switch (tier)
            {
                case LodTier.FullSim:
                    plateSolver?.Step(Time.deltaTime, player.position);
                    lava?.AdvectStep(Time.deltaTime);
                    break;
                case LodTier.MidPrebake:
                    _plateTimer += Time.deltaTime;
                    if (_plateTimer >= midZonePlateStepInterval)
                    {
                        plateSolver?.Step(1f, player.position);
                        _plateTimer = 0f;
                    }
                    break;
            }
        }
    }
}
