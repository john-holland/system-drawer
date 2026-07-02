using Planetary.Celestial;
using UnityEngine;

namespace Planetary
{
    /// <summary>Registers planet pathing solver and celestial bodies at startup.</summary>
    public sealed class PlanetarySystemBootstrap : MonoBehaviour
    {
        public PhysicalManifoldRelativitySolver relativitySolver;
        public GalacticBodyRegistry bodyRegistry;
        public GalacticBodyClient bodyClient;

        void Awake()
        {
            if (relativitySolver == null)
                relativitySolver = FindAnyObjectByType<PhysicalManifoldRelativitySolver>();
            var solver = new CurvedSpacetimeSd2PathingSolver { Relativity = relativitySolver };
            PhysicalPathingSolverRegistry.Register(PhysicalPathingMedium.Space, solver);

            if (bodyRegistry == null)
                bodyRegistry = FindAnyObjectByType<GalacticBodyRegistry>();
            if (bodyRegistry == null)
            {
                var go = new GameObject("GalacticBodyRegistry");
                bodyRegistry = go.AddComponent<GalacticBodyRegistry>();
            }
            if (bodyClient == null)
                bodyClient = FindAnyObjectByType<GalacticBodyClient>();

            var celestials = FindObjectsByType<CelestialManifoldHost>(FindObjectsSortMode.None);
            for (int i = 0; i < celestials.Length; i++)
            {
                if (celestials[i] != null)
                    celestials[i].Register();
            }
        }
    }
}
