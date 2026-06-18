#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

using UnityEngine;



public class MenuRagdollEventTests

{

    [SetUp]

    public void SetUp() => NetworkLaunchArgs.ResetForTests();



    [TearDown]

    public void TearDown()

    {

        foreach (var client in Object.FindObjectsByType<ClientOrchestrator>(FindObjectsSortMode.None))

            Object.DestroyImmediate(client.gameObject);

        foreach (var server in Object.FindObjectsByType<ServerOrchestrator>(FindObjectsSortMode.None))

            Object.DestroyImmediate(server.gameObject);

        foreach (var menu in Object.FindObjectsByType<MenuRagdoll>(FindObjectsSortMode.None))

            Object.DestroyImmediate(menu.gameObject);

        NetworkLaunchArgs.ResetForTests();

    }



    [Test]

    public void Root_HandleBubble_Start_SetsSinglePlayerMode()

    {

        var rootGo = new GameObject("root");

        var root = rootGo.AddComponent<MenuRagdoll>();

        var clientGo = new GameObject("client");

        var client = clientGo.AddComponent<ClientOrchestrator>();

        client.EnsureInitialized();



        var nodeGo = new GameObject("start");

        nodeGo.transform.SetParent(rootGo.transform);

        var node = nodeGo.AddComponent<MenuRagdollNode>();

        node.Send("start");



        Assert.AreEqual(NetworkServerMode.SinglePlayer, client.Mode);

    }



    [Test]

    public void LobbyHostStart_CallsServerOrchestrator()

    {

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        server.EnsureReady();



        var rootGo = new GameObject("root");

        var root = rootGo.AddComponent<MenuRagdoll>();



        root.HandleBubble(new MenuRagdollEvent("lobby.host.start", null));

        Assert.IsTrue(server.IsLobbyHosting);

    }



    [Test]

    public void NoLobbyFlag_BlocksMenuLobbyStart()

    {

        NetworkLaunchArgs.Parse(new[] { "game.exe", "--no-lobby" });



        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        server.EnsureReady();

        server.SetLobbyLockedByLaunchArgs(true);



        var rootGo = new GameObject("root");

        var root = rootGo.AddComponent<MenuRagdoll>();

        root.HandleBubble(new MenuRagdollEvent("lobby.host.start", null));



        Assert.IsFalse(server.IsLobbyHosting);

    }



    [Test]

    public void SinglePlayerLoopback_ConnectsClient()

    {

        var serverGo = new GameObject("server");

        var server = serverGo.AddComponent<ServerOrchestrator>();

        server.EnsureReady();

        server.StartSinglePlayerLoopback();

        var client = ClientOrchestrator.Instance;

        Assert.IsNotNull(client);

        Assert.AreEqual(NetworkConnectionState.Loopback, client.ConnectionState);

    }

    [Test]
    public void SpectateJoin_SetsSpectatorRole()
    {
        var clientGo = new GameObject("client");
        var client = clientGo.AddComponent<ClientOrchestrator>();
        client.EnsureInitialized();
        client.ConnectAsSpectator("127.0.0.1", 47792);
        Assert.AreEqual(NetworkClientRole.Spectator, client.ClientRole);
    }

    [Test]
    public void HostPassword_PropagatesToServer()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();

        var rootGo = new GameObject("root");
        var root = rootGo.AddComponent<MenuRagdoll>();
        root.hostLobbyPassword = "secret";
        root.requireLobbyPassword = true;

        root.HandleBubble(new MenuRagdollEvent("lobby.host.options", null));
        Assert.AreEqual("secret", server.LobbyPasswordPlaintext);
    }

}

#endif


