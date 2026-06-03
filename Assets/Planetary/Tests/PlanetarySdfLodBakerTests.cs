using NUnit.Framework;
using Planetary.Rendering;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetarySdfLodBakerTests
    {
        [Test]
        public void RebuildTiers_ProducesMeshes()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = 200f;
            body.RebakeComposition();
            var profile = ScriptableObject.CreateInstance<PlanetarySdfLodProfile>();
            profile.tierGridRes = new[] { 8, 12 };
            var baker = new PlanetarySdfLodBaker();
            baker.RebuildTiers(body, profile);
            Assert.GreaterOrEqual(baker.TierMeshes.Count, 1);
            Object.DestroyImmediate(go);
        }
    }
}
