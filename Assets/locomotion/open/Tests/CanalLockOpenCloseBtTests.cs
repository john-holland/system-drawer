using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class CanalLockOpenCloseBtTests
    {
        [Test]
        public void FromSteps_LinearStopsMatchLockOrder()
        {
            var go = new GameObject("canal_ta");
            var agent = go.AddComponent<CannalTravelAgent>();
            agent.steps = CannalTravelAgent.DefaultPipeline();
            var topology = CanalLockOpenCloseBt.FromSteps(agent);
            Assert.AreEqual("canal_lock", topology.Root.nodeId);
            int n = 0;
            foreach (var child in topology.GetChildren(topology.Root))
            {
                Assert.AreEqual(agent.steps[n].sgInstanceId, child.nodeId);
                n++;
            }
            Assert.AreEqual(4, n);
            Object.DestroyImmediate(topology);
            Object.DestroyImmediate(go);
        }
    }
}
