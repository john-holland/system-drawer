using NUnit.Framework;
using Planetary.Rendering;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryLodHandoffTests
    {
        [Test]
        public void RevealNadir_StartsAtZero()
        {
            var go = new GameObject("stream");
            var stream = go.AddComponent<PlanetMeshStreamingService>();
            var handoff = new PlanetaryLodHandoffController(stream);
            Assert.AreEqual(0f, handoff.RevealNadir);
            Object.DestroyImmediate(go);
        }
    }
}
