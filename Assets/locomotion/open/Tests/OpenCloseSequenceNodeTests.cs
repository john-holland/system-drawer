using Locomotion.Open.Nodes;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseSequenceNodeTests
    {
        [Test]
        public void RebuildFromTopology_CreatesStopPerEnabledNode()
        {
            var host = new GameObject("BT");
            var seq = host.AddComponent<OpenCloseSequenceNode>();
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "root";
            asset.Root.enabledInGameplay = true;
            asset.AddChild(asset.Root, new OpenCloseTopologyNode { nodeId = "child", enabledInGameplay = true });
            seq.topology = asset;
            seq.RebuildFromTopology();
            Assert.AreEqual(2, seq.children.Count);
            Object.DestroyImmediate(host);
        }
    }
}
