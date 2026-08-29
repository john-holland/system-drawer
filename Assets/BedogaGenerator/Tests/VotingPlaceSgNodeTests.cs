#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class VotingPlaceSgNodeTests
{
    [Test]
    public void Ensure_ExecutesDefaultInpaintPrompt()
    {
        var go = new GameObject("voting-sg");
        try
        {
            var hub = go.AddComponent<VotingQueueHub>();
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, hub.inpaintPrompt);
            var node = VotingPlaceSgNode.Ensure(go);
            Assert.IsNotNull(node);
            Assert.IsTrue(node.executed);
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, node.inpaintPrompt);
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, node.ExecuteInpaint());
            CollectionAssert.AreEqual(
                new[] { "queued", "by", "address", "or", "randomly-if-so" },
                node.lastTokens);
            Assert.AreEqual(1, node.lastIfs.Length);
            Assert.AreEqual(IfOperatorPosition.Postfix, node.lastIfs[0].Position);
            Assert.IsTrue(node.lastIfs[0].Composed);
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, hub.ExecuteInpaintPrompt());
            Assert.AreEqual(hub.inpaintPrompt, node.inpaintPrompt);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
