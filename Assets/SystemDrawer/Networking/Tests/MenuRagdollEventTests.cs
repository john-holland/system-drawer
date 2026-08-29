#if UNITY_INCLUDE_TESTS

using NUnit.Framework;

using UnityEngine;



public class MenuRagdollEventTests

{

    [SetUp]

    public void SetUp()

    {

        NetworkTestPorts.DestroyNetworkObjects();

        NetworkLaunchArgs.ResetForTests();

        GameLobbyContinuuuumClient.TransportOverride = (m, p, b) => "{}";

    }



    [TearDown]

    public void TearDown()

    {

        GameLobbyContinuuuumClient.TransportOverride = null;

        if (NetworkSettings.Default != null && NetworkSettings.Default.prefab != null)
            NetworkSettings.Default.prefab = new LobbyPrefabParameters();

        NetworkTestPorts.DestroyNetworkObjects();

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

        root.defaultLobbyPort = NetworkTestPorts.Allocate();



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

        server.StartListening(NetworkTestPorts.Allocate(1));

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

    [Test]
    public void NewGame_CreatesChildSessionUnderActive()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();
        server.StartLobbyHost(NetworkTestPorts.Allocate());
        Assert.AreEqual(1, server.GameSessions.sessions.Count);
        var parentId = server.GameSessions.ActiveId;

        var rootGo = new GameObject("root");
        var root = rootGo.AddComponent<MenuRagdoll>();
        root.minPlayersToStart = 1;
        root.HandleBubble(new MenuRagdollEvent("game.session.new", null));

        Assert.AreEqual(2, server.GameSessions.sessions.Count);
        var child = server.GameSessions.Active;
        Assert.AreEqual(parentId, child.parentId);
    }

    [Test]
    public void JoinAndSpectate_UseRole()
    {
        var clientGo = new GameObject("client");
        var client = clientGo.AddComponent<ClientOrchestrator>();
        client.EnsureInitialized();

        var rootGo = new GameObject("root");
        rootGo.AddComponent<MenuRagdoll>();
        var root = rootGo.GetComponent<MenuRagdoll>();
        root.HandleBubble(new MenuRagdollEvent("game.session.spectate", null, "127.0.0.1:47792"));
        Assert.AreEqual(NetworkClientRole.Spectator, client.ClientRole);

        root.HandleBubble(new MenuRagdollEvent("game.session.join", null, "127.0.0.1:47793"));
        Assert.AreEqual(NetworkClientRole.Player, client.ClientRole);
    }

    [Test]
    public void StartDenied_BelowMinPlayers()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();
        server.StartLobbyHost(NetworkTestPorts.Allocate());

        var rootGo = new GameObject("root");
        var root = rootGo.AddComponent<MenuRagdoll>();
        root.minPlayersToStart = 4;
        bool denied = root.HandleBubble(new MenuRagdollEvent("lobby.game.start", null));
        Assert.IsTrue(denied);
        denied = root.HandleBubble(new MenuRagdollEvent("game.session.new", null));
        Assert.IsTrue(denied);
        Assert.AreEqual(1, server.GameSessions.sessions.Count);
    }

    [Test]
    public void Hello_RejectedAtMaxPlayers()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();
        for (int i = 0; i < server.MaxPlayers; i++)
            server.RegisterClient("p" + i, "", NetworkClientRole.Player);
        Assert.IsFalse(server.TryAcceptHello("overflow", NetworkClientRole.Player, "", out var reason));
        Assert.AreEqual("player cap", reason);
        Assert.AreEqual("player cap", server.LastHelloRejectReason);
    }

    [Test]
    public void CanShow_HidesMismatchedLobbyType()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();
        server.Settings.prefab = new LobbyPrefabParameters
        {
            contentKind = LobbyContentKind.GameMode,
            contentId = "vanilla"
        };

        var rootGo = new GameObject("root");
        var menu = rootGo.AddComponent<MenuRagdoll>();
        menu.lobbyTypeBinding = new LobbyTypeBinding
        {
            hasBinding = true,
            contentKind = LobbyContentKind.Expansion,
            contentId = "xmas"
        };
        var nodeGo = new GameObject("node");
        nodeGo.transform.SetParent(rootGo.transform);
        var node = nodeGo.AddComponent<MenuRagdollNode>();
        node.lobbyTypeBinding = menu.lobbyTypeBinding;
        node.isEnabled = true;
        Assert.IsFalse(node.CanShow());

        server.Settings.prefab.contentKind = LobbyContentKind.Expansion;
        server.Settings.prefab.contentId = "xmas";
        Assert.IsTrue(node.CanShow());
    }

    [Test]
    public void CloseAdoptAndUmbrella_FromMenu()
    {
        var serverGo = new GameObject("server");
        var server = serverGo.AddComponent<ServerOrchestrator>();
        server.EnsureReady();
        var a = server.GameSessions.CreateSession("A");
        var b = server.GameSessions.CreateSession("B");
        var c = server.GameSessions.CreateSession("C");
        Assert.AreEqual(b.id, c.parentId);

        var rootGo = new GameObject("root");
        var root = rootGo.AddComponent<MenuRagdoll>();
        root.HandleBubble(new MenuRagdollEvent("game.session.close", null, b.id));
        Assert.IsNull(server.GameSessions.FindSession(b.id));
        Assert.AreEqual(a.id, server.GameSessions.FindSession(c.id).parentId);

        var d = server.GameSessions.CreateSession("D");
        root.HandleBubble(new MenuRagdollEvent("game.session.close.umbrella", null, c.id));
        Assert.IsNull(server.GameSessions.FindSession(c.id));
        Assert.IsNull(server.GameSessions.FindSession(d.id));
    }

    [Test]
    public void SaveSession_WritesLocalClient()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gs-menu-" + System.Guid.NewGuid().ToString("N"));
        GameSessionLocalSave.RootOverride = dir;
        try
        {
            var serverGo = new GameObject("server");
            var server = serverGo.AddComponent<ServerOrchestrator>();
            server.EnsureReady();
            var s = server.GameSessions.CreateSession("Saved");
            var rootGo = new GameObject("root");
            var root = rootGo.AddComponent<MenuRagdoll>();
            root.HandleBubble(new MenuRagdollEvent("game.session.save", null, s.id));
            Assert.IsTrue(System.IO.File.Exists(GameSessionLocalSave.SessionPath(s.lobbySessionName, s.id)));
        }
        finally
        {
            GameSessionLocalSave.RootOverride = null;
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
        }
    }
}

#endif


