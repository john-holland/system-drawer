#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

public class LobbyPasswordHashTests
{
    [Test]
    public void SamePasswordAndSession_ProducesSameHash()
    {
        string a = LobbyPasswordHash.Hash("secret", "Campaign");
        string b = LobbyPasswordHash.Hash("secret", "Campaign");
        Assert.AreEqual(a, b);
        Assert.IsNotEmpty(a);
    }

    [Test]
    public void WrongPassword_FailsVerify()
    {
        string hash = LobbyPasswordHash.Hash("secret", "Campaign");
        Assert.IsFalse(LobbyPasswordHash.Verify("wrong", "Campaign", hash));
        Assert.IsTrue(LobbyPasswordHash.Verify("secret", "Campaign", hash));
    }

    [Test]
    public void EmptyPassword_OpenLobby()
    {
        Assert.AreEqual("", LobbyPasswordHash.Hash("", "Campaign"));
        Assert.IsTrue(LobbyPasswordHash.Verify("", "Campaign", ""));
    }
}

#endif
