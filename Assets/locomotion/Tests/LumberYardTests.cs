using NUnit.Framework;
using UnityEngine;

public sealed class LumberYardTests
{
    [Test]
    public void TreeGrowth_MineralsWhitelist_FailureOmitsEventStops()
    {
        var go = new GameObject("tree_ta");
        try
        {
            var agent = go.AddComponent<TreeGrowthTravelAgent>();
            Assert.IsTrue(agent.MineralsAllowed("loam"));
            Assert.IsFalse(agent.MineralsAllowed("salt"));
            var step = agent.SelectedStep;
            step.hasFailureEvent = false;
            Assert.IsFalse(agent.CompleteSelected(false));
            agent.groundEvent.openCloseComplete = false;
            agent.ApplyGroundCompositionAfterOpenClose();
            Assert.IsFalse(agent.groundEvent.manifoldApplied);
            agent.groundEvent.openCloseComplete = true;
            agent.ApplyGroundCompositionAfterOpenClose();
            Assert.IsTrue(agent.groundEvent.manifoldApplied);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ChopLimb_SubtractsOnWoodVolume()
    {
        var go = new GameObject("tree");
        try
        {
            var vol = go.AddComponent<DiggableVolume>();
            vol.sdf = ScriptableObject.CreateInstance<SdfMax.SdfMaxCompositionAsset>();
            vol.sdf.nodes.Add(new SdfMax.SdfMaxNode
            {
                op = SdfMax.SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfMax.SdfPrimitiveType.Sphere,
                radius = 1f
            });
            vol.sdf.rootNodeIndex = 0;
            int n = LumberChopSdf.ChopLimb(vol, go.transform.position, 0.2f);
            Assert.AreEqual(1, n);
            Assert.AreEqual("wood", vol.materialClass);
            Assert.Greater(vol.sdf.nodes.Count, 1);
            Object.DestroyImmediate(vol.sdf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ChainsawSpec_HasFailureLanes()
    {
        var spec = ScriptableObject.CreateInstance<ChainsawSpec>();
        Assert.Greater(spec.failureLanes.Length, 0);
        Assert.Greater(spec.sharpness01, 0f);
        Object.DestroyImmediate(spec);
    }
}
