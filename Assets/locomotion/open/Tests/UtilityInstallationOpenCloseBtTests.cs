using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class UtilityInstallationOpenCloseBtTests
    {
        [Test]
        public void FromSteps_LinearStopsMatchInstallOrder()
        {
            var go = new GameObject("room");
            var room = go.AddComponent<UtilityRoomBootstrap>();
            room.Ensure();
            var topology = UtilityInstallationOpenCloseBt.FromSteps(room);
            Assert.AreEqual("utility_install", topology.Root.nodeId);
            Assert.IsFalse(topology.Root.enabledInGameplay);
            int n = 0;
            foreach (var child in topology.GetChildren(topology.Root))
            {
                Assert.AreEqual(room.installSteps[n].id, child.nodeId);
                n++;
            }
            Assert.Greater(n, 3);
            Object.DestroyImmediate(topology);
            Object.DestroyImmediate(go);
        }
    }
}
