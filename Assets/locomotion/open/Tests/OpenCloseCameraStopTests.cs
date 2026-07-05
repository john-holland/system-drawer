using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseCameraStopTests
    {
        [Test]
        public void Compute_ReturnsValidPose()
        {
            var node = new OpenCloseTopologyNode
            {
                openingNormal = Vector3.forward,
                cameraHintCenter = Vector3.zero,
                concaveVolume = new EnclosedVolumeRef { hasVolume = true, center = Vector3.zero, size = Vector3.one },
            };
            var stop = OpenCloseCameraStop.Compute(node);
            Assert.Greater(stop.fieldOfView, 0f);
            Assert.AreNotEqual(Vector3.zero, stop.position);
        }
    }
}
