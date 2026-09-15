using Locomotion.Open;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class TayloringOpenCloseBtTests
    {
        [Test]
        public void FromSteps_LinearStopsMatchTayloringOrder()
        {
            var go = new GameObject("tayloring_ta");
            var agent = go.AddComponent<TayloringTravelAgent>();
            agent.steps = TayloringTravelAgent.DefaultPipeline();
            var topology = TayloringOpenCloseBt.FromSteps(agent);
            Assert.AreEqual("tayloring_line", topology.Root.nodeId);
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
