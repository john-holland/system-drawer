using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseAmbulateArrivalTests
    {
        [Test]
        public void Gate_ZeroBlend_RequiresFullStop()
        {
            float gate = OpenCloseArrivalGate.ComputeGate(0f, 0.9f, 1f, true, true);
            Assert.Less(gate, 1f);
            gate = OpenCloseArrivalGate.ComputeGate(0f, 1f, 1f, true, true);
            Assert.GreaterOrEqual(gate, 1f);
        }

        [Test]
        public void Gate_OneBlend_AllowsReachOnly()
        {
            Assert.IsTrue(OpenCloseArrivalGate.ShouldAttemptOpen(1f, 1f, true));
            Assert.IsTrue(OpenCloseArrivalGate.ShouldAttemptOpen(0.5f, 1f, true));
        }

        [Test]
        public void ShouldAttemptOpen_ZeroBlend_NeedsFullGate()
        {
            Assert.IsFalse(OpenCloseArrivalGate.ShouldAttemptOpen(0.5f, 0f, true));
            Assert.IsTrue(OpenCloseArrivalGate.ShouldAttemptOpen(1f, 0f, true));
        }
    }
}
