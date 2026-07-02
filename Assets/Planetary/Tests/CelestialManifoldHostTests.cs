using NUnit.Framework;
using Planetary.Celestial;
using UnityEngine;

namespace Planetary.Tests
{
    public class CelestialManifoldHostTests
    {
        [Test]
        public void RegisterAndUnregister_RelativitySolver()
        {
            var solverGo = new GameObject("solver");
            var solver = solverGo.AddComponent<PhysicalManifoldRelativitySolver>();
            var bodyGo = new GameObject("star");
            var host = bodyGo.AddComponent<CelestialManifoldHost>();
            host.relativitySolver = solver;
            host.mass = 1000f;
            host.influenceRadius = 500f;

            host.Register();
            host.Unregister();

            Object.DestroyImmediate(bodyGo);
            Object.DestroyImmediate(solverGo);
        }
    }
}
