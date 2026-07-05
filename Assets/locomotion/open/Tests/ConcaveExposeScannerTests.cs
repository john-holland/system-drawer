using Locomotion.Open.Topology;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class ConcaveExposeScannerTests
    {
        [Test]
        public void ScanHierarchy_SetsApproachAnchor()
        {
            var root = new GameObject("Cabinet");
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(root.transform);
            door.AddComponent<OpenableJointDriver>();

            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            var node = asset.Root;
            node.nodeId = "cabinet";
            ConcaveExposeScanner.ScanHierarchy(root.transform, asset, node, AutoCloseBtMode.OnStopExit);

            Assert.IsTrue(node.hasApproachAnchor);
            Assert.Greater(node.reachRadiusMeters, 0f);
            Object.DestroyImmediate(root);
        }
    }
}
