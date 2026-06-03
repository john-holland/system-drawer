using NUnit.Framework;
using Planetary.Elemental;
using UnityEngine;

namespace Planetary.Tests
{
    public class ElementalCompositionRulesEngineTests
    {
        [Test]
        public void RegressToMinerals_DefaultSilicateWhenNoRules()
        {
            var engine = new ElementalCompositionRulesEngine();
            var stack = engine.RegressToMinerals(new MaterialSpec { tags = new[] { "unknown" } });
            Assert.Greater(stack.GetWeight("silicate"), 0.9f);
        }

        [Test]
        public void MineralCompatibility_IdenticalStacksHigh()
        {
            var s = new MineralStack
            {
                weights = new[] { new MineralWeight { mineralId = "silicate", weight = 1f } }
            };
            float c = SphereVoronoiPlates.MineralCompatibility(s, s);
            Assert.Greater(c, 0.99f);
        }
    }
}
