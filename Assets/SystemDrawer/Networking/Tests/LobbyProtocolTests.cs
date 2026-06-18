#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

public class LobbyProtocolTests
{
    LobbyServerHost _lobby;
    const int LobbyPort = 47790;
    const int GamePort = 47791;

    [SetUp]
    public void SetUp()
    {
        _lobby = new LobbyServerHost();
        _lobby.Start("127.0.0.1", LobbyPort, GamePort, "TestSession", 4, 2, true, "");
    }

    [TearDown]
    public void TearDown()
    {
        _lobby?.Dispose();
        _lobby = null;
    }

    [Test]
    public void Query_OpenLobby_ReportsCountsAndFlags()
    {
        var info = LobbyClientQuery.Query("127.0.0.1", LobbyPort);
        Assert.IsTrue(info.Ok);
        Assert.AreEqual("TestSession", info.sessionName);
        Assert.AreEqual(GamePort, info.gamePort);
        Assert.AreEqual(4, info.maxPlayers);
        Assert.AreEqual(2, info.maxSpectators);
        Assert.IsTrue(info.allowSpectators);
        Assert.IsFalse(info.passwordRequired);
    }

    [Test]
    public void Register_PlayerAndSpectator_IncrementsCounts()
    {
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", LobbyPort, NetworkClientRole.Player, "p1", "", out var p1));
        Assert.AreEqual(1, p1.playerCount);
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", LobbyPort, NetworkClientRole.Spectator, "s1", "", out var s1));
        Assert.AreEqual(1, s1.spectatorCount);
    }

    [Test]
    public void Register_WrongPassword_ReturnsErrPassword()
    {
        _lobby.Dispose();
        string hash = LobbyPasswordHash.Hash("pw", "TestSession");
        _lobby = new LobbyServerHost();
        _lobby.Start("127.0.0.1", LobbyPort, GamePort, "TestSession", 4, 2, true, hash);

        var info = LobbyClientQuery.Query("127.0.0.1", LobbyPort);
        Assert.IsTrue(info.passwordRequired);

        Assert.IsFalse(LobbyClientQuery.Register("127.0.0.1", LobbyPort, NetworkClientRole.Player, "p1", "bad", out var denied));
        Assert.AreEqual("password", denied.Error);
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", LobbyPort, NetworkClientRole.Player, "p1", "pw", out var ok));
        Assert.IsTrue(ok.Ok);
    }
}

#endif
