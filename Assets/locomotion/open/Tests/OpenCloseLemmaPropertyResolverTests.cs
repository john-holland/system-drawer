using NUnit.Framework;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseLemmaPropertyResolverTests
    {
        [Test]
        public void Resolve_DefaultsArrivalBlendZero()
        {
            var props = OpenCloseLemmaPropertyResolver.Resolve();
            Assert.AreEqual(0f, props.arrivalBlendCoefficient, 1e-3f);
            Assert.AreEqual(OpenCloseLemmaAutoCloseBtMode.OnStopExit, props.autoCloseBt);
        }
    }
}
