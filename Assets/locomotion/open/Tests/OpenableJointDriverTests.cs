using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenableJointDriverTests
    {
        [Test]
        public void BeginOpen_FromClosed_ReachesOpen()
        {
            var go = new GameObject("Door");
            go.AddComponent<Rigidbody>().isKinematic = true;
            var driver = go.AddComponent<OpenableJointDriver>();
            driver.usePhysicsMotor = false;
            driver.targetOpenAngle = 90f;

            Assert.IsTrue(driver.BeginOpen());
            driver.ForceOpen();
            Assert.AreEqual(OpenableJointState.Open, driver.state);
            Object.DestroyImmediate(go);
        }
    }
}
