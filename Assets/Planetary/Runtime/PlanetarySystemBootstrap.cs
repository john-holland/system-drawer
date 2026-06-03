using UnityEngine;

namespace Planetary
{
    /// <summary>Registers planet pathing solver at startup.</summary>
    public sealed class PlanetarySystemBootstrap : MonoBehaviour
    {
        public PhysicalManifoldRelativitySolver relativitySolver;

        void Awake()
        {
            var solver = new CurvedSpacetimeSd2PathingSolver { Relativity = relativitySolver };
            PhysicalPathingSolverRegistry.Register(PhysicalPathingMedium.Space, solver);
        }
    }
}
