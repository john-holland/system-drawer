using UnityEngine;

namespace Planetary.AsteroidBelt
{
    /// <summary>Scene host for statistical belt + disc + population + mutation log.</summary>
    [AddComponentMenu("Planetary/Asteroid Belt/Asteroid Belt Host")]
    public sealed class AsteroidBeltHost : MonoBehaviour
    {
        public PlanetBody parentPlanet;
        public AsteroidBeltStatisticalManifold manifold;
        public AsteroidBeltDiscRenderer discRenderer;
        public AsteroidBeltPopulationService population;
        public AsteroidBeltLodController lodController;
        public AsteroidBeltMutationLog mutationLog;

        void Reset()
        {
            EnsureComponents();
        }

        void Awake()
        {
            EnsureComponents();
            if (parentPlanet != null && manifold != null)
                manifold.parentPlanet = parentPlanet.transform;
        }

        public void EnsureComponents()
        {
            if (manifold == null)
                manifold = GetComponent<AsteroidBeltStatisticalManifold>() ?? gameObject.AddComponent<AsteroidBeltStatisticalManifold>();
            if (discRenderer == null)
                discRenderer = GetComponent<AsteroidBeltDiscRenderer>() ?? gameObject.AddComponent<AsteroidBeltDiscRenderer>();
            if (population == null)
                population = GetComponent<AsteroidBeltPopulationService>() ?? gameObject.AddComponent<AsteroidBeltPopulationService>();
            if (lodController == null)
                lodController = GetComponent<AsteroidBeltLodController>() ?? gameObject.AddComponent<AsteroidBeltLodController>();
            if (mutationLog == null)
                mutationLog = ScriptableObject.CreateInstance<AsteroidBeltMutationLog>();

            discRenderer.manifold = manifold;
            population.manifold = manifold;
            population.mutationLog = mutationLog;
            lodController.manifold = manifold;
            lodController.discRenderer = discRenderer;
            lodController.population = population;
        }
    }
}
