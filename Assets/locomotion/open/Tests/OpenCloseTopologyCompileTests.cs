using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseTopologyCompileTests
    {
        [Test]
        public void CompilePreview_PrunesDisabledBranch()
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            var root = asset.Root;
            root.nodeId = "root";
            root.enabledInGameplay = true;
            asset.AddChild(root, new OpenCloseTopologyNode { nodeId = "disabled", enabledInGameplay = false });
            asset.AddChild(root, new OpenCloseTopologyNode { nodeId = "enabled", enabledInGameplay = true, autoCloseBt = AutoCloseBtMode.None });
            var result = OpenCloseTopologyCompiler.CompilePreview(asset);
            Assert.IsTrue(result.previewLines.Any(l => l.Contains("enabled")));
            Assert.IsFalse(result.previewLines.Any(l => l.Contains("disabled")));
        }

        [Test]
        public void CompilePreview_NoneOmitsCloseCount()
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "box";
            asset.Root.autoCloseBt = AutoCloseBtMode.None;
            var result = OpenCloseTopologyCompiler.CompilePreview(asset);
            Assert.AreEqual(0, result.closeNodeCount);
        }
    }
}
