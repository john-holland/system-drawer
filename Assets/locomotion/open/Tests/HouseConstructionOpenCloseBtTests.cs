using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class HouseConstructionOpenCloseBtTests
    {
        [Test]
        public void FromSteps_LinearStopsMatchPlacementOrder()
        {
            var go = new GameObject("ta");
            var agent = go.AddComponent<HouseConstructionTravelAgent>();
            agent.steps.Clear();
            agent.PlanRtsFromFenceRun(3);
            var topology = HouseConstructionOpenCloseBt.FromSteps(agent);
            Assert.AreEqual("construction_site", topology.Root.nodeId);
            Assert.IsFalse(topology.Root.enabledInGameplay);
            int n = 0;
            foreach (var child in topology.GetChildren(topology.Root))
            {
                Assert.AreEqual(agent.steps[n].sgInstanceId, child.nodeId);
                n++;
            }
            Assert.AreEqual(5, n);
            Object.DestroyImmediate(topology);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bake_CreatesStopChildrenInOrder()
        {
            var go = new GameObject("ta");
            var agent = go.AddComponent<HouseConstructionTravelAgent>();
            agent.steps.Clear();
            agent.PlanRtsFromFenceRun(2);
            var parent = new GameObject("bt");
            var result = HouseConstructionOpenCloseBt.Bake(agent, parent.transform, agent.transform);
            Assert.AreEqual(3, result.stopNodes.Count);
            Assert.IsTrue(result.stopNodes[0].name.Contains("post_0"));
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(go);
        }
    }
}
