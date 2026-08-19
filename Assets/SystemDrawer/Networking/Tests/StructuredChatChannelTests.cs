#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

public class StructuredChatChannelTests
{
    [Test]
    public void OptionalChat_SendWithoutEntitlement_RequiresTos()
    {
        var code = StructuredChatChannel.EvaluateSend("optional", "text", null);
        Assert.AreEqual(StructuredChatChannel.DenyTos, code);
    }

    [Test]
    public void OptionalChat_SendWhenEntitledAndTextAllowed_Succeeds()
    {
        var snap = new ChatEntitlementSnapshot
        {
            entitled = true,
            tosSigned = true,
            textAllowed = true,
            structuredChat = "optional"
        };
        Assert.IsNull(StructuredChatChannel.EvaluateSend("optional", "text", snap));
    }

    [Test]
    public void JurisdictionDisable_BlocksTextWhenEntitled()
    {
        var snap = new ChatEntitlementSnapshot
        {
            entitled = true,
            tosSigned = true,
            textAllowed = false,
            voiceAllowed = false,
            structuredChat = "optional",
            denyCode = StructuredChatChannel.DenyJurisdiction
        };
        Assert.AreEqual(StructuredChatChannel.DenyJurisdiction, StructuredChatChannel.EvaluateSend("optional", "text", snap));
    }

    [Test]
    public void VoiceSlot_AlwaysDisabledWithoutVoiceAllowed()
    {
        var snap = new ChatEntitlementSnapshot
        {
            entitled = true,
            tosSigned = true,
            textAllowed = true,
            voiceAllowed = false,
            structuredChat = "optional"
        };
        Assert.AreEqual(StructuredChatChannel.DenyJurisdiction, StructuredChatChannel.EvaluateSend("optional", "voice", snap));
    }

    [Test]
    public void RequiredChat_JoinNeedsEntitlement()
    {
        Assert.AreEqual(StructuredChatChannel.DenyTos, StructuredChatChannel.EvaluateJoin("required", null));
        var snap = new ChatEntitlementSnapshot { entitled = true, tosSigned = true };
        Assert.IsNull(StructuredChatChannel.EvaluateJoin("required", snap));
    }

    [Test]
    public void Off_BlocksJoinAndSend()
    {
        Assert.AreEqual(StructuredChatChannel.DenyEntitlement, StructuredChatChannel.EvaluateJoin("off", null));
        Assert.AreEqual(StructuredChatChannel.DenyEntitlement, StructuredChatChannel.EvaluateSend("off", "text", null));
    }

    [Test]
    public void UnknownWord_BlocksSendWhenLexiconPresent()
    {
        var snap = new ChatEntitlementSnapshot
        {
            entitled = true,
            tosSigned = true,
            textAllowed = true,
            structuredChat = "optional",
            lexicon = new ChatLexiconData
            {
                words = new[] { new ChatLexiconWord { id = "yes", text = "Yes" } }
            }
        };
        var channel = new StructuredChatChannel { Entitlement = snap };
        Assert.IsFalse(channel.TrySend(null, "nope", new[] { "nope" }, out string deny));
        Assert.AreEqual(StructuredChatChannel.DenyWord, deny);
        Assert.IsTrue(channel.TrySend(null, "Yes", new[] { "yes" }, out deny));
        Assert.IsNull(deny);
    }
}

#endif
