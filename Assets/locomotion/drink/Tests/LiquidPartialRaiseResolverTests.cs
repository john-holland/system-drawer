using Locomotion.Liquid;
using NUnit.Framework;

namespace Locomotion.Drink.Tests
{
    public sealed class LiquidPartialRaiseResolverTests
    {
        [Test]
        public void StalledLemma_ReturnsPartialRaiseDefault()
        {
            var props = DrinkLemmaProperties.Defaults;
            props.partialRaiseDefaultWhenStalled = 0.65f;
            float raise = LiquidPartialRaiseResolver.Resolve(
                props, null, null, "{stalled} — cup hovers");
            Assert.AreEqual(0.65f, raise, 0.001f);
        }

        [Test]
        public void AlmostAndMouth_ReturnsPartialRaiseDefault()
        {
            var props = DrinkLemmaProperties.Defaults;
            props.partialRaiseDefaultWhenStalled = 0.7f;
            float raise = LiquidPartialRaiseResolver.Resolve(
                props, null, null, "almost to her mouth");
            Assert.AreEqual(0.7f, raise, 0.001f);
        }

        [Test]
        public void ShouldSuppressDispense_WhenStalledLemmaPresent()
        {
            var props = DrinkLemmaProperties.Defaults;
            Assert.IsTrue(LiquidPartialRaiseResolver.ShouldSuppressDispense(
                props, "{stalled} shaking", null));
        }

        [Test]
        public void ExplicitPartialRaise_OverridesLemmaInference()
        {
            var props = DrinkLemmaProperties.Defaults;
            props.partiallyRaiseAmount = 0.4f;
            float raise = LiquidPartialRaiseResolver.Resolve(
                props, null, null, "plain drink");
            Assert.AreEqual(0.4f, raise, 0.001f);
        }
    }
}
