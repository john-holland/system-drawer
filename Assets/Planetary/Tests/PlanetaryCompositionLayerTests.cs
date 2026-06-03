using NUnit.Framework;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryCompositionLayerTests
    {
        [Test]
        public void Baker_ProducesValidRoot()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 500f;
            var profile = ScriptableObject.CreateInstance<PlanetaryCompositionProfile>();
            var atmos = ScriptableObject.CreateInstance<AtmosphereRegressionProfile>();
            var asset = PlanetaryCompositionBaker.Bake(body, null, null, profile, atmos, null);
            Assert.IsNotNull(asset);
            Assert.Greater(asset.nodes.Count, 0);
            Object.DestroyImmediate(go);
        }
    }
}
