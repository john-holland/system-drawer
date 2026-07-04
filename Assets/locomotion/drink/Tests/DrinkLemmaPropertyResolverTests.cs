using Locomotion.Drink;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class DrinkLemmaPropertyResolverTests
    {
        [Test]
        public void Resolve_ClampsSipCountToAtLeastOne()
        {
            var props = new[]
            {
                new ThesaurusEntryPropertyRecord
                {
                    entryId = "e1",
                    propertyKey = DrinkLemmaPropertyKeys.SipCount,
                    propertyValue = "0",
                },
            };
            var d = DrinkLemmaPropertyResolver.Resolve(null, null, props);
            Assert.GreaterOrEqual(d.sipCount, 1);
        }

        [Test]
        public void Resolve_ReadsDrinkEfficacy()
        {
            var props = new[]
            {
                new ThesaurusEntryPropertyRecord
                {
                    entryId = "e1",
                    propertyKey = DrinkLemmaPropertyKeys.DrinkEfficacy,
                    propertyValue = "0.9",
                },
            };
            var d = DrinkLemmaPropertyResolver.Resolve(null, null, props);
            Assert.AreEqual(0.9f, d.drinkEfficacy, 0.001f);
        }

        [Test]
        public void VolumePerSip_DividesTotalBySipCount()
        {
            var d = DrinkLemmaProperties.Defaults;
            d.totalVolumeLiters = 0.3f;
            d.sipCount = 3;
            Assert.AreEqual(0.1f, d.VolumePerSipLiters, 0.0001f);
        }

        [Test]
        public void Resolve_ReadsComedyProperties()
        {
            var props = new[]
            {
                new ThesaurusEntryPropertyRecord { propertyKey = DrinkLemmaPropertyKeys.PartiallyRaiseAmount, propertyValue = "0.65" },
                new ThesaurusEntryPropertyRecord { propertyKey = DrinkLemmaPropertyKeys.ClosureMode, propertyValue = "stalled" },
                new ThesaurusEntryPropertyRecord { propertyKey = DrinkLemmaPropertyKeys.InfiniteDrain, propertyValue = "true" },
                new ThesaurusEntryPropertyRecord { propertyKey = DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds, propertyValue = "30" },
            };
            var d = DrinkLemmaPropertyResolver.Resolve(null, null, props);
            Assert.AreEqual(0.65f, d.partiallyRaiseAmount, 0.001f);
            Assert.AreEqual(DrinkClosureMode.Stalled, d.closureMode);
            Assert.IsTrue(d.infiniteDrain);
            Assert.AreEqual(30f, d.infiniteDrainClosureSeconds, 0.001f);
        }
    }
}
