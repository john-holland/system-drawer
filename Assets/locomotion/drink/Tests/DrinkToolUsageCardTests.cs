using Locomotion.Drink;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class DrinkToolUsageCardTests
    {
        [Test]
        public void Generate_OmitsReleaseWhenPutWithoutRelease()
        {
            var go = new GameObject("cup");
            var considerGo = new GameObject("consider");
            var consider = considerGo.AddComponent<Consider>();
            var props = DrinkLemmaProperties.Defaults;
            props.putWithoutRelease = true;
            props.sipCount = 2;
            var cards = DrinkToolUsageCardGenerator.Generate(
                consider, go, "drink", new RagdollState(), props);
            Assert.AreEqual(2, DrinkToolUsageCardGenerator.CountSipCards(cards));
            foreach (var c in cards)
                Assert.IsFalse(c.sectionName != null && c.sectionName.StartsWith("release_"));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(considerGo);
        }
    }
}
