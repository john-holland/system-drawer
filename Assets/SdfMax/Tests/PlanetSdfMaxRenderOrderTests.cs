using NUnit.Framework;
using Planetary;
using SdfMax;
using SpatialVolumes;
using UnityEngine;

namespace SdfMax.Tests
{
    public class PlanetSdfMaxRenderOrderTests
    {
        [Test]
        public void PlanetBody_RebuildAll_DoesNotThrow()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planarBase = ScriptableObject.CreateInstance<PlanetaryPlanarBase>();
            body.solverProfile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            body.volumeProvider = go.AddComponent<SpatialVolumeProvider>();
            body.planetRenderer = go.AddComponent<PlanetRenderer>();
            Assert.DoesNotThrow(() => body.RebuildAll());
            Object.DestroyImmediate(body.planarBase);
            Object.DestroyImmediate(body.solverProfile);
            Object.DestroyImmediate(go);
        }
    }
}
