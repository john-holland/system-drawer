using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class OpenCloseAutoCloseBtCompileTests
    {
        [Test]
        public void OnSequenceEnd_PushesCloseStack()
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "guard";
            asset.Root.autoCloseBt = AutoCloseBtMode.OnSequenceEnd;
            var result = OpenCloseTopologyCompiler.CompilePreview(asset);
            Assert.AreEqual(1, result.closeNodeCount);
            Assert.IsTrue(result.previewLines.Any(l => l.Contains("OnSequenceEnd")));
        }

        [Test]
        public void AfterChildren_EmitsCloseLine()
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "door";
            asset.Root.autoCloseBt = AutoCloseBtMode.AfterChildren;
            var result = OpenCloseTopologyCompiler.CompilePreview(asset);
            Assert.AreEqual(1, result.closeNodeCount);
        }
    }
}
