using Locomotion.Open;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class TreeGrowthOpenCloseBtTests
    {
        [Test]
        public void FromSteps_LinearStopsMatchGrowthOrder()
        {
            var go = new GameObject("tree_ta");
            var agent = go.AddComponent<TreeGrowthTravelAgent>();
            agent.steps = TreeGrowthTravelAgent.DefaultPipeline();
            var topology = TreeGrowthOpenCloseBt.FromSteps(agent);
            Assert.AreEqual("tree_plot", topology.Root.nodeId);
            int n = 0;
            foreach (var child in topology.GetChildren(topology.Root))
            {
                Assert.AreEqual(agent.steps[n].sgInstanceId, child.nodeId);
                n++;
            }
            Assert.AreEqual(agent.steps.Count, n);
            Object.DestroyImmediate(topology);
            Object.DestroyImmediate(go);
        }
    }
}
