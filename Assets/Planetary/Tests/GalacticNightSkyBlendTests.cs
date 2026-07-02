using NUnit.Framework;
using Planetary.Celestial;
using UnityEngine;

namespace Planetary.Tests
{
    public class GalacticNightSkyBlendTests
    {
        [Test]
        public void NormalizeWeights_SumsToOne()
        {
            var w = new[] { 2f, 3f, 5f };
            var norm = GalacticSkyLatticeIndex.NormalizeWeights(w);
            float sum = 0f;
            for (int i = 0; i < norm.Length; i++)
                sum += norm[i];
            Assert.AreEqual(1f, sum, 1e-4f);
        }

        [Test]
        public void OverlapWeight_IsBetweenZeroAndOne()
        {
            var a = new GalacticSkyLatticeCell
            {
                centroid = Vector3.zero,
                eggRadii = new Vector3(10f, 10f, 10f)
            };
            var b = new GalacticSkyLatticeCell
            {
                centroid = new Vector3(5f, 0f, 0f),
                eggRadii = new Vector3(10f, 10f, 10f)
            };
            float w = GalacticSkyLatticeIndex.OverlapWeight(Vector3.zero, a, b);
            Assert.GreaterOrEqual(w, 0f);
            Assert.LessOrEqual(w, 1f);
        }
    }
}
