#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

using UnityEngine;



public class NetworkLaunchArgsTests

{

    [SetUp]

    public void SetUp()

    {

        NetworkTestPorts.DestroyNetworkObjects();

        NetworkLaunchArgs.ResetForTests();

    }



    [TearDown]

    public void TearDown()

    {

        NetworkTestPorts.DestroyNetworkObjects();

        NetworkLaunchArgs.ResetForTests();

    }



    [Test]

    public void HostLobbyFlag_IsParsed()

    {

        NetworkLaunchArgs.Parse(new[] { "game.exe", "--host-lobby", "--lobby-port", "7781" });

        Assert.IsTrue(NetworkLaunchArgs.HostLobby);

        Assert.AreEqual(7781, NetworkLaunchArgs.LobbyPort);

    }



    [Test]

    public void NoLobbyFlag_IsParsed()

    {

        NetworkLaunchArgs.Parse(new[] { "game.exe", "--no-lobby" });

        Assert.IsTrue(NetworkLaunchArgs.NoLobby);

    }



    [Test]

    public void ApplyTo_StartsLobbyWhenHostFlagSet()

    {

        int gamePort = NetworkTestPorts.Allocate(1);
        int lobbyPort = NetworkTestPorts.Allocate();
        NetworkLaunchArgs.Parse(new[] { "game.exe", "-ds", "--host-lobby", "-p", gamePort.ToString(), "--lobby-port", lobbyPort.ToString() });

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        NetworkLaunchArgs.ApplyTo(server);

        Assert.IsTrue(server.IsLobbyHosting);

    }



    [Test]

    public void ApplyTo_NoLobbyBlocksLobbyStart()

    {

        int gamePort = NetworkTestPorts.Allocate(1);
        NetworkLaunchArgs.Parse(new[] { "game.exe", "-ds", "--host-lobby", "--no-lobby", "-p", gamePort.ToString() });

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        NetworkLaunchArgs.ApplyTo(server);

        Assert.IsFalse(server.IsLobbyHosting);

    }

}

#endif


