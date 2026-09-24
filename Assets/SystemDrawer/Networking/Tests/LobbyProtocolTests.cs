#if UNITY_INCLUDE_TESTS

using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using UnityEngine;

public class LobbyProtocolTests
{
    LobbyServerHost _lobby;
    int _lobbyPort;
    int _gamePort;

    [SetUp]
    public void SetUp()
    {
        _lobbyPort = NetworkTestPorts.Allocate();
        _gamePort = NetworkTestPorts.Allocate();
        _lobby = new LobbyServerHost();
        _lobby.Start("127.0.0.1", _lobbyPort, _gamePort, "TestSession", 4, 2, true, "");
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
        var info = LobbyClientQuery.Query("127.0.0.1", _lobbyPort);
        Assert.IsTrue(info.Ok);
        Assert.AreEqual("TestSession", info.sessionName);
        Assert.AreEqual(_gamePort, info.gamePort);
        Assert.AreEqual(4, info.maxPlayers);
        Assert.AreEqual(2, info.maxSpectators);
        Assert.IsTrue(info.allowSpectators);
        Assert.IsFalse(info.passwordRequired);
    }

    [Test]
    public void Register_PlayerAndSpectator_IncrementsCounts()
    {
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", _lobbyPort, NetworkClientRole.Player, "p1", "", out var p1));
        Assert.AreEqual(1, p1.playerCount);
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", _lobbyPort, NetworkClientRole.Spectator, "s1", "", out var s1));
        Assert.AreEqual(1, s1.spectatorCount);
    }

    [Test]
    public void Register_WrongPassword_ReturnsErrPassword()
    {
        _lobby.Dispose();
        string hash = LobbyPasswordHash.Hash("pw", "TestSession");
        _lobby = new LobbyServerHost();
        _lobby.Start("127.0.0.1", _lobbyPort, _gamePort, "TestSession", 4, 2, true, hash);

        var info = LobbyClientQuery.Query("127.0.0.1", _lobbyPort);
        Assert.IsTrue(info.passwordRequired);

        Assert.IsFalse(LobbyClientQuery.Register("127.0.0.1", _lobbyPort, NetworkClientRole.Player, "p1", "bad", out var denied));
        Assert.AreEqual("password", denied.Error);
        Assert.IsTrue(LobbyClientQuery.Register("127.0.0.1", _lobbyPort, NetworkClientRole.Player, "p1", "pw", out var ok));
        Assert.IsTrue(ok.Ok);
    }
}

static class NetworkTestPorts
{
    public static int Allocate(int extraConsecutive = 0)
    {
        for (int attempt = 0; attempt < 32; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            bool ok = true;
            for (int i = 1; i <= extraConsecutive; i++)
            {
                if (!IsFree(port + i))
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
                return port;
        }
        throw new SocketException();
    }

    public static bool IsFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static void DestroyNetworkObjects()
    {
        foreach (var client in Object.FindObjectsByType<ClientOrchestrator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(client.gameObject);
        foreach (var server in Object.FindObjectsByType<ServerOrchestrator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            server.StopLobbyHost();
            server.StopListening();
            Object.DestroyImmediate(server.gameObject);
        }
        foreach (var menu in Object.FindObjectsByType<MenuRagdoll>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(menu.gameObject);
    }
}

#endif
