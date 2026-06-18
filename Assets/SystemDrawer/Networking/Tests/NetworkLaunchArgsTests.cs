#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

using UnityEngine;



public class NetworkLaunchArgsTests

{

    [SetUp]

    public void SetUp() => NetworkLaunchArgs.ResetForTests();



    [TearDown]

    public void TearDown()

    {

        foreach (var server in Object.FindObjectsByType<ServerOrchestrator>(FindObjectsSortMode.None))

            Object.DestroyImmediate(server.gameObject);

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

        NetworkLaunchArgs.Parse(new[] { "game.exe", "-ds", "--host-lobby", "-p", "47778", "--lobby-port", "47780" });

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        NetworkLaunchArgs.ApplyTo(server);

        Assert.IsTrue(server.IsLobbyHosting);

    }



    [Test]

    public void ApplyTo_NoLobbyBlocksLobbyStart()

    {

        NetworkLaunchArgs.Parse(new[] { "game.exe", "-ds", "--host-lobby", "--no-lobby", "-p", "47779" });

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        NetworkLaunchArgs.ApplyTo(server);

        Assert.IsFalse(server.IsLobbyHosting);

    }

}

#endif


