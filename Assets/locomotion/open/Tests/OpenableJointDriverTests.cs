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

        [Test]
        public void Slide_SetOpen01_MovesAlongAxis()
        {
            var go = new GameObject("Sash");
            go.AddComponent<Rigidbody>().isKinematic = true;
            var driver = go.AddComponent<OpenableJointDriver>();
            driver.jointKind = OpenCloseJointKind.Slide;
            driver.targetSlideMeters = 0.5f;
            driver.slideAxisLocal = Vector3.right;
            driver.usePhysicsMotor = false;
            Vector3 start = go.transform.localPosition;
            driver.SetOpen01(1f);
            Assert.AreEqual(1f, driver.Open01, 0.01f);
            Assert.AreEqual(start.x + 0.5f, go.transform.localPosition.x, 0.01f);
            Object.DestroyImmediate(go);
        }
    }
}
