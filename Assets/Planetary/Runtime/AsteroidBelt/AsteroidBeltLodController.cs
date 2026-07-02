using Planetary.Composition;
using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public sealed class AsteroidBeltLodController : MonoBehaviour
    {
        public AsteroidBeltStatisticalManifold manifold;
        public AsteroidBeltDiscRenderer discRenderer;
        public AsteroidBeltPopulationService population;
        public Transform observer;
        public HorizonLodSettings horizonSettings;
        public float spawnFadeStartKm = 500f;
        public float spawnFadeEndKm = 50f;

        readonly PlanetaryHorizonLodController _horizon = new PlanetaryHorizonLodController(null);

        void Awake()
        {
            if (manifold == null)
                manifold = GetComponent<AsteroidBeltStatisticalManifold>();
            if (discRenderer == null)
                discRenderer = GetComponent<AsteroidBeltDiscRenderer>();
            if (population == null)
                population = GetComponent<AsteroidBeltPopulationService>();
            if (observer == null && Camera.main != null)
                observer = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (observer == null || manifold == null)
                return;
            Vector3 center = manifold.parentPlanet != null ? manifold.parentPlanet.position : transform.position;
            float distKm = Vector3.Distance(observer.position, center) * 0.001f;
            float planetRadius = 1000f;
            if (manifold.parentPlanet != null)
            {
                var pb = manifold.parentPlanet.GetComponent<PlanetBody>();
                if (pb != null)
                    planetRadius = pb.PlanetRadius * 0.001f;
            }

            var horizonCtrl = horizonSettings != null
                ? new PlanetaryHorizonLodController(horizonSettings)
                : _horizon;
            LodTier tier = horizonCtrl.SelectLod(distKm, 0f, 0f, 0f);

            float spawnT = Mathf.InverseLerp(spawnFadeStartKm, spawnFadeEndKm, distKm);
            float discOpacity = 1f - spawnT;
            if (tier == LodTier.SpaceImpostor || tier == LodTier.FarImpostor)
                discOpacity = Mathf.Max(discOpacity, 0.85f);

            if (discRenderer != null)
                discRenderer.SetOpacity(discOpacity, manifold.SampleDensity(observer.position));

            if (population != null)
                population.SetActive(spawnT > 0.1f, observer.position);
        }
    }
}
