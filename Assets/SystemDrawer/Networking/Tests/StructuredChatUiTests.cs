#if UNITY_INCLUDE_TESTS

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class StructuredChatUiTests
{
    GameObject _root;
    string _historyRoot;

    [SetUp]
    public void SetUp()
    {
        _historyRoot = Path.Combine(Path.GetTempPath(), "structured-chat-tests-" + Path.GetRandomFileName());
        StructuredChatSessionHistory.RootOverride = _historyRoot;
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
        StructuredChatSessionHistory.RootOverride = null;
        if (!string.IsNullOrEmpty(_historyRoot) && Directory.Exists(_historyRoot))
            Directory.Delete(_historyRoot, true);
    }

    static ChatLexiconWord[] SampleWords()
    {
        return new[]
        {
            new ChatLexiconWord { id = "yes", text = "Yes" },
            new ChatLexiconWord { id = "no", text = "No" },
            new ChatLexiconWord { id = "hello", text = "Hello" }
        };
    }

    [Test]
    public void Spec_CreatesWordNodesFromLexicon()
    {
        var words = SampleWords();
        var spec = StructuredChatNetworkRequirements.BuildCanonicalTree(words);
        Assert.AreEqual("chat.wordBank", spec.children[0].eventName);
        Assert.AreEqual(3, spec.children[0].children.Count);
        Assert.AreEqual("chat.word.yes", spec.children[0].children[0].eventName);
        Assert.IsTrue(spec.children.Any(c => c.eventName == "chat.send"));
        Assert.IsTrue(spec.children.Any(c => c.eventName == "chat.sentFlash"));

        _root = new GameObject("chat");
        var result = StructuredChatNetworkRequirementsSync.Apply(_root.transform, words, true);
        Assert.IsNotNull(result.rootNode);
        var nodes = _root.GetComponentsInChildren<StructuredChatRagdollNode>(true);
        Assert.AreEqual(3, nodes.Count(n => n.eventName != null && n.eventName.StartsWith("chat.word.")));
        Assert.IsTrue(nodes.Any(n => n.eventName == "chat.send" && n.GetComponent<StructuredChatNodeClick>() != null));
    }

    [Test]
    public void Preview_StreamsComposeDeltaOnAppend_SendButtonDoesNot()
    {
        var words = SampleWords();
        var preview = new StructuredChatComposer { ComposeMode = "preview" };
        preview.SetAllowedWords(words);
        Assert.IsTrue(preview.StreamOnAppend);
        Assert.IsTrue(preview.TryAppend("yes", out _));
        var delta = preview.BuildDelta(false);
        Assert.IsFalse(delta.committed);
        Assert.AreEqual("yes", delta.tokens[0]);

        var button = new StructuredChatComposer { ComposeMode = "sendButton" };
        button.SetAllowedWords(words);
        Assert.IsFalse(button.StreamOnAppend);
        Assert.IsTrue(button.TryAppend("no", out _));
    }

    [Test]
    public void Ragdoll_PreviewAppendsDelta_SendButtonWaitsForCommit()
    {
        var words = SampleWords();
        _root = BuildRagdoll(words, "preview", out var ragdoll);
        ragdoll.AppendWord("yes");
        Assert.IsNotNull(ragdoll.LastStreamed);
        Assert.IsFalse(ragdoll.LastStreamed.committed);
        Assert.AreEqual("yes", ragdoll.LastStreamed.tokens[0]);

        Object.DestroyImmediate(_root);
        _root = BuildRagdoll(words, "sendButton", out ragdoll);
        ragdoll.AppendWord("yes");
        Assert.IsNull(ragdoll.LastStreamed);
        Assert.IsTrue(ragdoll.Commit());
        Assert.IsNotNull(ragdoll.LastStreamed);
        Assert.IsTrue(ragdoll.LastStreamed.committed);
        Assert.AreEqual(0, ragdoll.Composer.Tokens.Count);
        Assert.IsTrue(ragdoll.SentFlashVisible);
    }

    [Test]
    public void UnknownWord_IsDenied()
    {
        var composer = new StructuredChatComposer();
        composer.SetAllowedWords(SampleWords());
        Assert.IsFalse(composer.TryAppend("banana", out string deny));
        Assert.AreEqual(StructuredChatComposer.DenyWord, deny);

        var channel = new StructuredChatChannel
        {
            Entitlement = new ChatEntitlementSnapshot
            {
                entitled = true,
                tosSigned = true,
                textAllowed = true,
                structuredChat = "optional",
                lexicon = new ChatLexiconData { words = SampleWords() }
            }
        };
        Assert.IsFalse(channel.TrySend(null, "banana", new[] { "banana" }, out deny));
        Assert.AreEqual(StructuredChatChannel.DenyWord, deny);
    }

    [Test]
    public void History_TruncatesOldestAfterByteCap()
    {
        const string product = "prod-cap";
        for (int i = 0; i < 6; i++)
        {
            StructuredChatSessionHistory.Append(product, "s1", new StructuredChatSessionHistory.Entry
            {
                userId = "u",
                tokens = new[] { "hello" },
                text = "hello-message-" + i + "-xxxxxxxxxx",
                direction = "out"
            });
        }
        long total = StructuredChatSessionHistory.ProductBytes(product);
        Assert.Greater(total, 0);
        StructuredChatSessionHistory.TruncateIfNeeded(product, System.Math.Max(1, total - 1));
        Assert.Less(StructuredChatSessionHistory.ProductBytes(product), total);
        var left = StructuredChatSessionHistory.Load(product, "s1");
        Assert.Greater(left.Count, 0);
        Assert.IsFalse(left[0].text.Contains("hello-message-0"));
    }

    [Test]
    public void ChatLemma_OpenClosePlaceholders_InferOp()
    {
        var open = ChatLemmaProperties.ResolveFromParams(null, "open-chat");
        Assert.AreEqual(ChatLemmaOp.Open, open.op);
        var close = ChatLemmaProperties.ResolveFromParams(null, "close-chat");
        Assert.AreEqual(ChatLemmaOp.Close, close.op);
        var dismiss = ChatLemmaProperties.ResolveFromParams(null, "dismiss");
        Assert.AreEqual(ChatLemmaOp.Close, dismiss.op);
        var toggle = ChatLemmaProperties.ResolveFromParams(
            new System.Collections.Generic.Dictionary<string, string> { { "op", "toggle" } },
            "chat");
        Assert.AreEqual(ChatLemmaOp.Toggle, toggle.op);
    }

    [Test]
    public void ChatLemma_ApplyFromPrompt_OpensAndClosesRagdoll()
    {
        var words = SampleWords();
        _root = BuildRagdoll(words, "preview", out var ragdoll);
        ragdoll.SetOpen(false);
        Assert.IsFalse(ragdoll.IsOpen);
        int n = ChatLemmaResolver.ApplyFromPrompt("{P:open-chat}", ragdoll);
        Assert.AreEqual(1, n);
        Assert.IsTrue(ragdoll.IsOpen);
        n = ChatLemmaResolver.ApplyFromPrompt("{P:close-chat}", ragdoll);
        Assert.AreEqual(1, n);
        Assert.IsFalse(ragdoll.IsOpen);
        ragdoll.HandleBubble(new MenuRagdollEvent("chat.word", null, "yes"));
        Assert.AreEqual(0, ragdoll.Composer.Tokens.Count);
        n = ChatLemmaResolver.ApplyFromPrompt("{P:chat|op=open|product-id=p2|session-id=s2}", ragdoll);
        Assert.AreEqual(1, n);
        Assert.AreEqual("p2", ragdoll.productId);
        Assert.AreEqual("s2", ragdoll.sessionId);
    }

    [Test]
    public void ChatLemma_Open_RequiresEntitlement()
    {
        var words = SampleWords();
        _root = BuildRagdoll(words, "preview", out var ragdoll);
        ragdoll.Channel.Entitlement = new ChatEntitlementSnapshot
        {
            entitled = false,
            tosSigned = false,
            textAllowed = false,
            structuredChat = "optional"
        };
        ragdoll.SetOpen(false);
        int n = ChatLemmaResolver.ApplyFromPrompt("{P:open-chat}", ragdoll);
        Assert.AreEqual(0, n);
        Assert.IsFalse(ragdoll.IsOpen);
        Assert.AreEqual(StructuredChatChannel.DenyTos, ragdoll.LastDenyCode);
        n = ChatLemmaResolver.ApplyFromPrompt("{P:chat|op=open|require-entitlement=false}", ragdoll);
        Assert.AreEqual(1, n);
        Assert.IsTrue(ragdoll.IsOpen);
    }

    static GameObject BuildRagdoll(ChatLexiconWord[] words, string composeMode, out StructuredChatRagdoll ragdoll)
    {
        var root = new GameObject("StructuredChatRagdollRoot");
        ragdoll = root.AddComponent<StructuredChatRagdoll>();
        ragdoll.productId = "test-product";
        ragdoll.sessionId = "test-session";
        ragdoll.Channel.Entitlement = new ChatEntitlementSnapshot
        {
            entitled = true,
            tosSigned = true,
            textAllowed = true,
            structuredChat = "optional",
            composeMode = composeMode,
            lexicon = new ChatLexiconData { words = words }
        };
        ragdoll.ApplyLexicon(words, composeMode);
        StructuredChatNetworkRequirementsSync.Apply(root.transform, words, true);
        return root;
    }
}

#endif
