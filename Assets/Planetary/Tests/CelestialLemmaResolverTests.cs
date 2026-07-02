using NUnit.Framework;
using Planetary.Celestial;
using UnityEngine;

namespace Planetary.Tests
{
    public class CelestialLemmaResolverTests
    {
        [Test]
        public void MutualGaze_IncreasesStareBackWeight()
        {
            var hint = new CelestialLemmaResolver.LemmaHint
            {
                targetBodyId = "mars",
                tintKeyword = "strange",
                mutualGaze = true
            };
            Vector3 observer = Vector3.zero;
            Vector3 target = new Vector3(100f, 0f, 0f);
            var app = CelestialLemmaResolver.Resolve(
                hint,
                observer,
                Vector3.right,
                target,
                Vector3.left,
                10f);
            Assert.Greater(app.stareBackWeight, 0.5f);
            Assert.AreNotEqual(Color.white, app.tint);
        }

        [Test]
        public void TryParseLemma_ExtractsObserverAndTarget()
        {
            Assert.IsTrue(CelestialLemmaResolver.TryParseLemma(
                "{P:celestial|observer=pluto|target=mars|tint=strange|gaze=mutual}",
                out var hint));
            Assert.AreEqual("pluto", hint.observerBodyId);
            Assert.AreEqual("mars", hint.targetBodyId);
            Assert.IsTrue(hint.mutualGaze);
        }
    }
}
